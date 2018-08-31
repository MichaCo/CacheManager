using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CacheManager.Core;
using CacheManager.Core.Internal;
using CacheManager.Core.Logging;
using StackExchange.Redis;
using static CacheManager.Core.Utility.Guard;

namespace CacheManager.Redis
{
    public partial class RedisCacheHandle<TCacheValue>
    {
        /// <summary>
        /// Clears this cache, removing all items in the base cache and all regions.
        /// </summary>
        public override async ValueTask ClearAsync()
        {
            try
            {
                foreach (var server in Servers.Where(p => !p.IsSlave))
                {
                    await RetryAsync(async () =>
                    {
                        if (server.IsConnected)
                        {
                            await server.FlushDatabaseAsync(_redisConfiguration.Database);
                        }
                    });
                }
            }
            catch (NotSupportedException ex)
            {
                throw new NotSupportedException($"Clear is not available because '{ex.Message}'", ex);
            }
        }

        /// <summary>
        /// Clears the cache region, removing all items from the specified <paramref name="region"/> only.
        /// </summary>
        /// <param name="region">The cache region.</param>
        public override ValueTask ClearRegionAsync(string region)
        {
            return RetryAsync(async () =>
            {
                // we are storing all keys stored in the region in the hash for key=region
                var hashKeys = await _connection.Database.HashKeysAsync(region);

                if (hashKeys.Length > 0)
                {
                    // lets remove all keys which where in the region
                    // 01/32/16 changed to remove one by one because on clusters the keys could belong to multiple slots
                    foreach (var key in hashKeys.Where(p => p.HasValue))
                    {
                        await _connection.Database.KeyDeleteAsync(key.ToString(), CommandFlags.FireAndForget);
                    }
                }

                // now delete the region
                await _connection.Database.KeyDeleteAsync(region);
            });
        }
        
        /// <inheritdoc />
        public override ValueTask<bool> ExistsAsync(string key)
        {
            var fullKey = GetKey(key);
            return RetryAsync(async () => await _connection.Database.KeyExistsAsync(fullKey));
        }

        /// <inheritdoc />
        public override ValueTask<bool> ExistsAsync(string key, string region)
        {
            NotNullOrWhiteSpace(region, nameof(region));

            var fullKey = GetKey(key, region);
            return RetryAsync(async () => await _connection.Database.KeyExistsAsync(fullKey));
        }
        
        /// <summary>
        /// Adds a value to the cache.
        /// <para>
        /// Add call is synced, so might be slower than put which is fire and forget but we want to
        /// return true|false if the operation was successfully or not. And always returning true
        /// could be misleading if the item already exists
        /// </para>
        /// </summary>
        /// <param name="item">The <c>CacheItem</c> to be added to the cache.</param>
        /// <returns>
        /// <c>true</c> if the key was not already added to the cache, <c>false</c> otherwise.
        /// </returns>
        protected override ValueTask<bool> AddInternalPreparedAsync(CacheItem<TCacheValue> item) =>
            RetryAsync(() => SetAsync(item, When.NotExists, true));
        
        /// <summary>
        /// Gets a <c>CacheItem</c> for the specified key.
        /// </summary>
        /// <param name="key">The key being used to identify the item within the cache.</param>
        /// <returns>The <c>CacheItem</c>.</returns>
        protected override ValueTask<CacheItem<TCacheValue>> GetCacheItemInternalAsync(string key)
            => GetCacheItemInternalAsync(key, null);

        /// <summary>
        /// Gets a <c>CacheItem</c> for the specified key.
        /// </summary>
        /// <param name="key">The key being used to identify the item within the cache.</param>
        /// <param name="region">The cache region.</param>
        /// <returns>The <c>CacheItem</c>.</returns>
        protected override async ValueTask<CacheItem<TCacheValue>> GetCacheItemInternalAsync(string key, string region)
        {
            return (await GetCacheItemAndVersionAsync(key, region)).Item1;
        }

