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
            this.onClear = clear;
        }

        public void SubscribeClearRegion(Action<string> clearRegion)
        {
            this.onClearRegion = clearRegion;
        }
        
        public void NotifyRemove(string key)
        {
            this.onRemoveKey(key);
        }

        public void NotifyRemove(string key, string region)
        {
            this.onRemoveKeyRegion(key, region);
        }

        public void NotifyChange(string key)
        {
            this.onChangeKey(key);
        }

        public void NotifyChange(string key, string region)
        {
            this.onChangeKeyRegion(key, region);
        }
        
        public void NotifyClear()
        {
            this.onClear();
        }

        public void NotifyClearRegion(string region)
        {
            this.onClearRegion(region);
        }
    }
}