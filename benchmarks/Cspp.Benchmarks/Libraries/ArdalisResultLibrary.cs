using Ardalis.Result;

namespace Cspp.Benchmarks.Libraries;

/// <summary>
/// The benchmark scenarios written with Ardalis.Result, reporting failures as <see cref="ValidationError" />s, which
/// carry a code and a message.
/// </summary>
internal static class ArdalisResultLibrary
{
    public static Result<int> Success(int value)
    {
        return Result.Success(value);
    }

    public static Result<int> Failure()
    {
        return Invalid(Errors.ParseCode, Errors.ParseMessage);
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
        return int.TryParse(input, out int value) ? Result.Success(value) : Invalid(Errors.ParseCode, Errors.ParseMessage);
    }

    private static Result<int> Validate(int value)
    {
        return value > 0 ? Result.Success(value) : Invalid(Errors.ValidateCode, Errors.ValidateMessage);
    }

    private static Result<int> Invalid(string code, string message)
    {
        return Result<int>.Invalid(new ValidationError { ErrorCode = code, ErrorMessage = message });
    }
}