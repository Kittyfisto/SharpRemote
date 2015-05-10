using System;
using System.IO;
using System.Reflection.Emit;

namespace SharpRemote.CodeGeneration.Serialization
{
	/// <summary>
	/// Responsible for providing the IL-code to serialize a value of the given type <see cref="ISerializationCompiler.Type"/>.
	/// </summary>
	public interface ISerializationCompiler
	{
		/// <summary>
		/// 
		/// </summary>
		Type Type { get; }

		/// <summary>
		/// Emits the code necessary to write a value of type <see cref="Type"/> into a <see cref="BinaryWriter"/>.
		/// </summary>
		/// <param name="gen"></param>
		/// <param name="loadWriter"></param>
		/// <param name="loadValue"></param>
		/// <param name="valueCanBeNull"></param>
		void EmitWriteValue(ILGenerator gen, Action loadWriter, Action loadValue, bool valueCanBeNull = true);

		/// <summary>
		/// Emits the code necessary to read a value of type <see cref="Type"/> from <see cref="BinaryReader"/> that was previously
		/// written to by the code emitted by <see cref="EmitWriteValue"/>.
		/// </summary>
		/// <param name="gen"></param>
		/// <param name="loadReader"></param>
		/// <param name="valueCanBeNull"></param>
		void EmitReadValue(ILGenerator gen, Action loadReader, bool valueCanBeNull = true);
	}
}