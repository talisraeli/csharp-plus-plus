using JetBrains.Annotations;

namespace Cspp.Results.Tests;

[TestFixture]
[TestSubject(typeof(Error))]
public class ErrorTests
{
    [Test]
    public void Constructor_WithNullCode_ThrowsInvalidOperationException()
    {
        Assert.That(() => new Error(null!, "Message"), Throws.TypeOf<InvalidOperationException>());
    }

    [Test]
    public void Constructor_WithNullMessage_ThrowsInvalidOperationException()
    {
        Assert.That(() => new Error("Code", null!), Throws.TypeOf<InvalidOperationException>());
    }

    [Test]
    public void Constructor_WithValidArguments_SetsCodeAndMessage()
    {
        Error error = new("Code", "Message");

        using (Assert.EnterMultipleScope())
        {
            Assert.That(error.Code, Is.EqualTo("Code"));
            Assert.That(error.Message, Is.EqualTo("Message"));
        }
    }

    [Test]
    public void Unknown_WhenAccessed_IsNotNull()
    {
        Assert.That(Error.Unknown, Is.Not.Null);
    }

    [Test]
    public void Uninitialized_ComparedToUnknown_HasDistinctCodeAndMessage()
    {
        using (Assert.EnterMultipleScope())
        {
            Assert.That(Error.Uninitialized.Code, Is.Not.EqualTo(Error.Unknown.Code));
            Assert.That(Error.Uninitialized.Message, Is.Not.EqualTo(Error.Unknown.Message));
        }
    }
}