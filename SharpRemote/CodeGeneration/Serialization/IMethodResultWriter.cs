using System;

// ReSharper disable once CheckNamespace
namespace SharpRemote
{
	/// <summary>
	///     Responsible for writing a message to a stream about the result of a method call.
	/// </summary>
	/// <remarks>
	///     A user of this class must either call <see cref="WriteFinished" />, <see cref="WriteResult(string)" />
	///     or <see cref="WriteException" />, but never more than one on the same writer-object.
	/// </remarks>
	public interface IMethodResultWriter
		: IDisposable
	{
		/// <summary>
		///     Writes a marker that the method finished executing.
		///     Only to be used for methods which have a <see cref="Void" /> return type
		///     (and thus <see cref="WriteResult(object)" /> would not be appropriate).
		/// </summary>
		void WriteFinished();

		/// <summary>
		///     Writes the result of the method invocation.
		/// </summary>
		void WriteResult(object value);

		/// <summary>
		///     Writes the result of the method invocation.
		/// </summary>
		void WriteResult(sbyte value);

		/// <summary>
		///     Writes the result of the method invocation.
		/// </summary>
		void WriteResult(byte value);

		/// <summary>
		///     Writes the result of the method invocation.
		/// </summary>
		void WriteResult(ushort value);

		/// <summary>
		///     Writes the result of the method invocation.
		/// </summary>
		void WriteResult(short value);

		/// <summary>
		///     Writes the result of the method invocation.
		/// </summary>
		void WriteResult(uint value);

		/// <summary>
		///     Writes the result of the method invocation.
		/// </summary>
		void WriteResult(int value);

		/// <summary>
		///     Writes the result of the method invocation.
		/// </summary>
		void WriteResult(ulong value);

		/// <summary>
		///     Writes the result of the method invocation.
		/// </summary>
		void WriteResult(long value);

		/// <summary>
		///     Writes the result of the method invocation.
		/// </summary>
		void WriteResult(float value);

		/// <summary>
		///     Writes the result of the method invocation.
		/// </summary>
		void WriteResult(double value);

		/// <summary>
		///     Writes the result of the method invocation.
		/// </summary>
		/// <param name="value"></param>
		void WriteResult(string value);

		/// <summary>
		///     Writes the result of the method invocation.
		/// </summary>
		/// <param name="value"></param>
		void WriteResult(byte[] value);

		/// <summary>
		///     Signals that the method call resulted in an unhandled exception being thrown.
		///     The exception should be serialized.
		/// </summary>
		/// <param name="e"></param>
		void WriteException(Exception e);
	}
}