using System;
using System.Reflection;
using System.Reflection.Emit;

namespace SharpRemote.CodeGeneration.Serialization.Serializers
{
	public sealed class TimeSpanSerializer
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
		                                    Serializer serializerCompiler,
		                                    Action loadWriter,
		                                    Action loadValue,
		                                    Action loadValueAddress,
		                                    Action loadSerializer,
		                                    Type type,
		                                    bool valueCanBeNull = true)
		{
			loadWriter();
			loadValueAddress();
			gen.Emit(OpCodes.Call, _getTicks);
			gen.Emit(OpCodes.Call, Methods.WriteLong);
		}

		public override void EmitReadValue(ILGenerator gen,
		                                   Serializer serializerCompiler,
		                                   Action loadReader,
		                                   Action loadSerializer,
		                                   Type type,
		                                   bool valueCanBeNull = true)
		{
			loadReader();
			gen.Emit(OpCodes.Call, Methods.ReadLong);
			gen.Emit(OpCodes.Newobj, _ctor);
		}
	}
}