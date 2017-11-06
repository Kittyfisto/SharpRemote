using System;
using System.IO;
using System.Reflection.Emit;

// ReSharper disable once CheckNamespace
namespace SharpRemote
{
	/// <summary>
	/// </summary>
	public interface ISerializerCompiler
		: ISerializer
	{
		/// <summary>
		///     Emits the code necessary to read a value of the given compile-time type from
		///     a <see cref="BinaryReader" />.
		/// </summary>
		/// <param name="gen"></param>
		/// <param name="loadReader"></param>
		/// <param name="loadSerializer"></param>
		/// <param name="loadRemotingEndPoint"></param>
		/// <param name="valueType"></param>
		void EmitReadValue(ILGenerator gen,
		                   Action loadReader,
		                   Action loadSerializer,
		                   Action loadRemotingEndPoint,
		                   Type valueType);

		/// <summary>
		/// Emits the code necessary to write a value of the given compile-time type to
		/// a <see cref="BinaryWriter"/>.
		/// </summary>
		/// <param name="gen"></param>
		/// <param name="loadWriter"></param>
		/// <param name="loadValue"></param>
		/// <param name="loadValueAddress"></param>
		/// <param name="loadSerializer"></param>
		/// <param name="loadRemotingEndPoint"></param>
		/// <param name="valueType"></param>
		void EmitWriteValue(ILGenerator gen,
		                    Action loadWriter,
		                    Action loadValue,
		                    Action loadValueAddress,
		                    Action loadSerializer,
		                    Action loadRemotingEndPoint,
		                    Type valueType);
	}
}