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
		///     This value matches a previous call which has been created using <see cref="ISerializer2.CreateMethodInvocationWriter" />.
		/// </summary>
		ulong RpcId { get; }

		/// <summary>
		///     Returns the exception of the method call or null if the call didn't cause
		///     an exception to be thrown.
		/// </summary>
		/// <returns></returns>
		Exception ReadException();

		/// <summary>
		///     Returns the result of the method call as an <see cref="Object" />.
		///     Returns null if the method doesn't return any value (because it's return type is <see cref="Void" />)
		///     or if it actually returned null.
		/// </summary>
		/// <returns></returns>
		object ReadResult();

		/// <summary>
		///     Returns the result of the method as an <see cref="sbyte" />.
		///     May throw an exception if the method didn't return a value of that type, but doesn't need to.
		/// </summary>
		/// <remarks>
		///     It's the responsibility of the caller of this method to call the correct GetResultXYZ method
		///     which matches the signature of the method being called.
		/// </remarks>
		/// <returns></returns>
		sbyte ReadResultAsSByte();

		/// <summary>
		///     Returns the result of the method as an <see cref="byte" />.
		///     May throw an exception if the method didn't return a value of that type, but doesn't need to.
		/// </summary>
		/// <remarks>
		///     It's the responsibility of the caller of this method to call the correct GetResultXYZ method
		///     which matches the signature of the method being called.
		/// </remarks>
		/// <returns></returns>
		byte ReadResultAsByte();

		/// <summary>
		///     Returns the result of the method as an <see cref="ushort" />.
		///     May throw an exception if the method didn't return a value of that type, but doesn't need to.
		/// </summary>
		/// <remarks>
		///     It's the responsibility of the caller of this method to call the correct GetResultXYZ method
		///     which matches the signature of the method being called.
		/// </remarks>
		/// <returns></returns>
		ushort ReadResultAsUInt16();

		/// <summary>
		///     Returns the result of the method as an <see cref="short" />.
		///     May throw an exception if the method didn't return a value of that type, but doesn't need to.
		/// </summary>
		/// <remarks>
		///     It's the responsibility of the caller of this method to call the correct GetResultXYZ method
		///     which matches the signature of the method being called.
		/// </remarks>
		/// <returns></returns>
		short ReadResultAsInt16();

		/// <summary>
		///     Returns the result of the method as an <see cref="uint" />.
		///     May throw an exception if the method didn't return a value of that type, but doesn't need to.
		/// </summary>
		/// <remarks>
		///     It's the responsibility of the caller of this method to call the correct GetResultXYZ method
		///     which matches the signature of the method being called.
		/// </remarks>
		/// <returns></returns>
		uint ReadResultUInt32();

		/// <summary>
		///     Returns the result of the method as an <see cref="int" />.
		///     May throw an exception if the method didn't return a value of that type, but doesn't need to.
		/// </summary>
		/// <remarks>
		///     It's the responsibility of the caller of this method to call the correct GetResultXYZ method
		///     which matches the signature of the method being called.
		/// </remarks>
		/// <returns></returns>
		int ReadResultAsInt32();

		/// <summary>
		///     Returns the result of the method as an <see cref="ulong" />.
		///     May throw an exception if the method didn't return a value of that type, but doesn't need to.
		/// </summary>
		/// <remarks>
		///     It's the responsibility of the caller of this method to call the correct GetResultXYZ method
		///     which matches the signature of the method being called.
		/// </remarks>
		/// <returns></returns>
		ulong ReadResultAsUInt64();

		/// <summary>
		///     Returns the result of the method as an <see cref="long" />.
		///     May throw an exception if the method didn't return a value of that type, but doesn't need to.
		/// </summary>
		/// <remarks>
		///     It's the responsibility of the caller of this method to call the correct GetResultXYZ method
		///     which matches the signature of the method being called.
		/// </remarks>
		/// <returns></returns>
		long ReadResultAsInt64();

		/// <summary>
		///     Returns the result of the method as an <see cref="float" />.
		///     May throw an exception if the method didn't return a value of that type, but doesn't need to.
		/// </summary>
		/// <remarks>
		///     It's the responsibility of the caller of this method to call the correct GetResultXYZ method
		///     which matches the signature of the method being called.
		/// </remarks>
		/// <returns></returns>
		float ReadResultAsFloat();

		/// <summary>
		///     Returns the result of the method as an <see cref="double" />.
		///     May throw an exception if the method didn't return a value of that type, but doesn't need to.
		/// </summary>
		/// <remarks>
		///     It's the responsibility of the caller of this method to call the correct GetResultXYZ method
		///     which matches the signature of the method being called.
		/// </remarks>
		/// <returns></returns>
		double ReadResultAsDouble();

		/// <summary>
		///     Returns the result of the method as an <see cref="string" />.
		///     May throw an exception if the method didn't return a value of that type, but doesn't need to.
		/// </summary>
		/// <remarks>
		///     It's the responsibility of the caller of this method to call the correct GetResultXYZ method
		///     which matches the signature of the method being called.
		/// </remarks>
		/// <returns></returns>
		string ReadResultAsString();

		/// <summary>
		///     Returns the result of the method as an <see cref="string" />.
		///     May throw an exception if the method didn't return a value of that type, but doesn't need to.
		/// </summary>
		/// <remarks>
		///     It's the responsibility of the caller of this method to call the correct GetResultXYZ method
		///     which matches the signature of the method being called.
		/// </remarks>
		/// <returns></returns>
		byte[] ReadResultAsBytes();
	}
}