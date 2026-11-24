namespace Cspp.Results;

/// <content>
/// Members for steps that run asynchronously, and for chaining on a <see cref="Task{TResult}" /> of a
/// <see cref="Result{T}" /> without awaiting each step. To cancel a step, close over a
/// <see cref="CancellationToken" /> in its delegate.
/// </content>
public static partial class ResultExtensions
{
    extension<T>(Result<T> result)
    {
        /// <summary>
        /// Transforms the success value with an asynchronous step and passes a failure through unchanged.
        /// </summary>
        /// <param name="map">
        /// Called with the success value if the result is a success. It must not return a <see cref="Failure" />
        /// (<c>CSPP0002</c>), because <c>MapAsync</c> would store it as a success value.
        /// </param>
        /// <typeparam name="TOut">
        /// The type of the new success value. It must not be <see cref="Failure" /> or derive from it (<c>CSPP0001</c>);
        /// to fail from inside the step, use <c>BindAsync</c> instead.
        /// </typeparam>
        /// <returns>A success holding the value <paramref name="map" /> completes with, or the original failure.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="map" /> is <see langword="null" />.</exception>
        public Task<Result<TOut>> MapAsync<TOut>(Func<T, Task<TOut>> map)
        {
            ArgumentNullException.ThrowIfNull(map);

            return result.IsSuccess
                ? ToResultAsync(map(result.SuccessValue))
                : Task.FromResult<Result<TOut>>(result.Failure);
        }

        /// <summary>
        /// Chains an asynchronous step that can fail. A failure is passed through unchanged without calling the step.
        /// </summary>
        /// <param name="bind">Called with the success value if the result is a success.</param>
        /// <typeparam name="TOut">The success value type of the result returned by <paramref name="bind" />.</typeparam>
        /// <returns>The result <paramref name="bind" /> completes with, or the original failure.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="bind" /> is <see langword="null" />.</exception>
        public Task<Result<TOut>> BindAsync<TOut>(Func<T, Task<Result<TOut>>> bind)
        {
            ArgumentNullException.ThrowIfNull(bind);

            return result.IsSuccess ? bind(result.SuccessValue) : Task.FromResult<Result<TOut>>(result.Failure);
        }

        /// <summary>
        /// Handles both cases of the result with asynchronous handlers and returns a value from either one.
        /// </summary>
        /// <param name="onSuccess">Called with the success value if the result is a success.</param>
        /// <param name="onFailure">
        /// Called with the failure if the result is a failure. A <see langword="default" /> result passes
        /// <see cref="Failure.Uninitialized" />.
        /// </param>
        /// <typeparam name="TOut">The type both handlers complete with.</typeparam>
        /// <returns>The task returned by the handler that was called.</returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="onSuccess" /> or <paramref name="onFailure" /> is <see langword="null" />.
        /// </exception>
        public Task<TOut> MatchAsync<TOut>(Func<T, Task<TOut>> onSuccess, Func<Failure, Task<TOut>> onFailure)
        {
            ArgumentNullException.ThrowIfNull(onSuccess);
            ArgumentNullException.ThrowIfNull(onFailure);

            return result.IsSuccess ? onSuccess(result.SuccessValue) : onFailure(result.Failure);
        }
    }

    extension<T>(Task<Result<T>> task)
    {
        /// <summary>
        /// Awaits the result, then transforms the success value and passes a failure through unchanged.
        /// </summary>
        /// <param name="map">
        /// Called with the success value if the result is a success. It must not return a <see cref="Failure" />
        /// (<c>CSPP0002</c>).
        /// </param>
        /// <typeparam name="TOut">The type of the new success value. It must not be a <see cref="Failure" /> (<c>CSPP0001</c>).</typeparam>
        /// <returns>A success holding the value returned by <paramref name="map" />, or the original failure.</returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="task" /> or <paramref name="map" /> is <see langword="null" />.
        /// </exception>
        public Task<Result<TOut>> Map<TOut>(Func<T, TOut> map)
        {
            ArgumentNullException.ThrowIfNull(task);
            ArgumentNullException.ThrowIfNull(map);

            return MapAwaitedAsync(task, map);
        }

        /// <summary>
        /// Awaits the result, then chains a step that can fail. A failure is passed through unchanged without calling
        /// the step.
        /// </summary>
        /// <param name="bind">Called with the success value if the result is a success.</param>
        /// <typeparam name="TOut">The success value type of the result returned by <paramref name="bind" />.</typeparam>
        /// <returns>The result returned by <paramref name="bind" />, or the original failure.</returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="task" /> or <paramref name="bind" /> is <see langword="null" />.
        /// </exception>
        public Task<Result<TOut>> Bind<TOut>(Func<T, Result<TOut>> bind)
        {
            ArgumentNullException.ThrowIfNull(task);
            ArgumentNullException.ThrowIfNull(bind);

            return BindAwaitedAsync(task, bind);
        }

