using JetBrains.Annotations;

namespace Cspp.Results.Tests;

[TestFixture]
[TestSubject(typeof(Failure))]
public class FailureTests
{
    [Test]
    public void Constructor_WithNullCode_ThrowsInvalidOperationException()
    {
        Assert.That(() => new Failure(null!, "Error"), Throws.TypeOf<InvalidOperationException>());
    }

    [Test]
    public void Constructor_WithNullError_ThrowsInvalidOperationException()
    {
        Assert.That(() => new Failure("Code", null!), Throws.TypeOf<InvalidOperationException>());
    }

    [Test]
    public void Constructor_WithValidArguments_SetsCodeAndError()
    {
        Failure failure = new("Code", "Error");

        using (Assert.EnterMultipleScope())
        {
            Assert.That(failure.Code, Is.EqualTo("Code"));
            Assert.That(failure.Error, Is.EqualTo("Error"));
        }
    }

    [Test]
    public void Unknown_WhenAccessed_IsNotNull()
    {
        Assert.That(Failure.Unknown, Is.Not.Null);
    }

    [Test]
    public void Uninitialized_ComparedToUnknown_HasDistinctCodeAndError()
    {
        using (Assert.EnterMultipleScope())
        {
            Assert.That(Failure.Uninitialized.Code, Is.Not.EqualTo(Failure.Unknown.Code));
            Assert.That(Failure.Uninitialized.Error, Is.Not.EqualTo(Failure.Unknown.Error));
        }
    }
}