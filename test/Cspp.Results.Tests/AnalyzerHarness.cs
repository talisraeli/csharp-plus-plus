using System.Collections.Immutable;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Emit;

namespace Cspp.Results.Tests;

/// <summary>
/// Compiles C# samples against Cspp.Results and runs an analyzer on them.
/// </summary>
internal static class AnalyzerHarness
{
    private const string ResultsAssemblyFileName = "Cspp.Results.dll";

    public static IEnumerable<string> Ids(ImmutableArray<Diagnostic> diagnostics)
    {
        return diagnostics.Select(diagnostic => diagnostic.Id);
    }

    public static string FlaggedText(Diagnostic diagnostic)
    {
        return diagnostic.Location.SourceTree!.GetText().ToString(diagnostic.Location.SourceSpan);
    }

    // Runs the analyzer on a statement placed in a method body, next to a ValidationFailure record and the helpers
    // Identity, Wrap and GetFailure.
    public static Task<ImmutableArray<Diagnostic>> AnalyzeStatementAsync(DiagnosticAnalyzer analyzer, string statement)
    {
        return AnalyzeSourceAsync(analyzer, $$"""
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

                private static Result<T> First<T>(Result<T>[] results) => results[0];

                private static Failure GetFailure() => Failure.Unknown;
            }
            """);
    }

    public static async Task<ImmutableArray<Diagnostic>> AnalyzeSourceAsync(
        DiagnosticAnalyzer analyzer,
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
            .WithAnalyzers([analyzer])
            .GetAnalyzerDiagnosticsAsync();
    }

    // Emits the source to an in-memory assembly, so its declarations reach an analyzed compilation as metadata.
    public static MetadataReference CompileLibrary(string source)
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