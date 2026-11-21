using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Order;
using BenchmarkDotNet.Running;

namespace Cspp.Benchmarks;

internal static class Program
{
    // Pass BenchmarkDotNet arguments through, for example "--filter *Pipeline*" or "--job short".
    private static void Main(string[] args)
    {
        IConfig config = DefaultConfig.Instance
            .AddDiagnoser(MemoryDiagnoser.Default)
            .AddColumn(RankColumn.Arabic)
            .WithOrderer(new DefaultOrderer(SummaryOrderPolicy.Default, MethodOrderPolicy.Declared));

        BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args, config);
    }
}