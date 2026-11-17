namespace Cspp.Results;

public static class ResultExtensions
{
    extension(Result result)
    {
        public static Result Success => Success.Instance;

        public bool IsSuccess => result.TryGetValue(out Success? _);
        public bool IsFailure => !result.IsSuccess;
    }

    extension<T>(Result<T> result)
    {
        public bool IsSuccess => result.TryGetValue(out T? _);
        public bool IsFailure => !result.IsSuccess;
    }
}