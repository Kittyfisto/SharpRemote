using System;
using System.Reflection;
using System.Reflection.Emit;

namespace SharpRemote.CodeGeneration.Serialization.Binary.Serializers
{
	internal sealed class TimeSpanSerializer
		: AbstractTypeSerializer
	{
		private readonly MethodInfo _getTicks;
		private readonly ConstructorInfo _ctor;

		public TimeSpanSerializer()
		{
			_getTicks = typeof(TimeSpan).GetProperty("Ticks").GetMethod;
			_ctor = typeof(TimeSpan).GetConstructor(new[] { typeof(long) });
		}

		public override bool Supports(Type type)
		{
			return type == typeof (TimeSpan);
		}

		public override void EmitWriteValue(ILGenerator gen,
		                                    BinarySerializer binarySerializerCompiler,
		                                    Action loadWriter,
		                                    Action loadValue,
		                                    Action loadValueAddress,
		                                    Action loadSerializer,
		                                    Action loadRemotingEndPoint,
		                                    Type type,
		                                    bool valueCanBeNull = true)
		{
			loadWriter();
			loadValueAddress();
			gen.Emit(OpCodes.Call, _getTicks);
			gen.Emit(OpCodes.Call, Methods.WriteLong);
		}

		public override void EmitReadValue(ILGenerator gen,
		                                   BinarySerializer binarySerializerCompiler,
		                                   Action loadReader,
		                                   Action loadSerializer,
		                                   Action loadRemotingEndPoint,
		                                   Type type,
		                                   bool valueCanBeNull = true)
		{
			loadReader();
			gen.Emit(OpCodes.Call, Methods.ReadLong);
			gen.Emit(OpCodes.Newobj, _ctor);
		}
	}
}