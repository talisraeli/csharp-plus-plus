using BenchmarkDotNet.Attributes;

using Cspp.Benchmarks.Libraries;

namespace Cspp.Benchmarks;

/// <summary>
/// Measures the cost of chaining on a <c>Task&lt;Result&lt;T&gt;&gt;</c>: the <see cref="PipelineBenchmarks" /> chain
/// with an asynchronous parse step. <c>AsyncCompleted</c> parses into a task that has already completed, so it measures
/// only the task members. <c>Async</c> parses into a task that awaits first, so <c>Bind</c>, <c>Map</c> and
/// <c>Match</c> each run as a continuation, which includes the cost of resuming on the thread pool. The synchronous
/// chain is the baseline. The inputs are the same as in <see cref="PipelineBenchmarks" />.
/// </summary>
public class AsyncPipelineBenchmarks
{
    [Params("42", "abc", "-5")]
    public string Input { get; set; } = null!;

    [Benchmark(Baseline = true)]
    public int Sync()
    {
        return CsppLibrary.Pipeline(Input);
    }

    [Benchmark]
    public Task<int> AsyncCompleted()
    {
        return CsppLibrary.PipelineCompletedAsync(Input);
    }

    [Benchmark]
    public Task<int> Async()
    {
        return CsppLibrary.PipelineAsync(Input);
    }
}