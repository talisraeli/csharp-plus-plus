namespace Cspp.Results;

public static class ResultExtensions
{
    extension(Result)
    {
        public static Success Success => Success.Instance;
    }
}