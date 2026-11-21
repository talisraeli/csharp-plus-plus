using ErrorOr;

namespace Cspp.Benchmarks.Libraries;

/// <summary>
/// The benchmark scenarios written with ErrorOr.
/// </summary>
internal static class ErrorOrLibrary
{
    public static ErrorOr<int> Success(int value)
    {
        return value;
    }

    public static ErrorOr<int> Failure()
    {
        return Error.Validation(Errors.ParseCode, Errors.ParseMessage);
    }

    public static int Match(ErrorOr<int> result)
    {
        return result.Match(static value => value, static _ => -1);
    }

    public static int Pipeline(string input)
    {
        return Parse(input)
            .Then(Validate)
            .Then(static value => value * 2)
            .Match(static value => value, static _ => -1);
    }

    private static ErrorOr<int> Parse(string input)
    {
        return int.TryParse(input, out int value) ? value : Error.Validation(Errors.ParseCode, Errors.ParseMessage);
    }

    private static ErrorOr<int> Validate(int value)
    {
        return value > 0 ? value : Error.Validation(Errors.ValidateCode, Errors.ValidateMessage);
    }
}