        private async ValueTask<Tuple<CacheItem<TCacheValue>, int>> GetCacheItemAndVersionAsync(string key, string region)
        {
            var version = -1;
            if (!_isLuaAllowed)
            {
                return Tuple.Create(
                    await GetCacheItemInternalNoScriptAsync(key, region),
                    version);
            }

            var fullKey = GetKey(key, region);

            var result = await RetryAsync(async () => await EvalAsync(ScriptType.Get, fullKey));
            if (result == null || result.IsNull)
            {
                // something went wrong. HMGET should return at least a null result for each requested field
                throw new InvalidOperationException("Error retrieving " + fullKey);
            }

            var values = (RedisValue[])result;

            // the first item stores the value
            var item = values[0];
            var expirationModeItem = values[1];
            var timeoutItem = values[2];
            var createdItem = values[3];
            var valueTypeItem = values[4];
            version = (int)values[5];
            var usesDefaultExpiration = values[6].HasValue ? (bool)values[6]        // if flag is set, all good...
                : (expirationModeItem.HasValue && timeoutItem.HasValue) ? false     // if not, but expiration flags have values, use those
                : true;                                                             // otherwise fall back to use default expiration from config

            if (!item.HasValue || !valueTypeItem.HasValue /* partially removed? */
                || item.IsNullOrEmpty || item.IsNull)
            {
                return null;
            }

            var expirationMode = ExpirationMode.None;
            var expirationTimeout = default(TimeSpan);

            // checking if the expiration mode is set on the hash
            if (expirationModeItem.HasValue && timeoutItem.HasValue)
            {
                if (!timeoutItem.IsNullOrEmpty && !expirationModeItem.IsNullOrEmpty)
                {
                    expirationMode = (ExpirationMode)(int)expirationModeItem;
                    expirationTimeout = TimeSpan.FromMilliseconds((long)timeoutItem);
                }
                else
                {
                    Logger.LogWarn("Expiration mode and timeout are set but are not valid '{0}', '{1}'.", expirationModeItem, timeoutItem);
                }
            }

            var value = FromRedisValue(item, (string)valueTypeItem);

            var cacheItem =
                usesDefaultExpiration ?
                string.IsNullOrWhiteSpace(region) ?
                    new CacheItem<TCacheValue>(key, value) :
                    new CacheItem<TCacheValue>(key, region, value) :
                string.IsNullOrWhiteSpace(region) ?
                    new CacheItem<TCacheValue>(key, value, expirationMode, expirationTimeout) :
                    new CacheItem<TCacheValue>(key, region, value, expirationMode, expirationTimeout);

            if (createdItem.HasValue)
            {
                cacheItem = cacheItem.WithCreated(new DateTime((long)createdItem, DateTimeKind.Utc));
            }

            if (cacheItem.IsExpired)
            {
                TriggerCacheSpecificRemove(key, region, CacheItemRemovedReason.Expired, cacheItem.Value);

                return null;
            }

            return Tuple.Create(cacheItem, version);
        }

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
#pragma warning disable SA1600
        
