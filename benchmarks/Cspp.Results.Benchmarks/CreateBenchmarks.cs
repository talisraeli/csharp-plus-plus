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
/// Measures creating a success or a failure result, including the failure's error object.
/// </summary>
public class CreateBenchmarks
{
    private readonly int value = 42;

    [Params(true, false)]
    public bool Succeeds { get; set; }

    [Benchmark(Baseline = true)]
    public Result<int> Cspp()
    {
        return Succeeds ? CsppLibrary.Success(value) : CsppLibrary.Failure();
    }

    [Benchmark]
    public FluentResult FluentResults()
    {
        return Succeeds ? FluentResultsLibrary.Success(value) : FluentResultsLibrary.Failure();
    }

    [Benchmark]
    public ErrorOr<int> ErrorOr()
    {
        return Succeeds ? ErrorOrLibrary.Success(value) : ErrorOrLibrary.Failure();
    }

    [Benchmark]
    public FunctionalExtensionsResult FunctionalExtensions()
    {
        return Succeeds ? FunctionalExtensionsLibrary.Success(value) : FunctionalExtensionsLibrary.Failure();
    }

    [Benchmark]
    public OneOf<int, ErrorInfo> OneOf()
    {
        return Succeeds ? OneOfLibrary.Success(value) : OneOfLibrary.Failure();
    }

    [Benchmark]
    public ArdalisResult ArdalisResult()
    {
        return Succeeds ? ArdalisResultLibrary.Success(value) : ArdalisResultLibrary.Failure();
    }
}