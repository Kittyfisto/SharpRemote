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
		///     Reads an exception, if an exception was written using a <see cref="ISerializer2.CreateMethodCallWriter" />.
		/// </summary>
		/// <param name="exception"></param>
		/// <returns>True if an exception was written to- and thus read from, false if no exception is present</returns>
		bool ReadException(out Exception exception);

		/// <summary>
		///     Returns the result of the method call as an <see cref="Object" />.
		///     Returns null if the method doesn't return any value (because it's return type is <see cref="Void" />)
		///     or if it actually returned null.
		/// </summary>
		/// <param name="value"></param>
		/// <returns>True if a value was written to- and thus read from, false if no result is present</returns>
		bool ReadResult(out object value);

		/// <summary>
		///     Returns the result of the method as an <see cref="sbyte" />.
		///     May throw an exception if the method didn't return a value of that type, but doesn't need to.
		/// </summary>
		/// <remarks>
		///     It's the responsibility of the caller of this method to call the correct GetResultXYZ method
		///     which matches the signature of the method being called.
		/// </remarks>
		/// <param name="value"></param>
		/// <returns>True if a value was written to- and thus read from, false if no result is present</returns>
		bool ReadResultSByte(out sbyte value);

		/// <summary>
		///     Returns the result of the method as an <see cref="byte" />.
		///     May throw an exception if the method didn't return a value of that type, but doesn't need to.
		/// </summary>
		/// <remarks>
		///     It's the responsibility of the caller of this method to call the correct GetResultXYZ method
		///     which matches the signature of the method being called.
		/// </remarks>
		/// <param name="value"></param>
		/// <returns>True if a value was written to- and thus read from, false if no result is present</returns>
		bool ReadResultByte(out byte value);

		/// <summary>
		///     Returns the result of the method as an <see cref="ushort" />.
		///     May throw an exception if the method didn't return a value of that type, but doesn't need to.
		/// </summary>
		/// <remarks>
		///     It's the responsibility of the caller of this method to call the correct GetResultXYZ method
		///     which matches the signature of the method being called.
		/// </remarks>
		/// <param name="value"></param>
		/// <returns>True if a value was written to- and thus read from, false if no result is present</returns>
		bool ReadResultUInt16(out ushort value);

		/// <summary>
		///     Returns the result of the method as an <see cref="short" />.
		///     May throw an exception if the method didn't return a value of that type, but doesn't need to.
		/// </summary>
		/// <remarks>
		///     It's the responsibility of the caller of this method to call the correct GetResultXYZ method
		///     which matches the signature of the method being called.
		/// </remarks>
		/// <param name="value"></param>
		/// <returns>True if a value was written to- and thus read from, false if no result is present</returns>
		bool ReadResultInt16(out short value);

		/// <summary>
		///     Returns the result of the method as an <see cref="uint" />.
		///     May throw an exception if the method didn't return a value of that type, but doesn't need to.
		/// </summary>
		/// <remarks>
		///     It's the responsibility of the caller of this method to call the correct GetResultXYZ method
		///     which matches the signature of the method being called.
		/// </remarks>
		/// <param name="value"></param>
		/// <returns>True if a value was written to- and thus read from, false if no result is present</returns>
		bool ReadResultUInt32(out uint value);

		/// <summary>
		///     Returns the result of the method as an <see cref="int" />.
		///     May throw an exception if the method didn't return a value of that type, but doesn't need to.
		/// </summary>
		/// <remarks>
		///     It's the responsibility of the caller of this method to call the correct GetResultXYZ method
		///     which matches the signature of the method being called.
		/// </remarks>
		/// <param name="value"></param>
		/// <returns>True if a value was written to- and thus read from, false if no result is present</returns>
		bool ReadResultInt32(out int value);

		/// <summary>
		///     Returns the result of the method as an <see cref="ulong" />.
		///     May throw an exception if the method didn't return a value of that type, but doesn't need to.
		/// </summary>
		/// <remarks>
		///     It's the responsibility of the caller of this method to call the correct GetResultXYZ method
		///     which matches the signature of the method being called.
		/// </remarks>
		/// <param name="value"></param>
		/// <returns>True if a value was written to- and thus read from, false if no result is present</returns>
		bool ReadResultUInt64(out ulong value);

		/// <summary>
		///     Returns the result of the method as an <see cref="long" />.
		///     May throw an exception if the method didn't return a value of that type, but doesn't need to.
		/// </summary>
		/// <remarks>
		///     It's the responsibility of the caller of this method to call the correct GetResultXYZ method
		///     which matches the signature of the method being called.
		/// </remarks>
		/// <param name="value"></param>
		/// <returns>True if a value was written to- and thus read from, false if no result is present</returns>
		bool ReadResultInt64(out long value);

		/// <summary>
		///     Returns the result of the method as an <see cref="float" />.
		///     May throw an exception if the method didn't return a value of that type, but doesn't need to.
		/// </summary>
		/// <remarks>
		///     It's the responsibility of the caller of this method to call the correct GetResultXYZ method
		///     which matches the signature of the method being called.
		/// </remarks>
		/// <param name="value"></param>
		/// <returns>True if a value was written to- and thus read from, false if no result is present</returns>
		bool ReadResultFloat(out float value);

		/// <summary>
		///     Returns the result of the method as an <see cref="double" />.
		///     May throw an exception if the method didn't return a value of that type, but doesn't need to.
		/// </summary>
		/// <remarks>
		///     It's the responsibility of the caller of this method to call the correct GetResultXYZ method
		///     which matches the signature of the method being called.
		/// </remarks>
		/// <param name="value"></param>
		/// <returns>True if a value was written to- and thus read from, false if no result is present</returns>
		bool ReadResultDouble(out double value);

		/// <summary>
		///     Returns the result of the method as an <see cref="string" />.
		///     May throw an exception if the method didn't return a value of that type, but doesn't need to.
		/// </summary>
		/// <remarks>
		///     It's the responsibility of the caller of this method to call the correct GetResultXYZ method
		///     which matches the signature of the method being called.
		/// </remarks>
		/// <param name="value"></param>
		/// <returns>True if a value was written to- and thus read from, false if no result is present</returns>
		bool ReadResultString(out string value);

		/// <summary>
		///     Returns the result of the method as an <see cref="string" />.
		///     May throw an exception if the method didn't return a value of that type, but doesn't need to.
		/// </summary>
		/// <remarks>
		///     It's the responsibility of the caller of this method to call the correct GetResultXYZ method
		///     which matches the signature of the method being called.
		/// </remarks>
		/// <param name="value"></param>
		/// <returns>True if a value was written to- and thus read from, false if no result is present</returns>
		bool ReadResultBytes(out byte[] value);
	}
}