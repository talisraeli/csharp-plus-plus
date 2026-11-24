using System.Collections.Immutable;

using Cspp.Results.Analyzers;

using JetBrains.Annotations;

using Microsoft.CodeAnalysis;

namespace Cspp.Results.Tests;

[TestFixture]
[TestSubject(typeof(ResultOfFailureAnalyzer))]
public class ResultOfFailureAnalyzerTests
{
    [Test]
    public void SupportedDiagnostics_WhenRead_ContainsEnabledErrorRule()
    {
        DiagnosticDescriptor rule = new ResultOfFailureAnalyzer().SupportedDiagnostics.Single();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(rule.Id, Is.EqualTo(ResultOfFailureAnalyzer.DiagnosticId));
            Assert.That(rule.DefaultSeverity, Is.EqualTo(DiagnosticSeverity.Error));
            Assert.That(rule.IsEnabledByDefault, Is.True);
            Assert.That(rule.Category, Is.EqualTo("Usage"));
        }
    }

    [TestCase("Result<Failure> result = default;")]
    [TestCase("Result<Failure?> result = default;")]
    [TestCase("Result<ValidationFailure> result = default;")]
    [TestCase("Result<Cspp.Results.Failure> result = default;")]
    [TestCase("List<Result<Failure>> results = new();")]
    [TestCase("var result = new Result<Failure>();")]
    [TestCase("var result = default(Result<Failure>);")]
    [TestCase("bool isResult = (object)1 is Result<Failure>;")]
    public async Task Analyze_ResultOfFailureType_ReportsDiagnostic(string statement)
    {
        ImmutableArray<Diagnostic> diagnostics = await AnalyzeStatementAsync(statement);

        Assert.That(AnalyzerHarness.Ids(diagnostics), Is.EqualTo(new[] { ResultOfFailureAnalyzer.DiagnosticId }));
    }

    [Test]
    public async Task Analyze_ResultOfFailure_ReportsAtTypeArgumentWithTypeNameInMessage()
    {
        ImmutableArray<Diagnostic> diagnostics =
            await AnalyzeStatementAsync("Result<ValidationFailure> result = default;");

        Diagnostic diagnostic = diagnostics.Single();
        string flaggedText = diagnostic.Location.SourceTree!.GetText().ToString(diagnostic.Location.SourceSpan);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(flaggedText, Is.EqualTo("ValidationFailure"));
            Assert.That(
                diagnostic.GetMessage(),
                Is.EqualTo(
                    "'ValidationFailure' cannot be the value type of Result<T> because it is or derives from Cspp.Results.Failure"));
        }
    }

    [Test]
    public async Task Analyze_SeveralResultsOfFailure_ReportsEachOne()
    {
        ImmutableArray<Diagnostic> diagnostics = await AnalyzeStatementAsync(
            "Result<Failure> first = default; Result<ValidationFailure> second = default;");

        Assert.That(diagnostics, Has.Length.EqualTo(2));
    }

    [TestCase("var result = Wrap(Failure.Unknown);")]
    [TestCase("var result = Wrap(new ValidationFailure(\"Code\", \"Error\"));")]
    [TestCase("var result = Wrap<Failure>(Failure.Unknown);")]
    [TestCase("var mapped = Wrap(1).Map(_ => Failure.Unknown);")]
    [TestCase("var mapped = Wrap(1).Map<int, Failure>(_ => Failure.Unknown);")]
    [TestCase("var mapped = Wrap(1).Map(_ => new ValidationFailure(\"Code\", \"Error\"));")]
    [TestCase("Result<int>? source = Wrap(1); var mapped = source?.Map(_ => Failure.Unknown);")]
    [TestCase("var bound = Wrap(1).Bind<int, ValidationFailure>(_ => default);")]
    [TestCase("Result ok = Result.Success; var mapped = ok.Map(() => Failure.Unknown);")]
    [TestCase("Result ok = Result.Success; var mapped = ok.Map<Failure>(() => Failure.Unknown);")]
    [TestCase("Result ok = Result.Success; var bound = ok.Bind<Failure>(() => default);")]
    [TestCase("var bound = Wrap(1).Bind<int, Failure>(_ => default);")]
    [TestCase("var mapped = Wrap(1).MapAsync(_ => Task.FromResult(Failure.Unknown));")]
    [TestCase("var bound = Wrap(1).BindAsync<int, Failure>(_ => default!);")]
    [TestCase("var mapped = WrapAsync(1).Map(_ => Failure.Unknown);")]
    [TestCase("var mapped = WrapAsync(1).MapAsync(_ => Task.FromResult(Failure.Unknown));")]
    [TestCase("var result = WrapAsync(Failure.Unknown);")]
    public async Task Analyze_GenericCallInferringResultOfFailure_ReportsDiagnostic(string statement)
    {
        ImmutableArray<Diagnostic> diagnostics = await AnalyzeStatementAsync(statement);

        Assert.That(AnalyzerHarness.Ids(diagnostics), Is.EqualTo(new[] { ResultOfFailureAnalyzer.DiagnosticId }));
    }

    [Test]
    public async Task Analyze_GenericCallInferringResultOfFailure_ReportsAtMethodNameWithTypeNameInMessage()
    {
        ImmutableArray<Diagnostic> diagnostics =
            await AnalyzeStatementAsync("var mapped = Wrap(1).Map(_ => Failure.Unknown);");

        Diagnostic diagnostic = diagnostics.Single();
        string flaggedText = diagnostic.Location.SourceTree!.GetText().ToString(diagnostic.Location.SourceSpan);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(flaggedText, Is.EqualTo("Map"));
            Assert.That(
                diagnostic.GetMessage(),
                Is.EqualTo(
                    "'Cspp.Results.Failure' cannot be the value type of Result<T> because it is or derives from Cspp.Results.Failure"));
        }
    }

    [TestCase("var bound = Wrap(1).Bind(_ => Wrap(Failure.Unknown));")]
    [TestCase("var chained = Wrap(Failure.Unknown).Map(failure => failure).Bind(Wrap);")]
    [TestCase("var same = Identity(Wrap(Failure.Unknown));")]
    [TestCase("var first = First(new[] { Wrap(Failure.Unknown) });")]
    [TestCase("var mapped = Task.FromResult(Wrap(Failure.Unknown)).Map(failure => failure.Code);")]
    public async Task Analyze_CallsChainedOnResultOfFailure_ReportsOnlyFirstCall(string statement)
    {
        ImmutableArray<Diagnostic> diagnostics = await AnalyzeStatementAsync(statement);

        Diagnostic diagnostic = diagnostics.Single();
        string flaggedText = diagnostic.Location.SourceTree!.GetText().ToString(diagnostic.Location.SourceSpan);

        Assert.That(flaggedText, Is.EqualTo("Wrap"));
    }

    [Test]
    public async Task Analyze_ValueTaskOfResultOfFailure_ReportsDiagnostic()
    {
        ImmutableArray<Diagnostic> diagnostics = await AnalyzeSourceAsync(
            """
            using System.Threading.Tasks;
            using Cspp.Results;

            public static class Sample
            {
                public static ValueTask<Result<T>> WrapAsync<T>(T value) => new(value);

                public static void Run() => WrapAsync(Failure.Unknown);
            }
            """);

        Assert.That(diagnostics.Select(AnalyzerHarness.FlaggedText), Is.EqualTo(new[] { "WrapAsync" }));
    }

    [TestCase("var task = Task.FromResult<Result<Failure>>(default);")]
    [TestCase("var same = Identity<Result<Failure>>(default);")]
    public async Task Analyze_TypeArgumentContainingResultOfFailure_ReportsOnlyTypeArgument(string statement)
    {
        ImmutableArray<Diagnostic> diagnostics = await AnalyzeStatementAsync(statement);

        Assert.That(diagnostics.Select(AnalyzerHarness.FlaggedText), Is.EqualTo(new[] { "Failure" }));
    }

    [TestCase("var bound = Wrap(1).Bind<int, Failure>(_ => default);", "Bind")]
    [TestCase("var mapped = Wrap(1).Map<int, Failure>(_ => Failure.Unknown);", "Map")]
    public async Task Analyze_ExplicitTypeArgumentsOfResultOfFailure_ReportsAtMethodName(string statement, string expected)
    {
        ImmutableArray<Diagnostic> diagnostics = await AnalyzeStatementAsync(statement);

        Assert.That(diagnostics.Select(AnalyzerHarness.FlaggedText), Is.EqualTo(new[] { expected }));
    }

    [Test]
    public async Task Analyze_ExtensionBlockMemberInferringResultOfFailure_ReportsDiagnostic()
    {
        ImmutableArray<Diagnostic> diagnostics = await AnalyzeSourceAsync("""
            using Cspp.Results;

            public static class ToResultExtensions
            {
                extension<T>(T value)
                {
                    public Result<T> ToResult() => value;
                }
            }

            public static class Sample
            {
                public static void Run() => Failure.Unknown.ToResult();
            }
            """);

        Assert.That(diagnostics.Select(AnalyzerHarness.FlaggedText), Is.EqualTo(new[] { "ToResult" }));
    }

    [Test]
    public async Task Analyze_MethodOfGenericTypeReturningResultOfFailure_ReportsDiagnostic()
    {
        ImmutableArray<Diagnostic> diagnostics = await AnalyzeSourceAsync("""
            using Cspp.Results;

            public sealed class Box<T>(T value)
            {
                public Result<T> Get() => value;
            }

            public static class Sample
            {
                public static void Run() => new Box<Failure>(Failure.Unknown).Get();
            }
            """);

        Assert.That(diagnostics.Select(AnalyzerHarness.FlaggedText), Is.EqualTo(new[] { "Get" }));
    }

    [Test]
    public async Task Analyze_CallToNonGenericMethodDeclaringResultOfFailure_ReportsOnlyDeclaration()
    {
        ImmutableArray<Diagnostic> diagnostics = await AnalyzeSourceAsync("""
            using Cspp.Results;

            public static class Sample
            {
                public static Result<Failure> Make() => default;

                public static void Run() => Make();
            }
            """);

        Assert.That(diagnostics.Select(AnalyzerHarness.FlaggedText), Is.EqualTo(new[] { "Failure" }));
    }

    [Test]
    public async Task Analyze_GenericCallWithArgumentOfTypeParameterConstrainedToFailure_ReportsDiagnostic()
    {
        ImmutableArray<Diagnostic> diagnostics = await AnalyzeSourceAsync("""
            using Cspp.Results;

            public static class Sample
            {
                public static void Run<TFailure>(TFailure failure) where TFailure : Failure => Wrap(failure);

                private static Result<T> Wrap<T>(T value) => value;
            }
            """);

        Assert.That(diagnostics.Select(AnalyzerHarness.FlaggedText), Is.EqualTo(new[] { "Wrap" }));
    }

    [Test]
    public async Task Analyze_QueryClauseInferringResultOfFailure_ReportsAtClause()
    {
        ImmutableArray<Diagnostic> diagnostics = await AnalyzeSourceAsync("""
            using System;
            using Cspp.Results;

            public static class QueryExtensions
            {
                public static Result<TOut> Select<T, TOut>(this Result<T> result, Func<T, TOut> selector) => default;
            }

            public static class Sample
            {
                public static void Run()
                {
                    var selected = from value in new Result<int>(1) select Failure.Unknown;
                }
            }
            """);

        Assert.That(diagnostics.Select(AnalyzerHarness.FlaggedText), Is.EqualTo(new[] { "select Failure.Unknown" }));
    }

    [Test]
    public async Task Analyze_CallToMethodDeclaringResultOfFailure_ReportsOnlyDeclaration()
    {
        ImmutableArray<Diagnostic> diagnostics = await AnalyzeSourceAsync("""
            using Cspp.Results;

            public static class Sample
            {
                public static Result<TFailure> Wrap<TFailure>(TFailure failure) where TFailure : Failure => default;

                public static void Run() => Wrap(Failure.Unknown);
            }
            """);

        Assert.That(diagnostics, Has.Length.EqualTo(1));
    }

    [Test]
    public async Task Analyze_CallToMetadataMethodDeclaringResultOfFailure_ReportsCall()
    {
        MetadataReference library = AnalyzerHarness.CompileLibrary("""
            using Cspp.Results;

            public static class Library
            {
                public static Result<TFailure> Wrap<TFailure>(TFailure failure) where TFailure : Failure => default;
            }
            """);

        ImmutableArray<Diagnostic> diagnostics = await AnalyzeSourceAsync(
            """
            using Cspp.Results;

            public static class Sample
            {
                public static void Run() => Library.Wrap(Failure.Unknown);
            }
            """,
            additionalReference: library);

        Assert.That(AnalyzerHarness.Ids(diagnostics), Is.EqualTo(new[] { ResultOfFailureAnalyzer.DiagnosticId }));
    }

    [TestCase("var result = Wrap(1);")]
    [TestCase("var result = Wrap<string?>(null);")]
    [TestCase("var mapped = Wrap(1).Map(value => value.ToString());")]
    [TestCase("var bound = Wrap(1).Bind(value => Wrap(value.ToString()));")]
    [TestCase("var code = Wrap(1).Match(_ => string.Empty, failure => failure.Code);")]
    [TestCase("var failure = Wrap(1).Match(_ => (Failure?)null, failure => failure);")]
    [TestCase("var same = Identity(Wrap(1));")]
    [TestCase("Result ok = Result.Success; var mapped = ok.Map(() => 1);")]
    [TestCase("Result ok = Result.Success; var bound = ok.Bind(() => Failure.Unknown);")]
    [TestCase("Result ok = Result.Success; var failure = ok.Match(() => (Failure?)null, failure => failure);")]
    [TestCase("var bound = Wrap(1).Bind(_ => Failure.Unknown);")]
    [TestCase("var result = Wrap(Failure.Unknown.Code).ToResult();")]
    [TestCase("var first = System.Linq.Enumerable.First(new[] { Wrap(1) });")]
    [TestCase("var mapped = Wrap(1).MapAsync(value => Task.FromResult(value.ToString()));")]
    [TestCase("var bound = WrapAsync(1).BindAsync(value => WrapAsync(value.ToString()));")]
    [TestCase("var code = WrapAsync(1).Match(_ => string.Empty, failure => failure.Code);")]
    [TestCase("var task = Task.FromResult(1);")]
    [TestCase("var empty = System.Array.Empty<int>();")]
    [TestCase("var empty = System.Linq.Enumerable.Empty<int>();")]
    public async Task Analyze_GenericCallNotInferringResultOfFailure_ReportsNothing(string statement)
    {
        ImmutableArray<Diagnostic> diagnostics = await AnalyzeStatementAsync(statement);

        Assert.That(diagnostics, Is.Empty);
    }

    [Test]
    public async Task Analyze_TypeParameterConstrainedToFailure_ReportsDiagnostic()
    {
        ImmutableArray<Diagnostic> diagnostics = await AnalyzeSourceAsync("""
            using Cspp.Results;

            public static class Sample
            {
                public static Result<TFailure> Wrap<TFailure>(TFailure failure) where TFailure : Failure => default;
            }
            """);

        Assert.That(AnalyzerHarness.Ids(diagnostics), Is.EqualTo(new[] { ResultOfFailureAnalyzer.DiagnosticId }));
    }

    [Test]
    public async Task Analyze_TypeParameterConstrainedThroughAnotherTypeParameter_ReportsDiagnostic()
    {
        ImmutableArray<Diagnostic> diagnostics = await AnalyzeSourceAsync("""
            using Cspp.Results;

            public static class Sample
            {
                public static Result<TInner> Wrap<TOuter, TInner>() where TOuter : Failure where TInner : TOuter => default;
            }
            """);

        Assert.That(AnalyzerHarness.Ids(diagnostics), Is.EqualTo(new[] { ResultOfFailureAnalyzer.DiagnosticId }));
    }

    [TestCase("")]
    [TestCase("where T : class")]
    [TestCase("where T : System.IDisposable")]
    public async Task Analyze_TypeParameterNotConstrainedToFailure_ReportsNothing(string constraint)
    {
        ImmutableArray<Diagnostic> diagnostics = await AnalyzeSourceAsync($$"""
            using Cspp.Results;

            public static class Sample
            {
                public static Result<T> Wrap<T>() {{constraint}} => default;
            }
            """);

        Assert.That(diagnostics, Is.Empty);
    }

    [TestCase("Result<int> result = default;")]
    [TestCase("Result<string?> result = default;")]
    [TestCase("Result<object> result = default;")]
    [TestCase("Result<System.IDisposable> result = default;")]
    [TestCase("Result<Result<int>> result = default;")]
    [TestCase("Result result = default;")]
    public async Task Analyze_ResultOfNonFailureType_ReportsNothing(string statement)
    {
        ImmutableArray<Diagnostic> diagnostics = await AnalyzeStatementAsync(statement);

        Assert.That(diagnostics, Is.Empty);
    }

    [TestCase("List<Failure> failures = new();")]
    [TestCase("Dictionary<string, Failure> failures = new();")]
    [TestCase("Failure failure = Identity<Failure>(Failure.Unknown);")]
    public async Task Analyze_OtherGenericNameWithFailure_ReportsNothing(string statement)
    {
        ImmutableArray<Diagnostic> diagnostics = await AnalyzeStatementAsync(statement);

        Assert.That(diagnostics, Is.Empty);
    }

    [Test]
    public async Task Analyze_GeneratedCode_ReportsNothing()
    {
        ImmutableArray<Diagnostic> diagnostics = await AnalyzeSourceAsync("""
            // <auto-generated/>
            using Cspp.Results;

            public static class Sample
            {
                public static Result<Failure> Create() => default;
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

            public struct Result<T>;

            public static class Sample
            {
                public static Result<Failure> Create() => default;
            }
            """,
            referenceResultsLibrary: false);

        Assert.That(diagnostics, Is.Empty);
    }

    [Test]
    public async Task Analyze_CompilationWithResultButWithoutFailure_ReportsNothing()
    {
        ImmutableArray<Diagnostic> diagnostics = await AnalyzeSourceAsync(
            """
            namespace Cspp.Results
            {
                public struct Result<T>;
            }

            public static class Sample
            {
                public static Cspp.Results.Result<string> Create() => default;
            }
            """,
            referenceResultsLibrary: false);

        Assert.That(diagnostics, Is.Empty);
    }

    private static Task<ImmutableArray<Diagnostic>> AnalyzeStatementAsync(string statement)
    {
        return AnalyzerHarness.AnalyzeStatementAsync(new ResultOfFailureAnalyzer(), statement);
    }

    private static Task<ImmutableArray<Diagnostic>> AnalyzeSourceAsync(
        string source,
        bool referenceResultsLibrary = true,
        MetadataReference? additionalReference = null)
    {
        return AnalyzerHarness.AnalyzeSourceAsync(
            new ResultOfFailureAnalyzer(),
            source,
            referenceResultsLibrary,
            additionalReference);
    }
}