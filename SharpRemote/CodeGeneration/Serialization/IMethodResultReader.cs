using System;

// ReSharper disable once CheckNamespace
namespace SharpRemote
{
	/// <summary>
	///     Responsible for reading a message from a stream about the result of a method call.
	///     Reads the output from <see cref="IMethodResultWriter" />.
	/// </summary>
	public interface IMethodResultReader
		: IDisposable
	{
		/// <summary>
		///     The id of this remote procedure call.
		///     This value matches a previous call which has been created using
		///     <see cref="ISerializer2.CreateMethodCallWriter" />.
		/// </summary>
		ulong RpcId { get; }

		/// <summary>
		///     Returns the result of the method call as an <see cref="Object" />.
		///     Returns null if the method doesn't return any value (because it's return type is <see cref="Void" />)
		///     or if it actually returned null.
		/// </summary>
		/// <remarks>
		///     When <paramref name="exception" /> is set to a non-null value, then the returned value may not be used
		///     as the method call did not produce a return value.
		/// </remarks>
		/// <param name="exception">The exception (if any) which occured during the method call</param>
		/// <returns></returns>
		object ReadResult(out Exception exception);

		/// <summary>
		///     Returns the result of the method as an <see cref="sbyte" />.
		///     May throw an exception if the method didn't return a value of that type, but doesn't need to.
		/// </summary>
		/// <remarks>
		///     It's the responsibility of the caller of this method to call the correct GetResultXYZ method
		///     which matches the signature of the method being called.
		/// </remarks>
		/// <remarks>
		///     When <paramref name="exception" /> is set to a non-null value, then the returned value may not be used
		///     as the method call did not produce a return value.
		/// </remarks>
		/// <param name="exception">The exception (if any) which occured during the method call</param>
		/// <returns></returns>
		sbyte ReadResultAsSByte(out Exception exception);

		/// <summary>
		///     Returns the result of the method as an <see cref="byte" />.
		///     May throw an exception if the method didn't return a value of that type, but doesn't need to.
		/// </summary>
		/// <remarks>
		///     It's the responsibility of the caller of this method to call the correct GetResultXYZ method
		///     which matches the signature of the method being called.
		/// </remarks>
		/// <remarks>
		///     When <paramref name="exception" /> is set to a non-null value, then the returned value may not be used
		///     as the method call did not produce a return value.
		/// </remarks>
		/// <param name="exception">The exception (if any) which occured during the method call</param>
		/// <returns></returns>
		byte ReadResultAsByte(out Exception exception);

		/// <summary>
		///     Returns the result of the method as an <see cref="ushort" />.
		///     May throw an exception if the method didn't return a value of that type, but doesn't need to.
		/// </summary>
		/// <remarks>
		///     It's the responsibility of the caller of this method to call the correct GetResultXYZ method
		///     which matches the signature of the method being called.
		/// </remarks>
		/// <remarks>
		///     When <paramref name="exception" /> is set to a non-null value, then the returned value may not be used
		///     as the method call did not produce a return value.
		/// </remarks>
		/// <param name="exception">The exception (if any) which occured during the method call</param>
		/// <returns></returns>
		ushort ReadResultAsUInt16(out Exception exception);

		/// <summary>
		///     Returns the result of the method as an <see cref="short" />.
		///     May throw an exception if the method didn't return a value of that type, but doesn't need to.
		/// </summary>
		/// <remarks>
		///     It's the responsibility of the caller of this method to call the correct GetResultXYZ method
		///     which matches the signature of the method being called.
		/// </remarks>
		/// <remarks>
		///     When <paramref name="exception" /> is set to a non-null value, then the returned value may not be used
		///     as the method call did not produce a return value.
		/// </remarks>
		/// <param name="exception">The exception (if any) which occured during the method call</param>
		/// <returns></returns>
		short ReadResultAsInt16(out Exception exception);

		/// <summary>
		///     Returns the result of the method as an <see cref="uint" />.
		///     May throw an exception if the method didn't return a value of that type, but doesn't need to.
		/// </summary>
		/// <remarks>
		///     It's the responsibility of the caller of this method to call the correct GetResultXYZ method
		///     which matches the signature of the method being called.
		/// </remarks>
		/// <remarks>
		///     When <paramref name="exception" /> is set to a non-null value, then the returned value may not be used
		///     as the method call did not produce a return value.
		/// </remarks>
		/// <param name="exception">The exception (if any) which occured during the method call</param>
		/// <returns></returns>
		uint ReadResultUInt32(out Exception exception);

		/// <summary>
		///     Returns the result of the method as an <see cref="int" />.
		///     May throw an exception if the method didn't return a value of that type, but doesn't need to.
		/// </summary>
		/// <remarks>
		///     It's the responsibility of the caller of this method to call the correct GetResultXYZ method
		///     which matches the signature of the method being called.
		/// </remarks>
		/// <remarks>
		///     When <paramref name="exception" /> is set to a non-null value, then the returned value may not be used
		///     as the method call did not produce a return value.
		/// </remarks>
		/// <param name="exception">The exception (if any) which occured during the method call</param>
		/// <returns></returns>
		int ReadResultAsInt32(out Exception exception);

		/// <summary>
		///     Returns the result of the method as an <see cref="ulong" />.
		///     May throw an exception if the method didn't return a value of that type, but doesn't need to.
		/// </summary>
		/// <remarks>
		///     It's the responsibility of the caller of this method to call the correct GetResultXYZ method
		///     which matches the signature of the method being called.
		/// </remarks>
		/// <remarks>
		///     When <paramref name="exception" /> is set to a non-null value, then the returned value may not be used
		///     as the method call did not produce a return value.
		/// </remarks>
		/// <param name="exception">The exception (if any) which occured during the method call</param>
		/// <returns></returns>
		ulong ReadResultAsUInt64(out Exception exception);

		/// <summary>
		///     Returns the result of the method as an <see cref="long" />.
		///     May throw an exception if the method didn't return a value of that type, but doesn't need to.
		/// </summary>
		/// <remarks>
		///     It's the responsibility of the caller of this method to call the correct GetResultXYZ method
		///     which matches the signature of the method being called.
		/// </remarks>
		/// <remarks>
		///     When <paramref name="exception" /> is set to a non-null value, then the returned value may not be used
		///     as the method call did not produce a return value.
		/// </remarks>
		/// <param name="exception">The exception (if any) which occured during the method call</param>
		/// <returns></returns>
		long ReadResultAsInt64(out Exception exception);

		/// <summary>
		///     Returns the result of the method as an <see cref="float" />.
		///     May throw an exception if the method didn't return a value of that type, but doesn't need to.
		/// </summary>
		/// <remarks>
		///     It's the responsibility of the caller of this method to call the correct GetResultXYZ method
		///     which matches the signature of the method being called.
		/// </remarks>
		/// <remarks>
		///     When <paramref name="exception" /> is set to a non-null value, then the returned value may not be used
		///     as the method call did not produce a return value.
		/// </remarks>
		/// <param name="exception">The exception (if any) which occured during the method call</param>
		/// <returns></returns>
		float ReadResultAsFloat(out Exception exception);

		/// <summary>
		///     Returns the result of the method as an <see cref="double" />.
		///     May throw an exception if the method didn't return a value of that type, but doesn't need to.
		/// </summary>
		/// <remarks>
		///     It's the responsibility of the caller of this method to call the correct GetResultXYZ method
		///     which matches the signature of the method being called.
		/// </remarks>
		/// <remarks>
		///     When <paramref name="exception" /> is set to a non-null value, then the returned value may not be used
		///     as the method call did not produce a return value.
		/// </remarks>
		/// <param name="exception">The exception (if any) which occured during the method call</param>
		/// <returns></returns>
		double ReadResultAsDouble(out Exception exception);

		/// <summary>
		///     Returns the result of the method as an <see cref="string" />.
		///     May throw an exception if the method didn't return a value of that type, but doesn't need to.
		/// </summary>
		/// <remarks>
		///     It's the responsibility of the caller of this method to call the correct GetResultXYZ method
		///     which matches the signature of the method being called.
		/// </remarks>
		/// <remarks>
		///     When <paramref name="exception" /> is set to a non-null value, then the returned value may not be used
		///     as the method call did not produce a return value.
		/// </remarks>
		/// <param name="exception">The exception (if any) which occured during the method call</param>
		/// <returns></returns>
		string ReadResultAsString(out Exception exception);

		/// <summary>
		///     Returns the result of the method as an <see cref="string" />.
		///     May throw an exception if the method didn't return a value of that type, but doesn't need to.
		/// </summary>
		/// <remarks>
		///     It's the responsibility of the caller of this method to call the correct GetResultXYZ method
		///     which matches the signature of the method being called.
		/// </remarks>
		/// <remarks>
		///     When <paramref name="exception" /> is set to a non-null value, then the returned value may not be used
		///     as the method call did not produce a return value.
		/// </remarks>
		/// <param name="exception">The exception (if any) which occured during the method call</param>
		/// <returns></returns>
		byte[] ReadResultAsBytes(out Exception exception);
	}
}