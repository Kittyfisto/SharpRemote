using System;
using System.IO;
using System.Net;
using System.Reflection;
using System.Reflection.Emit;

namespace SharpRemote.CodeGeneration
{
	public static class Methods
	{
		public static readonly MethodInfo ChannelCallRemoteMethod;
		public static readonly MethodInfo ReadDouble;
		public static readonly MethodInfo GrainGetObjectId;
		public static readonly MethodInfo GrainGetSerializer;
		public static readonly ConstructorInfo ObjectCtor;
		public static readonly ConstructorInfo MemoryStreamCtor;
		public static readonly ConstructorInfo BinaryWriterCtor;
		public static readonly ConstructorInfo IPAddressFromBytes;
		public static readonly MethodInfo WriteBytes;
		public static readonly MethodInfo WriteString;
		public static readonly MethodInfo BinaryWriterFlush;
		public static readonly ConstructorInfo BinaryReaderCtor;
		public static readonly MethodInfo WriteDouble;
		public static readonly MethodInfo WriteSingle;
		public static readonly MethodInfo WriteUInt;
		public static readonly MethodInfo WriteInt;
		public static readonly MethodInfo WriteULong;
		public static readonly MethodInfo WriteLong;
		public static readonly MethodInfo WriteUShort;
		public static readonly MethodInfo WriteShort;
		public static readonly MethodInfo WriteByte;
		public static readonly MethodInfo WriteSByte;
		public static readonly MethodInfo WriteBool;
		public static readonly MethodInfo WriteObject;
		public static readonly MethodInfo StreamSetPosition;
		public static readonly MethodInfo ObjectGetType;
		public static readonly MethodInfo TypeGetAssemblyQualifiedName;
		public static readonly FieldInfo StringEmpty;
		public static readonly MethodInfo ServantInvoke;
		public static readonly MethodInfo StringEquality;
		public static readonly MethodInfo ReadBytes;
		public static readonly MethodInfo ReadString;
		public static readonly MethodInfo ReadSingle;
		public static readonly MethodInfo ReadLong;
		public static readonly MethodInfo ReadULong;
		public static readonly MethodInfo ReadInt;
		public static readonly MethodInfo ReadUInt;
		public static readonly MethodInfo ReadShort;
		public static readonly MethodInfo ReadUShort;
		public static readonly MethodInfo ReadSByte;
		public static readonly MethodInfo ReadByte;
		public static readonly MethodInfo ReadBool;
		public static readonly ConstructorInfo NotImplementedCtor;
		public static readonly ConstructorInfo ArgumentExceptionCtor;
		public static readonly MethodInfo ServantGetSubject;
		public static readonly MethodInfo StringFormat;
		public static readonly MethodInfo IPAddressGetAddressBytes;
		public static readonly MethodInfo ArrayGetLength;

