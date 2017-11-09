using System;

// ReSharper disable once CheckNamespace
namespace SharpRemote
{
	/// <summary>
	///     Responsible for creating a serialized message out of a method invocation.
	/// </summary>
	public interface IMethodInvocationWriter
		: IDisposable
	{
		/// <summary>
		///     Adds an argument of the given name and value to this method call.
		/// </summary>
		/// <param name="name"></param>
		/// <param name="value"></param>
		void WriteNamedArgument(string name, object value);

		/// <summary>
		///     Adds an argument of the given name and value to this method call.
		/// </summary>
		/// <param name="name"></param>
		/// <param name="value"></param>
		void WriteNamedArgument<T>(string name, T value) where T : struct;

		/// <summary>
		///     Adds an argument of the given name and value to this method call.
		/// </summary>
		/// <param name="name"></param>
		/// <param name="value"></param>
		void WriteNamedArgument(string name, sbyte value);

		/// <summary>
		///     Adds an argument of the given name and value to this method call.
		/// </summary>
		/// <param name="name"></param>
		/// <param name="value"></param>
		void WriteNamedArgument(string name, byte value);

		/// <summary>
		///     Adds an argument of the given name and value to this method call.
		/// </summary>
		/// <param name="name"></param>
		/// <param name="value"></param>
		void WriteNamedArgument(string name, ushort value);

		/// <summary>
		///     Adds an argument of the given name and value to this method call.
		/// </summary>
		/// <param name="name"></param>
		/// <param name="value"></param>
		void WriteNamedArgument(string name, short value);

		/// <summary>
		///     Adds an argument of the given name and value to this method call.
		/// </summary>
		/// <param name="name"></param>
		/// <param name="value"></param>
		void WriteNamedArgument(string name, uint value);

		/// <summary>
		///     Adds an argument of the given name and value to this method call.
		/// </summary>
		/// <param name="name"></param>
		/// <param name="value"></param>
		void WriteNamedArgument(string name, int value);

		/// <summary>
		///     Adds an argument of the given name and value to this method call.
		/// </summary>
		/// <param name="name"></param>
		/// <param name="value"></param>
		void WriteNamedArgument(string name, ulong value);

		/// <summary>
		///     Adds an argument of the given name and value to this method call.
		/// </summary>
		/// <param name="name"></param>
		/// <param name="value"></param>
		void WriteNamedArgument(string name, long value);

		/// <summary>
		///     Adds an argument of the given name and value to this method call.
		/// </summary>
		/// <param name="name"></param>
		/// <param name="value"></param>
		void WriteNamedArgument(string name, float value);

		/// <summary>
		///     Adds an argument of the given name and value to this method call.
		/// </summary>
		/// <param name="name"></param>
		/// <param name="value"></param>
		void WriteNamedArgument(string name, double value);

		/// <summary>
		///     Adds an argument of the given name and value to this method call.
		/// </summary>
		/// <param name="name"></param>
		/// <param name="value"></param>
		void WriteNamedArgument(string name, string value);

		/// <summary>
		///     Adds an argument of the given name and value to this method call.
		/// </summary>
		/// <param name="name"></param>
		/// <param name="value"></param>
		void WriteNamedArgument(string name, byte[] value);
	}
}