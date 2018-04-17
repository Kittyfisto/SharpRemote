using System;
using System.Reflection;
using System.Reflection.Emit;

namespace SharpRemote.CodeGeneration.Serialization.Binary
{
	internal sealed class BinaryReadValueMethodCompiler
		: AbstractReadValueMethodCompiler
	{
		private static readonly MethodInfo BinarySerializer2ReadByte;
		private static readonly MethodInfo BinarySerializer2ReadSByte;
		private static readonly MethodInfo BinarySerializer2ReadDecimal;
		private static readonly MethodInfo BinarySerializer2ReadInt16;
		private static readonly MethodInfo BinarySerializer2ReadUInt16;
		private static readonly MethodInfo BinarySerializer2ReadInt32;
		private static readonly MethodInfo BinarySerializer2ReadUInt32;
		private static readonly MethodInfo BinarySerializer2ReadInt64;
		private static readonly MethodInfo BinarySerializer2ReadUInt64;
		private static readonly MethodInfo BinarySerializer2ReadString;
		private static readonly MethodInfo BinarySerializer2ReadDateTime;
		private static readonly MethodInfo BinarySerializer2ReadFloat;
		private static readonly MethodInfo BinarySerializer2ReadDouble;
		private static readonly MethodInfo BinarySerializer2ReadException;

		static BinaryReadValueMethodCompiler()
		{
			BinarySerializer2ReadByte = typeof(BinarySerializer2).GetMethod(nameof(BinarySerializer2.ReadValueAsByte));
			BinarySerializer2ReadSByte = typeof(BinarySerializer2).GetMethod(nameof(BinarySerializer2.ReadValueAsSByte));
			BinarySerializer2ReadInt16 = typeof(BinarySerializer2).GetMethod(nameof(BinarySerializer2.ReadValueAsInt16));
			BinarySerializer2ReadUInt16 = typeof(BinarySerializer2).GetMethod(nameof(BinarySerializer2.ReadValueAsUInt16));
			BinarySerializer2ReadInt32 = typeof(BinarySerializer2).GetMethod(nameof(BinarySerializer2.ReadValueAsInt32));
			BinarySerializer2ReadUInt32 = typeof(BinarySerializer2).GetMethod(nameof(BinarySerializer2.ReadValueAsUInt32));
			BinarySerializer2ReadInt64 = typeof(BinarySerializer2).GetMethod(nameof(BinarySerializer2.ReadValueAsInt64));
			BinarySerializer2ReadUInt64 = typeof(BinarySerializer2).GetMethod(nameof(BinarySerializer2.ReadValueAsUInt64));
			BinarySerializer2ReadString = typeof(BinarySerializer2).GetMethod(nameof(BinarySerializer2.ReadValueAsString));
			BinarySerializer2ReadDateTime = typeof(BinarySerializer2).GetMethod(nameof(BinarySerializer2.ReadValueAsDateTime));
			BinarySerializer2ReadFloat = typeof(BinarySerializer2).GetMethod(nameof(BinarySerializer2.ReadValueAsSingle));
			BinarySerializer2ReadDouble = typeof(BinarySerializer2).GetMethod(nameof(BinarySerializer2.ReadValueAsDouble));
			BinarySerializer2ReadDecimal = typeof(BinarySerializer2).GetMethod(nameof(BinarySerializer2.ReadValueAsDecimal));
			BinarySerializer2ReadException = typeof(BinarySerializer2).GetMethod(nameof(BinarySerializer2.ReadValueAsException));
		}

		public BinaryReadValueMethodCompiler(CompilationContext context)
			: base(context)
		{}

		protected override void EmitBeginRead(ILGenerator gen)
		{
		}

		protected override void EmitBeginReadField(ILGenerator gen, FieldDescription field)
		{
		}

		protected override void EmitEndReadField(ILGenerator gen, FieldDescription field)
		{
		}

		protected override void EmitBeginReadProperty(ILGenerator gen, PropertyDescription property)
		{
		}
		
		protected override void EmitEndReadProperty(ILGenerator gen, PropertyDescription property)
		{
		}

		protected override void EmitEndRead(ILGenerator gen)
		{
		}

		protected override void EmitReadByte(ILGenerator gen)
		{
			gen.Emit(OpCodes.Ldarg_0);
			gen.Emit(OpCodes.Call, BinarySerializer2ReadByte);
		}

		protected override void EmitReadSByte(ILGenerator gen)
		{
			gen.Emit(OpCodes.Ldarg_0);
			gen.Emit(OpCodes.Call, BinarySerializer2ReadSByte);
		}

		protected override void EmitReadUInt16(ILGenerator gen)
		{
			gen.Emit(OpCodes.Ldarg_0);
			gen.Emit(OpCodes.Call, BinarySerializer2ReadUInt16);
		}

		protected override void EmitReadInt16(ILGenerator gen)
		{
			gen.Emit(OpCodes.Ldarg_0);
			gen.Emit(OpCodes.Call, BinarySerializer2ReadInt16);
		}

		protected override void EmitReadUInt32(ILGenerator gen)
		{
			gen.Emit(OpCodes.Ldarg_0);
			gen.Emit(OpCodes.Call, BinarySerializer2ReadUInt32);
		}

		protected override void EmitReadInt32(ILGenerator gen)
		{
			gen.Emit(OpCodes.Ldarg_0);
			gen.Emit(OpCodes.Call, BinarySerializer2ReadInt32);
		}

		protected override void EmitReadUInt64(ILGenerator gen)
		{
			gen.Emit(OpCodes.Ldarg_0);
			gen.Emit(OpCodes.Call, BinarySerializer2ReadUInt64);
		}

		protected override void EmitReadInt64(ILGenerator gen)
		{
			gen.Emit(OpCodes.Ldarg_0);
			gen.Emit(OpCodes.Call, BinarySerializer2ReadInt64);
		}

		protected override void EmitReadDecimal(ILGenerator gen)
		{
			gen.Emit(OpCodes.Ldarg_0);
			gen.Emit(OpCodes.Call, BinarySerializer2ReadDecimal);
		}

		protected override void EmitReadFloat(ILGenerator gen)
		{
			gen.Emit(OpCodes.Ldarg_0);
			gen.Emit(OpCodes.Call, BinarySerializer2ReadFloat);
		}

		protected override void EmitReadDouble(ILGenerator gen)
		{
			gen.Emit(OpCodes.Ldarg_0);
			gen.Emit(OpCodes.Call, BinarySerializer2ReadDouble);
		}

		protected override void EmitReadString(ILGenerator gen)
		{
			gen.Emit(OpCodes.Ldarg_0);
			gen.Emit(OpCodes.Call, BinarySerializer2ReadString);
		}

		protected override void EmitReadDateTime(ILGenerator gen)
		{
			gen.Emit(OpCodes.Ldarg_0);
			gen.Emit(OpCodes.Call, BinarySerializer2ReadDateTime);
		}

		protected override void EmitReadLevel(ILGenerator gen)
		{
			throw new System.NotImplementedException();
		}

		protected override void EmitReadException(ILGenerator gen, Type exceptionType)
		{
			gen.Emit(OpCodes.Ldarg_0);
			gen.Emit(OpCodes.Call, BinarySerializer2ReadException);
		}

		protected override void EmitReadHintAndGrainId(ILGenerator generator)
		{
			generator.Emit(OpCodes.Ldarg_0);
			generator.Emit(OpCodes.Callvirt, Methods.ReadByte);
			generator.Emit(OpCodes.Ldarg_0);
			generator.Emit(OpCodes.Callvirt, Methods.ReadLong);
		}
	}
}