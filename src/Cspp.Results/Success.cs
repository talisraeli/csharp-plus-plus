namespace Cspp.Results;

public sealed record Success
{
    public static readonly Success Instance = new();

    private Success()
    {
    }
}