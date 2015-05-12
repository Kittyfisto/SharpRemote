using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;

namespace SharpRemote.CodeGeneration.Serialization.Serializers
{
	internal sealed class KeyValuePairSerializer
		: AbstractTypeSerializer
	{
		#region Public Methods

		public override void EmitReadValue(ILGenerator gen, Serializer serializerCompiler, Action loadReader,
			Action loadSerializer, Type type, bool valueCanBeNull = true)
		{
			Type keyType = type.GenericTypeArguments[0];
			Type valueType = type.GenericTypeArguments[1];
			ConstructorInfo ctor = type.GetConstructor(new[] {keyType, valueType});

			serializerCompiler.EmitReadValue(gen,
				loadReader,
				loadSerializer,
				keyType);
			serializerCompiler.EmitReadValue(gen,
				loadReader,
				loadSerializer,
				valueType);
			gen.Emit(OpCodes.Newobj, ctor);
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
			Type keyType = type.GenericTypeArguments[0];
			Type valueType = type.GenericTypeArguments[1];
			MethodInfo getKey = type.GetProperty("Key").GetMethod;
			MethodInfo getValue = type.GetProperty("Value").GetMethod;

			Action loadKeyValue = () =>
			{
				loadValueAddress();
				gen.Emit(OpCodes.Call, getKey);
			};
			LocalBuilder key = null;
			Action loadKeyValueAddress = () =>
			{
				if (key == null)
				{
					key = gen.DeclareLocal(keyType);
					loadValue();
					gen.Emit(OpCodes.Stloc, key);
				}

				gen.Emit(OpCodes.Ldloca, key);
			};

			serializerCompiler.EmitWriteValue(gen,
				loadWriter,
				loadKeyValue,
				loadKeyValueAddress,
				loadSerializer,
				keyType
				);

			Action loadValueValue = () =>
			{
				loadValueAddress();
				gen.Emit(OpCodes.Call, getValue);
			};
			LocalBuilder value = null;
			Action loadValueValueAddress = () =>
			{
				if (value == null)
				{
					value = gen.DeclareLocal(valueType);
					loadValueValue();
					gen.Emit(OpCodes.Stloc, value);
				}
				gen.Emit(OpCodes.Ldloca, value);
			};

			serializerCompiler.EmitWriteValue(gen,
				loadWriter,
				loadValueValue,
				loadValueValueAddress,
				loadSerializer,
				valueType);
		}

		public override bool Supports(Type type)
		{
			return type.IsGenericType &&
				type.GetGenericTypeDefinition() == typeof (KeyValuePair<,>);
		}

		#endregion
	}
}