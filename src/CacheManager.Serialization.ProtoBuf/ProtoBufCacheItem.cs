using System;
using CacheManager.Core;
using ProtoBuf;

namespace CacheManager.Serialization.ProtoBuf
{
    internal static class ProtoBufCacheItem
    {
        private static readonly Type ProtoBufCacheItemOpernGeneric = typeof(ProtoBufCacheItem<>);

        public static Type GetGenericJsonCacheItemType(Type targetValueType)
        {
            return ProtoBufCacheItemOpernGeneric.MakeGenericType(targetValueType);
        }

        public static object CreateFromCacheItem<T>(CacheItem<T> source)
        {
            Type tType = typeof(T);

            if (tType != source.ValueType || tType == typeof(object))
            {
                var targetType = GetGenericJsonCacheItemType(source.ValueType);
                return Activator.CreateInstance(targetType, (ICacheItemProperties)source, source.Value);
            }
            else
            {
                return new ProtoBufCacheItem<T>((ICacheItemProperties)source, source.Value);
            }
        }
    }

    internal interface ICacheItemConverter
    {
        CacheItem<TTarget> ToCacheItem<TTarget>();
    }

    [ProtoContract]
    internal class ProtoBufCacheItem<T> : ICacheItemConverter
    {
        public ProtoBufCacheItem()
        {
        }

        public ProtoBufCacheItem(ICacheItemProperties properties, object value)
        {
            this.CreatedUtc = properties.CreatedUtc;
            this.ExpirationMode = properties.ExpirationMode;
            this.ExpirationTimeout = properties.ExpirationTimeout;
            this.Key = properties.Key;
            this.LastAccessedUtc = properties.LastAccessedUtc;
            this.Region = properties.Region;
            this.UsesExpirationDefaults = properties.UsesExpirationDefaults;
            this.ValueType = properties.ValueType.AssemblyQualifiedName;
            this.Value = (T)value;
        }

        [ProtoMember(1)]
        public DateTime CreatedUtc { get; set; }

        [ProtoMember(2)]
        public DateTime LastAccessedUtc { get; set; }

        [ProtoMember(3)]
        public ExpirationMode ExpirationMode { get; set; }

        [ProtoMember(4)]
        public TimeSpan ExpirationTimeout { get; set; }

        [ProtoMember(5)]
        public string Key { get; set; }

        [ProtoMember(6)]
        public string Region { get; set; }

        [ProtoMember(7)]
        public T Value { get; set; }

        [ProtoMember(8)]
        public string ValueType { get; set; }

        [ProtoMember(9)]
        public bool UsesExpirationDefaults { get; set; }

        public CacheItem<TTarget> ToCacheItem<TTarget>()
        {
            var item = string.IsNullOrWhiteSpace(this.Region) ?
                new CacheItem<TTarget>(this.Key, (TTarget)(object)Value) :
                new CacheItem<TTarget>(this.Key, this.Region, (TTarget)(object)Value);

            if (!this.UsesExpirationDefaults)
            {
                if (this.ExpirationMode == ExpirationMode.Sliding)
                {
                    item = item.WithSlidingExpiration(this.ExpirationTimeout);
                }
                else if (this.ExpirationMode == ExpirationMode.Absolute)
                {
                    item = item.WithAbsoluteExpiration(this.ExpirationTimeout);
                }
                else if (this.ExpirationMode == ExpirationMode.None)
                {
                    item = item.WithNoExpiration();
                }
            }
            else
            {
                item = item.WithDefaultExpiration();
            }

            item.LastAccessedUtc = this.LastAccessedUtc;

            return item.WithCreated(this.CreatedUtc);
        }
    }
}