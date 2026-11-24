namespace Cspp.Results.Tests;

/// <summary>
/// Measures the managed memory that code under test allocates.
/// </summary>
internal static class Allocations
{
    private const int Attempts = 5;

    /// <summary>
    /// Gets the fewest bytes the current thread allocated in one run of <paramref name="action" />, over several runs
    /// after a warm-up run.
    /// </summary>
    /// <remarks>
    /// The runtime can allocate on the test's thread once, for example while it compiles or optimizes the measured code
    /// partway through a run, which would fail a single measurement. Code that allocates on every call allocates in
    /// every run, so the fewest bytes stay above zero for it.
    /// </remarks>
    /// <param name="action">The code to measure.</param>
    /// <returns>The fewest bytes allocated in one run.</returns>
    public static long Fewest(Action action)
    {
        action();

        long fewest = long.MaxValue;
        for (int attempt = 0; attempt < Attempts && fewest > 0; attempt++)
        {
            long before = GC.GetAllocatedBytesForCurrentThread();
            action();
            fewest = Math.Min(fewest, GC.GetAllocatedBytesForCurrentThread() - before);
        }

        return fewest;
    }
}