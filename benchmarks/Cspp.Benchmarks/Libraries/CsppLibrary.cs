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

    // The same chain with an asynchronous parse step, chained on the task without awaiting each step.
    public static Task<int> PipelineAsync(string input)
    {
        return Chain(ParseAsync(input));
    }

    // The same chain on a parse step that has already completed, which leaves only the cost of the task members.
    public static Task<int> PipelineCompletedAsync(string input)
    {
        return Chain(Task.FromResult(Parse(input)));
    }

    private static Task<int> Chain(Task<Result<int>> parsed)
    {
        return parsed
            .Bind(Validate)
            .Map(static value => value * 2)
            .Match(static value => value, static _ => -1);
    }

    // An asynchronous step that has to wait, such as I/O, so every step after it runs as a continuation.
    private static async Task<Result<int>> ParseAsync(string input)
    {
        await Task.Yield();
        return Parse(input);
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