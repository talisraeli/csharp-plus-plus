using static System.String;

namespace Cspp.Results;

/// <summary>
/// Why an operation failed: a <see cref="Code" /> for code to check and an <see cref="Error" /> message for people.
/// </summary>
/// <remarks>
/// Two failures are equal when they have the same type, <see cref="Code" /> and <see cref="Error" />. Derive a record
/// from <see cref="Failure" /> to carry more detail; a derived failure is still a failure, so it must not be the value
/// type of a <see cref="Result{T}" /> (<c>CSPP0001</c>).
/// </remarks>
public record Failure
{
    /// <summary>
    /// A failure with no more specific cause.
    /// </summary>
    public static readonly Failure Unknown = new(
        Join(".", nameof(Failure), nameof(Unknown)),
        "Unknown failure.");

    /// <summary>
    /// The failure a <see langword="default" /> <see cref="Result" /> or <see cref="Result{T}" /> holds, because it
    /// was never given a value or a failure.
    /// </summary>
    public static readonly Failure Uninitialized = new(
        Join(".", nameof(Failure), nameof(Uninitialized)),
        "The result was not initialized with a value or a failure.");

    /// <summary>
    /// The failure <c>Result.Try</c> and <c>Result.TryAsync</c> return when the code they run throws and the caller
    /// passes no <c>onException</c> delegate.
    /// </summary>
    public static readonly Failure Exception = new(
        Join(".", nameof(Failure), nameof(Exception)),
        "An exception was thrown.");

    /// <summary>
    /// Initializes a new instance of the <see cref="Failure" /> class.
    /// </summary>
    /// <param name="code">The failure code. Cannot be null, empty, or whitespace.</param>
    /// <param name="error">The error message. Cannot be null, empty, or whitespace.</param>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="code" /> or <paramref name="error" /> is
    /// <see langword="null" />.
    /// </exception>
    /// <exception cref="ArgumentException"><paramref name="code" /> or <paramref name="error" /> is empty or whitespace.</exception>
    public Failure(string code, string error)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(code);
        ArgumentException.ThrowIfNullOrWhiteSpace(error);

        Code = code;
        Error = error;
    }

    /// <summary>
    /// Gets the failure code, such as <c>Parse.Invalid</c>, for code to check. Never <see langword="null" />, empty or
    /// whitespace.
    /// </summary>
    public string Code { get; }

    /// <summary>
    /// Gets the error message, for people. Never <see langword="null" />, empty or whitespace.
    /// </summary>
    public string Error { get; }

    /// <summary>
    /// Describes the failure as <i>code</i><c>: </c><i>error</i>, for example
    /// <c>Parse.Invalid: 'x' is not a number.</c>.
    /// </summary>
    /// <returns>The code and the error.</returns>
    /// <remarks>
    /// A record derived from <see cref="Failure" /> gets its own generated <c>ToString</c>, which lists its members.
    /// </remarks>
    public override string ToString()
    {
        return $"{Code}: {Error}";
    }
}