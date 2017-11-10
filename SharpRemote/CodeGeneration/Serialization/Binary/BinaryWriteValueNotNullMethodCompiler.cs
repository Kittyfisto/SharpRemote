using System;
using System.Reflection.Emit;

namespace SharpRemote.CodeGeneration.Serialization.Binary
{
	/// <summary>
	/// 
	/// </summary>
	internal sealed class BinaryWriteValueNotNullMethodCompiler
		: AbstractWriteValueNotNullMethodCompiler
	{
		public BinaryWriteValueNotNullMethodCompiler(CompilationContext context) : base(context)
		{
		}

		protected override void EmitWriteHint(ILGenerator generator, ByReferenceHint hint)
		{
			generator.Emit(OpCodes.Ldarg_0);
			generator.Emit(OpCodes.Ldc_I4, (int)hint);
			generator.Emit(OpCodes.Callvirt, Methods.WriteByte);
		}

		protected override void EmitBeginWriteFieldOrProperty(ILGenerator generator, TypeDescription valueType, string name)
		{
			throw new NotImplementedException();
		}

		protected override void EmitEndWriteFieldOrProperty(ILGenerator generator, TypeDescription valueType, string name)
		{
			throw new NotImplementedException();
		}

		protected override void EmitWriteByte(ILGenerator gen, Action loadValue)
		{
			throw new NotImplementedException();
		}

		protected override void EmitWriteSByte(ILGenerator gen, Action loadValue)
		{
			throw new NotImplementedException();
		}

		protected override void EmitWriteUShort(ILGenerator gen, Action loadValue)
		{
			throw new NotImplementedException();
		}

		protected override void EmitWriteShort(ILGenerator gen, Action loadValue)
		{
			throw new NotImplementedException();
		}

		protected override void EmitWriteUInt(ILGenerator gen, Action loadValue)
		{
			throw new NotImplementedException();
		}

		protected override void EmitWriteInt(ILGenerator gen, Action loadValue)
		{
			throw new NotImplementedException();
		}

		protected override void EmitWriteULong(ILGenerator gen, Action loadValue)
		{
			throw new NotImplementedException();
		}

		protected override void EmitWriteLong(ILGenerator gen, Action loadValue)
		{
			throw new NotImplementedException();
		}

		protected override void EmitWriteDecimal(ILGenerator gen, Action loadValue)
		{
			throw new NotImplementedException();
		}

		protected override void EmitWriteFloat(ILGenerator gen, Action loadValue)
		{
			throw new NotImplementedException();
		}

		protected override void EmitWriteDouble(ILGenerator gen, Action loadValue)
		{
			throw new NotImplementedException();
		}

		protected override void EmitWriteString(ILGenerator gen, Action loadValue)
		{
			throw new NotImplementedException();
		}

		protected override void EmitWriteObjectId(ILGenerator generator, LocalBuilder proxy)
		{
			generator.Emit(OpCodes.Ldarg_0);
			generator.Emit(OpCodes.Ldloc, proxy);
			generator.Emit(OpCodes.Callvirt, Methods.GrainGetObjectId);
			generator.Emit(OpCodes.Callvirt, Methods.WriteLong);
		}
	}
}