        protected ValueTask<CacheItem<TCacheValue>> GetCacheItemInternalNoScriptAsync(string key, string region)
        {
            return RetryAsync(async () =>
            {
                var fullKey = GetKey(key, region);

                // getting both, the value and, if exists, the expiration mode. if that one is set
                // and it is sliding, we also retrieve the timeout later
                var values = await _connection.Database.HashGetAsync(
                    fullKey,
                    new RedisValue[]
                    {
                        HashFieldValue,
                        HashFieldExpirationMode,
                        HashFieldExpirationTimeout,
                        HashFieldCreated,
                        HashFieldType,
                        HashFieldUsesDefaultExp
                    });

                // the first item stores the value
                var item = values[0];
                var expirationModeItem = values[1];
                var timeoutItem = values[2];
                var createdItem = values[3];
                var valueTypeItem = values[4];
                var usesDefaultExpiration = values[5].HasValue ? (bool)values[5]        // if flag is set, all good...
                    : (expirationModeItem.HasValue && timeoutItem.HasValue) ? false     // if not, but expiration flags have values, use those
                    : true;                                                             // otherwise fall back to use default expiration from config

                if (!item.HasValue || !valueTypeItem.HasValue /* partially removed? */
                    || item.IsNullOrEmpty || item.IsNull)
                {
                    return null;
                }

                var expirationMode = ExpirationMode.None;
                var expirationTimeout = default(TimeSpan);

                // checking if the expiration mode is set on the hash
                if (expirationModeItem.HasValue && timeoutItem.HasValue)
                {
                    // adding sanity check for empty string results. Could happen in rare cases like #74
                    if (!timeoutItem.IsNullOrEmpty && !expirationModeItem.IsNullOrEmpty)
                    {
                        expirationMode = (ExpirationMode)(int)expirationModeItem;
                        expirationTimeout = TimeSpan.FromMilliseconds((long)timeoutItem);
                    }
                    else
                    {
                        Logger.LogWarn("Expiration mode and timeout are set but are not valid '{0}', '{1}'.", expirationModeItem, timeoutItem);
                    }
                }

                var value = FromRedisValue(item, (string)valueTypeItem);

                var cacheItem =
                    usesDefaultExpiration ?
                    string.IsNullOrWhiteSpace(region) ?
                        new CacheItem<TCacheValue>(key, value) :
                        new CacheItem<TCacheValue>(key, region, value) :
                    string.IsNullOrWhiteSpace(region) ?
                        new CacheItem<TCacheValue>(key, value, expirationMode, expirationTimeout) :
                        new CacheItem<TCacheValue>(key, region, value, expirationMode, expirationTimeout);

                if (createdItem.HasValue)
                {
                    cacheItem = cacheItem.WithCreated(new DateTime((long)createdItem, DateTimeKind.Utc));
                }

                if (cacheItem.IsExpired)
                {
                    TriggerCacheSpecificRemove(key, region, CacheItemRemovedReason.Expired, cacheItem.Value);

                    return null;
                }

                // update sliding
                if (expirationMode == ExpirationMode.Sliding && expirationTimeout != default(TimeSpan))
                {
                    await _connection.Database.KeyExpireAsync(fullKey, cacheItem.ExpirationTimeout, CommandFlags.FireAndForget);
                }

                return cacheItem;
            });
        }

#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
#pragma warning restore SA1600

        /// <inheritdoc />
        protected override ValueTask PutInternalPreparedAsync(CacheItem<TCacheValue> item)
        {
            return RetryAsync(async () =>
            {
                await SetAsync(item, When.Always, false);
            });
        }

        /// <summary>
        /// Removes a value from the cache for the specified key.
        /// </summary>
        /// <param name="key">The key being used to identify the item within the cache.</param>
        /// <returns>
        /// <c>true</c> if the key was found and removed from the cache, <c>false</c> otherwise.
        /// </returns>
        protected override ValueTask<bool> RemoveInternalAsync(string key) => RemoveInternalAsync(key, null);

        /// <summary>
        /// Removes a value from the cache for the specified key.
        /// </summary>
        /// <param name="key">The key being used to identify the item within the cache.</param>
        /// <param name="region">The cache region.</param>
        /// <returns>
        /// <c>true</c> if the key was found and removed from the cache, <c>false</c> otherwise.
        /// </returns>
        protected override async ValueTask<bool> RemoveInternalAsync(string key, string region)
        {
            return await RetryAsync(async () =>
            {
                var fullKey = GetKey(key, region);

                // clean up region
                if (!string.IsNullOrWhiteSpace(region))
                {
                    await _connection.Database.HashDeleteAsync(region, fullKey, CommandFlags.FireAndForget);
                }

                // remove key
                var result = await _connection.Database.KeyDeleteAsync(fullKey);

                return result;
            });
        }
        
