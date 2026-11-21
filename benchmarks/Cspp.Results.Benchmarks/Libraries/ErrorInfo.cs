namespace Cspp.Benchmarks.Libraries;

/// <summary>
/// An error with a code and a message, for libraries that let the caller choose the error type.
/// </summary>
/// <param name="Code">The error code.</param>
/// <param name="Message">The error message.</param>
public sealed record ErrorInfo(string Code, string Message);