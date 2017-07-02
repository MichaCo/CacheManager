using System;
using System.Linq;
using System.Threading;

namespace CacheManager.Core.Utility
{
    /// <summary>
    /// Contract used by <see cref="ObjectPool{T}"/> to define how to create and return instances to a pool.
    /// </summary>
    /// <typeparam name="T">The type of objects of the pool.</typeparam>
    public interface IObjectPoolPolicy<T>
    {
        /// <summary>
        /// Creates a new instance of <typeparamref name="T"/>.
        /// </summary>
        /// <returns>The new instance.</returns>
        T CreateNew();

        /// <summary>
        /// Checks if the instance can be returned and may reset the instance to a state which can be reused.
        /// </summary>
        /// <param name="value">The instance which should be returned.</param>
        /// <returns><c>True</c> if the instance can be returned, <c>False</c> otherwise.</returns>
        bool Return(T value);
    }

    /// <summary>
    /// Simple policy based pool for objects.
    /// </summary>
    /// <typeparam name="T">The object type to pool.</typeparam>
    public class ObjectPool<T> where T : class
    {
        private readonly T[] _items;
        private readonly IObjectPoolPolicy<T> _policy;

        /// <summary>
        /// Initializes a new instance of the <see cref="ObjectPool{T}"/> class.
        /// </summary>
        /// <param name="policy">The object pool policy.</param>
        /// <param name="maxItems">Number of items to keep, defaults to number of processors * 2.</param>
        public ObjectPool(IObjectPoolPolicy<T> policy, int? maxItems = null)
        {
            if (policy == null)
            {
                throw new ArgumentNullException(nameof(policy));
            }

            if (maxItems == null || maxItems <= 0)
            {
                maxItems = Environment.ProcessorCount * 2;
            }

            _policy = policy;
            _items = new T[maxItems.Value];
        }

        /// <summary>
        /// Returns either a pooled or new instance of <typeparamref name="T"/>.
        /// </summary>
        /// <returns>The pooled or new instance.</returns>
        public T Lease()
        {
            for (var i = 0; i < _items.Length; i++)
            {
                var item = _items[i];
                if (item != null && Interlocked.CompareExchange(ref _items[i], null, item) == item)
                {
                    return item;
                }
            }

            return _policy.CreateNew();
        }

        /// <summary>
        /// Returns the instance to the pool (if possible).
        /// </summary>
        /// <param name="value">The instance to return to the pool.</param>
        public void Return(T value)
        {
            if (!_policy.Return(value))
            {
                return;
            }

            for (var i = 0; i < _items.Length; i++)
            {
                if (_items[i] == null)
                {
                    _items[i] = value;
                    return;
                }
            }
        }
    }
}