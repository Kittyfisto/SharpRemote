using System;
using System.IO;
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
		private static readonly MethodInfo BinarySerializer2WriteObjectNotNull;
		private static readonly MethodInfo BinarySerializer2WriteByte;
		private static readonly MethodInfo BinarySerializer2WriteSByte;
		private static readonly MethodInfo BinarySerializer2WriteDecimal;
		private static readonly MethodInfo BinarySerializer2WriteInt16;
		private static readonly MethodInfo BinarySerializer2WriteUInt16;
		private static readonly MethodInfo BinarySerializer2WriteInt32;
		private static readonly MethodInfo BinarySerializer2WriteUInt32;
		private static readonly MethodInfo BinarySerializer2WriteInt64;
		private static readonly MethodInfo BinarySerializer2WriteUInt64;
		private static readonly MethodInfo BinarySerializer2WriteSingle;
		private static readonly MethodInfo BinarySerializer2WriteDouble;
		private static readonly MethodInfo BinarySerializer2WriteString;
		private static readonly MethodInfo BinarySerializer2WriteDateTime;
		private static readonly MethodInfo BinarySerializer2WriteException;

		static BinaryWriteValueMethodCompiler()
		{
			BinarySerializer2WriteObjectNotNull = typeof(BinarySerializer2).GetMethod(nameof(BinarySerializer2.WriteObjectNotNull), new []{typeof(BinaryWriter), typeof(object), typeof(IRemotingEndPoint)});
			BinarySerializer2WriteByte = typeof(BinarySerializer2).GetMethod(nameof(BinarySerializer2.WriteValue), new []{typeof(BinaryWriter), typeof(byte)});
			BinarySerializer2WriteSByte = typeof(BinarySerializer2).GetMethod(nameof(BinarySerializer2.WriteValue), new []{typeof(BinaryWriter), typeof(sbyte)});
			BinarySerializer2WriteDecimal = typeof(BinarySerializer2).GetMethod(nameof(BinarySerializer2.WriteValue), new []{typeof(BinaryWriter), typeof(decimal)});
			BinarySerializer2WriteInt16 = typeof(BinarySerializer2).GetMethod(nameof(BinarySerializer2.WriteValue), new []{typeof(BinaryWriter), typeof(Int16)});
			BinarySerializer2WriteUInt16 = typeof(BinarySerializer2).GetMethod(nameof(BinarySerializer2.WriteValue), new []{typeof(BinaryWriter), typeof(UInt16)});
			BinarySerializer2WriteInt32 = typeof(BinarySerializer2).GetMethod(nameof(BinarySerializer2.WriteValue), new []{typeof(BinaryWriter), typeof(Int32)});
			BinarySerializer2WriteUInt32 = typeof(BinarySerializer2).GetMethod(nameof(BinarySerializer2.WriteValue), new []{typeof(BinaryWriter), typeof(UInt32)});
			BinarySerializer2WriteInt64 = typeof(BinarySerializer2).GetMethod(nameof(BinarySerializer2.WriteValue), new []{typeof(BinaryWriter), typeof(Int64)});
			BinarySerializer2WriteUInt64 = typeof(BinarySerializer2).GetMethod(nameof(BinarySerializer2.WriteValue), new []{typeof(BinaryWriter), typeof(UInt64)});
			BinarySerializer2WriteSingle = typeof(BinarySerializer2).GetMethod(nameof(BinarySerializer2.WriteValue), new []{typeof(BinaryWriter), typeof(Single)});
			BinarySerializer2WriteDouble = typeof(BinarySerializer2).GetMethod(nameof(BinarySerializer2.WriteValue), new []{typeof(BinaryWriter), typeof(Double)});
			BinarySerializer2WriteString = typeof(BinarySerializer2).GetMethod(nameof(BinarySerializer2.WriteValue), new []{typeof(BinaryWriter), typeof(string)});
			BinarySerializer2WriteDateTime = typeof(BinarySerializer2).GetMethod(nameof(BinarySerializer2.WriteValue), new []{typeof(BinaryWriter), typeof(DateTime)});
			BinarySerializer2WriteException = typeof(BinarySerializer2).GetMethod(nameof(BinarySerializer2.WriteValue), new []{typeof(BinaryWriter), typeof(Exception)});
		}

		public BinaryWriteValueMethodCompiler(CompilationContext context)
			: base(context)
		{
			
		}

		protected override void EmitWriteHint(ILGenerator generator, ByReferenceHint hint)
		{
			generator.Emit(OpCodes.Ldarg_0);
			generator.Emit(OpCodes.Ldc_I4, (int)hint);
			generator.Emit(OpCodes.Callvirt, Methods.WriteByte);
		}

		protected override void EmitWriteDynamicDispatch(ILGenerator gen)
		{
			gen.Emit(OpCodes.Ldarg_2);
			gen.Emit(OpCodes.Ldarg_0);
			gen.Emit(OpCodes.Ldarg_1);
			gen.Emit(OpCodes.Ldarg_3);
			gen.Emit(OpCodes.Call, BinarySerializer2WriteObjectNotNull);
		}

		protected override void EmitBeginWriteField(ILGenerator gen, FieldDescription field)
		{
			
		}

		protected override void EmitEndWriteField(ILGenerator gen, FieldDescription field)
		{
			
		}

		protected override void EmitEndWriteProperty(ILGenerator gen, PropertyDescription property)
		{
			
		}

		protected override void EmitWriteEnum(ILGenerator gen, ITypeDescription typeDescription, Action loadMember, Action loadMemberAddress)
		{
			gen.Emit(OpCodes.Ldarg_0);
			loadMember();

			var storageType = typeDescription.StorageType.Type;
			if (storageType == typeof(byte))
			{
				gen.Emit(OpCodes.Call, BinarySerializer2WriteByte);
			}
			else if (storageType == typeof(sbyte))
			{
				gen.Emit(OpCodes.Call, BinarySerializer2WriteSByte);
			}
			else if (storageType == typeof(short))
			{
				gen.Emit(OpCodes.Call, BinarySerializer2WriteInt16);
			}
			else if (storageType == typeof(ushort))
			{
				gen.Emit(OpCodes.Call, BinarySerializer2WriteUInt16);
			}
			else if (storageType == typeof(int))
			{
				gen.Emit(OpCodes.Call, BinarySerializer2WriteInt32);
			}
			else if (storageType == typeof(uint))
			{
				gen.Emit(OpCodes.Call, BinarySerializer2WriteUInt32);
			}
			else if (storageType == typeof(long))
			{
				gen.Emit(OpCodes.Call, BinarySerializer2WriteInt64);
			}
			else if (storageType == typeof(ulong))
			{
				gen.Emit(OpCodes.Call, BinarySerializer2WriteUInt64);
			}
			else
			{
				throw new NotImplementedException();
			}
		}

		protected override void EmitBeginWriteProperty(ILGenerator gen, PropertyDescription property)
		{
			
		}

		protected override void EmitWriteByte(ILGenerator gen, Action loadMember, Action loadMemberAddress)
		{
			gen.Emit(OpCodes.Ldarg_0);
			loadMember();
			gen.Emit(OpCodes.Call, BinarySerializer2WriteByte);
		}

		protected override void EmitWriteSByte(ILGenerator gen, Action loadMember, Action loadMemberAddress)
		{
			gen.Emit(OpCodes.Ldarg_0);
			loadMember();
			gen.Emit(OpCodes.Call, BinarySerializer2WriteSByte);
		}

		protected override void EmitWriteUInt16(ILGenerator gen, Action loadMember, Action loadMemberAddress)
		{
			gen.Emit(OpCodes.Ldarg_0);
			loadMember();
			gen.Emit(OpCodes.Call, BinarySerializer2WriteUInt16);
		}

		protected override void EmitWriteInt16(ILGenerator gen, Action loadMember, Action loadMemberAddress)
		{
			gen.Emit(OpCodes.Ldarg_0);
			loadMember();
			gen.Emit(OpCodes.Call, BinarySerializer2WriteInt16);
		}

		protected override void EmitWriteUInt32(ILGenerator gen, Action loadMember, Action loadMemberAddress)
		{
			gen.Emit(OpCodes.Ldarg_0);
			loadMember();
			gen.Emit(OpCodes.Call, BinarySerializer2WriteUInt32);
		}

		protected override void EmitWriteInt32(ILGenerator gen, Action loadMember, Action loadMemberAddress)
		{
			gen.Emit(OpCodes.Ldarg_0);
			loadMember();
			gen.Emit(OpCodes.Call, BinarySerializer2WriteInt32);
		}

		protected override void EmitWriteUInt64(ILGenerator gen, Action loadMember, Action loadMemberAddress)
		{
			gen.Emit(OpCodes.Ldarg_0);
			loadMember();
			gen.Emit(OpCodes.Call, BinarySerializer2WriteUInt64);
		}

		protected override void EmitWriteInt64(ILGenerator gen, Action loadMember, Action loadMemberAddress)
		{
			gen.Emit(OpCodes.Ldarg_0);
			loadMember();
			gen.Emit(OpCodes.Call, BinarySerializer2WriteInt64);
		}

		protected override void EmitWriteDecimal(ILGenerator gen, Action loadMember, Action loadMemberAddress)
		{
			gen.Emit(OpCodes.Ldarg_0);
			loadMember();
			gen.Emit(OpCodes.Call, BinarySerializer2WriteDecimal);
		}

		protected override void EmitWriteSingle(ILGenerator gen, Action loadMember, Action loadMemberAddress)
		{
			gen.Emit(OpCodes.Ldarg_0);
			loadMember();
			gen.Emit(OpCodes.Call, BinarySerializer2WriteSingle);
		}

		protected override void EmitWriteDouble(ILGenerator gen, Action loadMember, Action loadMemberAddress)
		{
			gen.Emit(OpCodes.Ldarg_0);
			loadMember();
			gen.Emit(OpCodes.Call, BinarySerializer2WriteDouble);
		}

		protected override void EmitWriteString(ILGenerator gen, Action loadMember, Action loadMemberAddress)
		{
			gen.Emit(OpCodes.Ldarg_0);
			loadMember();
			gen.Emit(OpCodes.Call, BinarySerializer2WriteString);
		}

		protected override void EmitWriteDateTime(ILGenerator gen, Action loadMember, Action loadMemberAddress)
		{
			gen.Emit(OpCodes.Ldarg_0);
			loadMember();
			gen.Emit(OpCodes.Call, BinarySerializer2WriteDateTime);
		}

		protected override void EmitWriteLevel(ILGenerator gen, Action loadMember, Action loadMemberAddress)
		{
			var end = gen.DefineLabel();

			for (int i = 0; i < HardcodedLevels.Count; ++i)
			{
				var next = gen.DefineLabel();

				// if (ReferenceEquals(value, <fld>))
				loadMember();
				gen.Emit(OpCodes.Ldsfld, HardcodedLevels[i].Field);
				gen.Emit(OpCodes.Call, Methods.ObjectReferenceEquals);
				gen.Emit(OpCodes.Brfalse, next);

			//	// writer.WriteByte(<constant>)
				gen.Emit(OpCodes.Ldarg_0);
				gen.Emit(OpCodes.Ldc_I4, i);
				gen.Emit(OpCodes.Call, BinarySerializer2WriteByte);
				gen.Emit(OpCodes.Br, end);

				gen.MarkLabel(next);
			}

			gen.MarkLabel(end);
		}

		protected override void EmitWriteException(ILGenerator gen, Action loadMember, Action loadMemberAddress)
		{
			gen.Emit(OpCodes.Ldarg_0);
			loadMember();
			gen.Emit(OpCodes.Call, BinarySerializer2WriteException);
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