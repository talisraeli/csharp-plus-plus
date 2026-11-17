using System.Collections.Immutable;

using Cspp.Results.Analyzers;

using JetBrains.Annotations;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Cspp.Results.Tests;

[TestFixture]
[TestSubject(typeof(ResultOfFailureAnalyzer))]
public class ResultOfFailureAnalyzerTests
{
    [TestCase("Result<Failure> result = default;")]
    [TestCase("Result<Failure?> result = default;")]
    [TestCase("Result<ValidationFailure> result = default;")]
    [TestCase("System.Collections.Generic.List<Result<Failure>> results = new();")]
    public async Task Analyze_ResultOfFailureType_ReportsDiagnostic(string statement)
    {
        ImmutableArray<Diagnostic> diagnostics = await AnalyzeAsync(statement);

        Assert.That(diagnostics.Select(diagnostic => diagnostic.Id), Is.EqualTo(new[] { ResultOfFailureAnalyzer.DiagnosticId }));
    }

    [Test]
    public async Task Analyze_TypeParameterConstrainedToFailure_ReportsDiagnostic()
    {
        const string source = """
            using Cspp.Results;

            public static class Sample
            {
                public static Result<TFailure> Wrap<TFailure>(TFailure failure) where TFailure : Failure => default;
            }
            """;

        ImmutableArray<Diagnostic> diagnostics = await AnalyzeSourceAsync(source);

        Assert.That(diagnostics.Select(diagnostic => diagnostic.Id), Is.EqualTo(new[] { ResultOfFailureAnalyzer.DiagnosticId }));
    }

    [TestCase("Result<int> result = default;")]
    [TestCase("Result<string?> result = default;")]
    [TestCase("Result<object> result = default;")]
    [TestCase("Result result = default;")]
    public async Task Analyze_ResultOfNonFailureType_ReportsNothing(string statement)
    {
        ImmutableArray<Diagnostic> diagnostics = await AnalyzeAsync(statement);

        Assert.That(diagnostics, Is.Empty);
    }

    private static Task<ImmutableArray<Diagnostic>> AnalyzeAsync(string statement)
    {
        return AnalyzeSourceAsync($$"""
            using Cspp.Results;

            public sealed record ValidationFailure(string Code, string Error) : Failure(Code, Error);

            public static class Sample
            {
                public static void Run()
                {
                    {{statement}}
                }
            }
            """);
    }

    private static async Task<ImmutableArray<Diagnostic>> AnalyzeSourceAsync(string source)
    {
        var platformAssemblies = ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!).Split(Path.PathSeparator);
        IEnumerable<MetadataReference> references = platformAssemblies
            .Append(typeof(Result).Assembly.Location)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Select(path => MetadataReference.CreateFromFile(path));

        CSharpCompilation compilation = CSharpCompilation.Create(
            "AnalyzerTest",
            [CSharpSyntaxTree.ParseText(source, new CSharpParseOptions(LanguageVersion.Preview))],
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, nullableContextOptions: NullableContextOptions.Enable));

        return await compilation
            .WithAnalyzers([new ResultOfFailureAnalyzer()])
            .GetAnalyzerDiagnosticsAsync();
    }
}