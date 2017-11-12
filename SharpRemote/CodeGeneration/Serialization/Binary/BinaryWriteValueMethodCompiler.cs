using System;
using System.Reflection;
using System.Reflection.Emit;

namespace SharpRemote.CodeGeneration.Serialization.Binary
{
	/// <summary>
	/// 
	/// </summary>
	internal sealed class BinaryWriteValueMethodCompiler
		: AbstractWriteValueMethodCompiler
	{
		public BinaryWriteValueMethodCompiler(CompilationContext context) : base(context)
		{
		}

		protected override void EmitWriteHint(ILGenerator generator, ByReferenceHint hint)
		{
			generator.Emit(OpCodes.Ldarg_0);
			generator.Emit(OpCodes.Ldc_I4, (int)hint);
			generator.Emit(OpCodes.Callvirt, Methods.WriteByte);
		}

		protected override void EmitBeginWriteField(ILGenerator gen, FieldDescription field)
		{
			throw new NotImplementedException();
		}

		protected override void EmitEndWriteField(ILGenerator gen, FieldDescription field)
		{
			throw new NotImplementedException();
		}

		protected override void EmitEndWriteProperty(ILGenerator gen, PropertyDescription property)
		{
			throw new NotImplementedException();
		}

		protected override void EmitBeginWriteProperty(ILGenerator gen, PropertyDescription property)
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

		protected override void EmitWriteUInt16(ILGenerator gen, Action loadValue)
		{
			throw new NotImplementedException();
		}

		protected override void EmitWriteInt16(ILGenerator gen, Action loadValue)
		{
			throw new NotImplementedException();
		}

		protected override void EmitWriteUInt32(ILGenerator gen, IMemberDescription member, Action loadMember, Action loadMemberAddress)
		{
			throw new NotImplementedException();
		}

		protected override void EmitWriteInt32(ILGenerator gen, IMemberDescription member, Action loadMember, Action loadMemberAddress)
		{
			throw new NotImplementedException();
		}

		protected override void EmitWriteUInt64(ILGenerator gen, Action loadValue)
		{
			throw new NotImplementedException();
		}

		protected override void EmitWriteInt64(ILGenerator gen, Action loadValue)
		{
			throw new NotImplementedException();
		}

		protected override void EmitWriteDecimal(ILGenerator gen, IMemberDescription member, Action loadMember, Action loadMemberAddress)
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

		protected override void EmitWriteString(ILGenerator gen, IMemberDescription member, Action loadMember, Action loadMemberAddress)
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