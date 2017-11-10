using System.Reflection.Emit;

namespace SharpRemote.CodeGeneration.Serialization.Binary
{
	internal sealed class BinaryWriteValueMethodCompiler
		: AbstractWriteValueMethodCompiler
	{
		public BinaryWriteValueMethodCompiler(CompilationContext context) : base(context)
		{
		}

		protected override void EmitWriteHasValue(ILGenerator generator)
		{
			generator.Emit(OpCodes.Ldarg_0);
			generator.Emit(OpCodes.Ldc_I4_1);
			generator.Emit(OpCodes.Call, Methods.WriteBool);
		}

		protected override void EmitWriteNoValue(ILGenerator generator)
		{
			generator.Emit(OpCodes.Ldarg_0);
			generator.Emit(OpCodes.Ldc_I4_0);
			generator.Emit(OpCodes.Call, Methods.WriteBool);
		}
	}
}