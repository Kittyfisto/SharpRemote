using System;

// ReSharper disable once CheckNamespace
namespace SharpRemote
{
	/// <summary>
	///     Responsible for writing a message to a stream about the result of a method call.
	/// </summary>
	public interface IMethodResultWriter
		: IDisposable
	{
		/// <summary>
		/// 
		/// </summary>
		void SetFinished();

		/// <summary>
		/// 
		/// </summary>
		void WriteResult(object value);

		/// <summary>
		/// 
		/// </summary>
		void WriteResult(sbyte value);

		/// <summary>
		/// 
		/// </summary>
		void WriteResult(byte value);

		/// <summary>
		/// 
		/// </summary>
		void WriteResult(ushort value);

		/// <summary>
		/// 
		/// </summary>
		void WriteResult(short value);

		/// <summary>
		/// 
		/// </summary>
		void WriteResult(uint value);

		/// <summary>
		/// 
		/// </summary>
		void WriteResult(int value);

		/// <summary>
		/// 
		/// </summary>
		void WriteResult(ulong value);

		/// <summary>
		/// 
		/// </summary>
		void WriteResult(long value);

		/// <summary>
		/// 
		/// </summary>
		void WriteResult(float value);

		/// <summary>
		/// 
		/// </summary>
		void WriteResult(double value);

		/// <summary>
		///     Writes the result of the method invocation.
		/// </summary>
		/// <param name="value"></param>
		void WriteResult(string value);

		/// <summary>
		///     Signals that the method call resulted in an unhandled exception being thrown.
		///     The exception should be serialized.
		/// </summary>
		/// <param name="e"></param>
		void WriteException(Exception e);
	}
}