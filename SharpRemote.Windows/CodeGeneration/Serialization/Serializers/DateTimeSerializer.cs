using System;
using System.Reflection;
using System.Reflection.Emit;

namespace SharpRemote.CodeGeneration.Serialization.Serializers
{
	internal sealed class DateTimeSerializer
		: AbstractTypeSerializer
	{
		private readonly ConstructorInfo _ctor;
		private readonly MethodInfo _getTicks;
		private readonly MethodInfo _getKind;

		public DateTimeSerializer()
		{
			_ctor = typeof (DateTime).GetConstructor(new[] {typeof (long), typeof (DateTimeKind)});
			_getTicks = typeof (DateTime).GetProperty("Ticks").GetMethod;
			_getKind = typeof (DateTime).GetProperty("Kind").GetMethod;
		}

		public override bool Supports(Type type)
		{
			return type == typeof (DateTime);
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

			loadWriter();
			loadValueAddress();
			gen.Emit(OpCodes.Call, _getKind);
			gen.Emit(OpCodes.Conv_I1);
			gen.Emit(OpCodes.Call, Methods.WriteByte);
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

			loadReader();
			gen.Emit(OpCodes.Call, Methods.ReadByte);
			gen.Emit(OpCodes.Conv_I4);

			gen.Emit(OpCodes.Newobj, _ctor);
		}
	}
}