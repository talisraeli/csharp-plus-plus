using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Cspp.Results;

[Union]
public readonly record struct Result : IUnion
{
    private readonly Failure? failure;
    private readonly ResultType type;

    public Result(Success success)
    {
        type = ResultType.Success;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Result" /> struct from a failure.
    /// </summary>
    /// <param name="failure">The failure. Cannot be <see langword="null" />.</param>
    /// <exception cref="ArgumentNullException"><paramref name="failure" /> is <see langword="null" />.</exception>
    public Result(Failure failure)
    {
        ArgumentNullException.ThrowIfNull(failure);
        this.failure = failure;
        type = ResultType.Failure;
    }

    public bool HasValue => true;

    public bool IsSuccess => type == ResultType.Success;

    public bool IsFailure => type == ResultType.Failure;

    public object? Value => IsSuccess ? Success.Instance : Failure;

    internal Failure Failure => failure ?? Failure.Uninitialized;

    public bool TryGetValue([NotNullWhen(true)] out Success? success)
    {
        success = IsSuccess ? Success.Instance : null;
        return IsSuccess;
    }

    public bool TryGetValue([NotNullWhen(true)] out Failure? failure)
    {
        failure = IsFailure ? Failure : null;
        return IsFailure;
    }

    /// <summary>
    /// Determines whether this result and <paramref name="other" /> hold the same case. Two failures are equal when their
    /// failures are equal, so a <see langword="default" /> result equals one created from
    /// <see cref="Failure.Uninitialized" />.
    /// </summary>
    /// <param name="other">The result to compare with.</param>
    /// <returns><see langword="true" /> if both results are successes, or both hold equal failures.</returns>
    public bool Equals(Result other)
    {
        return type == other.type && (IsSuccess || Failure == other.Failure);
    }

    /// <inheritdoc />
    public override int GetHashCode()
    {
        return IsSuccess ? HashCode.Combine(type) : HashCode.Combine(type, Failure);
    }
}

[Union]
public readonly record struct Result<T> : IUnion
{
    private readonly Result state;
    private readonly T value;

    public Result(T value)
    {
        this.value = value;
        state = Success.Instance;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Result{T}" /> struct from a failure.
    /// </summary>
    /// <param name="failure">The failure. Cannot be <see langword="null" />.</param>
    /// <exception cref="ArgumentNullException"><paramref name="failure" /> is <see langword="null" />.</exception>
    public Result(Failure failure)
    {
        ArgumentNullException.ThrowIfNull(failure);
        value = default!;
        state = failure;
    }

    public bool HasValue => true;

    public bool IsSuccess => state.IsSuccess;

    public bool IsFailure => state.IsFailure;

    public object? Value => IsSuccess ? value : state.Failure;

    internal Failure Failure => state.Failure;

    public bool TryGetValue([MaybeNullWhen(false)] out T value)
    {
        value = IsSuccess ? this.value : default;
        return IsSuccess;
    }

    public bool TryGetValue([NotNullWhen(true)] out Failure? failure)
    {
        return state.TryGetValue(out failure);
    }

    /// <summary>
    /// Determines whether this result and <paramref name="other" /> hold the same case. Two successes are equal when
    /// their values are equal, and two failures when their failures are equal, so a <see langword="default" /> result
    /// equals one created from <see cref="Failure.Uninitialized" />.
    /// </summary>
    /// <param name="other">The result to compare with.</param>
    /// <returns><see langword="true" /> if both results hold equal values or equal failures.</returns>
    public bool Equals(Result<T> other)
    {
        return state == other.state && (IsFailure || EqualityComparer<T>.Default.Equals(value, other.value));
    }

    /// <inheritdoc />
    public override int GetHashCode()
    {
        return IsSuccess ? HashCode.Combine(state, value) : state.GetHashCode();
    }
}