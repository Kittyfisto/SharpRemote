﻿using System;
using System.Reflection;
using System.Reflection.Emit;

namespace SharpRemote.CodeGeneration.Serialization.Binary.Serializers
{
	/// <summary>
	/// Takes care of serializing / deserializing Type values.
	/// Serialization simply writes the type's assembly qualified name, e.g. "System.Int32, mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089"
	/// and deserialization calls <see cref="TypeResolver.GetType(string,bool)"/> or a user specified method.
	/// </summary>
	internal sealed class BuiltInTypeSerializer
		: AbstractTypeSerializer
	{
		public static readonly MethodInfo GetAssemblyQualifiedName;

		static BuiltInTypeSerializer()
		{
			GetAssemblyQualifiedName = typeof(Type).GetProperty("AssemblyQualifiedName").GetGetMethod();
		}

		public override bool Supports(Type type)
		{
			return type == typeof (Type);
		}

		public override void EmitWriteValue(ILGenerator gen,
		                                    ISerializerCompiler serializerCompiler,
		                                    Action loadWriter,
		                                    Action loadValue,
		                                    Action loadValueAddress,
		                                    Action loadSerializer,
		                                    Action loadRemotingEndPoint,
		                                    Type type,
		                                    bool valueCanBeNull = true)
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

		public override void EmitReadValue(ILGenerator gen,
		                                   ISerializerCompiler serializerCompiler,
		                                   Action loadReader,
		                                   Action loadSerializer,
		                                   Action loadRemotingEndPoint,
		                                   Type type,
		                                   bool valueCanBeNull = true)
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