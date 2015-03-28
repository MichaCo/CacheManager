using System;
using CacheManager.Core.Configuration;

namespace CacheManager.Core
{
    /*
     * CacheA -> Removes Key -> Calls notify item removed
     * CacheB -> onRemove(key) will be called
     * CacheManager subscriped to SubscribeRemove() -> callback will be called.
     */

    public interface ICacheBackPlate : IDisposable
    {
        string Name { get; }

        ICacheManagerConfiguration Configuration { get; }

        void SubscribeRemove(Action<string> remove);

        void SubscribeRemove(Action<string, string> remove);

        void SubscribeChanged(Action<string> change);

        void SubscribeChanged(Action<string, string> change);

        void SubscribeClear(Action clear);

        void SubscribeClearRegion(Action<string> clearRegion);

        void OnRemove(string key);

        void OnRemove(string key, string region);

        void OnChange(string key);

        void OnChange(string key, string region);

        void OnClear();

        void OnClearRegion(string region);

        void NotifyClear();
        
        void NotifyClearRegion(string region);
        
        void NotifyChange(string key);
        
        void NotifyChange(string key, string region);
        
        void NotifyRemove(string key);
        
        void NotifyRemove(string key, string region);    
    }
}