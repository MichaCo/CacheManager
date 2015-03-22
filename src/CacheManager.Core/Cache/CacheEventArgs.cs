using System;

namespace CacheManager.Core.Cache
{
    public sealed class CacheActionEventArgs : EventArgs
    {
        public CacheActionEventArgs(string key, string region)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                throw new ArgumentNullException("key");
            }

            this.Key = key;
            this.Region = region;
        }

        public string Key { get; private set; }

        public string Region { get; private set; }
    }

    public sealed class CacheUpdateEventArgs : EventArgs
    {
        public CacheUpdateEventArgs(string key, string region, UpdateItemConfig config, UpdateItemResult result)
        {
            this.Key = key;
            this.Region = region;
            this.Result = result;
            this.Config = config;
        }

        public string Key { get; private set; }

        public string Region { get; private set; }

        public UpdateItemResult Result { get; private set; }

        public UpdateItemConfig Config { get; private set; }
    }

    public sealed class CacheClearEventArgs : EventArgs { }

    public sealed class CacheClearRegionEventArgs : EventArgs
    {
        public CacheClearRegionEventArgs(string region)
        {
            if (string.IsNullOrWhiteSpace(region))
            {
                throw new ArgumentNullException("region");
            }

            this.Region = region;
        }

        public string Region { get; private set; }
    }
}