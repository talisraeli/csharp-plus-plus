namespace Cspp.Results;

/// <content>
/// <c>Select</c> and <c>SelectMany</c>, which let C# query syntax (<c>from</c> … <c>select</c>) chain results.
/// </content>
public static partial class ResultExtensions
{
    extension<T>(Result<T> result)
    {
        /// <summary>
        /// Transforms the success value and passes a failure through unchanged. The same as <c>Map</c>; query syntax
        /// calls it for a <c>select</c> clause.
        /// </summary>
        /// <param name="selector">
        /// Called with the success value if the result is a success. It must not return a <see cref="Failure" />
        /// (<c>CSPP0002</c>).
        /// </param>
        /// <typeparam name="TOut">
        /// The type of the new success value. It must not be <see cref="Failure" /> or derive from it (<c>CSPP0001</c>).
        /// </typeparam>
        /// <returns>A success holding the value returned by <paramref name="selector" />, or the original failure.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="selector" /> is <see langword="null" />.</exception>
        public Result<TOut> Select<TOut>(Func<T, TOut> selector)
        {
            ArgumentNullException.ThrowIfNull(selector);

            return result.IsSuccess ? selector(result.SuccessValue) : result.Failure;
        }

        /// <summary>
        /// Chains a step that can fail, then combines both success values. Query syntax calls it for a second
        /// <c>from</c> clause. A failure from either step is passed through unchanged.
        /// </summary>
        /// <param name="selector">Called with the success value if the result is a success; the step that can fail.</param>
        /// <param name="resultSelector">
        /// Called with both success values if both steps succeed. It must not return a <see cref="Failure" />
        /// (<c>CSPP0002</c>).
        /// </param>
        /// <typeparam name="TMiddle">The success value type of the result returned by <paramref name="selector" />.</typeparam>
        /// <typeparam name="TOut">
        /// The type of the combined success value. It must not be <see cref="Failure" /> or derive from it
        /// (<c>CSPP0001</c>).
        /// </typeparam>
        /// <returns>
        /// A success holding the value returned by <paramref name="resultSelector" />, or the first failure.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="selector" /> or <paramref name="resultSelector" /> is <see langword="null" />.
        /// </exception>
        public Result<TOut> SelectMany<TMiddle, TOut>(
            Func<T, Result<TMiddle>> selector,
            Func<T, TMiddle, TOut> resultSelector)
        {
            ArgumentNullException.ThrowIfNull(selector);
            ArgumentNullException.ThrowIfNull(resultSelector);

            if (result.IsFailure)
            {
                return result.Failure;
            }

            T value = result.SuccessValue;
            Result<TMiddle> middle = selector(value);
            return middle.IsSuccess ? resultSelector(value, middle.SuccessValue) : middle.Failure;
        }
    }
}