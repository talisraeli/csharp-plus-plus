using System.Collections.ObjectModel;

using static System.String;

namespace Cspp.Results;

/// <summary>
/// Several failures reported together, such as every field that failed validation. <c>Result.CombineAll</c> returns it
/// when more than one result fails.
/// </summary>
/// <remarks>
/// Its <see cref="Failure.Code" /> is <c>Failure.Multiple</c>. Two aggregate failures are equal when they hold equal
/// failures in the same order.
/// </remarks>
public sealed record AggregateFailure : Failure
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AggregateFailure" /> class.
    /// </summary>
    /// <param name="failures">The failures, in order. Must contain at least one failure and no <see langword="null" />.</param>
    /// <exception cref="ArgumentNullException"><paramref name="failures" /> is <see langword="null" />.</exception>
    /// <exception cref="ArgumentException">
    /// <paramref name="failures" /> is empty or contains <see langword="null" />.
    /// </exception>
    public AggregateFailure(IEnumerable<Failure> failures)
        : this(Validate(failures))
    {
    }

    private AggregateFailure(ReadOnlyCollection<Failure> failures)
        : base(Join(".", nameof(Failure), "Multiple"), Describe(failures.Count))
    {
        Failures = failures;
    }

    /// <summary>
    /// Gets the failures, in order.
    /// </summary>
    public IReadOnlyList<Failure> Failures { get; }

    /// <summary>
    /// Determines whether this failure and <paramref name="other" /> hold equal failures in the same order.
    /// </summary>
    /// <param name="other">The failure to compare with.</param>
    /// <returns><see langword="true" /> if both hold equal failures in the same order.</returns>
    public bool Equals(AggregateFailure? other)
    {
        return other is not null && base.Equals(other) && Failures.SequenceEqual(other.Failures);
    }

    /// <inheritdoc />
    public override int GetHashCode()
    {
        HashCode hash = default;
        hash.Add(base.GetHashCode());
        foreach (Failure failure in Failures)
        {
            hash.Add(failure);
        }

        return hash.ToHashCode();
    }

    private static ReadOnlyCollection<Failure> Validate(IEnumerable<Failure> failures)
    {
        ArgumentNullException.ThrowIfNull(failures);

        Failure[] copy = [.. failures];
        if (copy.Length == 0)
        {
            throw new ArgumentException("At least one failure is required.", nameof(failures));
        }

        if (copy.Any(failure => failure is null))
        {
            throw new ArgumentException("The failures must not contain null.", nameof(failures));
        }

        return copy.AsReadOnly();
    }

    private static string Describe(int count)
    {
        return count == 1 ? "1 failure occurred." : $"{count} failures occurred.";
    }
}