        /// <summary>
        /// Awaits the result, then handles both cases and returns a value from either one.
        /// </summary>
        /// <param name="onSuccess">Called with the success value if the result is a success.</param>
        /// <param name="onFailure">
        /// Called with the failure if the result is a failure. A <see langword="default" /> result passes
        /// <see cref="Failure.Uninitialized" />.
        /// </param>
        /// <typeparam name="TOut">The type returned by both handlers.</typeparam>
        /// <returns>The value returned by the handler that was called.</returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="task" />, <paramref name="onSuccess" /> or <paramref name="onFailure" /> is
        /// <see langword="null" />.
        /// </exception>
        public Task<TOut> Match<TOut>(Func<T, TOut> onSuccess, Func<Failure, TOut> onFailure)
        {
            ArgumentNullException.ThrowIfNull(task);
            ArgumentNullException.ThrowIfNull(onSuccess);
            ArgumentNullException.ThrowIfNull(onFailure);

            return MatchAwaitedAsync(task, onSuccess, onFailure);
        }

        /// <summary>
        /// Awaits the result, then transforms the success value with an asynchronous step and passes a failure through
        /// unchanged.
        /// </summary>
        /// <param name="map">
        /// Called with the success value if the result is a success. It must not return a <see cref="Failure" />
        /// (<c>CSPP0002</c>).
        /// </param>
        /// <typeparam name="TOut">The type of the new success value. It must not be a <see cref="Failure" /> (<c>CSPP0001</c>).</typeparam>
        /// <returns>A success holding the value <paramref name="map" /> completes with, or the original failure.</returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="task" /> or <paramref name="map" /> is <see langword="null" />.
        /// </exception>
        public Task<Result<TOut>> MapAsync<TOut>(Func<T, Task<TOut>> map)
        {
            ArgumentNullException.ThrowIfNull(task);
            ArgumentNullException.ThrowIfNull(map);

            return MapAwaitedAsync(task, map);
        }

        /// <summary>
        /// Awaits the result, then chains an asynchronous step that can fail. A failure is passed through unchanged
        /// without calling the step.
        /// </summary>
        /// <param name="bind">Called with the success value if the result is a success.</param>
        /// <typeparam name="TOut">The success value type of the result returned by <paramref name="bind" />.</typeparam>
        /// <returns>The result <paramref name="bind" /> completes with, or the original failure.</returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="task" /> or <paramref name="bind" /> is <see langword="null" />.
        /// </exception>
        public Task<Result<TOut>> BindAsync<TOut>(Func<T, Task<Result<TOut>>> bind)
        {
            ArgumentNullException.ThrowIfNull(task);
            ArgumentNullException.ThrowIfNull(bind);

            return BindAwaitedAsync(task, bind);
        }

        /// <summary>
        /// Awaits the result, then handles both cases with asynchronous handlers and returns a value from either one.
        /// </summary>
        /// <param name="onSuccess">Called with the success value if the result is a success.</param>
        /// <param name="onFailure">
        /// Called with the failure if the result is a failure. A <see langword="default" /> result passes
        /// <see cref="Failure.Uninitialized" />.
        /// </param>
        /// <typeparam name="TOut">The type both handlers complete with.</typeparam>
        /// <returns>The value the handler that was called completes with.</returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="task" />, <paramref name="onSuccess" /> or <paramref name="onFailure" /> is
        /// <see langword="null" />.
        /// </exception>
        public Task<TOut> MatchAsync<TOut>(Func<T, Task<TOut>> onSuccess, Func<Failure, Task<TOut>> onFailure)
        {
            ArgumentNullException.ThrowIfNull(task);
            ArgumentNullException.ThrowIfNull(onSuccess);
            ArgumentNullException.ThrowIfNull(onFailure);

            return MatchAwaitedAsync(task, onSuccess, onFailure);
        }
    }

    // The awaiting parts live outside the public members, so argument checks throw when the member is called instead of
    // when its task is awaited.
    private static async Task<Result<T>> ToResultAsync<T>(Task<T> task)
    {
        return await task.ConfigureAwait(false);
    }

    private static async Task<Result<TOut>> MapAwaitedAsync<T, TOut>(Task<Result<T>> task, Func<T, TOut> map)
    {
        return (await task.ConfigureAwait(false)).Map(map);
    }

    private static async Task<Result<TOut>> BindAwaitedAsync<T, TOut>(Task<Result<T>> task, Func<T, Result<TOut>> bind)
    {
        return (await task.ConfigureAwait(false)).Bind(bind);
    }

    private static async Task<TOut> MatchAwaitedAsync<T, TOut>(
        Task<Result<T>> task,
        Func<T, TOut> onSuccess,
        Func<Failure, TOut> onFailure)
    {
        return (await task.ConfigureAwait(false)).Match(onSuccess, onFailure);
    }

    private static async Task<Result<TOut>> MapAwaitedAsync<T, TOut>(Task<Result<T>> task, Func<T, Task<TOut>> map)
    {
        return await (await task.ConfigureAwait(false)).MapAsync(map).ConfigureAwait(false);
    }

    private static async Task<Result<TOut>> BindAwaitedAsync<T, TOut>(
        Task<Result<T>> task,
        Func<T, Task<Result<TOut>>> bind)
    {
        return await (await task.ConfigureAwait(false)).BindAsync(bind).ConfigureAwait(false);
    }

    private static async Task<TOut> MatchAwaitedAsync<T, TOut>(
        Task<Result<T>> task,
        Func<T, Task<TOut>> onSuccess,
        Func<Failure, Task<TOut>> onFailure)
    {
        return await (await task.ConfigureAwait(false)).MatchAsync(onSuccess, onFailure).ConfigureAwait(false);
    }
}