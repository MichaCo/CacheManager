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
#if NETSTANDARD2 || NETSTANDARD1
    public partial class RedisCacheHandle<TCacheValue>
    {
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
        protected override Task<bool> AddInternalPreparedAsync(CacheItem<TCacheValue> item) =>
            RetryAsync(() => SetAsync(item, When.NotExists, true));

        /// <summary>
        /// Removes a value from the cache for the specified key.
        /// </summary>
        /// <param name="key">The key being used to identify the item within the cache.</param>
        /// <returns>
        /// <c>true</c> if the key was found and removed from the cache, <c>false</c> otherwise.
        /// </returns>
        protected override Task<bool> RemoveInternalAsync(string key) => RemoveInternalAsync(key, null);

        /// <summary>
        /// Removes a value from the cache for the specified key.
        /// </summary>
        /// <param name="key">The key being used to identify the item within the cache.</param>
        /// <param name="region">The cache region.</param>
        /// <returns>
        /// <c>true</c> if the key was found and removed from the cache, <c>false</c> otherwise.
        /// </returns>
        protected override async Task<bool> RemoveInternalAsync(string key, string region)
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
        
        private async Task<RedisResult> EvalAsync(ScriptType scriptType, RedisKey redisKey, RedisValue[] values = null, CommandFlags flags = CommandFlags.None)
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

        private async Task<bool> SetAsync(CacheItem<TCacheValue> item, When when, bool sync = false)
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

        private Task<bool> SetNoScriptAsync(CacheItem<TCacheValue> item, When when, bool sync = false)
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

        private Task<T> RetryAsync<T>(Func<Task<T>> retryme) =>
            RetryHelper.RetryAsync(retryme, _managerConfiguration.RetryTimeout, _managerConfiguration.MaxRetries, Logger);

        private async Task RetryAsync(Func<Task> retryme)
            => await RetryAsync(async () =>
            {
                await retryme();
                return true;
            });
    }
#endif
}
