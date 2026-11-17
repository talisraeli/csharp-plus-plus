using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Cspp.Results;

[Union]
public readonly record struct Result : IUnion
{
    private readonly Error? error;
    private readonly ResultType type;

    public Result(Success success)
    {
        type = ResultType.Success;
    }

    public Result(Error error)
    {
        this.error = error;
        type = ResultType.Failure;
    }

    public object? Value => type == ResultType.Success ? Success.Instance : Error;

    public bool HasValue => true;

    private Error Error => error ?? Error.Unknown;

    public bool TryGetValue([NotNullWhen(true)] out Success? success)
    {
        success = type == ResultType.Success ? Success.Instance : null;
        return type == ResultType.Success;
    }

    public bool TryGetValue([NotNullWhen(true)] out Error? error)
    {
        error = type == ResultType.Failure ? Error : null;
        return type == ResultType.Failure;
    }
}

[Union]
public readonly record struct Result<T> : IUnion
{
    private readonly T value;
    private readonly Error? error;
    private readonly ResultType type;

    public Result(T value)
    {
        this.value = value;
        type = ResultType.Success;
    }

    public Result(Error error)
    {
        value = default!;
        this.error = error;
        type = ResultType.Failure;
    }

    public object? Value => type == ResultType.Success ? value : Error;

    public bool HasValue => true;

    private Error Error => error ?? Error.Unknown;

    public bool TryGetValue([MaybeNullWhen(false)] out T value)
    {
        value = type == ResultType.Success ? this.value : default;
        return type == ResultType.Success;
    }

    public bool TryGetValue([NotNullWhen(true)] out Error? error)
    {
        error = type == ResultType.Failure ? Error : null;
        return type == ResultType.Failure;
    }
}