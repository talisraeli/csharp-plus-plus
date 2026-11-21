using OneOf;

namespace Cspp.Benchmarks.Libraries;

/// <summary>
/// The benchmark scenarios written with OneOf. OneOf has no <c>Bind</c>, so the pipeline chains steps with
/// <c>Match</c>, as OneOf code usually does.
/// </summary>
internal static class OneOfLibrary
{
    public static OneOf<int, ErrorInfo> Success(int value)
    {
        return value;
    }

    public static OneOf<int, ErrorInfo> Failure()
    {
        return new ErrorInfo(Errors.ParseCode, Errors.ParseMessage);
    }

    public static int Match(OneOf<int, ErrorInfo> result)
    {
        return result.Match(static value => value, static _ => -1);
    }

    public static int Pipeline(string input)
    {
        return Parse(input)
            .Match(Validate, static error => error)
            .MapT0(static value => value * 2)
            .Match(static value => value, static _ => -1);
    }

    private static OneOf<int, ErrorInfo> Parse(string input)
    {
        return int.TryParse(input, out int value) ? value : new ErrorInfo(Errors.ParseCode, Errors.ParseMessage);
    }

    private static OneOf<int, ErrorInfo> Validate(int value)
    {
        return value > 0 ? value : new ErrorInfo(Errors.ValidateCode, Errors.ValidateMessage);
    }
}