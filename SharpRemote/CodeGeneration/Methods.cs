using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using SharpRemote.Tasks;

namespace SharpRemote.CodeGeneration
{
	internal static class Methods
	{
		public static readonly MethodInfo ChannelCallRemoteMethod;
		public static readonly MethodInfo ChannelCallRemoteAsyncMethod;
		public static readonly MethodInfo ReadDouble;
		public static readonly MethodInfo GrainGetObjectId;
		public static readonly MethodInfo GrainGetSerializer;
		public static readonly MethodInfo GrainGetEndPoint;
		public static readonly ConstructorInfo ObjectCtor;
		public static readonly ConstructorInfo MemoryStreamCtor;
		public static readonly ConstructorInfo BinaryWriterCtor;
		public static readonly MethodInfo ObjectReferenceEquals;
		public static readonly MethodInfo WriteBytes;
		public static readonly MethodInfo WriteString;
		public static readonly MethodInfo BinaryWriterFlush;
		public static readonly ConstructorInfo BinaryReaderCtor;
		public static readonly MethodInfo WriteDecimal;
		public static readonly MethodInfo WriteDouble;
		public static readonly MethodInfo WriteSingle;
		public static readonly MethodInfo WriteUInt;
		public static readonly MethodInfo WriteInt32;
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
		public static readonly MethodInfo GrainInvoke;
		public static readonly MethodInfo GrainGetTaskScheduler;
		public static readonly MethodInfo GrainGetInterfaceType;
		public static readonly MethodInfo StringEquality;
		public static readonly MethodInfo ReadBytes;
		public static readonly MethodInfo ReadString;
		public static readonly MethodInfo ReadDecimal;
		public static readonly MethodInfo ReadSingle;
		public static readonly MethodInfo ReadLong;
		public static readonly MethodInfo ReadULong;
		public static readonly MethodInfo ReadInt32;
		public static readonly MethodInfo ReadUInt;
		public static readonly MethodInfo ReadShort;
		public static readonly MethodInfo ReadUShort;
		public static readonly MethodInfo ReadSByte;
		public static readonly MethodInfo ReadByte;
		public static readonly MethodInfo ReadBool;
		public static readonly ConstructorInfo NotImplementedCtor;
		public static readonly ConstructorInfo ArgumentExceptionCtor;
		public static readonly ConstructorInfo NotSupportedExceptionCtor;
		public static readonly MethodInfo ServantGetSubject;
		public static readonly MethodInfo StringFormatOneObject;
		public static readonly MethodInfo ArrayGetLength;
		public static readonly MethodInfo DelegateCombine;
		public static readonly MethodInfo DelegateRemove;
		public static readonly MethodInfo InterlockedCompareExchangeGeneric;
		public static readonly MethodInfo SerializerWriteObject;
		public static readonly MethodInfo SerializerReadObject;
		public static readonly MethodInfo SerializerGetType;
		public static readonly MethodInfo CreateTypeFromName;
		public static readonly MethodInfo RemotingEndPointGetOrCreateServant;
		public static readonly MethodInfo RemotingEndPointGetOrCreateProxy;
		public static readonly MethodInfo RemotingEndPointTryGetProxy;
		public static readonly MethodInfo TaskGetFactory;
		public static readonly ConstructorInfo ActionIntPtrCtor;
		public static readonly ConstructorInfo ActionObjectIntPtrCtor;
		public static readonly ConstructorInfo ActionTaskOfMemoryStreamIntPtrCtor;
		public static readonly MethodInfo TaskFactoryStartNew;
		public static readonly MethodInfo TaskFactoryStartNewObject;
		public static readonly MethodInfo TaskWait;
		public static readonly MethodInfo TaskGetStatus;
		public static readonly MethodInfo TaskGetIsFaulted;
		public static readonly MethodInfo TaskGetException;
		public static readonly MethodInfo TaskSchedulerGetCurrent;
		public static readonly MethodInfo TaskSchedulerGetDefault;
		public static readonly MethodInfo TaskMemoryStreamContinueWith;
		public static readonly MethodInfo TaskMemoryStreamGetResult;
		public static readonly MethodInfo TaskExTimeoutAfter;
		public static readonly MethodInfo AggregateExceptionGetInnerExceptions;
		public static readonly MethodInfo StringFormat3Objects;
		public static readonly MethodInfo TypeGetTypeFromHandle;
		public static readonly ConstructorInfo SerialTaskSchedulerCtor;
		public static readonly MethodInfo DebuggerNotifyOfCrossThreadDependency;
		public static readonly ConstructorInfo NullableUInt64Ctor;
		public static readonly ConstructorInfo NoSuchServantExceptionCtor;

