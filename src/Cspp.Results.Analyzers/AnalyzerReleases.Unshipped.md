; Unshipped analyzer release
; https://github.com/dotnet/roslyn-analyzers/blob/main/src/Microsoft.CodeAnalysis.Analyzers/ReleaseTrackingAnalyzers.Help.md

### New Rules

Rule ID  | Category | Severity | Notes
---------|----------|----------|------------------------------------------------
CSPP0001 | Usage    | Error    | Result<T> value type must not be or derive from Failure
CSPP0002 | Usage    | Error    | Map must not return a Failure
CSPP0003 | Usage    | Error    | A Failure must not become a success value
