using JetBrains.Annotations;

namespace Cspp.Results.Tests;

[TestFixture]
[TestSubject(typeof(Failure))]
public class FailureTests
{
    [Test]
    public void Constructor_WithNullCode_ThrowsArgumentNullException()
    {
        Assert.That(
            () => new Failure(null!, "Error"),
            Throws.TypeOf<ArgumentNullException>().With.Property("ParamName").EqualTo("code"));
    }

    [Test]
    public void Constructor_WithNullError_ThrowsArgumentNullException()
    {
        Assert.That(
            () => new Failure("Code", null!),
            Throws.TypeOf<ArgumentNullException>().With.Property("ParamName").EqualTo("error"));
    }

    [TestCase("")]
    [TestCase(" ")]
    [TestCase("\t\r\n")]
    [TestCase("\u2003")]
    public void Constructor_WithBlankCode_ThrowsArgumentException(string code)
    {
        Assert.That(
            () => new Failure(code, "Error"),
            Throws.TypeOf<ArgumentException>().With.Property("ParamName").EqualTo("code"));
    }

    [TestCase("")]
    [TestCase(" ")]
    [TestCase("\t\r\n")]
    [TestCase("\u2003")]
    public void Constructor_WithBlankError_ThrowsArgumentException(string error)
    {
        Assert.That(
            () => new Failure("Code", error),
            Throws.TypeOf<ArgumentException>().With.Property("ParamName").EqualTo("error"));
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

    [Test]
    public void Exception_ComparedToOtherPredefinedFailures_HasDistinctCode()
    {
        using (Assert.EnterMultipleScope())
        {
            Assert.That(Failure.Exception.Code, Is.EqualTo("Failure.Exception"));
            Assert.That(Failure.Exception.Code, Is.Not.EqualTo(Failure.Unknown.Code));
            Assert.That(Failure.Exception.Code, Is.Not.EqualTo(Failure.Uninitialized.Code));
        }
    }

    [Test]
    public void CodeAndError_AreGetterOnly()
    {
        // with-expressions and object initializers cannot assign these properties after
        // the constructor, so those bypass routes are compile-time errors rather than
        // runtime ArgumentException cases.
        using (Assert.EnterMultipleScope())
        {
            Assert.That(typeof(Failure).GetProperty(nameof(Failure.Code))!.SetMethod, Is.Null);
            Assert.That(typeof(Failure).GetProperty(nameof(Failure.Error))!.SetMethod, Is.Null);
        }
    }
}