		static Methods()
		{
			MemoryStreamCtor = typeof (MemoryStream).GetConstructor(new Type[0]);
			StreamSetPosition = typeof (MemoryStream).GetProperty("Position").GetSetMethod();

			BinaryWriterCtor = typeof(BinaryWriter).GetConstructor(new[] { typeof(Stream) });
			BinaryWriterFlush = typeof(BinaryWriter).GetMethod("Flush");
			BinaryReaderCtor = typeof (BinaryReader).GetConstructor(new[] {typeof (Stream)});

			GrainGetObjectId = typeof(IGrain).GetMethod("get_ObjectId");
			GrainGetSerializer = typeof(IGrain).GetMethod("get_Serializer");
			ServantInvoke = typeof (IServant).GetMethod("Invoke");
			ServantGetSubject = typeof (IServant).GetMethod("get_Subject");

			ObjectCtor = typeof(object).GetConstructor(new Type[0]);
			ChannelCallRemoteMethod = typeof(IEndPointChannel).GetMethod("CallRemoteMethod");

			ReadBytes = typeof (BinaryReader).GetMethod("ReadBytes");
			ReadString = typeof(BinaryReader).GetMethod("ReadString");
			ReadDouble = typeof(BinaryReader).GetMethod("ReadDouble");
			ReadSingle = typeof(BinaryReader).GetMethod("ReadSingle");
			ReadLong = typeof(BinaryReader).GetMethod("ReadInt64");
			ReadULong = typeof(BinaryReader).GetMethod("ReadUInt64");
			ReadInt = typeof(BinaryReader).GetMethod("ReadInt32");
			ReadUInt = typeof(BinaryReader).GetMethod("ReadUInt32");
			ReadShort = typeof(BinaryReader).GetMethod("ReadInt16");
			ReadUShort = typeof(BinaryReader).GetMethod("ReadUInt16");
			ReadSByte = typeof(BinaryReader).GetMethod("ReadSByte");
			ReadByte = typeof(BinaryReader).GetMethod("ReadByte");
			ReadBool = typeof(BinaryReader).GetMethod("ReadBoolean");

			WriteBytes = typeof (BinaryWriter).GetMethod("Write", new[] {typeof (byte[])});
			WriteString = typeof(BinaryWriter).GetMethod("Write", new[]{typeof(string)});
			WriteDouble = typeof(BinaryWriter).GetMethod("Write", new[]{typeof(double)});
			WriteSingle = typeof(BinaryWriter).GetMethod("Write", new[]{typeof(Single)});
			WriteLong = typeof(BinaryWriter).GetMethod("Write", new[]{typeof(long)});
			WriteULong = typeof(BinaryWriter).GetMethod("Write", new[] { typeof(ulong) });
			WriteInt = typeof(BinaryWriter).GetMethod("Write", new[] { typeof(int) });
			WriteUInt = typeof(BinaryWriter).GetMethod("Write", new[] { typeof(uint) });
			WriteShort = typeof(BinaryWriter).GetMethod("Write", new[] { typeof(short) });
			WriteUShort = typeof(BinaryWriter).GetMethod("Write", new[] { typeof(ushort) });
			WriteSByte = typeof(BinaryWriter).GetMethod("Write", new[] { typeof(sbyte) });
			WriteByte = typeof(BinaryWriter).GetMethod("Write", new[] { typeof(byte) });
			WriteBool = typeof(BinaryWriter).GetMethod("Write", new[] { typeof(bool) });
			WriteObject = typeof (ISerializer).GetMethod("WriteObject",
			                                             new[] {typeof (BinaryWriter), typeof (object)});

			ObjectGetType = typeof (object).GetMethod("GetType");
			TypeGetAssemblyQualifiedName = typeof (Type).GetProperty("AssemblyQualifiedName").GetGetMethod();

			StringEmpty = typeof (string).GetField("Empty", BindingFlags.Public | BindingFlags.Static);
			StringEquality = typeof (string).GetMethod("op_Equality", new[] {typeof (string), typeof (string)});
			StringFormat = typeof (string).GetMethod("Format", new[]{typeof(string), typeof(object)});

			NotImplementedCtor = typeof (NotImplementedException).GetConstructor(new Type[0]);
			ArgumentExceptionCtor = typeof (ArgumentException).GetConstructor(new[] {typeof (string)});

			IPAddressGetAddressBytes = typeof (IPAddress).GetMethod("GetAddressBytes");
			IPAddressFromBytes = typeof (IPAddress).GetConstructor(new[] {typeof(byte[])});

			ArrayGetLength = typeof (byte[]).GetProperty("Length").GetMethod;
		}

		public static bool EmitReadPod(this ILGenerator gen, Type valueType)
		{
			return EmitReadPod(gen, () => { }, valueType);
		}

