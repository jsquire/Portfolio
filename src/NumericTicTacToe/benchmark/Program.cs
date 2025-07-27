using BenchmarkDotNet.Running;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;

namespace Squire.NumTic.Benchmark;

/// <summary>
///   Entry point for the NumTic benchmark application.
/// </summary>
///
public class Program
{
    /// <summary>
    ///   Runs the benchmark suite with optional quick-test mode.
    /// </summary>
    ///
    /// <param name="args">Command line arguments. Use "--quick-test" for faster execution.</param>
    ///
    public static void Main(string[] args)
    {
        // For testing, run a quick verification of all benchmark suites.

        if ((args.Length > 0) && (args[0] == "--quick-test"))
        {
            var config = ManualConfig.Create(DefaultConfig.Instance)
                .AddJob(Job.ShortRun);

            BenchmarkRunner.Run<GameStateBenchmarks>(config);
            BenchmarkRunner.Run<TokenManagementBenchmarks>(config);
            BenchmarkRunner.Run<BotPlayerBenchmarks>(config);
        }
        else
        {
            // Full benchmark suite.

            BenchmarkRunner.Run<GameStateBenchmarks>();
            BenchmarkRunner.Run<TokenManagementBenchmarks>();
            BenchmarkRunner.Run<BotPlayerBenchmarks>();
        }
    }
}
