using System;
using System.Linq;
using System.Reflection.Emit;
using SharpRemote.CodeGeneration;

// ReSharper disable CheckNamespace
namespace SharpRemote
// ReSharper restore CheckNamespace
{
	internal partial class Serializer
	{
		private bool EmitReadNativeType(ILGenerator gen,
			Action loadReader,
			Action loadSerializer,
			Action loadRemotingEndPoint,
			Type valueType,
			bool valueCanBeNull = true)
		{
			if (valueType == typeof(bool))
			{
				loadReader();
				gen.Emit(OpCodes.Call, Methods.ReadBool);
			}
			else if (valueType == typeof(float))
			{
				loadReader();
				gen.Emit(OpCodes.Call, Methods.ReadSingle);
			}
			else if (valueType == typeof(double))
			{
				loadReader();
				gen.Emit(OpCodes.Call, Methods.ReadDouble);
			}
			else if (valueType == typeof(ulong))
			{
				loadReader();
				gen.Emit(OpCodes.Call, Methods.ReadULong);
			}
			else if (valueType == typeof(long))
			{
				loadReader();
				gen.Emit(OpCodes.Call, Methods.ReadLong);
			}
			else if (valueType == typeof(uint))
			{
				loadReader();
				gen.Emit(OpCodes.Call, Methods.ReadUInt);
			}
			else if (valueType == typeof(ushort))
			{
				loadReader();
				gen.Emit(OpCodes.Call, Methods.ReadUShort);
			}
			else if (valueType == typeof(short))
			{
				loadReader();
				gen.Emit(OpCodes.Call, Methods.ReadShort);
			}
			else if (valueType == typeof(sbyte))
			{
				loadReader();
				gen.Emit(OpCodes.Call, Methods.ReadSByte);
			}
			else if (valueType == typeof(byte))
			{
				loadReader();
				gen.Emit(OpCodes.Call, Methods.ReadByte);
			}
			else
			{
				var customSerializer = _customSerializers.FirstOrDefault(x => x.Supports(valueType));
				if (customSerializer == null)
					return false;

				customSerializer.EmitReadValue(gen,
					this,
					loadReader,
					loadSerializer,
					loadRemotingEndPoint,
					valueType,
					valueCanBeNull);
			}

			return true;
		}

		private bool EmitWriteNativeType(ILGenerator gen,
			Action loadWriter,
			Action loadValue,
			Action loadValueAddress,
			Action loadSerializer,
			Action loadRemotingEndPoint,
			Type valueType,
			bool valueCanBeNull = true)
		{
			if (valueType == typeof(bool))
			{
				loadWriter();
				loadValue();
				gen.Emit(OpCodes.Call, Methods.WriteBool);
			}
			else if (valueType == typeof(float))
			{
				loadWriter();
				loadValue();
				gen.Emit(OpCodes.Call, Methods.WriteSingle);
			}
			else if (valueType == typeof(double))
			{
				loadWriter();
				loadValue();
				gen.Emit(OpCodes.Call, Methods.WriteDouble);
			}
			else if (valueType == typeof(ulong))
			{
				loadWriter();
				loadValue();
				gen.Emit(OpCodes.Call, Methods.WriteULong);
			}
			else if (valueType == typeof(long))
			{
				loadWriter();
				loadValue();
				gen.Emit(OpCodes.Call, Methods.WriteLong);
			}
			else if (valueType == typeof(uint))
			{
				loadWriter();
				loadValue();
				gen.Emit(OpCodes.Call, Methods.WriteUInt);
			}
			else if (valueType == typeof(short))
			{
				loadWriter();
				loadValue();
				gen.Emit(OpCodes.Call, Methods.WriteShort);
			}
			else if (valueType == typeof(ushort))
			{
				loadWriter();
				loadValue();
				gen.Emit(OpCodes.Call, Methods.WriteUShort);
			}
			else if (valueType == typeof(sbyte))
			{
				loadWriter();
				loadValue();
				gen.Emit(OpCodes.Call, Methods.WriteSByte);
			}
			else if (valueType == typeof(byte))
			{
				loadWriter();
				loadValue();
				gen.Emit(OpCodes.Call, Methods.WriteByte);
			}
			else
			{
				var customSerializer = _customSerializers.FirstOrDefault(x => x.Supports(valueType));
				if (customSerializer == null)
					return false;

				customSerializer.EmitWriteValue(gen,
					this,
					loadWriter,
					loadValue,
					loadValueAddress,
					loadSerializer,
					loadRemotingEndPoint,
					valueType,
					valueCanBeNull);
			}

			return true;
		}
	}
}