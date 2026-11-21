using CSharpFunctionalExtensions;

namespace Cspp.Benchmarks.Libraries;

/// <summary>
/// The benchmark scenarios written with CSharpFunctionalExtensions, using <c>Result&lt;T, E&gt;</c> so the error
/// carries a code as well as a message.
/// </summary>
internal static class FunctionalExtensionsLibrary
{
    public static Result<int, ErrorInfo> Success(int value)
    {
        return Result.Success<int, ErrorInfo>(value);
    }

    public static Result<int, ErrorInfo> Failure()
    {
        return Result.Failure<int, ErrorInfo>(new ErrorInfo(Errors.ParseCode, Errors.ParseMessage));
    }

    public static int Match(Result<int, ErrorInfo> result)
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

    private static Result<int, ErrorInfo> Parse(string input)
    {
        return int.TryParse(input, out int value)
            ? Result.Success<int, ErrorInfo>(value)
            : Result.Failure<int, ErrorInfo>(new ErrorInfo(Errors.ParseCode, Errors.ParseMessage));
    }

    private static Result<int, ErrorInfo> Validate(int value)
    {
        return value > 0
            ? Result.Success<int, ErrorInfo>(value)
            : Result.Failure<int, ErrorInfo>(new ErrorInfo(Errors.ValidateCode, Errors.ValidateMessage));
    }
}