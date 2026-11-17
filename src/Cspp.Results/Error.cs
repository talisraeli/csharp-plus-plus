using static System.String;

namespace Cspp.Results;

public record Error
{
    public static readonly Error Unknown = new(
        Join(".", nameof(Error), nameof(Unknown)),
        "Unknown Error");

    public static readonly Error Uninitialized = new(
        Join(".", nameof(Error), nameof(Uninitialized)),
        "The result was not initialized with a value or an error.");

    public Error(string code, string message)
    {
        if (!IsValidError(code, message))
        {
            throw new InvalidOperationException("None error provided.");
        }

        Code = code;
        Message = message;
    }

    public string Code { get; init; }
    public string Message { get; init; }

    private static bool IsValidError(string code, string message)
    {
        return !IsNullOrWhiteSpace(code) && !IsNullOrWhiteSpace(message);
    }
}