        private async ValueTask<RedisResult> EvalAsync(ScriptType scriptType, RedisKey redisKey, RedisValue[] values = null, CommandFlags flags = CommandFlags.None)
        {
            if (!_scriptsLoaded)
            {
                lock (_lockObject)
                {
                    if (!_scriptsLoaded)
                    {
                        LoadScriptsAsync();
                        _scriptsLoaded = true;
                    }
                }
            }

            LoadedLuaScript script = null;
            if (!_luaScripts.TryGetValue(scriptType, out LuaScript luaScript)
                || (_canPreloadScripts && !_shaScripts.TryGetValue(scriptType, out script)))
            {
                Logger.LogCritical("Something is wrong with the Lua scripts. Seem to be not loaded.");
                _scriptsLoaded = false;
                throw new InvalidOperationException("Something is wrong with the Lua scripts. Seem to be not loaded.");
            }

            try
            {
                if (_canPreloadScripts && script != null)
                {
                    return await _connection.Database.ScriptEvaluateAsync(script.Hash, new[] {redisKey}, values, flags);
                }
                else
                {
                    return await _connection.Database.ScriptEvaluateAsync(luaScript.ExecutableScript, new[] {redisKey}, values, flags);
                }
            }
            catch (RedisServerException ex) when (ex.Message.StartsWith("NOSCRIPT", StringComparison.OrdinalIgnoreCase))
            {
                Logger.LogInfo("Received NOSCRIPT from server. Reloading scripts...");
                LoadScriptsAsync();

                // retry
                throw;
            }
        }

        private async ValueTask<bool> SetAsync(CacheItem<TCacheValue> item, When when, bool sync = false)
        {
            if (!_isLuaAllowed)
            {
                return await SetNoScriptAsync(item, when, sync);
            }

            var fullKey = GetKey(item.Key, item.Region);
            var value = ToRedisValue(item.Value);

            var flags = sync ? CommandFlags.None : CommandFlags.FireAndForget;

            ValidateExpirationTimeout(item);

            // ARGV [1]: value, [2]: type, [3]: expirationMode, [4]: expirationTimeout(millis), [5]: created(ticks)
            var parameters = new RedisValue[]
            {
                value,
                item.ValueType.AssemblyQualifiedName,
                (int)item.ExpirationMode,
                (long)item.ExpirationTimeout.TotalMilliseconds,
                item.CreatedUtc.Ticks,
                item.UsesExpirationDefaults
            };

            RedisResult result;
            if (when == When.NotExists)
            {
                result = await EvalAsync(ScriptType.Add, fullKey, parameters, flags);
            }
            else
            {
                result = await EvalAsync(ScriptType.Put, fullKey, parameters, flags);
            }

            if (result == null)
            {
                if (flags.HasFlag(CommandFlags.FireAndForget))
                {
                    if (!string.IsNullOrWhiteSpace(item.Region))
                    {
                        // setting region lookup key if region is being used
                        await _connection.Database.HashSetAsync(item.Region, fullKey, "regionKey", When.Always, CommandFlags.FireAndForget);
                    }

                    // put runs via fire and forget, so we don't get a result back
                    return true;
                }

                // should never happen, something went wrong with the script
                throw new InvalidOperationException("Something went wrong adding an item, result must not be null.");
            }
            else
            {
                if (result.IsNull && when == When.NotExists)
                {
                    // add failed because element exists already
                    if (Logger.IsEnabled(LogLevel.Debug))
                    {
                        Logger.LogDebug("DB {0} | Failed to add item [{1}] because it exists.", _connection.Database.Database, item.ToString());
                    }

                    return false;
                }

                var resultValue = (RedisValue)result;

                if (resultValue.HasValue && resultValue.ToString().Equals("OK", StringComparison.OrdinalIgnoreCase))
                {
                    // Added successfully:
                    if (!string.IsNullOrWhiteSpace(item.Region))
                    {
                        // setting region lookup key if region is being used
                        // we cannot do that within the lua because the region could be on another cluster node!
                        await _connection.Database.HashSetAsync(item.Region, fullKey, "regionKey", When.Always, CommandFlags.FireAndForget);
                    }

                    return true;
                }

                Logger.LogWarn("DB {0} | Failed to set item [{1}]: {2}.", _connection.Database.Database, item.ToString(), resultValue.ToString());
                return false;
            }
        }

