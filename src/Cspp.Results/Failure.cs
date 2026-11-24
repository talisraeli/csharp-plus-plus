using static System.String;

namespace Cspp.Results;

public record Failure
{
    public static readonly Failure Unknown = new(
        Join(".", nameof(Failure), nameof(Unknown)),
        "Unknown failure.");

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

    public string Code { get; }

    public string Error { get; }
}