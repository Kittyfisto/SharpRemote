using System;
using System.Reflection.Emit;

namespace SharpRemote.CodeGeneration.Serialization.Serializers
{
	internal sealed class NullableSerializer
		: AbstractTypeSerializer
	{
		public override bool Supports(Type type)
		{
			return type.IsGenericType && type.GetGenericTypeDefinition() == typeof (Nullable<>);
		}

		public override void EmitWriteValue(ILGenerator gen, Serializer serializerCompiler, Action loadWriter,
		                                    Action loadValue,
		                                    Action loadValueAddress, Action loadSerializer, Type type,
		                                    bool valueCanBeNull = true)
		{
			var valueType = type.GenericTypeArguments[0];
			var getHasValue = type.GetProperty("HasValue").GetMethod;
			var getValue = type.GetProperty("Value").GetMethod;

			var hasValue = gen.DeclareLocal(typeof (bool));
			var end = gen.DefineLabel();

			loadWriter();
			loadValueAddress();
			gen.Emit(OpCodes.Call, getHasValue);
			gen.Emit(OpCodes.Stloc, hasValue);

			gen.Emit(OpCodes.Ldloc, hasValue);
			gen.Emit(OpCodes.Call, Methods.WriteBool);

			gen.Emit(OpCodes.Ldloc, hasValue);
			gen.Emit(OpCodes.Brfalse, end);

			LocalBuilder value = null;
			serializerCompiler.EmitWriteValue(gen,
			                                  loadWriter,
			                                  () =>
				                                  {
					                                  loadValueAddress();
					                                  gen.Emit(OpCodes.Call, getValue);
				                                  },
			                                  () =>
				                                  {
					                                  if (value == null)
					                                  {
						                                  value = gen.DeclareLocal(valueType);
						                                  loadValueAddress();
						                                  gen.Emit(OpCodes.Call, getValue);
						                                  gen.Emit(OpCodes.Stloc, value);
					                                  }

					                                  gen.Emit(OpCodes.Ldloc, value);
				                                  },
			                                  loadSerializer,
			                                  valueType);

			gen.MarkLabel(end);
		}

		public override void EmitReadValue(ILGenerator gen, Serializer serializerCompiler, Action loadReader,
		                                   Action loadSerializer, Type type,
		                                   bool valueCanBeNull = true)
		{
			var valueType = type.GenericTypeArguments[0];
			var ctor = type.GetConstructor(new[] { valueType });

			var end = gen.DefineLabel();
			var noValue = gen.DefineLabel();

			loadReader();
			gen.Emit(OpCodes.Call, Methods.ReadBool);
			gen.Emit(OpCodes.Brfalse, noValue);

			serializerCompiler.EmitReadValue(gen,
			                                 loadReader,
			                                 loadSerializer,
			                                 valueType);
			gen.Emit(OpCodes.Newobj, ctor);

			gen.Emit(OpCodes.Br_S, end);

			gen.MarkLabel(noValue);

			var @null = gen.DeclareLocal(type);
			gen.Emit(OpCodes.Ldloca, @null);
			gen.Emit(OpCodes.Initobj, type);
			gen.Emit(OpCodes.Ldloc, @null);

			gen.MarkLabel(end);
		}
	}
}