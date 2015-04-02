namespace CacheManager.Core.Cache
{
    using System;
    using CacheManager.Core.Configuration;

    public abstract class CacheBackPlate : ICacheBackPlate
    {
        private Action<string> onChangeKey;
        private Action<string, string> onChangeKeyRegion;
        private Action onClear;
        private Action<string> onClearRegion;
        private Action<string> onRemoveKey;
        private Action<string, string> onRemoveKeyRegion;

        public CacheBackPlate(string name, ICacheManagerConfiguration configuration)
        {
            this.Name = name;
            this.Configuration = configuration;
        }

        ~CacheBackPlate()
        {
            this.Dispose(false);
        }

        public ICacheManagerConfiguration Configuration { get; private set; }

        public string Name { get; private set; }

        public void Dispose()
        {
            this.Dispose(true);

            GC.SuppressFinalize(this);
        }

        public virtual void Dispose(bool managed)
        {
        }

        public abstract void NotifyChange(string key);

        public abstract void NotifyChange(string key, string region);

        public abstract void NotifyClear();

        public abstract void NotifyClearRegion(string region);

        public abstract void NotifyRemove(string key);

        public abstract void NotifyRemove(string key, string region);

        public void OnChange(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                throw new ArgumentNullException("key");
            }

            this.onChangeKey(key);
        }

        public void OnChange(string key, string region)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                throw new ArgumentNullException("key");
            }
            if (string.IsNullOrWhiteSpace(region))
            {
                throw new ArgumentNullException("region");
            }

            this.onChangeKeyRegion(key, region);
        }

        public void OnClear()
        {
            this.onClear();
        }

        public void OnClearRegion(string region)
        {
            if (string.IsNullOrWhiteSpace(region))
            {
                throw new ArgumentNullException("region");
            }

            this.onClearRegion(region);
        }

        public void OnRemove(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                throw new ArgumentNullException("key");
            }

            this.onRemoveKey(key);
        }

        public void OnRemove(string key, string region)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                throw new ArgumentNullException("key");
            }
            if (string.IsNullOrWhiteSpace(region))
            {
                throw new ArgumentNullException("region");
            }

            this.onRemoveKeyRegion(key, region);
        }

        public void SubscribeChanged(Action<string> change)
        {
            if (change == null)
            {
                throw new ArgumentNullException("change");
            }

            this.onChangeKey = change;
        }

        public void SubscribeChanged(Action<string, string> change)
        {
            if (change == null)
            {
                throw new ArgumentNullException("change");
            }

            this.onChangeKeyRegion = change;
        }

        public void SubscribeClear(Action clear)
        {
            if (clear == null)
            {
                throw new ArgumentNullException("clear");
            }

            this.onClear = clear;
        }

        public void SubscribeClearRegion(Action<string> clearRegion)
        {
            if (clearRegion == null)
            {
                throw new ArgumentNullException("clearRegion");
            }

            this.onClearRegion = clearRegion;
        }

        public void SubscribeRemove(Action<string> remove)
        {
            if (remove == null)
            {
                throw new ArgumentNullException("remove");
            }

            this.onRemoveKey = remove;
        }

        public void SubscribeRemove(Action<string, string> remove)
        {
            if (remove == null)
            {
                throw new ArgumentNullException("remove");
            }

            this.onRemoveKeyRegion = remove;
        }
    }
}