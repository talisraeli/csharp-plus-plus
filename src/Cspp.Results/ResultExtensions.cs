namespace Cspp.Results;

public static class ResultExtensions
{
    extension(Result)
    {
        public static Result Success => Success.Instance;
    }
}