using System;

// ReSharper disable once CheckNamespace
namespace SharpRemote
{
	/// <summary>
	///     Responsible for creating a serialized message out of a method invocation.
	/// </summary>
	public interface  IMethodInvocationWriter
		: IDisposable
	{
		/// <summary>
		///     Adds an argument of the given name and value to this method call.
		/// </summary>
		/// <param name="value"></param>
		void WriteArgument(object value);

		/// <summary>
		///     Adds an argument of the given name and value to this method call.
		/// </summary>
		/// <param name="value"></param>
		void WriteArgument<T>(T value) where T : struct;

		/// <summary>
		///     Adds an argument of the given name and value to this method call.
		/// </summary>
		/// <param name="value"></param>
		void WriteArgument(sbyte value);

		/// <summary>
		///     Adds an argument of the given name and value to this method call.
		/// </summary>
		/// <param name="value"></param>
		void WriteArgument(byte value);

		/// <summary>
		///     Adds an argument of the given name and value to this method call.
		/// </summary>
		/// <param name="value"></param>
		void WriteArgument(ushort value);

		/// <summary>
		///     Adds an argument of the given name and value to this method call.
		/// </summary>
		/// <param name="value"></param>
		void WriteArgument(short value);

		/// <summary>
		///     Adds an argument of the given name and value to this method call.
		/// </summary>
		/// <param name="value"></param>
		void WriteArgument(uint value);

		/// <summary>
		///     Adds an argument of the given name and value to this method call.
		/// </summary>
		/// <param name="value"></param>
		void WriteArgument(int value);

		/// <summary>
		///     Adds an argument of the given name and value to this method call.
		/// </summary>
		/// <param name="value"></param>
		void WriteArgument(ulong value);

		/// <summary>
		///     Adds an argument of the given name and value to this method call.
		/// </summary>
		/// <param name="value"></param>
		void WriteArgument(long value);

		/// <summary>
		///     Adds an argument of the given name and value to this method call.
		/// </summary>
		/// <param name="value"></param>
		void WriteArgument(float value);

		/// <summary>
		///     Adds an argument of the given name and value to this method call.
		/// </summary>
		/// <param name="value"></param>
		void WriteArgument(double value);

		/// <summary>
		///     Adds an argument of the given name and value to this method call.
		/// </summary>
		/// <param name="value"></param>
		void WriteArgument(decimal value);

		/// <summary>
		///     Adds an argument of the given name and value to this method call.
		/// </summary>
		/// <param name="value"></param>
		void WriteArgument(string value);

		/// <summary>
		///     Adds an argument of the given name and value to this method call.
		/// </summary>
		/// <param name="value"></param>
		void WriteArgument(byte[] value);
	}
}