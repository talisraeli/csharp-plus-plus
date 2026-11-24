using System.Collections.Immutable;

using Cspp.Results.Analyzers;

using JetBrains.Annotations;

using Microsoft.CodeAnalysis;

namespace Cspp.Results.Tests;

[TestFixture]
[TestSubject(typeof(MapReturnsFailureAnalyzer))]
public class MapReturnsFailureAnalyzerTests
{
    [Test]
    public void SupportedDiagnostics_WhenRead_ContainsEnabledErrorRule()
    {
        DiagnosticDescriptor rule = new MapReturnsFailureAnalyzer().SupportedDiagnostics.Single();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(rule.Id, Is.EqualTo(MapReturnsFailureAnalyzer.DiagnosticId));
            Assert.That(rule.DefaultSeverity, Is.EqualTo(DiagnosticSeverity.Error));
            Assert.That(rule.IsEnabledByDefault, Is.True);
            Assert.That(rule.Category, Is.EqualTo("Usage"));
        }
    }

    [TestCase("var mapped = Wrap(1).Map<int, object>(_ => Failure.Unknown);", "Failure.Unknown")]
    [TestCase("var mapped = Wrap(1).Map(_ => (object)Failure.Unknown);", "Failure.Unknown")]
    [TestCase("Result<object> mapped = Wrap(1).Map<int, object>(_ => new ValidationFailure(\"Code\", \"Error\"));", "new ValidationFailure(\"Code\", \"Error\")")]
    [TestCase("var mapped = Wrap(1).Map<int, System.IEquatable<Failure>>(_ => Failure.Unknown);", "Failure.Unknown")]
    [TestCase("var mapped = Wrap(1).Map<int, object>(value => { if (value > 0) { return value; } return Failure.Unknown; });", "Failure.Unknown")]
    [TestCase("var mapped = Wrap(1).Map<int, object>(value => value > 0 ? value : Failure.Unknown);", "Failure.Unknown")]
    [TestCase("var mapped = Wrap(1).Map<int, object>(value => value switch { > 0 => value, _ => Failure.Unknown });", "Failure.Unknown")]
    [TestCase("var mapped = Wrap(1).Map<int, object?>(_ => (Failure?)null ?? Failure.Unknown);", "Failure.Unknown")]
    [TestCase("var mapped = Wrap(1).Map<int, object>(_ => GetFailure());", "GetFailure()")]
    [TestCase("var mapped = ResultExtensions.Map<int, object>(Wrap(1), _ => Failure.Unknown);", "Failure.Unknown")]
    [TestCase("var mapped = Wrap(1).Map((System.Func<int, object>)(_ => Failure.Unknown));", "Failure.Unknown")]
    [TestCase("Result ok = Result.Success; var mapped = ok.Map<object>(() => Failure.Unknown);", "Failure.Unknown")]
    [TestCase("Result ok = Result.Success; var mapped = ok.Map(() => (object)Failure.Unknown);", "Failure.Unknown")]
    [TestCase("var mapped = Wrap(1).MapAsync<int, object>(async _ => { await Task.Yield(); return Failure.Unknown; });", "Failure.Unknown")]
    [TestCase("var mapped = WrapAsync(1).Map<int, object>(_ => Failure.Unknown);", "Failure.Unknown")]
    [TestCase("var mapped = WrapAsync(1).MapAsync<int, object>(async _ => { await Task.Yield(); return GetFailure(); });", "GetFailure()")]
    [TestCase("var selected = from x in Wrap(1) select (object)Failure.Unknown;", "Failure.Unknown")]
    [TestCase("var selected = Wrap(1).Select<int, object>(_ => Failure.Unknown);", "Failure.Unknown")]
    [TestCase("var combined = from x in Wrap(1) from y in Wrap(2) select (object)Failure.Unknown;", "Failure.Unknown")]
    public async Task Analyze_MapDelegateReturningFailure_ReportsAtReturnedExpression(string statement, string expected)
    {
        ImmutableArray<Diagnostic> diagnostics = await AnalyzeStatementAsync(statement);

        Assert.That(diagnostics.Select(AnalyzerHarness.FlaggedText), Is.EqualTo(new[] { expected }));
    }

    [Test]
    public async Task Analyze_MapDelegateReturningFailure_ReportsTypeNameInMessage()
    {
        ImmutableArray<Diagnostic> diagnostics =
            await AnalyzeStatementAsync("var mapped = Wrap(1).Map<int, object>(_ => new ValidationFailure(\"Code\", \"Error\"));");

        Diagnostic diagnostic = diagnostics.Single();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(diagnostic.Id, Is.EqualTo(MapReturnsFailureAnalyzer.DiagnosticId));
            Assert.That(
                diagnostic.GetMessage(),
                Is.EqualTo(
                    "Map stores this 'ValidationFailure' as a success value; to fail the step, use Bind and return the failure as a Result"));
        }
    }

    [Test]
    public async Task Analyze_MapMethodGroupReturningFailure_ReportsAtMethodGroup()
    {
        ImmutableArray<Diagnostic> diagnostics =
            await AnalyzeStatementAsync("var mapped = Wrap(1).Map<int, object>(ToFailure); static Failure ToFailure(int value) => Failure.Unknown;");

        Assert.That(diagnostics.Select(AnalyzerHarness.FlaggedText), Is.EqualTo(new[] { "ToFailure" }));
    }

    [TestCase("var mapped = Wrap(1).Map(_ => Failure.Unknown);")]
    [TestCase("var mapped = Wrap(1).Map(value => value.ToString());")]
    [TestCase("var mapped = Wrap(1).Map<int, object>(value => value);")]
    [TestCase("var mapped = Wrap(1).Map<int, object?>(_ => null);")]
    [TestCase("var mapped = Wrap(1).Map(_ => Failure.Unknown.Code);")]
    [TestCase("var mapped = Wrap(1).Map<int, object>(_ => (object)Failure.Unknown.Error);")]
    [TestCase("var mapped = Wrap(1).Map<int, System.Func<Failure>>(_ => () => Failure.Unknown);")]
    [TestCase("var mapped = Wrap(1).Map<int, object>(_ => { Failure Local() => Failure.Unknown; return Local().Code; });")]
    [TestCase("var mapped = Wrap(1).Map<int, object>(_ => { System.Func<Failure> make = () => Failure.Unknown; return make().Code; });")]
    [TestCase("var mapped = Wrap(1).Map<int, object>(ToText); static object ToText(int value) => value.ToString();")]
    [TestCase("System.Func<int, object> toFailure = _ => Failure.Unknown; var mapped = Wrap(1).Map(toFailure);")]
    [TestCase("var bound = Wrap(1).Bind<int, object>(_ => Failure.Unknown);")]
    [TestCase("var matched = Wrap(1).Match<int, object>(value => value, failure => failure);")]
    [TestCase("Result ok = Result.Success; var bound = ok.Bind(() => Failure.Unknown);")]
    [TestCase("Result ok = Result.Success; var mapped = ok.Map<object>(() => 1);")]
    [TestCase("var bound = Wrap(1).Bind(_ => Failure.Unknown);")]
    [TestCase("var mapped = Wrap(1).MapAsync(value => Task.FromResult<object>(value));")]
    [TestCase("var bound = Wrap(1).BindAsync<int, object>(_ => Task.FromResult<Result<object>>(Failure.Unknown));")]
    [TestCase("var matched = WrapAsync(1).Match<int, object>(value => value, failure => failure);")]
    [TestCase("var combined = Wrap(1).SelectMany<int, int, object>(_ => Failure.Unknown, (x, y) => y);")]
    [TestCase("var combined = from x in Wrap(1) from y in Wrap(2) select x + y;")]
    public async Task Analyze_MapNotReturningFailureAsValue_ReportsNothing(string statement)
    {
        ImmutableArray<Diagnostic> diagnostics = await AnalyzeStatementAsync(statement);

        Assert.That(diagnostics, Is.Empty);
    }

    [Test]
    public async Task Analyze_MapReturningUserDefinedConversionOfFailure_ReportsNothing()
    {
        ImmutableArray<Diagnostic> diagnostics = await AnalyzeSourceAsync("""
            using Cspp.Results;

            public sealed record CodedFailure(string Code, string Error) : Failure(Code, Error)
            {
                public static implicit operator string(CodedFailure failure) => failure.Code;
            }

            public static class Sample
            {
                public static void Run() => new Result<int>(1).Map<int, string>(_ => new CodedFailure("Code", "Error"));
            }
            """);

        Assert.That(diagnostics, Is.Empty);
    }

    [Test]
    public async Task Analyze_OtherMethodNamedMap_ReportsNothing()
    {
        ImmutableArray<Diagnostic> diagnostics = await AnalyzeSourceAsync("""
            using System;
            using Cspp.Results;

            public static class Other
            {
                public static Result<TOut> Map<T, TOut>(this Result<T> result, Func<T, TOut> map) => default;
            }

            public static class Sample
            {
                public static void Run() => Other.Map<int, object>(default, _ => Failure.Unknown);
            }
            """);

        Assert.That(diagnostics, Is.Empty);
    }

    [Test]
    public async Task Analyze_CompilationWithoutResultsLibrary_ReportsNothing()
    {
        ImmutableArray<Diagnostic> diagnostics = await AnalyzeSourceAsync(
            """
            public sealed class Failure;

            public static class Sample
            {
                public static object Map(System.Func<int, object> map) => map(1);

                public static void Run() => Map(_ => new Failure());
            }
            """,
            referenceResultsLibrary: false);

        Assert.That(diagnostics, Is.Empty);
    }

    private static Task<ImmutableArray<Diagnostic>> AnalyzeStatementAsync(string statement)
    {
        return AnalyzerHarness.AnalyzeStatementAsync(new MapReturnsFailureAnalyzer(), statement);
    }

    private static Task<ImmutableArray<Diagnostic>> AnalyzeSourceAsync(string source, bool referenceResultsLibrary = true)
    {
        return AnalyzerHarness.AnalyzeSourceAsync(new MapReturnsFailureAnalyzer(), source, referenceResultsLibrary);
    }
}