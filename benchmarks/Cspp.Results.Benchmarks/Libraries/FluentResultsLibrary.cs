using FluentResults;

namespace Cspp.Benchmarks.Libraries;

/// <summary>
/// The benchmark scenarios written with FluentResults. The error code is stored as metadata, which is how
/// FluentResults attaches structured data to an error.
/// </summary>
internal static class FluentResultsLibrary
{
    private const string CodeKey = "Code";

    public static Result<int> Success(int value)
    {
        return Result.Ok(value);
    }

    public static Result<int> Failure()
    {
        return Result.Fail<int>(new Error(Errors.ParseMessage).WithMetadata(CodeKey, Errors.ParseCode));
    }

    public static int Match(Result<int> result)
    {
        return result.IsSuccess ? result.Value : -1;
    }

    public static int Pipeline(string input)
    {
        Result<int> result = Parse(input)
            .Bind(Validate)
            .Map(static value => value * 2);

        return result.IsSuccess ? result.Value : -1;
    }

    private static Result<int> Parse(string input)
    {
        return int.TryParse(input, out int value)
            ? Result.Ok(value)
            : Result.Fail<int>(new Error(Errors.ParseMessage).WithMetadata(CodeKey, Errors.ParseCode));
    }

    private static Result<int> Validate(int value)
    {
        return value > 0
            ? Result.Ok(value)
            : Result.Fail<int>(new Error(Errors.ValidateMessage).WithMetadata(CodeKey, Errors.ValidateCode));
    }
}