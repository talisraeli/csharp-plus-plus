namespace Cspp.Results;

/// <summary>
/// Members for consuming a <see cref="Result" /> or a <see cref="Result{T}" />: matching, transforming and chaining.
/// </summary>
public static partial class ResultExtensions
{
    extension(Result)
    {
        /// <summary>
        /// Gets the success marker. Return it where a <see cref="Result" /> is expected to report success.
        /// </summary>
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
        public T? GetValueOrDefault()
        {
            return result.GetValueOrDefault(default!);
        }

        /// <summary>
        /// Gets the success value, or <paramref name="defaultValue" /> if the result is a failure.
        /// </summary>
        /// <param name="defaultValue">The value to return if the result is a failure.</param>
        /// <returns>The success value, or <paramref name="defaultValue" /> if the result is a failure.</returns>
        public T GetValueOrDefault(T defaultValue)
        {
            return result.IsSuccess ? result.SuccessValue : defaultValue;
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

            return result.IsSuccess ? onSuccess(result.SuccessValue) : onFailure(result.Failure);
        }

        /// <summary>
        /// Transforms the success value and passes a failure through unchanged.
        /// </summary>
        /// <param name="map">
        /// Called with the success value if the result is a success. It must not return a <see cref="Failure" />
        /// (<c>CSPP0002</c>), because <c>Map</c> would store it as a success value.
        /// </param>
        /// <typeparam name="TOut">
        /// The type of the new success value. It must not be <see cref="Failure" /> or derive from it (<c>CSPP0001</c>);
        /// to fail from inside the step, use <c>Bind</c> instead.
        /// </typeparam>
        /// <returns>A success holding the value returned by <paramref name="map" />, or the original failure.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="map" /> is <see langword="null" />.</exception>
        public Result<TOut> Map<TOut>(Func<T, TOut> map)
        {
            ArgumentNullException.ThrowIfNull(map);

            return result.IsSuccess ? map(result.SuccessValue) : result.Failure;
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

            return result.IsSuccess ? bind(result.SuccessValue) : result.Failure;
        }

        /// <summary>
        /// Chains a step that can fail and has no value of its own. A failure is passed through unchanged without
        /// calling the step.
        /// </summary>
        /// <param name="bind">Called with the success value if the result is a success.</param>
        /// <returns>The result returned by <paramref name="bind" />, or the original failure.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="bind" /> is <see langword="null" />.</exception>
        public Result Bind(Func<T, Result> bind)
        {
            ArgumentNullException.ThrowIfNull(bind);

            return result.IsSuccess ? bind(result.SuccessValue) : result.Failure;
        }

        /// <summary>
        /// Drops the success value, keeping only whether the result succeeded.
        /// </summary>
        /// <returns>A success if the result is a success, or the original failure.</returns>
        public Result ToResult()
        {
            if (result.IsSuccess)
            {
                return Result.Success;
            }

            return result.Failure;
        }

        /// <summary>
        /// Runs a side effect, such as logging, with the success value, and returns the result unchanged.
        /// </summary>
        /// <param name="onSuccess">Called with the success value if the result is a success.</param>
        /// <returns>The same result.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="onSuccess" /> is <see langword="null" />.</exception>
        public Result<T> Tap(Action<T> onSuccess)
        {
            ArgumentNullException.ThrowIfNull(onSuccess);

            if (result.IsSuccess)
            {
                onSuccess(result.SuccessValue);
            }

            return result;
        }

        /// <summary>
        /// Runs a side effect, such as logging, with the failure, and returns the result unchanged.
        /// </summary>
        /// <param name="onFailure">
        /// Called with the failure if the result is a failure. A <see langword="default" /> result passes
        /// <see cref="Failure.Uninitialized" />.
        /// </param>
        /// <returns>The same result.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="onFailure" /> is <see langword="null" />.</exception>
        public Result<T> TapFailure(Action<Failure> onFailure)
        {
            ArgumentNullException.ThrowIfNull(onFailure);

            if (result.IsFailure)
            {
                onFailure(result.Failure);
            }

            return result;
        }

        /// <summary>
        /// Replaces the failure, for example to add context or translate its code, and passes a success through
        /// unchanged.
        /// </summary>
        /// <param name="map">
        /// Called with the failure if the result is a failure. A <see langword="default" /> result passes
        /// <see cref="Failure.Uninitialized" />. It must not return <see langword="null" />.
        /// </param>
        /// <returns>A failure holding the failure returned by <paramref name="map" />, or the original success.</returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="map" /> is <see langword="null" />, or it returned <see langword="null" />.
        /// </exception>
        public Result<T> MapFailure(Func<Failure, Failure> map)
        {
            ArgumentNullException.ThrowIfNull(map);

            return result.IsSuccess ? result : ApplyFailureMap(result.Failure, map);
        }
    }

