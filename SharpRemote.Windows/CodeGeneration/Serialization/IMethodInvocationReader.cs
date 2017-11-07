using System;

// ReSharper disable once CheckNamespace
namespace SharpRemote
{
	/// <summary>
	///     A forward-only reader which reads the invocation of a method from a stream which has been previously
	///     created using a <see cref="IMethodInvocationWriter" />.
	/// </summary>
	public interface IMethodInvocationReader
		: IDisposable
	{
		/// <summary>
		///     The id which identifies this remote procedure call.
		///     Is used to match method call and -result.
		/// </summary>
		ulong RpcId { get; }
		
		/// <summary>
		///     The id of the grain on which the method is to be invoked.
		/// </summary>
		ulong GrainId { get; }

		/// <summary>
		///     The name of the method which is to be invoked.
		/// </summary>
		string MethodName { get; }

		/// <summary>
		///     Reads the value of the next argument from the method call message.
		/// </summary>
		/// <param name="argumentName">The name of the next argument, if it was written to the next message.</param>
		/// <returns>The value of the next argument.</returns>
		object ReadNextArgument(out string argumentName);

		/// <summary>
		///     Reads the value of the next argument from the method call message.
		/// </summary>
		/// <param name="argumentName">The name of the next argument, if it was written to the next message.</param>
		/// <returns>The value of the next argument.</returns>
		sbyte ReadNextArgumentAsSByte(out string argumentName);

		/// <summary>
		///     Reads the value of the next argument from the method call message.
		/// </summary>
		/// <param name="argumentName">The name of the next argument, if it was written to the next message.</param>
		/// <returns>The value of the next argument.</returns>
		byte ReadNextArgumentAsByte(out string argumentName);

		/// <summary>
		///     Reads the value of the next argument from the method call message.
		/// </summary>
		/// <param name="argumentName">The name of the next argument, if it was written to the next message.</param>
		/// <returns>The value of the next argument.</returns>
		ushort ReadNextArgumentAsUInt16(out string argumentName);

		/// <summary>
		///     Reads the value of the next argument from the method call message.
		/// </summary>
		/// <param name="argumentName">The name of the next argument, if it was written to the next message.</param>
		/// <returns>The value of the next argument.</returns>
		short ReadNextArgumentAsInt16(out string argumentName);

		/// <summary>
		///     Reads the value of the next argument from the method call message.
		/// </summary>
		/// <param name="argumentName">The name of the next argument, if it was written to the next message.</param>
		/// <returns>The value of the next argument.</returns>
		uint ReadNextArgumentAsUInt32(out string argumentName);

		/// <summary>
		///     Reads the value of the next argument from the method call message.
		/// </summary>
		/// <param name="argumentName">The name of the next argument, if it was written to the next message.</param>
		/// <returns>The value of the next argument.</returns>
		int ReadNextArgumentAsInt32(out string argumentName);

		/// <summary>
		///     Reads the value of the next argument from the method call message.
		/// </summary>
		/// <param name="argumentName">The name of the next argument, if it was written to the next message.</param>
		/// <returns>The value of the next argument.</returns>
		ulong ReadNextArgumentAsUInt64(out string argumentName);

		/// <summary>
		///     Reads the value of the next argument from the method call message.
		/// </summary>
		/// <param name="argumentName">The name of the next argument, if it was written to the next message.</param>
		/// <returns>The value of the next argument.</returns>
		long ReadNextArgumentAsInt64(out string argumentName);

		/// <summary>
		///     Reads the value of the next argument from the method call message.
		/// </summary>
		/// <param name="argumentName">The name of the next argument, if it was written to the next message.</param>
		/// <returns>The value of the next argument.</returns>
		float ReadNextArgumentAsFloat(out string argumentName);

		/// <summary>
		///     Reads the value of the next argument from the method call message.
		/// </summary>
		/// <param name="argumentName">The name of the next argument, if it was written to the next message.</param>
		/// <returns>The value of the next argument.</returns>
		double ReadNextArgumentAsDouble(out string argumentName);

		/// <summary>
		///     Reads the value of the next argument from the method call message.
		/// </summary>
		/// <param name="argumentName">The name of the next argument, if it was written to the next message.</param>
		/// <returns>The value of the next argument.</returns>
		string ReadNextArgumentAsString(out string argumentName);

		/// <summary>
		///     Reads the value of the next argument from the method call message.
		/// </summary>
		/// <param name="argumentName">The name of the next argument, if it was written to the next message.</param>
		/// <returns>The value of the next argument.</returns>
		byte[] ReadNextArgumentAsBytes(out string argumentName);
	}
}