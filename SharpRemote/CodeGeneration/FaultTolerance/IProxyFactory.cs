using System;

namespace SharpRemote.CodeGeneration.FaultTolerance
{
	/// <summary>
	/// </summary>
	/// <typeparam name="T"></typeparam>
	public interface IProxyFactory<T>
		where T : class
	{
		/// <summary>
		///     Method calls on the created never exceed the given maximum latency (within reason).
		///     Any method which appears to execute longer will be aborted and a <see cref="TimeoutException" />
		///     will be thrown.
		/// </summary>
		/// <remarks>
		///     This method should **only** be used in cases where the method you are calling cannot possibly
		///     be cancelled in any way.
		/// </remarks>
		/// <param name="maximumMethodLatency"></param>
		/// <returns></returns>
		IProxyFactory<T> WithMaximumLatencyOf(TimeSpan maximumMethodLatency);

		/// <summary>
		///     All exceptions thrown by methods of the subject are caught and handled.
		///     If the method call has a return value, then the default value for that
		///     particular type is returned instead.
		/// </summary>
		/// <returns></returns>
		IProxyFactory<T> WithDefaultFallback();

		/// <summary>
		///     All exceptions thrown by methods of the subject are caught and handled.
		///     The specified fallback implementation is called in this case and, if
		///     the method happens to have a return type, the value returned from the fallback
		///     is returned.
		/// </summary>
		/// <remarks>
		///     Exceptions thrown by the fallback are NOT handled.
		/// </remarks>
		/// <remarks>
		///     You can chain various fallbacks and they will be executed in the chained order.
		/// </remarks>
		/// <param name="fallback"></param>
		/// <returns></returns>
		IProxyFactory<T> WithFallbackTo(T fallback);

		/// <summary>
		///     All exceptions thrown by methods of the subject are caught and handled.
		///     The method in question will be invoked again for the given amount of times
		///     or until it returns without throwing.
		/// </summary>
		/// <param name="numberOfRetries"></param>
		/// <returns></returns>
		IProxyFactory<T> WithMaximumRetries(int numberOfRetries);

		/// <summary>
		///     Finally creates the proxy with the configured behaviour.
		/// </summary>
		/// <returns></returns>
		T Create();
	}
}