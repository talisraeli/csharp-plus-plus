namespace Cspp.Benchmarks.Libraries;

/// <summary>
/// The pipeline scenario written with exceptions instead of a result type, as a reference point.
/// </summary>
internal static class ExceptionsLibrary
{
    public static int Pipeline(string input)
    {
        try
        {
            return Validate(Parse(input)) * 2;
        }
        catch (InputException)
        {
            return -1;
        }
    }

    private static int Parse(string input)
    {
        return int.TryParse(input, out int value)
            ? value
            : throw new InputException(Errors.ParseCode, Errors.ParseMessage);
    }

    private static int Validate(int value)
    {
        return value > 0 ? value : throw new InputException(Errors.ValidateCode, Errors.ValidateMessage);
    }

    private sealed class InputException(string code, string message) : Exception(message)
    {
        public string Code { get; } = code;
    }
}