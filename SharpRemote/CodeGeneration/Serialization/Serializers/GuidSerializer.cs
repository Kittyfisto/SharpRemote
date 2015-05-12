using System;
using System.Reflection;
using System.Reflection.Emit;

namespace SharpRemote.CodeGeneration.Serialization.Serializers
{
	internal sealed class GuidSerializer
		: AbstractTypeSerializer
	{
		private readonly ConstructorInfo _ctor;
		private readonly MethodInfo _toByteArray;

		public GuidSerializer()
		{
			_ctor = typeof (Guid).GetConstructor(new[] {typeof (byte[])});
			_toByteArray = typeof (Guid).GetMethod("ToByteArray");
		}

		public override bool Supports(Type type)
		{
			return type == typeof (Guid);
		}

		public override void EmitWriteValue(ILGenerator gen, Serializer serializerCompiler, Action loadWriter,
		                                    Action loadValue,
		                                    Action loadValueAddress, Action loadSerializer, Type type,
		                                    bool valueCanBeNull = true)
		{
			loadWriter();
			loadValueAddress();
			gen.Emit(OpCodes.Call, _toByteArray);
			gen.Emit(OpCodes.Call, Methods.WriteBytes);
		}

		public override void EmitReadValue(ILGenerator gen, Serializer serializerCompiler, Action loadReader,
		                                   Action loadSerializer, Type type,
		                                   bool valueCanBeNull = true)
		{
			loadReader();
			gen.Emit(OpCodes.Ldc_I4, 16);
			gen.Emit(OpCodes.Call, Methods.ReadBytes);
			gen.Emit(OpCodes.Newobj, _ctor);
		}
	}
}