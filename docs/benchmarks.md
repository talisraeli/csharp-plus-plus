# Benchmarks

`benchmarks/Cspp.Benchmarks` uses [BenchmarkDotNet](https://benchmarkdotnet.org) to compare `Cspp.Results`
with other result libraries: FluentResults, ErrorOr, CSharpFunctionalExtensions, OneOf and Ardalis.Result. Each library
writes the same scenarios in its own idiomatic style, in `Libraries/`, and every failure carries a code and a message:

| Benchmark            | What it measures                                                                         |
|----------------------|------------------------------------------------------------------------------------------|
| `CreateBenchmarks`   | Creating a success or a failure, including the failure's error object.                   |
| `MatchBenchmarks`    | Reading an existing success or failure into a value.                                     |
| `PipelineBenchmarks` | Parse, `Bind` a validation step, `Map` and read the outcome, ending in success, a parse failure or a validation failure. Also runs the same steps with exceptions as a reference point. |
| `AsyncPipelineBenchmarks` | The Cspp.Results pipeline with an asynchronous parse step, chained on the `Task<Result<T>>`, against the synchronous pipeline. Cspp.Results only. |

`Cspp` is the baseline, so the `Ratio` column shows each library relative to it. `MatchBenchmarks` and
`PipelineBenchmarks` fail the run if the libraries don't all return the same value.

## Running

```sh
dotnet run -c Release --project benchmarks/Cspp.Benchmarks                         # pick interactively
dotnet run -c Release --project benchmarks/Cspp.Benchmarks -- --filter '*'         # run everything
dotnet run -c Release --project benchmarks/Cspp.Benchmarks -- --filter '*Pipeline*'
```

Results are written to `BenchmarkDotNet.Artifacts/`. BenchmarkDotNet 0.15 doesn't recognize the `net11.0` runtime, so
the project uses the 0.16 preview until a stable release supports it.

## Results

Mean time and allocated memory per operation, from one run on an AMD Ryzen AI 7 350 laptop, Windows 11, .NET 11.0.0,
BenchmarkDotNet 0.16.0-preview.2. Differences under about 1 ns are within noise, and BenchmarkDotNet reported bimodal
distributions for a few cases, so run the benchmarks on your own hardware before relying on small gaps.

**Pipeline** (parse, `Bind` validate, `Map`, read the outcome):

| Library                    | `42` (success)   | `abc` (parse fails) | `-5` (validation fails) |
|----------------------------|------------------|---------------------|-------------------------|
| Cspp.Results               | 10.6 ns, 0 B     | 9.7 ns, 32 B        | 12.0 ns, 32 B           |
| FluentResults              | 111 ns, 784 B    | 177 ns, 1048 B      | 223 ns, 1216 B          |
| ErrorOr                    | 15.3 ns, 0 B     | 14.9 ns, 88 B       | 23.2 ns, 88 B           |
| CSharpFunctionalExtensions | 16.1 ns, 0 B     | 10.7 ns, 32 B       | 16.5 ns, 32 B           |
| OneOf                      | 16.1 ns, 0 B     | 11.5 ns, 32 B       | 18.7 ns, 32 B           |
| Ardalis.Result             | 20.2 ns, 128 B   | 30.8 ns, 264 B      | 34.7 ns, 264 B          |
| Exceptions                 | 7.6 ns, 0 B      | 854 ns, 200 B       | 1234 ns, 200 B          |

**Create** (a success, or a failure including its error object):

| Library                    | Success         | Failure         |
|----------------------------|-----------------|-----------------|
| Cspp.Results               | 1.39 ns, 0 B    | 5.08 ns, 32 B   |
| FluentResults              | 9.35 ns, 112 B  | 30.5 ns, 408 B  |
| ErrorOr                    | 0.92 ns, 0 B    | 6.99 ns, 88 B   |
| CSharpFunctionalExtensions | 1.10 ns, 0 B    | 3.33 ns, 32 B   |
| OneOf                      | 1.08 ns, 0 B    | 3.34 ns, 32 B   |
| Ardalis.Result             | 4.66 ns, 64 B   | 12.6 ns, 136 B  |

**Match** (read an existing result into a value):

| Library                    | Success         | Failure         |
|----------------------------|-----------------|-----------------|
| Cspp.Results               | 1.82 ns, 0 B    | 1.38 ns, 0 B    |
| FluentResults              | 11.1 ns, 96 B   | 9.62 ns, 88 B   |
| ErrorOr                    | 1.65 ns, 0 B    | 1.16 ns, 0 B    |
| CSharpFunctionalExtensions | 1.63 ns, 0 B    | 2.13 ns, 0 B    |
| OneOf                      | 1.18 ns, 0 B    | 1.90 ns, 0 B    |
| Ardalis.Result             | 0.86 ns, 0 B    | 0.87 ns, 0 B    |

**Async pipeline** (the pipeline above chained on a `Task<Result<T>>`, Cspp.Results only, from a separate run on the
same machine). `AsyncCompleted` parses into a task that has already completed, so it measures only the task members;
`Async` parses into a task that awaits `Task.Yield()` first, so every later step runs as a continuation:

| Chain          | `42` (success)   | `abc` (parse fails) | `-5` (validation fails) |
|----------------|------------------|---------------------|-------------------------|
| Sync           | 12.8 ns, 0 B     | 13.9 ns, 32 B       | 11.0 ns, 32 B           |
| AsyncCompleted | 51.5 ns, 336 B   | 51.1 ns, 296 B      | 49.5 ns, 296 B          |
| Async          | 662 ns, 517 B    | 734 ns, 549 B       | 660 ns, 548 B           |

In short:

- In the pipeline, Cspp.Results was the fastest result library on all three paths (CSharpFunctionalExtensions is within
  noise when parsing fails), and it allocates only the `Failure` on a failure path.
- Creating a failure takes about 5 ns, against about 3.3 ns for CSharpFunctionalExtensions and OneOf with the same
  32 B; the gap is likely the `Failure` constructor's validation of its code and error.
- Creating a success takes about 1 ns in every library except FluentResults and Ardalis.Result, which allocate.
  Matching takes about 2 ns or less everywhere except FluentResults, which allocates on every read.
- Exceptions are cheapest when nothing fails, because there is no wrapper, but about 90 to 105 times slower than
  Cspp.Results when something does.
- Chaining on a task adds about 36 to 40 ns and about 300 B, one task per step, to a three-step chain. When a step
  really waits, resuming after the wait costs far more than the task members.