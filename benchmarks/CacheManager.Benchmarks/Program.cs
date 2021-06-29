using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Running;

namespace CacheManager.Benchmarks
{
    [ExcludeFromCodeCoverage]
    public class CacheManagerBenchConfig : ManualConfig
    {
        public CacheManagerBenchConfig()
        {
            Add(Job.MediumRun
                .With(Platform.X64));
        }
    }

    [ExcludeFromCodeCoverage]
    public class Program
    {
        public static void Main(string[] args)
        {
            //new BackplaneMessageBenchmark().DeserializeChange();
            new BackplaneMessageBenchmarkMultiple().SerializeMany();
            new BackplaneMessageBenchmarkMultiple().DeserializeMany();

            do
            {
                var config = ManualConfig.Create(DefaultConfig.Instance)
                    //.With(exporters: BenchmarkDotNet.Exporters.DefaultExporters.)
                    .With(BenchmarkDotNet.Analysers.EnvironmentAnalyser.Default)
                    .With(BenchmarkDotNet.Exporters.MarkdownExporter.GitHub)
                    .With(BenchmarkDotNet.Diagnosers.MemoryDiagnoser.Default)
                    .With(StatisticColumn.Mean)
                    .With(StatisticColumn.Median)
                    //.With(StatisticColumn.Min)
                    //.With(StatisticColumn.Max)
                    .With(StatisticColumn.StdDev)
                    .With(StatisticColumn.OperationsPerSecond)
                    .With(BaselineRatioColumn.RatioMean)
                    //.With(BaselineScaledColumn.ScaledStdDev)
                    .With(RankColumn.Arabic)

                    .With(Job.Clr
                        .WithIterationCount(10)
                        .WithWarmupCount(4)
                        .WithLaunchCount(1));

                //.With(Job.Clr
                //    .WithTargetCount(10)
                //    .WithWarmupCount(5)
                //    .WithLaunchCount(1));

                BenchmarkSwitcher
                    .FromAssembly(typeof(Program).GetTypeInfo().Assembly)
                    .Run(args, config);

                Console.WriteLine("done!");
                Console.WriteLine("Press escape to exit or any key to continue...");
            } while (Console.ReadKey().Key != ConsoleKey.Escape);
        }
    }
}