        private ValueTask<bool> SetNoScriptAsync(CacheItem<TCacheValue> item, When when, bool sync = false)
        {
            return RetryAsync(async () =>
            {
                var fullKey = GetKey(item.Key, item.Region);
                var value = ToRedisValue(item.Value);

                ValidateExpirationTimeout(item);

                var metaValues = new[]
                {
                    new HashEntry(HashFieldType, item.ValueType.AssemblyQualifiedName),
                    new HashEntry(HashFieldExpirationMode, (int)item.ExpirationMode),
                    new HashEntry(HashFieldExpirationTimeout, (long)item.ExpirationTimeout.TotalMilliseconds),
                    new HashEntry(HashFieldCreated, item.CreatedUtc.Ticks),
                    new HashEntry(HashFieldUsesDefaultExp, item.UsesExpirationDefaults)
                };

                var flags = sync ? CommandFlags.None : CommandFlags.FireAndForget;

                var setResult = await _connection.Database.HashSetAsync(fullKey, HashFieldValue, value, when, flags);

                // setResult from fire and forget is alwys false, so we have to assume it works...
                setResult = flags == CommandFlags.FireAndForget ? true : setResult;

                if (setResult)
                {
                    if (!string.IsNullOrWhiteSpace(item.Region))
                    {
                        // setting region lookup key if region is being used
                        await _connection.Database.HashSetAsync(item.Region, fullKey, "regionKey", When.Always, CommandFlags.FireAndForget);
                    }

                    // set the additional fields in case sliding expiration should be used in this
                    // case we have to store the expiration mode and timeout on the hash, too so
                    // that we can extend the expiration period every time we do a get
                    if (metaValues != null)
                    {
                        await _connection.Database.HashSetAsync(fullKey, metaValues, flags);
                    }

                    if (item.ExpirationMode != ExpirationMode.None && item.ExpirationMode != ExpirationMode.Default)
                    {
                        await _connection.Database.KeyExpireAsync(fullKey, item.ExpirationTimeout, CommandFlags.FireAndForget);
                    }
                    else
                    {
                        // bugfix #9
                        await _connection.Database.KeyPersistAsync(fullKey, CommandFlags.FireAndForget);
                    }
                }

                return setResult;
            });
        }
        
        // TODO: to async
        private void LoadScriptsAsync()
        {
            lock (_lockObject)
            {
                Logger.LogInfo("Loading scripts.");

                var putLua = LuaScript.Prepare(_scriptPut);
                var addLua = LuaScript.Prepare(_scriptAdd);
                var updateLua = LuaScript.Prepare(_scriptUpdate);
                var getLua = LuaScript.Prepare(_scriptGet);
                _luaScripts.Clear();
                _luaScripts.Add(ScriptType.Add, addLua);
                _luaScripts.Add(ScriptType.Put, putLua);
                _luaScripts.Add(ScriptType.Update, updateLua);
                _luaScripts.Add(ScriptType.Get, getLua);

                // servers feature might be disabled
                if (_canPreloadScripts)
                {
                    try
                    {
                        foreach (var server in Servers)
                        {
                            if (server.IsConnected)
                            {
                                _shaScripts[ScriptType.Put] = putLua.Load(server);
                                _shaScripts[ScriptType.Add] = addLua.Load(server);
                                _shaScripts[ScriptType.Update] = updateLua.Load(server);
                                _shaScripts[ScriptType.Get] = getLua.Load(server);
                            }
                        }
                    }
                    catch (NotSupportedException)
                    {
                        _canPreloadScripts = false;
                    }
                }
            }
        }

        private ValueTask<T> RetryAsync<T>(Func<ValueTask<T>> retryme) =>
            RetryHelper.RetryAsync(retryme, _managerConfiguration.RetryTimeout, _managerConfiguration.MaxRetries, Logger);

        private async ValueTask RetryAsync(Func<ValueTask> retryme)
            => await RetryAsync(async () =>
            {
                await retryme();
                return true;
            });
    }
}
