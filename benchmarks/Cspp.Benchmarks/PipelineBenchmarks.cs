using BenchmarkDotNet.Attributes;

using Cspp.Benchmarks.Libraries;

namespace Cspp.Benchmarks;

/// <summary>
/// Measures a typical chain: parse the input, validate it (<c>Bind</c>), double it (<c>Map</c>) and read the outcome.
/// The input decides where the chain ends: <c>42</c> succeeds, <c>abc</c> fails to parse, and <c>-5</c> fails
/// validation. Exceptions run the same steps by throwing and catching, as a reference point. Every library must return
/// the same value for the same input, or the run fails.
/// </summary>
[ReturnValueValidator(failOnError: true)]
public class PipelineBenchmarks
{
    [Params("42", "abc", "-5")]
    public string Input { get; set; } = null!;

    [Benchmark(Baseline = true)]
    public int Cspp()
    {
        return CsppLibrary.Pipeline(Input);
    }

    [Benchmark]
    public int FluentResults()
    {
        return FluentResultsLibrary.Pipeline(Input);
    }

    [Benchmark]
    public int ErrorOr()
    {
        return ErrorOrLibrary.Pipeline(Input);
    }

    [Benchmark]
    public int FunctionalExtensions()
    {
        return FunctionalExtensionsLibrary.Pipeline(Input);
    }

    [Benchmark]
    public int OneOf()
    {
        return OneOfLibrary.Pipeline(Input);
    }

    [Benchmark]
    public int ArdalisResult()
    {
        return ArdalisResultLibrary.Pipeline(Input);
    }

    [Benchmark]
    public int Exceptions()
    {
        return ExceptionsLibrary.Pipeline(Input);
    }
}