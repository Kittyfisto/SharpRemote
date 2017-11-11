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
		/// <param name="value">The value of the next argument</param>
		/// <returns>True if the next argument could be read, false when the end of arguments has been reached.</returns>
		bool ReadNextArgument(out object value);

		/// <summary>
		///     Reads the value of the next argument from the method call message.
		/// </summary>
		/// <remarks>
		/// TODO: This method doesn't make much sense in the greater scope as it still requires dynamic dispatch.
		/// Instead, the serializer shall offer a method to emit code which eliminates dynamic dispatch alltogether (for structs/sealed types).
		/// </remarks>
		/// <param name="value">The value of the next argument</param>
		/// <returns>True if the next argument could be read, false when the end of arguments has been reached.</returns>
		bool ReadNextArgumentAsStruct<T>(out T value) where T : struct;

		/// <summary>
		///     Reads the value of the next argument from the method call message.
		/// </summary>
		/// <param name="value">The value of the next argument</param>
		/// <returns>True if the next argument could be read, false when the end of arguments has been reached.</returns>
		bool ReadNextArgumentAsSByte(out sbyte value);

		/// <summary>
		///     Reads the value of the next argument from the method call message.
		/// </summary>
		/// <param name="value">The value of the next argument</param>
		/// <returns>True if the next argument could be read, false when the end of arguments has been reached.</returns>
		bool ReadNextArgumentAsByte(out byte value);

		/// <summary>
		///     Reads the value of the next argument from the method call message.
		/// </summary>
		/// <param name="value">The value of the next argument</param>
		/// <returns>True if the next argument could be read, false when the end of arguments has been reached.</returns>
		bool ReadNextArgumentAsUInt16(out ushort value);

		/// <summary>
		///     Reads the value of the next argument from the method call message.
		/// </summary>
		/// <param name="value">The value of the next argument</param>
		/// <returns>True if the next argument could be read, false when the end of arguments has been reached.</returns>
		bool ReadNextArgumentAsInt16(out short value);

		/// <summary>
		///     Reads the value of the next argument from the method call message.
		/// </summary>
		/// <param name="value">The value of the next argument</param>
		/// <returns>True if the next argument could be read, false when the end of arguments has been reached.</returns>
		bool ReadNextArgumentAsUInt32(out uint value);

		/// <summary>
		///     Reads the value of the next argument from the method call message.
		/// </summary>
		/// <param name="value">The value of the next argument</param>
		/// <returns>True if the next argument could be read, false when the end of arguments has been reached.</returns>
		bool ReadNextArgumentAsInt32(out int value);

		/// <summary>
		///     Reads the value of the next argument from the method call message.
		/// </summary>
		/// <param name="value">The value of the next argument</param>
		/// <returns>True if the next argument could be read, false when the end of arguments has been reached.</returns>
		bool ReadNextArgumentAsUInt64(out ulong value);

		/// <summary>
		///     Reads the value of the next argument from the method call message.
		/// </summary>
		/// <param name="value">The value of the next argument</param>
		/// <returns>True if the next argument could be read, false when the end of arguments has been reached.</returns>
		bool ReadNextArgumentAsInt64(out long value);

		/// <summary>
		///     Reads the value of the next argument from the method call message.
		/// </summary>
		/// <param name="value">The value of the next argument</param>
		/// <returns>True if the next argument could be read, false when the end of arguments has been reached.</returns>
		bool ReadNextArgumentAsFloat(out float value);

		/// <summary>
		///     Reads the value of the next argument from the method call message.
		/// </summary>
		/// <param name="value">The value of the next argument</param>
		/// <returns>True if the next argument could be read, false when the end of arguments has been reached.</returns>
		bool ReadNextArgumentAsDouble(out double value);

		/// <summary>
		///     Reads the value of the next argument from the method call message.
		/// </summary>
		/// <param name="value">The value of the next argument</param>
		/// <returns>True if the next argument could be read, false when the end of arguments has been reached.</returns>
		bool ReadNextArgumentAsDecimal(out decimal value);

		/// <summary>
		///     Reads the value of the next argument from the method call message.
		/// </summary>
		/// <param name="value">The value of the next argument</param>
		/// <returns>True if the next argument could be read, false when the end of arguments has been reached.</returns>
		bool ReadNextArgumentAsString(out string value);

		/// <summary>
		///     Reads the value of the next argument from the method call message.
		/// </summary>
		/// <param name="value">The value of the next argument</param>
		/// <returns>True if the next argument could be read, false when the end of arguments has been reached.</returns>
		bool ReadNextArgumentAsBytes(out byte[] value);
	}
}