		static Methods()
		{
			MemoryStreamCtor = typeof (MemoryStream).GetConstructor(new Type[0]);
			StreamSetPosition = typeof (MemoryStream).GetProperty("Position").GetSetMethod();

			BinaryWriterCtor = typeof(BinaryWriter).GetConstructor(new[] { typeof(Stream) });
			BinaryWriterFlush = typeof(BinaryWriter).GetMethod("Flush");
			BinaryReaderCtor = typeof (BinaryReader).GetConstructor(new[] {typeof (Stream)});

			GrainGetObjectId = typeof(IGrain).GetMethod("get_ObjectId");
			GrainGetSerializer = typeof(IGrain).GetMethod("get_Serializer");
			GrainGetEndPoint = typeof(IGrain).GetMethod("get_EndPoint");
			ServantGetSubject = typeof (IServant).GetMethod("get_Subject");
			GrainInvoke = typeof (IGrain).GetMethod("Invoke");
			GrainGetInterfaceType = typeof (IGrain).GetMethod("get_InterfaceType");
			GrainGetTaskScheduler = typeof (IGrain).GetMethod("GetTaskScheduler");

			ObjectCtor = typeof(object).GetConstructor(new Type[0]);
			ChannelCallRemoteMethod = typeof(IEndPointChannel).GetMethod("CallRemoteMethod");
			ChannelCallRemoteAsyncMethod = typeof (IEndPointChannel).GetMethod("CallRemoteMethodAsync");

			ObjectReferenceEquals = typeof(object).GetMethod(nameof(ReferenceEquals));

			ReadBytes = typeof (BinaryReader).GetMethod("ReadBytes");
			ReadString = typeof(BinaryReader).GetMethod("ReadString");
			ReadDecimal = typeof(BinaryReader).GetMethod("ReadDecimal");
			ReadDouble = typeof(BinaryReader).GetMethod("ReadDouble");
			ReadSingle = typeof(BinaryReader).GetMethod("ReadSingle");
			ReadLong = typeof(BinaryReader).GetMethod("ReadInt64");
			ReadULong = typeof(BinaryReader).GetMethod("ReadUInt64");
			ReadInt32 = typeof(BinaryReader).GetMethod("ReadInt32");
			ReadUInt = typeof(BinaryReader).GetMethod("ReadUInt32");
			ReadShort = typeof(BinaryReader).GetMethod("ReadInt16");
			ReadUShort = typeof(BinaryReader).GetMethod("ReadUInt16");
			ReadSByte = typeof(BinaryReader).GetMethod("ReadSByte");
			ReadByte = typeof(BinaryReader).GetMethod("ReadByte");
			ReadBool = typeof(BinaryReader).GetMethod("ReadBoolean");

			WriteBytes = typeof (BinaryWriter).GetMethod("Write", new[] {typeof (byte[])});
			WriteString = typeof(BinaryWriter).GetMethod("Write", new[]{typeof(string)});
			WriteDecimal = typeof(BinaryWriter).GetMethod("Write", new[] { typeof(decimal) });
			WriteDouble = typeof(BinaryWriter).GetMethod("Write", new[]{typeof(double)});
			WriteSingle = typeof(BinaryWriter).GetMethod("Write", new[]{typeof(Single)});
			WriteLong = typeof(BinaryWriter).GetMethod("Write", new[]{typeof(long)});
			WriteULong = typeof(BinaryWriter).GetMethod("Write", new[] { typeof(ulong) });
			WriteInt32 = typeof(BinaryWriter).GetMethod("Write", new[] { typeof(int) });
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
			StringFormatOneObject = typeof (string).GetMethod("Format", new[]{typeof(string), typeof(object)});

			NotImplementedCtor = typeof (NotImplementedException).GetConstructor(new Type[0]);
			ArgumentExceptionCtor = typeof (ArgumentException).GetConstructor(new[] {typeof (string)});
			NotSupportedExceptionCtor = typeof (NotSupportedException).GetConstructor(new[] {typeof (string)});

			ArrayGetLength = typeof (byte[]).GetProperty("Length").GetMethod;

			DelegateCombine = typeof (Delegate).GetMethod("Combine", new[]{typeof(Delegate), typeof(Delegate)});
			DelegateRemove = typeof (Delegate).GetMethod("Remove", new[] {typeof (Delegate), typeof (Delegate)});

			InterlockedCompareExchangeGeneric =
				typeof (Interlocked).GetMethods().First(x => x.Name == "CompareExchange" && x.IsGenericMethod);

			SerializerWriteObject = typeof (ISerializer).GetMethod("WriteObject", new[]{typeof(BinaryWriter), typeof(object), typeof(IRemotingEndPoint)});
			SerializerReadObject = typeof(ISerializer).GetMethod("ReadObject", new[] { typeof(BinaryReader), typeof(IRemotingEndPoint) });
			SerializerGetType = typeof(ISerializer).GetMethod("GetType");

			RemotingEndPointGetOrCreateServant = typeof (IRemotingEndPoint).GetMethod("GetExistingOrCreateNewServant");
			RemotingEndPointGetOrCreateProxy = typeof (IRemotingEndPoint).GetMethod("GetExistingOrCreateNewProxy");
			RemotingEndPointTryGetProxy = typeof(IRemotingEndPoint).GetMethod("TryGetProxy");

			CreateTypeFromName = typeof(TypeResolver).GetMethod("GetType", new[] { typeof(string) });

			TaskGetFactory = typeof (Task).GetProperty("Factory").GetMethod;
			ActionIntPtrCtor = typeof (Action).GetConstructor(new[] {typeof(object), typeof(IntPtr)});
			ActionObjectIntPtrCtor = typeof (Action<object>).GetConstructor(new[] {typeof(object), typeof(IntPtr)});
			ActionTaskOfMemoryStreamIntPtrCtor = typeof(Action<Task<MemoryStream>>).GetConstructor(new[] { typeof(object), typeof(IntPtr) });
			TaskFactoryStartNew = typeof (TaskFactory).GetMethod("StartNew", new[]
			{
				typeof(Action)
			});
			TaskFactoryStartNewObject = typeof (TaskFactory).GetMethod("StartNew", new[]
				{
					typeof(Action<object>),
					typeof(object)
				});

			TaskWait = typeof (Task).GetMethod("Wait", new Type[0]);
			TaskGetStatus = typeof (Task).GetProperty("Status").GetMethod;
			TaskGetIsFaulted = typeof (Task).GetProperty("IsFaulted").GetMethod;
			TaskGetException = typeof (Task).GetProperty("Exception").GetMethod;
			TaskMemoryStreamContinueWith = typeof (Task<MemoryStream>).GetMethod("ContinueWith",
			                                                                     new[]
				                                                                     {
					                                                                     typeof (Action<Task<MemoryStream>>)
				                                                                     });
			TaskMemoryStreamGetResult = typeof (Task<MemoryStream>).GetProperty("Result").GetMethod;

			TaskSchedulerGetCurrent = typeof (TaskScheduler).GetProperty("Current").GetMethod;
			TaskSchedulerGetDefault = typeof (TaskScheduler).GetProperty("Default").GetMethod;

			TaskExTimeoutAfter = typeof(TaskEx).GetMethod(nameof(TaskEx.TimeoutAfter),
			                                              new[] {typeof(Task), typeof(TimeSpan)});

			AggregateExceptionGetInnerExceptions = typeof(AggregateException)
			                                       .GetProperty(nameof(AggregateException.InnerExceptions))
			                                       .GetMethod;

			StringFormat3Objects = typeof (string).GetMethod("Format", new[] {typeof(string), typeof (object), typeof (object), typeof (object)});

			TypeGetTypeFromHandle = typeof (Type).GetMethod("GetTypeFromHandle");

			SerialTaskSchedulerCtor = typeof (SerialTaskScheduler).GetConstructor(new[]
				{
					typeof(string),
					typeof(string),
					typeof(long?),
					typeof (bool)
				});

			DebuggerNotifyOfCrossThreadDependency = typeof (Debugger).GetMethod("NotifyOfCrossThreadDependency");

			NullableUInt64Ctor = typeof (ulong?).GetConstructors().First();

			NoSuchServantExceptionCtor = typeof (NoSuchServantException).GetConstructor(new[] {typeof (ulong)});
		}
	}
}