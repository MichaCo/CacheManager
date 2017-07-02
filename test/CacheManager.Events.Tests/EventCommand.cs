using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using CacheManager.Core;
using CacheManager.Core.Internal;
using Microsoft.Extensions.CommandLineUtils;
using Microsoft.Extensions.Logging;

namespace CacheManager.Events.Tests
{
    public abstract class EventCommand
    {
        public EventCommand(CommandLineApplication app, ILoggerFactory loggerFactory)
        {
            LoggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));
            Logger = loggerFactory.CreateLogger(GetType());
            App = app ?? throw new ArgumentNullException(nameof(app));

            App.OnExecute(() => Execute());
            Configure();
        }

        public CommandLineApplication App { get; }

        public ILoggerFactory LoggerFactory { get; }

        public ILogger Logger { get; }

        public CommandOption RuntimeArg { get; private set; }

        public CommandOption NumberOfJobs { get; private set; }

        public abstract Task<int> Execute();

        protected virtual void Configure()
        {
            RuntimeArg = App.Option("-r | --runtime", "Time in seconds to run", CommandOptionType.SingleValue);
            NumberOfJobs = App.Option("-j | --jobs", "Number of concurrent jobs", CommandOptionType.SingleValue);

            App.HelpOption("-? | -h | --help");
        }

        protected async Task RunWithConfigurationOneCache<TCacheItem>(
            ICacheManagerConfiguration configuration,
            Action<ICacheManager<TCacheItem>, EventCounter<TCacheItem>> job)
        {
            var cache = CacheFactory.FromConfiguration<TCacheItem>("CacheA", configuration);

            cache.Clear();

            var handlingA = new EventCounter<TCacheItem>(cache);

            Func<Task> task = () => Task.Run(() => job(cache, handlingA));

            await Runner(task, handlingA);
        }

        protected async Task RunWithConfigurationTwoCaches<TCacheItem>(
            ICacheManagerConfiguration configuration,
            Func<ICacheManager<TCacheItem>, ICacheManager<TCacheItem>, EventCounter<TCacheItem>, EventCounter<TCacheItem>, Task> job)
        {
            var cache = CacheFactory.FromConfiguration<TCacheItem>("CacheA", configuration);
            var cache2 = CacheFactory.FromConfiguration<TCacheItem>("CacheB", configuration);

            var handlingA = new EventCounter<TCacheItem>(cache);
            var handlingB = new EventCounter<TCacheItem>(cache2);

            Func<Task> task = () => Task.Run(async () => await job(cache, cache2, handlingA, handlingB));

            await Runner(task, handlingA, handlingB);
        }

        protected async Task Runner<TCacheValue>(Func<Task> task, params EventCounter<TCacheValue>[] handlings)
        {
            var spinner = new Spinner();
            spinner.Start();
            var swatch = Stopwatch.StartNew();
            var tasks = new List<Task>();
            int.TryParse(RuntimeArg.Value(), out int runtimeSeconds);

            var source = new CancellationTokenSource(runtimeSeconds * 1000);

            int.TryParse(NumberOfJobs.Value(), out int concurrentJobs);

            Console.WriteLine($"Displaying event counter for cache(s): {string.Join(", ", handlings.Select(p => p.Cache.Name))}; showing [local][remote] events.");
            try
            {
                var reportTask = Task.Run(async () =>
                {
                    while (true)
                    {
                        await Task.Delay(100);

                        spinner.Message = "";
                        foreach (var h in handlings)
                        {
                            spinner.Message += h.Cache.Name + " " + string.Join(" ", GetStatus(new[] { h }));
                        }
                        //spinner.Message = string.Join(" ", GetStatus(handlings));

                        if (source.IsCancellationRequested)
                        {
                            return;
                        }
                    }
                });

                while (!source.IsCancellationRequested)
                {
                    tasks.Add(task());

                    if (tasks.Count >= concurrentJobs)
                    {
                        await Task.WhenAll(tasks);
                        tasks.Clear();
                    }
                }

                await Task.WhenAll(tasks);

                // wait for all events to complete
                await Task.Delay(1000);
            }
            catch
            {
                throw;
            }
            finally
            {
                spinner.Stop();

                var counter = 0;
                foreach (var status in GetStatus(handlings))
                {
                    counter++;
                    Console.WriteLine($"Cache {counter} events received: {status}");
                }
                Console.WriteLine($"Backplane stats: {CacheBackplane.MessagesSent} messages sent in {CacheBackplane.SentChunks} chunks; {CacheBackplane.MessagesReceived} messages received.");
            }
        }

        private static IEnumerable<string> GetStatus<TCacheValue>(EventCounter<TCacheValue>[] handlings, bool printEmpty = false)
        {
            foreach (var handling in handlings)
            {
                var report = new StringBuilder();
                var status = handling.GetExpectedState();

                foreach (var kv in status)
                {
                    if (printEmpty || kv.Value.Any(p => p > 0))
                    {
                        report.Append($"{kv.Key}:[{string.Join("][", kv.Value)}] ");
                    }
                }

                //Console.WriteLine("Expected: " + report.ToString());
                yield return report.ToString();
            }
        }
    }
}