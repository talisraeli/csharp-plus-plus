using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Cspp.Results;

/// <summary>
/// The outcome of an operation that has no value to return: a success, or a <see cref="Results.Failure" />.
/// </summary>
/// <remarks>
/// <para>
/// Return <c>Result.Success</c> or a <see cref="Results.Failure" />; both convert to a <see cref="Result" />
/// implicitly. A <c>switch</c> over the cases <see cref="Success" /> and
/// <see cref="Results.Failure" /> is exhaustive without a discard arm.
/// </para>
/// <para>
/// A <see langword="default" /> result is a failure carrying <see cref="Failure.Uninitialized" />.
/// </para>
/// </remarks>
[Union]
[DebuggerDisplay("{ToString(),nq}")]
public readonly record struct Result : IUnion
{
    private readonly Failure? failure;
    private readonly ResultType type;

    /// <summary>
    /// Initializes a new instance of the <see cref="Result" /> struct as a success. The implicit conversion from
    /// <c>Result.Success</c> calls this constructor.
    /// </summary>
    /// <param name="success">The success marker, <c>Result.Success</c>.</param>
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

    /// <summary>
    /// Gets a value indicating whether <see cref="Value" /> is not <see langword="null" />. Always
    /// <see langword="true" />. It is part of the <see cref="IUnion" /> pattern that <c>switch</c> and <c>is</c> use.
    /// </summary>
    public bool HasValue => true;

    /// <summary>
    /// Gets a value indicating whether the result is a success.
    /// </summary>
    public bool IsSuccess => type == ResultType.Success;

    /// <summary>
    /// Gets a value indicating whether the result is a failure, including a <see langword="default" /> result.
    /// </summary>
    public bool IsFailure => type == ResultType.Failure;

    /// <summary>
    /// Gets the case the result holds: the <see cref="Success" /> marker, or the <see cref="Results.Failure" />. It is
    /// part of the <see cref="IUnion" /> pattern; pattern matching or <c>TryGetValue</c> is usually clearer.
    /// </summary>
    public object? Value => IsSuccess ? Success.Instance : Failure;

    internal Failure Failure => failure ?? Failure.Uninitialized;

    /// <summary>
    /// Gets the success marker if the result is a success. Pattern matching on <see cref="Success" /> uses this.
    /// </summary>
    /// <param name="success">
    /// The success marker, or <see langword="null" /> if this method returns <see langword="false" />.
    /// </param>
    /// <returns><see langword="true" /> if the result is a success.</returns>
    public bool TryGetValue([NotNullWhen(true)] out Success? success)
    {
        success = IsSuccess ? Success.Instance : null;
        return IsSuccess;
    }

    /// <summary>
    /// Gets the failure if the result is a failure. Pattern matching on <see cref="Results.Failure" /> uses this.
    /// </summary>
    /// <param name="failure">
    /// The failure, <see cref="Failure.Uninitialized" /> for a <see langword="default" /> result, or
    /// <see langword="null" /> if this method returns <see langword="false" />.
    /// </param>
    /// <returns><see langword="true" /> if the result is a failure.</returns>
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

    /// <summary>
    /// Describes the result: <c>Success</c>, or <c>Failure(</c><i>failure</i><c>)</c> with the failure's own
    /// description, such as <c>Failure(Parse.Invalid: 'x' is not a number.)</c>.
    /// </summary>
    /// <returns>A short description of the case the result holds.</returns>
    public override string ToString()
    {
        return IsSuccess ? nameof(Success) : Describe(Failure);
    }

    // The description of a failure case, shared with Result<T>.
    internal static string Describe(Failure failure)
    {
        return $"{nameof(Results.Failure)}({failure})";
    }
}

/// <summary>
/// The outcome of an operation that returns a value: a success holding a <typeparamref name="T" />, or a
/// <see cref="Results.Failure" />.
/// </summary>
/// <remarks>
/// <para>
/// Return a <typeparamref name="T" /> or a <see cref="Results.Failure" />; both convert to a <see cref="Result{T}" />
/// implicitly. A <c>switch</c> over the cases <typeparamref name="T" /> and <see cref="Results.Failure" /> is exhaustive
/// without a discard arm. A success value may be <see langword="null" /> when <typeparamref name="T" /> allows it; a
/// <c>null</c> pattern matches it, and it is still a success.
/// </para>
/// <para>
/// A <see langword="default" /> result is a failure carrying <see cref="Failure.Uninitialized" />.
/// </para>
/// </remarks>
/// <typeparam name="T">
/// The type of the success value. It must not be <see cref="Results.Failure" /> or derive from it, because a success
/// value and a failure would then look the same; the <c>CSPP0001</c> analyzer reports this as an error.
/// </typeparam>
[Union]
[DebuggerDisplay("{ToString(),nq}")]
public readonly record struct Result<T> : IUnion
{
    // Whether T can hold null. HasValue checks it first so that the null check never runs on a value type T, where the
    // runtime would box the value to check it until it optimizes the code.
    private static readonly bool CanBeNull = default(T) is null;

    private readonly Result state;
    private readonly T value;

    /// <summary>
    /// Initializes a new instance of the <see cref="Result{T}" /> struct as a success holding
    /// <paramref name="value" />. The implicit conversion from <typeparamref name="T" /> calls this constructor.
    /// </summary>
    /// <param name="value">The success value. May be <see langword="null" /> when <typeparamref name="T" /> allows it.</param>
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

    /// <summary>
    /// Gets a value indicating whether the result is a failure, including a <see langword="default" /> result.
    /// </summary>
    public bool IsFailure => state.IsFailure;

    /// <summary>
    /// Gets the case the result holds: the success value, or the <see cref="Results.Failure" />. It is part of the
    /// <see cref="IUnion" /> pattern. It boxes a value type <typeparamref name="T" />, so pattern matching or
    /// <c>TryGetValue</c>, which don't, are usually better.
    /// </summary>
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

    /// <summary>
    /// Gets the failure if the result is a failure. Pattern matching on <see cref="Results.Failure" /> uses this.
    /// </summary>
    /// <param name="failure">
    /// The failure, <see cref="Failure.Uninitialized" /> for a <see langword="default" /> result, or
    /// <see langword="null" /> if this method returns <see langword="false" />.
    /// </param>
    /// <returns><see langword="true" /> if the result is a failure.</returns>
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

    /// <summary>
    /// Describes the result: <c>Success(</c><i>value</i><c>)</c>, with <c>null</c> for a <see langword="null" /> value,
    /// or <c>Failure(</c><i>failure</i><c>)</c> with the failure's own description.
    /// </summary>
    /// <returns>A short description of the case the result holds.</returns>
    public override string ToString()
    {
        return IsSuccess ? $"{nameof(Success)}({(value is null ? "null" : value.ToString())})" : Result.Describe(Failure);
    }
}