using JetBrains.Annotations;

namespace Cspp.Results.Tests;

[TestFixture]
[TestSubject(typeof(AggregateFailure))]
public class AggregateFailureTests
{
    private static readonly Failure First = new("Name.Empty", "The name is empty.");

    private static readonly Failure Second = new("Email.Invalid", "The email is invalid.");

    [Test]
    public void Constructor_WithFailures_HoldsThemInOrderWithMultipleCode()
    {
        AggregateFailure aggregate = new([First, Second]);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(aggregate.Failures, Is.EqualTo(new[] { First, Second }));
            Assert.That(aggregate.Code, Is.EqualTo("Failure.Multiple"));
            Assert.That(aggregate.Error, Is.EqualTo("2 failures occurred."));
        }
    }

    [Test]
    public void Constructor_WithOneFailure_DescribesOneFailure()
    {
        AggregateFailure aggregate = new([First]);

        Assert.That(aggregate.Error, Is.EqualTo("1 failure occurred."));
    }

    [Test]
    public void Constructor_WithNullFailures_ThrowsArgumentNullException()
    {
        Assert.That(
            () => new AggregateFailure(null!),
            Throws.TypeOf<ArgumentNullException>().With.Property("ParamName").EqualTo("failures"));
    }

    [Test]
    public void Constructor_WithNoFailures_ThrowsArgumentException()
    {
        Assert.That(
            () => new AggregateFailure([]),
            Throws.TypeOf<ArgumentException>().With.Property("ParamName").EqualTo("failures"));
    }

    [Test]
    public void Constructor_WithNullFailure_ThrowsArgumentException()
    {
        Assert.That(
            () => new AggregateFailure([First, null!]),
            Throws.TypeOf<ArgumentException>().With.Property("ParamName").EqualTo("failures"));
    }

    [Test]
    public void Constructor_WhenSourceChangesLater_KeepsItsOwnCopy()
    {
        List<Failure> source = [First];
        AggregateFailure aggregate = new(source);

        source.Add(Second);

        Assert.That(aggregate.Failures, Is.EqualTo(new[] { First }));
    }

    [Test]
    public void Failures_WhenCastToList_IsReadOnly()
    {
        AggregateFailure aggregate = new([First, Second]);

        Assert.That(((IList<Failure>)aggregate.Failures).IsReadOnly, Is.True);
    }

    [Test]
    public void Equals_SameFailuresInSameOrder_ReturnsTrueWithSameHash()
    {
        AggregateFailure first = new([First, Second]);
        AggregateFailure second = new([First, Second]);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(first, Is.EqualTo(second));
            Assert.That(first.GetHashCode(), Is.EqualTo(second.GetHashCode()));
        }
    }

    [Test]
    public void Equals_SameFailuresInOtherOrder_ReturnsFalse()
    {
        AggregateFailure first = new([First, Second]);
        AggregateFailure second = new([Second, First]);

        Assert.That(first, Is.Not.EqualTo(second));
    }

    [Test]
    public void Equals_Null_ReturnsFalse()
    {
        AggregateFailure aggregate = new([First]);

        Assert.That(aggregate.Equals((AggregateFailure?)null), Is.False);
    }

    [Test]
    public void Equals_PlainFailureWithSameCodeAndError_ReturnsFalse()
    {
        AggregateFailure aggregate = new([First, Second]);
        Failure plain = new(aggregate.Code, aggregate.Error);

        Assert.That(aggregate, Is.Not.EqualTo(plain));
    }
}