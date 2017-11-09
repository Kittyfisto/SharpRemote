using System;
using System.Reflection;
using System.Reflection.Emit;

namespace SharpRemote.CodeGeneration.Serialization
{
	/// <summary>
	///     Base class which is used by all serializers.
	/// </summary>
	public abstract class AbstractSerializationMethods
		: ISerializationMethods
	{
		private readonly TypeBuilder _typeBuilder;
		private readonly MethodInfo _readObjectMethod;
		private readonly MethodInfo _readValueMethod;
		private readonly MethodBuilder _readValueNotNullMethod;
		private readonly ITypeDescription _typeDescription;
		private readonly MethodInfo _writeObjectMethod;
		private readonly MethodInfo _writeValueMethod;

		private readonly MethodBuilder _writeValueNotNullMethod;

		/// <summary>
		///     Initializes this object.
		/// </summary>
		/// <param name="typeBuilder"></param>
		/// <param name="typeDescription"></param>
		protected AbstractSerializationMethods(TypeBuilder typeBuilder, TypeDescription typeDescription)
		{
			_typeBuilder = typeBuilder;
			_typeDescription = typeDescription;
			var type = typeDescription.Type;
			_writeValueNotNullMethod = typeBuilder.DefineMethod("WriteValueNotNull",
			                                                    MethodAttributes.Public | MethodAttributes.Static,
			                                                    CallingConventions.Standard, typeof(void), new[]
			                                                    {
				                                                    WriterType,
				                                                    type,
				                                                    typeof(ISerializer2),
				                                                    typeof(IRemotingEndPoint)
			                                                    });

			_writeObjectMethod = CreateWriteValueWithTypeInformation(typeBuilder, _writeValueNotNullMethod, type);
			_writeValueMethod = CreateWriteValue(typeBuilder, _writeValueNotNullMethod, type);
			_readValueNotNullMethod = typeBuilder.DefineMethod("ReadValueNotNull",
			                                                   MethodAttributes.Public | MethodAttributes.Static,
			                                                   CallingConventions.Standard,
			                                                   type,
			                                                   new[]
			                                                   {
				                                                   ReaderType,
				                                                   typeof(ISerializer2),
				                                                   typeof(IRemotingEndPoint)
			                                                   });

			_readObjectMethod = CreateReadObject(typeBuilder, _readValueNotNullMethod, type);
			_readValueMethod = CreateReadValue(typeBuilder, _readValueNotNullMethod, type);
		}

		/// <summary>
		/// </summary>
		protected abstract Type WriterType { get; }

		/// <summary>
		/// </summary>
		protected abstract Type ReaderType { get; }

		/// <inheritdoc />
		public ITypeDescription TypeDescription => _typeDescription;

		/// <inheritdoc />
		public MethodInfo WriteValueMethod => _writeValueMethod;

		/// <inheritdoc />
		public MethodInfo WriteObjectMethod => _writeObjectMethod;

		/// <inheritdoc />
		public MethodInfo ReadValueMethod => _readValueMethod;

		/// <inheritdoc />
		public MethodInfo ReadObjectMethod => _readObjectMethod;

		private MethodInfo CreateWriteValueWithTypeInformation(TypeBuilder typeBuilder,
		                                                       MethodInfo writeValueNotNull,
		                                                       Type type)
		{
			var method = typeBuilder.DefineMethod("WriteObject", MethodAttributes.Public | MethodAttributes.Static,
			                                      CallingConventions.Standard, typeof(void), new[]
			                                      {
				                                      WriterType,
				                                      typeof(object),
				                                      typeof(ISerializer2),
				                                      typeof(IRemotingEndPoint)
			                                      });
			var gen = method.GetILGenerator();

			EmitWriteTypeInformationOrNull(gen, () =>
			{
				// WriteValueNotNull(writer, value, serializer, remotingEndPoint);
				gen.Emit(OpCodes.Ldarg_0);
				gen.Emit(OpCodes.Ldarg_1);

				if (type.IsPrimitive || type.IsValueType)
					gen.Emit(OpCodes.Unbox_Any, type);
				else
					gen.Emit(OpCodes.Castclass, type);

				gen.Emit(OpCodes.Ldarg_2);
				gen.Emit(OpCodes.Ldarg_3);
				gen.Emit(OpCodes.Call, writeValueNotNull);
			}, type);

			return method;
		}

		private void EmitWriteTypeInformationOrNull(ILGenerator gen, Action writeValue, Type type)
		{
			//gen.EmitWriteLine("writing type info");

			var result = gen.DeclareLocal(typeof(bool));

			// if (object == null)
			var @true = gen.DefineLabel();
			gen.Emit(OpCodes.Ldarg_1);
			gen.Emit(OpCodes.Ldnull);
			gen.Emit(OpCodes.Ceq);
			gen.Emit(OpCodes.Ldc_I4_0);
			gen.Emit(OpCodes.Ceq);
			gen.Emit(OpCodes.Stloc, result);
			gen.Emit(OpCodes.Ldloc, result);
			gen.Emit(OpCodes.Brtrue, @true);

			// { writer.WriteString(string.Empty); }
			gen.Emit(OpCodes.Ldarg_0);
			gen.Emit(OpCodes.Ldsfld, Methods.StringEmpty);
			gen.Emit(OpCodes.Call, Methods.WriteString);
			var end = gen.DefineLabel();
			gen.Emit(OpCodes.Br, end);

			// else { writer.WriteString(type.AssemblyQualifiedName);
			gen.MarkLabel(@true);
			//gen.EmitWriteLine("writer.WriteString(type.AssemblyQualifiedName)");
			gen.Emit(OpCodes.Ldarg_0);
			gen.Emit(OpCodes.Ldstr, type.AssemblyQualifiedName);
			gen.Emit(OpCodes.Call, Methods.WriteString);

			writeValue();

			gen.MarkLabel(end);
			gen.Emit(OpCodes.Ret);

			//gen.EmitWriteLine("Type info written");
		}

		private MethodInfo CreateWriteValue(TypeBuilder typeBuilder, MethodBuilder valueNotNullMethod, Type type)
		{
			if (type.IsValueType)
				return valueNotNullMethod;

			var method = typeBuilder.DefineMethod("WriteValue", MethodAttributes.Public | MethodAttributes.Static,
			                                      CallingConventions.Standard, typeof(void), new[]
			                                      {
				                                      WriterType,
				                                      type,
				                                      typeof(ISerializer2),
				                                      typeof(IRemotingEndPoint)
			                                      });

			var gen = method.GetILGenerator();

			var result = gen.DeclareLocal(type);

			// if (object == null)
			var @true = gen.DefineLabel();
			gen.Emit(OpCodes.Ldarg_1);
			gen.Emit(OpCodes.Ldnull);
			gen.Emit(OpCodes.Ceq);
			gen.Emit(OpCodes.Ldc_I4_0);
			gen.Emit(OpCodes.Ceq);
			gen.Emit(OpCodes.Stloc, result);
			gen.Emit(OpCodes.Ldloc, result);
			gen.Emit(OpCodes.Brtrue, @true);

			// { writer.Write(false); }
			gen.Emit(OpCodes.Ldarg_0);
			gen.Emit(OpCodes.Ldc_I4_0);
			gen.Emit(OpCodes.Call, Methods.WriteBool);

			var end = gen.DefineLabel();
			gen.Emit(OpCodes.Br, end);

			// else { writer.Write(true); <Serialize Fields> }
			gen.MarkLabel(@true);
			gen.Emit(OpCodes.Ldarg_0);
			gen.Emit(OpCodes.Ldc_I4_1);
			gen.Emit(OpCodes.Call, Methods.WriteBool);

			// WriteValueNotNull(writer, value, serializer, remotingEndPoint)
			gen.Emit(OpCodes.Ldarg_0);
			gen.Emit(OpCodes.Ldarg_1);
			gen.Emit(OpCodes.Ldarg_2);
			gen.Emit(OpCodes.Ldarg_3);
			gen.Emit(OpCodes.Call, valueNotNullMethod);

			gen.MarkLabel(end);
			gen.Emit(OpCodes.Ret);

			return method;
		}

		private MethodInfo CreateReadObject(TypeBuilder typeBuilder, MethodBuilder readValueNotNull, Type type)
		{
			var method = typeBuilder.DefineMethod("ReadObject", MethodAttributes.Public | MethodAttributes.Static,
			                                      CallingConventions.Standard, typeof(object), new[]
			                                      {
				                                      ReaderType,
				                                      typeof(ISerializer2),
				                                      typeof(IRemotingEndPoint)
			                                      });

			var requiresBoxing = type.IsPrimitive || type.IsValueType;
			var gen = method.GetILGenerator();

			// return ReadValueNotNull(reader, serializer, remoteEndPoint);
			gen.Emit(OpCodes.Ldarg_0);
			gen.Emit(OpCodes.Ldarg_1);
			gen.Emit(OpCodes.Ldarg_2);
			gen.Emit(OpCodes.Call, readValueNotNull);

			if (requiresBoxing)
				gen.Emit(OpCodes.Box, type);

			gen.Emit(OpCodes.Ret);
			return method;
		}

		private MethodInfo CreateReadValue(TypeBuilder typeBuilder, MethodBuilder readValueNotNull, Type type)
		{
			if (type.IsValueType)
				return readValueNotNull;

			var method = typeBuilder.DefineMethod("ReadValue", MethodAttributes.Public | MethodAttributes.Static,
			                                      CallingConventions.Standard, type, new[]
			                                      {
				                                      ReaderType,
				                                      typeof(ISerializer2),
				                                      typeof(IRemotingEndPoint)
			                                      });

			var gen = method.GetILGenerator();

			gen.Emit(OpCodes.Ldarg_0);
			gen.Emit(OpCodes.Call, Methods.ReadBool);
			var end = gen.DefineLabel();
			var @null = gen.DefineLabel();
			gen.Emit(OpCodes.Brfalse, @null);

			// ReadValueNotNull(reader, serializer, remotingEndPoint);
			gen.Emit(OpCodes.Ldarg_0);
			gen.Emit(OpCodes.Ldarg_1);
			gen.Emit(OpCodes.Ldarg_2);
			gen.Emit(OpCodes.Call, readValueNotNull);
			gen.Emit(OpCodes.Br_S, end);

			gen.MarkLabel(@null);
			gen.Emit(OpCodes.Ldnull);

			gen.MarkLabel(end);
			gen.Emit(OpCodes.Ret);

			return method;
		}

		/// <summary>
		///     Emits il-code for all methods.
		/// </summary>
		protected void Compile(ISerializationMethodStorage<AbstractSerializationMethods> methodStorage)
		{
			EmitWriteValueNotNullMethod();
			EmitReadValueNotNullMethod();
			_typeBuilder.CreateType();
		}

		private void EmitWriteValueNotNullMethod()
		{
			//Action loadWriter = () => gen.Emit(OpCodes.Ldarg_0);
			//Action loadValue = () => gen.Emit(OpCodes.Ldarg_1);
			//Action loadValueAddress = () => gen.Emit(OpCodes.Ldarga, arg: 1);
			//Action loadSerializer = () => gen.Emit(OpCodes.Ldarg_2);
			//Action loadRemotingEndPoint = () => gen.Emit(OpCodes.Ldarg_3);
			//
			//MethodInfo method;
			//if (IsSingleton(typeInformation.Type, out method))
			//{
			//	// Nothing to do, all possible instance information has already been written....
			//}
			//else if (EmitWriteNativeType(
			//                             gen,
			//                             loadWriter,
			//                             loadValue,
			//                             loadValueAddress,
			//                             loadSerializer,
			//                             loadRemotingEndPoint,
			//                             typeInformation.Type,
			//                             false))
			//{
			//}
			//else if (typeInformation.IsArray)
			//{
			//	EmitWriteArray(gen, typeInformation, loadWriter, loadValue, loadSerializer, loadRemotingEndPoint);
			//}
			//else if (typeInformation.IsCollection)
			//{
			//	EmitWriteCollection(gen, typeInformation, loadWriter, loadValue, loadSerializer, loadRemotingEndPoint);
			//}
			//else if (typeInformation.IsStack)
			//{
			//	EmitWriteStack(gen, typeInformation, loadWriter, loadValue, loadSerializer, loadRemotingEndPoint);
			//}
			//else if (typeInformation.IsQueue)
			//{
			//	EmitWriteQueue(gen, typeInformation, loadWriter, loadValue, loadSerializer, loadRemotingEndPoint);
			//}
			//else
			//{
			//	WriteCustomType(gen, typeInformation.Type, loadWriter, loadRemotingEndPoint);
			//}
			//
			//gen.Emit(OpCodes.Ret);
		}
		
		private void EmitReadValueNotNullMethod()
		{
			//Action loadReader = () => gen.Emit(OpCodes.Ldarg_0);
			//Action loadSerializer = () => gen.Emit(OpCodes.Ldarg_1);
			//Action loadRemotingEndPoint = () => gen.Emit(OpCodes.Ldarg_2);
			//
			//MethodInfo method;
			//if (IsSingleton(typeInformation.Type, out method))
			//{
			//	EmitReadSingleton(gen, method);
			//}
			//else if (EmitReadNativeType(gen,
			//                            loadReader,
			//                            loadSerializer,
			//                            loadRemotingEndPoint,
			//                            typeInformation.Type,
			//                            false))
			//{
			//}
			//else if (typeInformation.IsArray)
			//{
			//	EmitReadArray(gen,
			//	              loadReader,
			//	              loadSerializer,
			//	              loadRemotingEndPoint,
			//	              typeInformation);
			//}
			//else if (typeInformation.IsCollection)
			//{
			//	EmitReadCollection(gen,
			//	                   loadReader,
			//	                   loadSerializer,
			//	                   loadRemotingEndPoint,
			//	                   typeInformation);
			//}
			//else if (typeInformation.IsStack)
			//{
			//	EmitReadStack(gen,
			//	              loadReader,
			//	              loadSerializer,
			//	              loadRemotingEndPoint,
			//	              typeInformation);
			//}
			//else if (typeInformation.IsQueue)
			//{
			//	EmitReadQueue(gen,
			//	              loadReader,
			//	              loadSerializer,
			//	              loadRemotingEndPoint,
			//	              typeInformation);
			//}
			//else
			//{
			//	var value = gen.DeclareLocal(typeInformation.Type);
			//	EmitReadCustomType(gen,
			//	                   loadReader,
			//	                   loadSerializer,
			//	                   loadRemotingEndPoint,
			//	                   typeInformation,
			//	                   value);
			//}
			//
			//gen.Emit(OpCodes.Ret);
		}
	}
}