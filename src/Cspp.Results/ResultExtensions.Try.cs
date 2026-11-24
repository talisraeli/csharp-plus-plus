namespace Cspp.Results;

/// <content>
/// <c>Result.Try</c> and <c>Result.TryAsync</c>, which run code that can throw and turn an exception into a failure.
/// </content>
public static partial class ResultExtensions
{
    extension(Result)
    {
        /// <summary>
        /// Runs <paramref name="action" /> and returns a success, or a failure if it throws.
        /// </summary>
        /// <param name="action">The code to run.</param>
        /// <param name="onException">
        /// Called with the exception if <paramref name="action" /> throws, and returns the failure to return, for example
        /// after logging the exception. If <see langword="null" />, the failure is <see cref="Failure.Exception" />. It
        /// must not return <see langword="null" />.
        /// </param>
        /// <returns>A success, or the failure for the exception if <paramref name="action" /> threw.</returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="action" /> is <see langword="null" />, or <paramref name="onException" /> returned
        /// <see langword="null" />.
        /// </exception>
        /// <remarks>
        /// Every exception is caught, including <see cref="OperationCanceledException" />.
        /// </remarks>
        public static Result Try(Action action, Func<Exception, Failure>? onException = null)
        {
            ArgumentNullException.ThrowIfNull(action);

            try
            {
                action();
                return Result.Success;
            }
            catch (Exception exception)
            {
                return ToFailure(exception, onException);
            }
        }

        /// <summary>
        /// Runs <paramref name="func" /> and returns its value, or a failure if it throws.
        /// </summary>
        /// <param name="func">The code to run. Its value type must not be a <see cref="Failure" /> (<c>CSPP0001</c>).</param>
        /// <param name="onException">
        /// Called with the exception if <paramref name="func" /> throws, and returns the failure to return, for example
        /// after logging the exception. If <see langword="null" />, the failure is <see cref="Failure.Exception" />. It
        /// must not return <see langword="null" />.
        /// </param>
        /// <typeparam name="T">The type of the value <paramref name="func" /> returns.</typeparam>
        /// <returns>A success holding the value, or the failure for the exception if <paramref name="func" /> threw.</returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="func" /> is <see langword="null" />, or <paramref name="onException" /> returned
        /// <see langword="null" />.
        /// </exception>
        /// <remarks>
        /// Every exception is caught, including <see cref="OperationCanceledException" />.
        /// </remarks>
        public static Result<T> Try<T>(Func<T> func, Func<Exception, Failure>? onException = null)
        {
            ArgumentNullException.ThrowIfNull(func);

            try
            {
                return func();
            }
            catch (Exception exception)
            {
                return ToFailure(exception, onException);
            }
        }

        /// <summary>
        /// Runs <paramref name="func" /> and returns a success, or a failure if it throws.
        /// </summary>
        /// <param name="func">The asynchronous code to run.</param>
        /// <param name="onException">
        /// Called with the exception if <paramref name="func" /> throws, and returns the failure to return, for example
        /// after logging the exception. If <see langword="null" />, the failure is <see cref="Failure.Exception" />. It
        /// must not return <see langword="null" />.
        /// </param>
        /// <returns>A success, or the failure for the exception if <paramref name="func" /> threw.</returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="func" /> is <see langword="null" />, or <paramref name="onException" /> returned
        /// <see langword="null" />.
        /// </exception>
        /// <remarks>
        /// <see cref="OperationCanceledException" /> is not caught, so a cancelled operation still ends as cancelled.
        /// </remarks>
        public static Task<Result> TryAsync(Func<Task> func, Func<Exception, Failure>? onException = null)
        {
            ArgumentNullException.ThrowIfNull(func);

            return TryCoreAsync(func, onException);
        }

        /// <summary>
        /// Runs <paramref name="func" /> and returns its value, or a failure if it throws.
        /// </summary>
        /// <param name="func">
        /// The asynchronous code to run. Its value type must not be a <see cref="Failure" /> (<c>CSPP0001</c>).
        /// </param>
        /// <param name="onException">
        /// Called with the exception if <paramref name="func" /> throws, and returns the failure to return, for example
        /// after logging the exception. If <see langword="null" />, the failure is <see cref="Failure.Exception" />. It
        /// must not return <see langword="null" />.
        /// </param>
        /// <typeparam name="T">The type of the value <paramref name="func" /> completes with.</typeparam>
        /// <returns>A success holding the value, or the failure for the exception if <paramref name="func" /> threw.</returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="func" /> is <see langword="null" />, or <paramref name="onException" /> returned
        /// <see langword="null" />.
        /// </exception>
        /// <remarks>
        /// <see cref="OperationCanceledException" /> is not caught, so a cancelled operation still ends as cancelled.
        /// </remarks>
        public static Task<Result<T>> TryAsync<T>(Func<Task<T>> func, Func<Exception, Failure>? onException = null)
        {
            ArgumentNullException.ThrowIfNull(func);

            return TryCoreAsync(func, onException);
        }
    }

    // The failure for a caught exception: Failure.Exception, or what onException returns for it.
    private static Failure ToFailure(Exception exception, Func<Exception, Failure>? onException)
    {
        if (onException is null)
        {
            return Failure.Exception;
        }

        return onException(exception)
            ?? throw new ArgumentNullException(
                nameof(onException),
                "The onException delegate returned null instead of a Failure.");
    }

    // The awaiting parts of TryAsync, apart so that a null func throws when TryAsync is called. An exception thrown
    // before func returns its task is caught too.
    private static async Task<Result> TryCoreAsync(Func<Task> func, Func<Exception, Failure>? onException)
    {
        try
        {
            await func().ConfigureAwait(false);
            return Result.Success;
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            return ToFailure(exception, onException);
        }
    }

    private static async Task<Result<T>> TryCoreAsync<T>(Func<Task<T>> func, Func<Exception, Failure>? onException)
    {
        try
        {
            return await func().ConfigureAwait(false);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            return ToFailure(exception, onException);
        }
    }
}