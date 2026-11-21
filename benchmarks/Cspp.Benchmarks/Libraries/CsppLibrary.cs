using Cspp.Results;

namespace Cspp.Benchmarks.Libraries;

/// <summary>
/// The benchmark scenarios written with Cspp.Results.
/// </summary>
internal static class CsppLibrary
{
    public static Result<int> Success(int value)
    {
        return value;
    }

    public static Result<int> Failure()
    {
        return new Failure(Errors.ParseCode, Errors.ParseMessage);
    }

    public static int Match(Result<int> result)
    {
        return result.Match(static value => value, static _ => -1);
    }

    public static int Pipeline(string input)
    {
        return Parse(input)
            .Bind(Validate)
            .Map(static value => value * 2)
            .Match(static value => value, static _ => -1);
    }

    private static Result<int> Parse(string input)
    {
        return int.TryParse(input, out int value) ? value : new Failure(Errors.ParseCode, Errors.ParseMessage);
    }

    private static Result<int> Validate(int value)
    {
        return value > 0 ? value : new Failure(Errors.ValidateCode, Errors.ValidateMessage);
    }
}