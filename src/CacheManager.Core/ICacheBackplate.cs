using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CacheManager.Core
{
    /*
        CacheA -> Removes Key -> Calls notify item removed
        CacheB -> onRemove(key) will be called
     */

    public interface ICacheBackPlate
    {
        void SubscribeRemove(Action<string> remove);

        void SubscribeRemove(Action<string, string> remove);

        void SubscribeChanged(Action<string> change);

        void SubscribeChanged(Action<string, string> change);

        void SubscribeClear(Action clear);

        void SubscribeClearRegion(Action<string> clearRegion);

        void NotifyRemove(string key);

        void NotifyRemove(string key, string region);

        void NotifyChange(string key);

        void NotifyChange(string key, string region);

        void NotifyClear();

        void NotifyClearRegion(string region);
    }
}