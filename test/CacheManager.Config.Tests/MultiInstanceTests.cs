using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace CacheManager.Core
{
    public class MultiInstanceTests
    {
        public class RunConfiguration
        {
            public ICacheManagerConfiguration CacheConfiguration { get; set; }

            // in seconds
            public int Runtime { get; set; }
        }

        public abstract class Runner<T>
        {
            private readonly ICacheManager<T> _cache;
            private readonly int _runtime;
            private bool _running;
            private CancellationTokenSource _source;

            public Runner(RunConfiguration cfg)
            {
                if (cfg == null) throw new ArgumentNullException(nameof(cfg));
                _cache = new BaseCacheManager<T>(cfg.CacheConfiguration);
                _runtime = cfg.Runtime;
            }

            public async Task Start()
            {
                _source = new CancellationTokenSource(_runtime * 1000);
                try
                {
                    while (_running)
                    {
                        _source.Token.ThrowIfCancellationRequested();
                        await Execute(_source.Token);
                    }
                }
                catch (TaskCanceledException) { }
                catch (OperationCanceledException) { }
            }

            public void Stop()
            {
                _source.Cancel(true);
            }

            protected abstract Task Execute(CancellationToken token);
        }
    }
}