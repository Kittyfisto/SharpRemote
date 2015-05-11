using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Reflection.Emit;
using System.Threading;
using SharpRemote.CodeGeneration.Serialization;
using SharpRemote.CodeGeneration.Serialization.Serializers;

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
		public static readonly FieldInfo StringEmpty;
		public static readonly MethodInfo ServantInvokeMethod;
		public static readonly MethodInfo ProxyInvokeEvent;
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
		public static readonly MethodInfo ArrayGetLength;
		public static readonly MethodInfo DelegateCombine;
		public static readonly MethodInfo DelegateRemove;
		public static readonly MethodInfo InterlockedCompareExchangeGeneric;
		public static readonly MethodInfo SerializerWriteObject;
		public static readonly MethodInfo SerializerReadObject;

		private static readonly Dictionary<Type, ITypeSerializer> Serializers;

		static Methods()
		{
			MemoryStreamCtor = typeof (MemoryStream).GetConstructor(new Type[0]);
			StreamSetPosition = typeof (MemoryStream).GetProperty("Position").GetSetMethod();

			BinaryWriterCtor = typeof(BinaryWriter).GetConstructor(new[] { typeof(Stream) });
			BinaryWriterFlush = typeof(BinaryWriter).GetMethod("Flush");
			BinaryReaderCtor = typeof (BinaryReader).GetConstructor(new[] {typeof (Stream)});

			GrainGetObjectId = typeof(IGrain).GetMethod("get_ObjectId");
			GrainGetSerializer = typeof(IGrain).GetMethod("get_Serializer");
			ServantInvokeMethod = typeof (IServant).GetMethod("InvokeMethod");
			ServantGetSubject = typeof (IServant).GetMethod("get_Subject");
			ProxyInvokeEvent = typeof (IProxy).GetMethod("InvokeEvent");

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

			StringEmpty = typeof (string).GetField("Empty", BindingFlags.Public | BindingFlags.Static);
			StringEquality = typeof (string).GetMethod("op_Equality", new[] {typeof (string), typeof (string)});
			StringFormat = typeof (string).GetMethod("Format", new[]{typeof(string), typeof(object)});

			NotImplementedCtor = typeof (NotImplementedException).GetConstructor(new Type[0]);
			ArgumentExceptionCtor = typeof (ArgumentException).GetConstructor(new[] {typeof (string)});

			ArrayGetLength = typeof (byte[]).GetProperty("Length").GetMethod;

			DelegateCombine = typeof (Delegate).GetMethod("Combine", new[]{typeof(Delegate), typeof(Delegate)});
			DelegateRemove = typeof (Delegate).GetMethod("Remove", new[] {typeof (Delegate), typeof (Delegate)});

			InterlockedCompareExchangeGeneric =
				typeof (Interlocked).GetMethods().First(x => x.Name == "CompareExchange" && x.IsGenericMethod);

			SerializerWriteObject = typeof (ISerializer).GetMethod("WriteObject", new[]{typeof(BinaryWriter), typeof(object)});
			SerializerReadObject = typeof (ISerializer).GetMethod("ReadObject", new[] {typeof (BinaryReader)});

			Serializers = new Dictionary<Type, ITypeSerializer>
				{
					{typeof (Int32), new Int32Serializer()},
					{typeof (IPEndPoint), new IPEndPointSerializer()},
					{typeof (IPAddress), new IPAddressSerializer()},
					{typeof (Type), new TypeSerializer()},
					{typeof (string), new StringSerializer()},
					{typeof(byte[]), new ByteArraySerializer()}
				};
		}

		[Pure]
		public static bool HasSerializer(Type type)
		{
			return Serializers.ContainsKey(type);
		}

		public static bool EmitReadNativeType(this ILGenerator gen, Action loadReader, Type valueType, bool valueCanBeNull = true)
		{
			if (valueType == typeof (bool))
			{
				loadReader();
				gen.Emit(OpCodes.Call, ReadBool);
			}
			else if (valueType == typeof(float))
			{
				loadReader();
				gen.Emit(OpCodes.Call, ReadSingle);
			}
			else if (valueType == typeof(double))
			{
				loadReader();
				gen.Emit(OpCodes.Call, ReadDouble);
			}
			else if (valueType == typeof(ulong))
			{
				loadReader();
				gen.Emit(OpCodes.Call, ReadULong);
			}
			else if (valueType == typeof(long))
			{
				loadReader();
				gen.Emit(OpCodes.Call, ReadLong);
			}
			else if (valueType == typeof(uint))
			{
				loadReader();
				gen.Emit(OpCodes.Call, ReadUInt);
			}
			else if (valueType == typeof(ushort))
			{
				loadReader();
				gen.Emit(OpCodes.Call, ReadUShort);
			}
			else if (valueType == typeof(short))
			{
				loadReader();
				gen.Emit(OpCodes.Call, ReadShort);
			}
			else if (valueType == typeof(sbyte))
			{
				loadReader();
				gen.Emit(OpCodes.Call, ReadSByte);
			}
			else if (valueType == typeof(byte))
			{
				loadReader();
				gen.Emit(OpCodes.Call, ReadByte);
			}
			else
			{
				ITypeSerializer serializer;
				if (!Serializers.TryGetValue(valueType, out serializer))
					return false;

				serializer.EmitReadValue(gen, loadReader, valueCanBeNull);
			}

			return true;
		}

		public static bool EmitWriteNativeType(this ILGenerator gen, Action loadWriter, Action loadValue, Type valueType, bool valueCanBeNull = true)
		{
			if (valueType == typeof (bool))
			{
				loadWriter();
				loadValue();
				gen.Emit(OpCodes.Call, WriteBool);
			}
			else if (valueType == typeof(float))
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
			else
			{
				ITypeSerializer serializer;
				if (!Serializers.TryGetValue(valueType, out serializer))
					return false;

				serializer.EmitWriteValue(gen, loadWriter, loadValue, valueCanBeNull);
			}

			return true;
		}
	}
}