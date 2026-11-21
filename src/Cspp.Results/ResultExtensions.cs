namespace Cspp.Results;

public static class ResultExtensions
{
    extension(Result)
    {
        public static Success Success => Success.Instance;
    }

    extension<T>(Result<T> result)
    {
        /// <summary>
        /// Gets the success value, or <see langword="default" />(<typeparamref name="T" />) if the result is a failure.
        /// </summary>
        /// <returns>
        /// The success value, or <see langword="default" />(<typeparamref name="T" />) if the result is a failure.
        /// </returns>
        public T? GetValueOrFallback()
        {
            return result.GetValueOrFallback(default!);
        }

        /// <summary>
        /// Gets the success value, or <paramref name="fallback" /> if the result is a failure.
        /// </summary>
        /// <param name="fallback">The value to return if the result is a failure.</param>
        /// <returns>The success value, or <paramref name="fallback" /> if the result is a failure.</returns>
        public T GetValueOrFallback(T fallback)
        {
            return result.TryGetValue(out T? value) ? value : fallback;
        }

        /// <summary>
        /// Handles both cases of the result and returns a value from either one.
        /// </summary>
        /// <param name="onSuccess">Called with the success value if the result is a success.</param>
        /// <param name="onFailure">
        /// Called with the failure if the result is a failure. A <see langword="default" /> result passes
        /// <see cref="Failure.Uninitialized" />.
        /// </param>
        /// <typeparam name="TOut">The type returned by both handlers.</typeparam>
        /// <returns>The value returned by the handler that was called.</returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="onSuccess" /> or <paramref name="onFailure" /> is <see langword="null" />.
        /// </exception>
        public TOut Match<TOut>(Func<T, TOut> onSuccess, Func<Failure, TOut> onFailure)
        {
            ArgumentNullException.ThrowIfNull(onSuccess);
            ArgumentNullException.ThrowIfNull(onFailure);

            return result.TryGetValue(out T? value) ? onSuccess(value) : onFailure(result.Failure);
        }

        /// <summary>
        /// Transforms the success value and passes a failure through unchanged.
        /// </summary>
        /// <param name="map">Called with the success value if the result is a success.</param>
        /// <typeparam name="TOut">
        /// The type of the new success value. It must not be <see cref="Failure" /> or derive from it (<c>CSPP0001</c>);
        /// to fail from inside the step, use <c>Bind</c> instead.
        /// </typeparam>
        /// <returns>A success holding the value returned by <paramref name="map" />, or the original failure.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="map" /> is <see langword="null" />.</exception>
        public Result<TOut> Map<TOut>(Func<T, TOut> map)
        {
            ArgumentNullException.ThrowIfNull(map);

            return result.TryGetValue(out T? value) ? map(value) : result.Failure;
        }

        /// <summary>
        /// Chains a step that can fail. A failure is passed through unchanged without calling the step.
        /// </summary>
        /// <param name="bind">Called with the success value if the result is a success.</param>
        /// <typeparam name="TOut">The success value type of the result returned by <paramref name="bind" />.</typeparam>
        /// <returns>The result returned by <paramref name="bind" />, or the original failure.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="bind" /> is <see langword="null" />.</exception>
        public Result<TOut> Bind<TOut>(Func<T, Result<TOut>> bind)
        {
            ArgumentNullException.ThrowIfNull(bind);

            return result.TryGetValue(out T? value) ? bind(value) : result.Failure;
        }
    }
}