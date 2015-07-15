using System;
using System.Reflection;
using System.Reflection.Emit;

namespace SharpRemote.CodeGeneration.Serialization.Serializers
{
	/// <summary>
	/// Takes care of serializing / deserializing Type values.
	/// Serialization simply writes the type's assembly qualified name, e.g. "System.Int32, mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089"
	/// and deserialization calls <see cref="TypeResolver.GetType()"/> or a user specified method.
	/// </summary>
	internal sealed class TypeSerializer
		: AbstractTypeSerializer
	{
		public static readonly MethodInfo GetAssemblyQualifiedName;

		static TypeSerializer()
		{
			GetAssemblyQualifiedName = typeof(Type).GetProperty("AssemblyQualifiedName").GetGetMethod();
		}

		public override bool Supports(Type type)
		{
			return type == typeof (Type);
		}

		public override void EmitWriteValue(ILGenerator gen, Serializer serializerCompiler, Action loadWriter, Action loadValue, Action loadValueAddress, Action loadSerializer, Type type, bool valueCanBeNull = true)
		{
			EmitWriteNullableValue(
				gen,
				loadWriter,
				loadValue,
				() =>
				{
					loadWriter();
					loadValue();
					gen.Emit(OpCodes.Callvirt, GetAssemblyQualifiedName);
					gen.Emit(OpCodes.Call, Methods.WriteString);
				}
				,
				valueCanBeNull);
		}

		public override void EmitReadValue(ILGenerator gen, Serializer serializerCompiler, Action loadReader, Action loadSerializer, Type type, bool valueCanBeNull = true)
		{
			EmitReadNullableValue(
				gen,
				loadReader,
				() =>
				{
					loadSerializer();
					loadReader();
					gen.Emit(OpCodes.Call, Methods.ReadString);
					gen.Emit(OpCodes.Callvirt, Methods.SerializerGetType);
				},
				valueCanBeNull
				);
		}
	}
}