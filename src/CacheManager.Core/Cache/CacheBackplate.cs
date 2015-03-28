namespace CacheManager.Core.Cache
{
    using System;

    public sealed class CacheBackPlate : ICacheBackPlate
    {
        private Action<string> onRemoveKey;
        private Action<string, string> onRemoveKeyRegion;
        private Action<string> onChangeKey;
        private Action<string, string> onChangeKeyRegion;
        private Action<string> onClearRegion;
        private Action onClear;

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
        
        public void NotifyRemove(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                throw new ArgumentNullException("key");
            }

            this.onRemoveKey(key);
        }

        public void NotifyRemove(string key, string region)
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

        public void NotifyChange(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                throw new ArgumentNullException("key");
            }

            this.onChangeKey(key);
        }

        public void NotifyChange(string key, string region)
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
        
        public void NotifyClear()
        {
            this.onClear();
        }

        public void NotifyClearRegion(string region)
        {
            if (string.IsNullOrWhiteSpace(region))
            {
                throw new ArgumentNullException("region");
            }

            this.onClearRegion(region);
        }
    }
}