using System.Reflection;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Running;

namespace Squire.NumTic.Benchmark;

/// <summary>
///   Entry point for the NumTic benchmark application.
/// </summary>
///
public class Program
{
    /// <summary>
    ///   Runs the benchmark suite with optional quick-test mode or filtering support.
    /// </summary>
    ///
    /// <param name="args">Command line arguments. Use "--quick-test" for faster execution, or standard BenchmarkDotNet args for filtering.</param>
    ///
    public static void Main(string[] args)
    {
        // For testing, run a quick verification of all benchmark suites.

        if (args.Length > 0 && args[0] == "--quick-test")
        {
            var config = ManualConfig
                .Create(DefaultConfig.Instance)
                .AddJob(Job.ShortRun);

            BenchmarkRunner.Run<GameStateBenchmarks>(config);
            BenchmarkRunner.Run<TokenManagementBenchmarks>(config);
            BenchmarkRunner.Run<BotPlayerBenchmarks>(config);
            BenchmarkRunner.Run<WinningLinesBenchmarks>(config);
            BenchmarkRunner.Run<BoardRenderingBenchmarks>(config);
            BenchmarkRunner.Run<BoardBuildingBenchmarks>(config);
            BenchmarkRunner.Run<TokenFormattingBenchmarks>(config);
        }
        else
        {
            // Use BenchmarkSwitcher to enable filtering and other BenchmarkDotNet features.

            BenchmarkSwitcher
                .FromAssembly(Assembly.GetExecutingAssembly())
                .Run(args);
        }
    }
}
