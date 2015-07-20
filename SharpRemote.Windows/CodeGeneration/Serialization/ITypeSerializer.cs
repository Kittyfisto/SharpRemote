using System;
using System.IO;
using System.Reflection.Emit;

// ReSharper disable CheckNamespace
namespace SharpRemote
// ReSharper restore CheckNamespace
{
	/// <summary>
	/// Responsible for providing the IL-code to serialize a value of the given type <see cref="Type"/>.
	/// </summary>
	internal interface ITypeSerializer
	{
		/// <summary>
		/// 
		/// </summary>
		/// <param name="type"></param>
		/// <returns></returns>
		bool Supports(Type type);

		/// <summary>
		/// Emits the code necessary to write a value of type <see cref="Type"/> into a <see cref="BinaryWriter"/>.
		/// </summary>
		/// <param name="gen"></param>
		/// <param name="serializerCompiler"></param>
		/// <param name="loadWriter"></param>
		/// <param name="loadValue"></param>
		/// <param name="loadValueAddress"></param>
		/// <param name="loadSerializer"></param>
		/// <param name="loadRemotingEndPoint"></param>
		/// <param name="type"></param>
		/// <param name="valueCanBeNull"></param>
		void EmitWriteValue(ILGenerator gen, Serializer serializerCompiler, Action loadWriter, Action loadValue, Action loadValueAddress, Action loadSerializer, Action loadRemotingEndPoint, Type type, bool valueCanBeNull = true);

		/// <summary>
		/// Emits the code necessary to read a value of type <see cref="Type"/> from <see cref="BinaryReader"/> that was previously
		/// written to by the code emitted by <see cref="EmitWriteValue"/>.
		/// </summary>
		/// <param name="gen"></param>
		/// <param name="serializerCompiler"></param>
		/// <param name="loadReader"></param>
		/// <param name="loadSerializer"></param>
		/// <param name="loadRemotingEndPoint"></param>
		/// <param name="type"></param>
		/// <param name="valueCanBeNull"></param>
		void EmitReadValue(ILGenerator gen, Serializer serializerCompiler, Action loadReader, Action loadSerializer, Action loadRemotingEndPoint, Type type, bool valueCanBeNull = true);
	}
}