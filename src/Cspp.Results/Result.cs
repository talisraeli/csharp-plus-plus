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
    // Whether T can hold null. HasValue checks it first so that the null check never runs on a value type T, where the
    // runtime would box the value to check it until it optimizes the code.
    private static readonly bool CanBeNull = default(T) is null;

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

    /// <summary>
    /// Gets a value indicating whether <see cref="Value" /> is not <see langword="null" />. It is
    /// <see langword="false" /> only for a success whose value is <see langword="null" />, which a <c>null</c> pattern
    /// matches.
    /// </summary>
    public bool HasValue => IsFailure || !CanBeNull || value is not null;

    /// <summary>
    /// Gets a value indicating whether the result is a success. A success whose value is <see langword="null" /> is
    /// still a success.
    /// </summary>
    public bool IsSuccess => state.IsSuccess;

    public bool IsFailure => state.IsFailure;

    public object? Value => IsSuccess ? value : state.Failure;

    internal Failure Failure => state.Failure;

    // The success value, which may be null, for members that treat a null value as a success.
    internal T SuccessValue => value;

    /// <summary>
    /// Gets the success value if it is not <see langword="null" />. Pattern matching uses this, so a type pattern such as
    /// <c>string s</c> doesn't match a <see langword="null" /> value, as with any other <see langword="null" />.
    /// </summary>
    /// <param name="value">The success value, or <see langword="default" /> if this method returns <see langword="false" />.</param>
    /// <returns>
    /// <see langword="true" /> if the result is a success whose value is not <see langword="null" />. Use
    /// <see cref="IsSuccess" /> to check for a success that may hold <see langword="null" />.
    /// </returns>
    public bool TryGetValue([NotNullWhen(true)] out T? value)
    {
        value = IsSuccess ? this.value : default;
        return IsSuccess && HasValue;
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