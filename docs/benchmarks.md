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
BenchmarkDotNet 0.16.0-preview.2. Differences under about 1 ns are within noise, so run the benchmarks on your own
hardware before relying on small gaps.

**Pipeline** (parse, `Bind` validate, `Map`, read the outcome):

| Library                    | `42` (success)   | `abc` (parse fails) | `-5` (validation fails) |
|----------------------------|------------------|---------------------|-------------------------|
| Cspp.Results               | 7.2 ns, 0 B      | 7.2 ns, 32 B        | 9.1 ns, 32 B            |
| FluentResults              | 75.1 ns, 784 B   | 129 ns, 1048 B      | 155 ns, 1216 B          |
| ErrorOr                    | 11.2 ns, 0 B     | 10.2 ns, 88 B       | 16.2 ns, 88 B           |
| CSharpFunctionalExtensions | 11.2 ns, 0 B     | 7.3 ns, 32 B        | 12.4 ns, 32 B           |
| OneOf                      | 11.2 ns, 0 B     | 7.7 ns, 32 B        | 12.4 ns, 32 B           |
| Ardalis.Result             | 13.6 ns, 128 B   | 23.8 ns, 264 B      | 23.7 ns, 264 B          |
| Exceptions                 | 5.3 ns, 0 B      | 841 ns, 200 B       | 843 ns, 200 B           |

**Create** (a success, or a failure including its error object):

| Library                    | Success         | Failure         |
|----------------------------|-----------------|-----------------|
| Cspp.Results               | 1.07 ns, 0 B    | 4.42 ns, 32 B   |
| FluentResults              | 9.09 ns, 112 B  | 30.1 ns, 408 B  |
| ErrorOr                    | 0.89 ns, 0 B    | 6.67 ns, 88 B   |
| CSharpFunctionalExtensions | 1.07 ns, 0 B    | 3.20 ns, 32 B   |
| OneOf                      | 1.07 ns, 0 B    | 3.20 ns, 32 B   |
| Ardalis.Result             | 4.48 ns, 64 B   | 9.32 ns, 136 B  |

**Match** (read an existing result into a value):

| Library                    | Success         | Failure         |
|----------------------------|-----------------|-----------------|
| Cspp.Results               | 1.34 ns, 0 B    | 1.38 ns, 0 B    |
| FluentResults              | 8.17 ns, 96 B   | 9.17 ns, 88 B   |
| ErrorOr                    | 1.21 ns, 0 B    | 1.13 ns, 0 B    |
| CSharpFunctionalExtensions | 1.59 ns, 0 B    | 1.50 ns, 0 B    |
| OneOf                      | 1.18 ns, 0 B    | 1.34 ns, 0 B    |
| Ardalis.Result             | 0.85 ns, 0 B    | 0.85 ns, 0 B    |

**Async pipeline** (the pipeline above chained on a `Task<Result<T>>`, Cspp.Results only). `AsyncCompleted` parses
into a task that has already completed, so it measures only the task members; `Async` parses into a task that awaits
`Task.Yield()` first, so every later step runs as a continuation:

| Chain          | `42` (success)   | `abc` (parse fails) | `-5` (validation fails) |
|----------------|------------------|---------------------|-------------------------|
| Sync           | 7.7 ns, 0 B      | 7.8 ns, 32 B        | 9.5 ns, 32 B            |
| AsyncCompleted | 34.2 ns, 336 B   | 32.5 ns, 296 B      | 33.2 ns, 296 B          |
| Async          | 487 ns, 513 B    | 491 ns, 541 B       | 511 ns, 546 B           |

In short:

- In the pipeline, Cspp.Results was the fastest result library on all three paths (CSharpFunctionalExtensions is within
  noise when parsing fails), and it allocates only the `Failure` on a failure path.
- Creating a failure takes about 4.4 ns, against about 3.2 ns for CSharpFunctionalExtensions and OneOf with the same
  32 B; the gap is likely the `Failure` constructor's validation of its code and error.
- Creating a success takes about 1 ns in every library except FluentResults and Ardalis.Result, which allocate.
  Matching takes under 2 ns everywhere except FluentResults, which allocates on every read.
- Exceptions are cheapest when nothing fails, because there is no wrapper, but about 90 to 120 times slower than
  Cspp.Results when something does.
- Chaining on a task adds about 24 to 27 ns and about 300 B, one task per step, to a three-step chain. When a step
  really waits, resuming after the wait costs far more than the task members.
