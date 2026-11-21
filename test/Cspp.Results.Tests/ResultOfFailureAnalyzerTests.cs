using System.Collections.Immutable;

using Cspp.Results.Analyzers;

using JetBrains.Annotations;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Emit;

namespace Cspp.Results.Tests;

[TestFixture]
[TestSubject(typeof(ResultOfFailureAnalyzer))]
public class ResultOfFailureAnalyzerTests
{
    private const string ResultsAssemblyFileName = "Cspp.Results.dll";

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

        Assert.That(Ids(diagnostics), Is.EqualTo(new[] { ResultOfFailureAnalyzer.DiagnosticId }));
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
    public async Task Analyze_GenericCallInferringResultOfFailure_ReportsDiagnostic(string statement)
    {
        ImmutableArray<Diagnostic> diagnostics = await AnalyzeStatementAsync(statement);

        Assert.That(Ids(diagnostics), Is.EqualTo(new[] { ResultOfFailureAnalyzer.DiagnosticId }));
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
    public async Task Analyze_CallsChainedOnResultOfFailure_ReportsOnlyFirstCall(string statement)
    {
        ImmutableArray<Diagnostic> diagnostics = await AnalyzeStatementAsync(statement);

        Diagnostic diagnostic = diagnostics.Single();
        string flaggedText = diagnostic.Location.SourceTree!.GetText().ToString(diagnostic.Location.SourceSpan);

        Assert.That(flaggedText, Is.EqualTo("Wrap"));
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
        MetadataReference library = CompileLibrary("""
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

        Assert.That(Ids(diagnostics), Is.EqualTo(new[] { ResultOfFailureAnalyzer.DiagnosticId }));
    }

    [TestCase("var result = Wrap(1);")]
    [TestCase("var result = Wrap<string?>(null);")]
    [TestCase("var mapped = Wrap(1).Map(value => value.ToString());")]
    [TestCase("var bound = Wrap(1).Bind(value => Wrap(value.ToString()));")]
    [TestCase("var code = Wrap(1).Match(_ => string.Empty, failure => failure.Code);")]
    [TestCase("var failure = Wrap(1).Match(_ => (Failure?)null, failure => failure);")]
    [TestCase("var same = Identity(Wrap(1));")]
    [TestCase("var first = System.Linq.Enumerable.First(new[] { Wrap(1) });")]
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

        Assert.That(Ids(diagnostics), Is.EqualTo(new[] { ResultOfFailureAnalyzer.DiagnosticId }));
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

        Assert.That(Ids(diagnostics), Is.EqualTo(new[] { ResultOfFailureAnalyzer.DiagnosticId }));
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

    private static IEnumerable<string> Ids(ImmutableArray<Diagnostic> diagnostics)
    {
        return diagnostics.Select(diagnostic => diagnostic.Id);
    }

    private static Task<ImmutableArray<Diagnostic>> AnalyzeStatementAsync(string statement)
    {
        return AnalyzeSourceAsync($$"""
            using System.Collections.Generic;
            using Cspp.Results;

            public sealed record ValidationFailure(string Code, string Error) : Failure(Code, Error);

            public static class Sample
            {
                public static void Run()
                {
                    {{statement}}
                }

                private static T Identity<T>(T value) => value;

                private static Result<T> Wrap<T>(T value) => value;
            }
            """);
    }

    private static async Task<ImmutableArray<Diagnostic>> AnalyzeSourceAsync(
        string source,
        bool referenceResultsLibrary = true,
        MetadataReference? additionalReference = null)
    {
        CSharpCompilation compilation = CreateCompilation("AnalyzerTest", source, referenceResultsLibrary);
        if (additionalReference is not null)
        {
            compilation = compilation.AddReferences(additionalReference);
        }

        // A sample that does not compile could hide a missed diagnostic, so require valid source first.
        Assert.That(
            compilation.GetDiagnostics().Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error),
            Is.Empty,
            "The test source must compile.");

        return await compilation
            .WithAnalyzers([new ResultOfFailureAnalyzer()])
            .GetAnalyzerDiagnosticsAsync();
    }

    // Emits the source to an in-memory assembly, so its declarations reach the analyzed compilation as metadata.
    private static MetadataReference CompileLibrary(string source)
    {
        using MemoryStream image = new();
        EmitResult emitResult = CreateCompilation("Library", source, referenceResultsLibrary: true).Emit(image);

        Assert.That(emitResult.Success, Is.True, "The library source must compile.");

        return MetadataReference.CreateFromImage(image.ToArray());
    }

    private static CSharpCompilation CreateCompilation(string assemblyName, string source, bool referenceResultsLibrary)
    {
        IEnumerable<string> platformAssemblies =
            ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!).Split(Path.PathSeparator);
        IEnumerable<string> referencePaths = referenceResultsLibrary
            ? platformAssemblies.Append(typeof(Result).Assembly.Location)
            : platformAssemblies.Where(path => !Path.GetFileName(path).Equals(ResultsAssemblyFileName, StringComparison.OrdinalIgnoreCase));

        return CSharpCompilation.Create(
            assemblyName,
            [CSharpSyntaxTree.ParseText(source, new CSharpParseOptions(LanguageVersion.Preview))],
            referencePaths
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Select(path => MetadataReference.CreateFromFile(path)),
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, nullableContextOptions: NullableContextOptions.Enable));
    }
}