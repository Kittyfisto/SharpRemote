using System.Reflection.Emit;

namespace SharpRemote.CodeGeneration.Serialization.Binary
{
	internal sealed class BinaryWriterObjectMethodCompiler
		: AbstractWriteObjectMethodCompiler
	{
		public BinaryWriterObjectMethodCompiler(CompilationContext context) : base(context)
		{
		}

		//protected override void EmitWriteNull(ILGenerator generator)
		//{
		//	// BinaryWriter.WriteString(string.Empty);
		//	generator.Emit(OpCodes.Ldarg_0);
		//	generator.Emit(OpCodes.Ldsfld, Methods.StringEmpty);
		//	generator.Emit(OpCodes.Call, Methods.WriteString);
		//}
		//
		//protected override void EmitWriteTypeInformation(ILGenerator generator)
		//{
		//	// BinaryWriter.WriteString(type.AssemblyQualifiedName)
		//	generator.Emit(OpCodes.Ldarg_0);
		//	generator.Emit(OpCodes.Ldstr, Type.AssemblyQualifiedName);
		//	generator.Emit(OpCodes.Call, Methods.WriteString);
		//}
	}
}