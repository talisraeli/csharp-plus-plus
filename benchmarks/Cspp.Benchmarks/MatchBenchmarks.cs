using BenchmarkDotNet.Attributes;

using Cspp.Benchmarks.Libraries;
using Cspp.Results;

using ErrorOr;

using OneOf;

using ArdalisResult = Ardalis.Result.Result<int>;
using FluentResult = FluentResults.Result<int>;
using FunctionalExtensionsResult = CSharpFunctionalExtensions.Result<int, Cspp.Benchmarks.Libraries.ErrorInfo>;

namespace Cspp.Benchmarks;

/// <summary>
/// Measures reading an existing result: getting the value on a success, or a fallback on a failure. Libraries with a
/// <c>Match</c> method use it; the others check <c>IsSuccess</c> and read <c>Value</c>.
/// </summary>
[ReturnValueValidator(failOnError: true)]
public class MatchBenchmarks
{
    private Result<int> cspp;
    private FluentResult fluentResults = null!;
    private ErrorOr<int> errorOr;
    private FunctionalExtensionsResult functionalExtensions;
    private OneOf<int, ErrorInfo> oneOf;
    private ArdalisResult ardalisResult = null!;

    [Params(true, false)]
    public bool Succeeds { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        cspp = Succeeds ? CsppLibrary.Success(42) : CsppLibrary.Failure();
        fluentResults = Succeeds ? FluentResultsLibrary.Success(42) : FluentResultsLibrary.Failure();
        errorOr = Succeeds ? ErrorOrLibrary.Success(42) : ErrorOrLibrary.Failure();
        functionalExtensions = Succeeds ? FunctionalExtensionsLibrary.Success(42) : FunctionalExtensionsLibrary.Failure();
        oneOf = Succeeds ? OneOfLibrary.Success(42) : OneOfLibrary.Failure();
        ardalisResult = Succeeds ? ArdalisResultLibrary.Success(42) : ArdalisResultLibrary.Failure();
    }

    [Benchmark(Baseline = true)]
    public int Cspp()
    {
        return CsppLibrary.Match(cspp);
    }

    [Benchmark]
    public int FluentResults()
    {
        return FluentResultsLibrary.Match(fluentResults);
    }

    [Benchmark]
    public int ErrorOr()
    {
        return ErrorOrLibrary.Match(errorOr);
    }

    [Benchmark]
    public int FunctionalExtensions()
    {
        return FunctionalExtensionsLibrary.Match(functionalExtensions);
    }

    [Benchmark]
    public int OneOf()
    {
        return OneOfLibrary.Match(oneOf);
    }

    [Benchmark]
    public int ArdalisResult()
    {
        return ArdalisResultLibrary.Match(ardalisResult);
    }
}