    extension(Result result)
    {
        /// <summary>
        /// Handles both cases of the result and returns a value from either one.
        /// </summary>
        /// <param name="onSuccess">Called if the result is a success.</param>
        /// <param name="onFailure">
        /// Called with the failure if the result is a failure. A <see langword="default" /> result passes
        /// <see cref="Failure.Uninitialized" />.
        /// </param>
        /// <typeparam name="TOut">The type returned by both handlers.</typeparam>
        /// <returns>The value returned by the handler that was called.</returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="onSuccess" /> or <paramref name="onFailure" /> is <see langword="null" />.
        /// </exception>
        public TOut Match<TOut>(Func<TOut> onSuccess, Func<Failure, TOut> onFailure)
        {
            ArgumentNullException.ThrowIfNull(onSuccess);
            ArgumentNullException.ThrowIfNull(onFailure);

            return result.IsSuccess ? onSuccess() : onFailure(result.Failure);
        }

        /// <summary>
        /// Produces a success value if the result is a success, and passes a failure through unchanged.
        /// </summary>
        /// <param name="map">
        /// Called if the result is a success. It must not return a <see cref="Failure" /> (<c>CSPP0002</c>), because
        /// <c>Map</c> would store it as a success value.
        /// </param>
        /// <typeparam name="TOut">
        /// The type of the success value. It must not be <see cref="Failure" /> or derive from it (<c>CSPP0001</c>); to
        /// fail from inside the step, use <c>Bind</c> instead.
        /// </typeparam>
        /// <returns>A success holding the value returned by <paramref name="map" />, or the original failure.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="map" /> is <see langword="null" />.</exception>
        public Result<TOut> Map<TOut>(Func<TOut> map)
        {
            ArgumentNullException.ThrowIfNull(map);

            return result.IsSuccess ? map() : result.Failure;
        }

        /// <summary>
        /// Chains a step that can fail and has no value of its own. A failure is passed through unchanged without
        /// calling the step.
        /// </summary>
        /// <param name="bind">Called if the result is a success.</param>
        /// <returns>The result returned by <paramref name="bind" />, or the original failure.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="bind" /> is <see langword="null" />.</exception>
        public Result Bind(Func<Result> bind)
        {
            ArgumentNullException.ThrowIfNull(bind);

            return result.IsSuccess ? bind() : result;
        }

        /// <summary>
        /// Chains a step that can fail and produces a value. A failure is passed through unchanged without calling the
        /// step.
        /// </summary>
        /// <param name="bind">Called if the result is a success.</param>
        /// <typeparam name="TOut">The success value type of the result returned by <paramref name="bind" />.</typeparam>
        /// <returns>The result returned by <paramref name="bind" />, or the original failure.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="bind" /> is <see langword="null" />.</exception>
        public Result<TOut> Bind<TOut>(Func<Result<TOut>> bind)
        {
            ArgumentNullException.ThrowIfNull(bind);

            return result.IsSuccess ? bind() : result.Failure;
        }

        /// <summary>
        /// Runs a side effect, such as logging, if the result is a success, and returns the result unchanged.
        /// </summary>
        /// <param name="onSuccess">Called if the result is a success.</param>
        /// <returns>The same result.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="onSuccess" /> is <see langword="null" />.</exception>
        public Result Tap(Action onSuccess)
        {
            ArgumentNullException.ThrowIfNull(onSuccess);

            if (result.IsSuccess)
            {
                onSuccess();
            }

            return result;
        }

        /// <summary>
        /// Runs a side effect, such as logging, with the failure, and returns the result unchanged.
        /// </summary>
        /// <param name="onFailure">
        /// Called with the failure if the result is a failure. A <see langword="default" /> result passes
        /// <see cref="Failure.Uninitialized" />.
        /// </param>
        /// <returns>The same result.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="onFailure" /> is <see langword="null" />.</exception>
        public Result TapFailure(Action<Failure> onFailure)
        {
            ArgumentNullException.ThrowIfNull(onFailure);

            if (result.IsFailure)
            {
                onFailure(result.Failure);
            }

            return result;
        }

        /// <summary>
        /// Replaces the failure, for example to add context or translate its code, and passes a success through
        /// unchanged.
        /// </summary>
        /// <param name="map">
        /// Called with the failure if the result is a failure. A <see langword="default" /> result passes
        /// <see cref="Failure.Uninitialized" />. It must not return <see langword="null" />.
        /// </param>
        /// <returns>A failure holding the failure returned by <paramref name="map" />, or the original success.</returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="map" /> is <see langword="null" />, or it returned <see langword="null" />.
        /// </exception>
        public Result MapFailure(Func<Failure, Failure> map)
        {
            ArgumentNullException.ThrowIfNull(map);

            return result.IsSuccess ? result : ApplyFailureMap(result.Failure, map);
        }
    }

    // The failure map returns, checked here so a null return names the delegate instead of the Result constructor.
    private static Failure ApplyFailureMap(Failure failure, Func<Failure, Failure> map)
    {
        return map(failure)
            ?? throw new ArgumentNullException(nameof(map), "The map delegate returned null instead of a Failure.");
    }
}