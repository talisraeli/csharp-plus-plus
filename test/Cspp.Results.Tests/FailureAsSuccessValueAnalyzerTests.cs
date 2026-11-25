using System.Collections.Immutable;

using Cspp.Results.Analyzers;

using JetBrains.Annotations;

using Microsoft.CodeAnalysis;

namespace Cspp.Results.Tests;

[TestFixture]
[TestSubject(typeof(FailureAsSuccessValueAnalyzer))]
public class FailureAsSuccessValueAnalyzerTests
{
    [Test]
    public void SupportedDiagnostics_WhenRead_ContainsEnabledErrorRule()
    {
        DiagnosticDescriptor rule = new FailureAsSuccessValueAnalyzer().SupportedDiagnostics.Single();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(rule.Id, Is.EqualTo(FailureAsSuccessValueAnalyzer.DiagnosticId));
            Assert.That(rule.DefaultSeverity, Is.EqualTo(DiagnosticSeverity.Error));
            Assert.That(rule.IsEnabledByDefault, Is.True);
            Assert.That(rule.Category, Is.EqualTo("Usage"));
        }
    }

    [TestCase("Result<object> result = (object)Failure.Unknown;", "Failure.Unknown")]
    [TestCase("var result = new Result<object>((object)Failure.Unknown);", "Failure.Unknown")]
    [TestCase("Result<object> result = (object)new ValidationFailure(\"Code\", \"Error\");", "new ValidationFailure(\"Code\", \"Error\")")]
    [TestCase("Result<System.IEquatable<Failure>> result = (System.IEquatable<Failure>)Failure.Unknown;", "Failure.Unknown")]
    [TestCase("Result<object> result = true ? (object)Failure.Unknown : 1;", "Failure.Unknown")]
    [TestCase("Result<object?> result = (object?)null ?? Failure.Unknown;", "Failure.Unknown")]
    [TestCase("Result<object> result = 1 switch { 1 => (object)GetFailure(), _ => 2 };", "GetFailure()")]
    [TestCase("System.Func<Result<object>> make = () => (object)Failure.Unknown;", "Failure.Unknown")]
    [TestCase("var bound = Wrap(1).Bind<int, object>(_ => (object)Failure.Unknown);", "Failure.Unknown")]
    public async Task Analyze_FailureBecomingSuccessValue_ReportsAtFailure(string statement, string expected)
    {
        ImmutableArray<Diagnostic> diagnostics = await AnalyzeStatementAsync(statement);

        Assert.That(diagnostics.Select(AnalyzerHarness.FlaggedText), Is.EqualTo(new[] { expected }));
    }

    [Test]
    public async Task Analyze_FailureBecomingSuccessValue_ReportsTypesInMessage()
    {
        ImmutableArray<Diagnostic> diagnostics =
            await AnalyzeStatementAsync("Result<object> result = (object)new ValidationFailure(\"Code\", \"Error\");");

        Diagnostic diagnostic = diagnostics.Single();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(diagnostic.Id, Is.EqualTo(FailureAsSuccessValueAnalyzer.DiagnosticId));
            Assert.That(
                diagnostic.GetMessage(),
                Is.EqualTo(
                    "This 'ValidationFailure' becomes the success value of 'Cspp.Results.Result<object>'; convert the Failure itself, without a cast, to make a failed result"));
        }
    }

    [TestCase("Result<object> result = Failure.Unknown;")]
    [TestCase("var result = new Result<object>(Failure.Unknown);")]
    [TestCase("Result<int> result = 1;")]
    [TestCase("Result<object> result = (object)\"text\";")]
    [TestCase("Result<object> result = (object)Failure.Unknown.Code;")]
    [TestCase("Result<object> result = (object)(Result<int>)Failure.Unknown;")]
    [TestCase("Result<ValidationFailure> result = new ValidationFailure(\"Code\", \"Error\");")]
    [TestCase("object boxed = Failure.Unknown; Result<object> result = boxed;")]
    [TestCase("var result = new Result<object>();")]
    [TestCase("var list = new List<int>(4);")]
    [TestCase("var value = new object();")]
    [TestCase("System.DateTimeOffset now = System.DateTime.Now;")]
    public async Task Analyze_NoFailureBecomingSuccessValue_ReportsNothing(string statement)
    {
        ImmutableArray<Diagnostic> diagnostics = await AnalyzeStatementAsync(statement);

        Assert.That(diagnostics, Is.Empty);
    }

    [Test]
    public async Task Analyze_WithoutResultsLibrary_ReportsNothing()
    {
        ImmutableArray<Diagnostic> diagnostics = await AnalyzerHarness.AnalyzeSourceAsync(
            new FailureAsSuccessValueAnalyzer(),
            """
            public static class Sample
            {
                public static object Run() => (object)"text";
            }
            """,
            referenceResultsLibrary: false);

        Assert.That(diagnostics, Is.Empty);
    }

    private static Task<ImmutableArray<Diagnostic>> AnalyzeStatementAsync(string statement)
    {
        return AnalyzerHarness.AnalyzeStatementAsync(new FailureAsSuccessValueAnalyzer(), statement);
    }
}