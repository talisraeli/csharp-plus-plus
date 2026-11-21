namespace Cspp.Benchmarks.Libraries;

/// <summary>
/// The codes and messages every library reports, so each one fails with the same information.
/// </summary>
internal static class Errors
{
    public const string ParseCode = "Input.NotANumber";

    public const string ParseMessage = "The input is not a number.";

    public const string ValidateCode = "Input.NotPositive";

    public const string ValidateMessage = "The number must be positive.";
}