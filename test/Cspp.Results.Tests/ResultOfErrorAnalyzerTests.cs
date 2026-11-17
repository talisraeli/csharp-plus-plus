using System.Collections.Immutable;

using Cspp.Results.Analyzers;

using JetBrains.Annotations;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Cspp.Results.Tests;

[TestFixture]
[TestSubject(typeof(ResultOfErrorAnalyzer))]
public class ResultOfErrorAnalyzerTests
{
    [TestCase("Result<Error> result = default;")]
    [TestCase("Result<Error?> result = default;")]
    [TestCase("Result<ValidationError> result = default;")]
    [TestCase("System.Collections.Generic.List<Result<Error>> results = new();")]
    public async Task Analyze_ResultOfErrorType_ReportsDiagnostic(string statement)
    {
        ImmutableArray<Diagnostic> diagnostics = await AnalyzeAsync(statement);

        Assert.That(diagnostics.Select(diagnostic => diagnostic.Id), Is.EqualTo(new[] { ResultOfErrorAnalyzer.DiagnosticId }));
    }

    [Test]
    public async Task Analyze_TypeParameterConstrainedToError_ReportsDiagnostic()
    {
        const string source = """
            using Cspp.Results;

            public static class Sample
            {
                public static Result<TError> Wrap<TError>(TError error) where TError : Error => default;
            }
            """;

        ImmutableArray<Diagnostic> diagnostics = await AnalyzeSourceAsync(source);

        Assert.That(diagnostics.Select(diagnostic => diagnostic.Id), Is.EqualTo(new[] { ResultOfErrorAnalyzer.DiagnosticId }));
    }

    [TestCase("Result<int> result = default;")]
    [TestCase("Result<string?> result = default;")]
    [TestCase("Result<object> result = default;")]
    [TestCase("Result result = default;")]
    public async Task Analyze_ResultOfNonErrorType_ReportsNothing(string statement)
    {
        ImmutableArray<Diagnostic> diagnostics = await AnalyzeAsync(statement);

        Assert.That(diagnostics, Is.Empty);
    }

    private static Task<ImmutableArray<Diagnostic>> AnalyzeAsync(string statement)
    {
        return AnalyzeSourceAsync($$"""
            using Cspp.Results;

            public sealed record ValidationError(string Code, string Message) : Error(Code, Message);

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
            .WithAnalyzers([new ResultOfErrorAnalyzer()])
            .GetAnalyzerDiagnosticsAsync();
    }
}