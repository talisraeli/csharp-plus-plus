namespace Cspp.Results;

/// <content>
/// <c>Result.Combine</c> and <c>Result.CombineAll</c>, which turn several results into one.
/// </content>
public static partial class ResultExtensions
{
    extension(Result)
    {
        /// <summary>
        /// Combines several results into one: a success if every result is a success, or the first failure.
        /// </summary>
        /// <param name="results">The results to combine. Reading stops at the first failure.</param>
        /// <returns>A success, or the first failure in <paramref name="results" />.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="results" /> is <see langword="null" />.</exception>
        public static Result Combine(params IEnumerable<Result> results)
        {
            ArgumentNullException.ThrowIfNull(results);

            foreach (Result result in results)
            {
                if (result.IsFailure)
                {
                    return result;
                }
            }

            return Result.Success;
        }

        /// <summary>
        /// Combines several results into one: a success holding every value, or the first failure.
        /// </summary>
        /// <param name="results">The results to combine. Reading stops at the first failure.</param>
        /// <typeparam name="T">The type of the success values.</typeparam>
        /// <returns>A success holding the values in order, or the first failure in <paramref name="results" />.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="results" /> is <see langword="null" />.</exception>
        public static Result<IReadOnlyList<T>> Combine<T>(params IEnumerable<Result<T>> results)
        {
            ArgumentNullException.ThrowIfNull(results);

            List<T> values = [];
            foreach (Result<T> result in results)
            {
                if (result.IsFailure)
                {
                    return result.Failure;
                }

                values.Add(result.SuccessValue);
            }

            return values.AsReadOnly();
        }

        /// <summary>
        /// Combines several results into one, reporting every failure: a success if every result is a success, the
        /// failure if exactly one result fails, or an <see cref="AggregateFailure" /> holding every failure in order.
        /// </summary>
        /// <param name="results">The results to combine. Every result is read.</param>
        /// <returns>A success, the single failure, or an <see cref="AggregateFailure" />.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="results" /> is <see langword="null" />.</exception>
        public static Result CombineAll(params IEnumerable<Result> results)
        {
            ArgumentNullException.ThrowIfNull(results);

            List<Failure> failures = [];
            foreach (Result result in results)
            {
                if (result.IsFailure)
                {
                    failures.Add(result.Failure);
                }
            }

            return failures.Count == 0 ? Result.Success : OneOrAggregate(failures);
        }

        /// <summary>
        /// Combines several results into one, reporting every failure: a success holding every value if every result is
        /// a success, the failure if exactly one result fails, or an <see cref="AggregateFailure" /> holding every
        /// failure in order.
        /// </summary>
        /// <param name="results">The results to combine. Every result is read.</param>
        /// <typeparam name="T">The type of the success values.</typeparam>
        /// <returns>A success holding the values in order, the single failure, or an <see cref="AggregateFailure" />.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="results" /> is <see langword="null" />.</exception>
        public static Result<IReadOnlyList<T>> CombineAll<T>(params IEnumerable<Result<T>> results)
        {
            ArgumentNullException.ThrowIfNull(results);

            List<T> values = [];
            List<Failure> failures = [];
            foreach (Result<T> result in results)
            {
                if (result.IsFailure)
                {
                    failures.Add(result.Failure);
                }
                else
                {
                    values.Add(result.SuccessValue);
                }
            }

            return failures.Count == 0 ? values.AsReadOnly() : OneOrAggregate(failures);
        }
    }

    // The failure for one or more collected failures: the failure itself, or an AggregateFailure of all of them.
    private static Failure OneOrAggregate(List<Failure> failures)
    {
        return failures.Count == 1 ? failures[0] : new AggregateFailure(failures);
    }
}