		public static bool EmitReadPod(this ILGenerator gen, Action loadWriter, Type valueType)
		{
			if (valueType == typeof(float))
			{
				loadWriter();
				gen.Emit(OpCodes.Call, ReadSingle);
			}
			else if (valueType == typeof(double))
			{
				loadWriter();
				gen.Emit(OpCodes.Call, ReadDouble);
			}
			else if (valueType == typeof(ulong))
			{
				loadWriter();
				gen.Emit(OpCodes.Call, ReadULong);
			}
			else if (valueType == typeof(long))
			{
				loadWriter();
				gen.Emit(OpCodes.Call, ReadLong);
			}
			else if (valueType == typeof(uint))
			{
				loadWriter();
				gen.Emit(OpCodes.Call, ReadUInt);
			}
			else if (valueType == typeof(int))
			{
				loadWriter();
				gen.Emit(OpCodes.Call, ReadInt);
			}
			else if (valueType == typeof(ushort))
			{
				loadWriter();
				gen.Emit(OpCodes.Call, ReadUShort);
			}
			else if (valueType == typeof(short))
			{
				loadWriter();
				gen.Emit(OpCodes.Call, ReadShort);
			}
			else if (valueType == typeof(sbyte))
			{
				loadWriter();
				gen.Emit(OpCodes.Call, ReadSByte);
			}
			else if (valueType == typeof(byte))
			{
				loadWriter();
				gen.Emit(OpCodes.Call, ReadByte);
			}
			else if (valueType == typeof(string))
			{
				loadWriter();
				gen.Emit(OpCodes.Call, ReadString);
			}
			else if (valueType == typeof (IPAddress))
			{
				// new IPAddress(writer.ReadBytes(writer.ReadInt()));
				loadWriter();
				loadWriter();
				gen.Emit(OpCodes.Call, ReadInt);
				gen.Emit(OpCodes.Call, ReadBytes);
				gen.Emit(OpCodes.Newobj, IPAddressFromBytes);
			}
			else
			{
				return false;
			}

			return true;
		}

		public static bool EmitWritePodToWriter(this ILGenerator gen, Type valueType)
		{
			return EmitWritePodToWriter(gen, () => { }, () => { }, valueType);
		}

		public static bool EmitWritePodToWriter(this ILGenerator gen, Action loadWriter, Action loadValue, Type valueType)
		{
			if (valueType == typeof(float))
			{
				loadWriter();
				loadValue();
				gen.Emit(OpCodes.Call, WriteSingle);
			}
			else if (valueType == typeof(double))
			{
				loadWriter();
				loadValue();
				gen.Emit(OpCodes.Call, WriteDouble);
			}
			else if (valueType == typeof(ulong))
			{
				loadWriter();
				loadValue();
				gen.Emit(OpCodes.Call, WriteULong);
			}
			else if (valueType == typeof(long))
			{
				loadWriter();
				loadValue();
				gen.Emit(OpCodes.Call, WriteLong);
			}
			else if (valueType == typeof(uint))
			{
				loadWriter();
				loadValue();
				gen.Emit(OpCodes.Call, WriteUInt);
			}
			else if (valueType == typeof(int))
			{
				loadWriter();
				loadValue();
				gen.Emit(OpCodes.Call, WriteInt);
			}
			else if (valueType == typeof(short))
			{
				loadWriter();
				loadValue();
				gen.Emit(OpCodes.Call, WriteShort);
			}
			else if (valueType == typeof(ushort))
			{
				loadWriter();
				loadValue();
				gen.Emit(OpCodes.Call, WriteUShort);
			}
			else if (valueType == typeof(sbyte))
			{
				loadWriter();
				loadValue();
				gen.Emit(OpCodes.Call, WriteSByte);
			}
			else if (valueType == typeof(byte))
			{
				loadWriter();
				loadValue();
				gen.Emit(OpCodes.Call, WriteByte);
			}
			else if (valueType == typeof (string))
			{
				loadWriter();
				loadValue();
				gen.Emit(OpCodes.Call, WriteString);
			}
			else if (valueType == typeof(IPAddress))
			{
				var data = gen.DeclareLocal(typeof(byte[]));

				loadValue();
				gen.Emit(OpCodes.Call, IPAddressGetAddressBytes);
				gen.Emit(OpCodes.Stloc, data);

				loadWriter();
				gen.Emit(OpCodes.Ldloc, data);
				gen.Emit(OpCodes.Call, ArrayGetLength);

				gen.Emit(OpCodes.Call, WriteInt);

				loadWriter();
				gen.Emit(OpCodes.Ldloc, data);
				gen.Emit(OpCodes.Call, WriteBytes);
			}
			else
			{
				return false;
			}

			return true;
		}
	}
}