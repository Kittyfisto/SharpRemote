using System;
using System.Reflection;
using System.Reflection.Emit;

namespace SharpRemote.CodeGeneration.Serialization.Serializers
{
	public sealed class TypeSerializationCompiler
		: AbstractSerializationCompiler<Type>
	{
		public static readonly MethodInfo CreateTypeFromName;
		public static readonly MethodInfo GetAssemblyQualifiedName;

		static TypeSerializationCompiler()
		{
			CreateTypeFromName = typeof(TypeSerializationCompiler).GetMethod("GetType", new[] { typeof(string) });
			GetAssemblyQualifiedName = typeof(Type).GetProperty("AssemblyQualifiedName").GetGetMethod();
		}

		public static Type GetType(string name)
		{
			return Type.GetType(name);
		}

		public override void EmitWriteValue(ILGenerator gen, Action loadWriter, Action loadValue, bool valueCanBeNull = true)
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

		public override void EmitReadValue(ILGenerator gen, Action loadReader, bool valueCanBeNull = true)
		{
			EmitReadNullableValue(
				gen,
				loadReader,
				() =>
				{
					loadReader();
					gen.Emit(OpCodes.Call, Methods.ReadString);
					gen.Emit(OpCodes.Call, CreateTypeFromName);
				},
				valueCanBeNull
				);
		}
	}
}