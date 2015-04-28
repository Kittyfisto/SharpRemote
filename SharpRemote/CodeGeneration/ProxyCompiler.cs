using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using SharpRemote.CodeGeneration.Serialization;

namespace SharpRemote.CodeGeneration
{
	public class ProxyCompiler
	{
		private readonly Serializer _serializerCompiler;
		private readonly Type _interfaceType;
		private readonly AssemblyBuilder _assembly;
		private readonly ModuleBuilder _module;
		private readonly TypeBuilder _typeBuilder;
		private readonly FieldBuilder _objectId;
		private readonly FieldBuilder _channel;
		private readonly FieldBuilder _serializer;

		private readonly string _moduleName;

		#region Methods

		#endregion

		public ProxyCompiler(Serializer serializer, AssemblyName assemblyName, string proxyTypeName, Type interfaceType)
		{
			_serializerCompiler = serializer;
			_interfaceType = interfaceType;
			_assembly = AppDomain.CurrentDomain.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.RunAndSave);
			_moduleName = assemblyName.Name + ".dll";
			_module = _assembly.DefineDynamicModule(_moduleName);

			_typeBuilder = _module.DefineType(proxyTypeName, TypeAttributes.Class, typeof (object), new[]
				{
					interfaceType
				});
			_typeBuilder.AddInterfaceImplementation(typeof(IProxy));

			_objectId = _typeBuilder.DefineField("_objectId", typeof (ulong), FieldAttributes.Private | FieldAttributes.InitOnly);
			_channel = _typeBuilder.DefineField("_channel", typeof (IEndPointChannel),
			                                    FieldAttributes.Private | FieldAttributes.InitOnly);
			_serializer = _typeBuilder.DefineField("_serializer", typeof(ISerializer),
												FieldAttributes.Private | FieldAttributes.InitOnly);
		}

		public Type Generate()
		{
			GenerateCtor();
			GenerateGetObjectId();
			GenerateGetSerializer();
			GenerateMethods();

			var proxyType = _typeBuilder.CreateType();
			return proxyType;
		}

		public void Save()
		{
			_assembly.Save(_moduleName);
		}

		private void GenerateCtor()
		{
			var builder = _typeBuilder.DefineConstructor(MethodAttributes.Public, CallingConventions.Standard, new[]
				{
					typeof (ulong),
					typeof (IEndPointChannel),
					typeof (ISerializer)
				});

			var gen = builder.GetILGenerator();
			gen.Emit(OpCodes.Ldarg_0);
			gen.Emit(OpCodes.Call, Methods.ObjectCtor);

			gen.Emit(OpCodes.Ldarg_0);
			gen.Emit(OpCodes.Ldarg_1);
			gen.Emit(OpCodes.Stfld, _objectId);

			gen.Emit(OpCodes.Ldarg_0);
			gen.Emit(OpCodes.Ldarg_2);
			gen.Emit(OpCodes.Stfld, _channel);

			gen.Emit(OpCodes.Ldarg_0);
			gen.Emit(OpCodes.Ldarg_3);
			gen.Emit(OpCodes.Stfld, _serializer);
		}

		private void GenerateGetSerializer()
		{
			var method = _typeBuilder.DefineMethod("get_Serializer", MethodAttributes.Public | MethodAttributes.Virtual | MethodAttributes.Final, typeof(ISerializer), null);
			var gen = method.GetILGenerator();

			gen.Emit(OpCodes.Ldarg_0);
			gen.Emit(OpCodes.Ldfld, _serializer);
			gen.Emit(OpCodes.Ret);

			_typeBuilder.DefineMethodOverride(method, Methods.GrainGetSerializer);
		}

		private void GenerateGetObjectId()
		{
			var method = _typeBuilder.DefineMethod("get_Id", MethodAttributes.Public | MethodAttributes.Virtual | MethodAttributes.Final, typeof (ulong), null);
			var gen = method.GetILGenerator();

			gen.Emit(OpCodes.Ldarg_0);
			gen.Emit(OpCodes.Ldfld, _objectId);
			gen.Emit(OpCodes.Ret);

			_typeBuilder.DefineMethodOverride(method, Methods.GrainGetObjectId);
		}

		private void GenerateMethods()
		{
			var allMethods = _interfaceType.GetMethods(BindingFlags.FlattenHierarchy | BindingFlags.Instance | BindingFlags.Public);
			foreach (var method in allMethods)
			{
				GenerateMethod(method);
			}
		}

		private void GenerateMethod(MethodInfo originalMethod)
		{
			var methodName = originalMethod.Name;
			var parameterTypes = originalMethod.GetParameters().Select(x => x.ParameterType).ToArray();
			var method = _typeBuilder.DefineMethod(methodName,
												   MethodAttributes.Public |
													MethodAttributes.Virtual,
													originalMethod.ReturnType,
													parameterTypes);

			var gen = method.GetILGenerator();

			var stream = gen.DeclareLocal(typeof(MemoryStream));
			var binaryWriter = gen.DeclareLocal(typeof(StreamWriter));
			var binaryReader = gen.DeclareLocal(typeof(StreamReader));

			if (parameterTypes.Length > 0)
			{
				// var stream = new MemoryStream();
				gen.Emit(OpCodes.Newobj, Methods.MemoryStreamCtor);
				gen.Emit(OpCodes.Stloc, stream);

				// var binaryWriter = new BinaryWriter(stream);
				gen.Emit(OpCodes.Ldloc, stream);
				gen.Emit(OpCodes.Newobj, Methods.BinaryWriterCtor);
				gen.Emit(OpCodes.Stloc, binaryWriter);

				var allParameters = originalMethod.GetParameters();
				int index = 0;
				foreach (var parameter in allParameters)
				{
					//WriteXXX(_serializer, arg[y], binaryWriter);
					gen.Emit(OpCodes.Ldloc, binaryWriter);
					gen.Emit(OpCodes.Ldarg, ++index);
					_serializerCompiler.WriteValue(gen, parameter.ParameterType, _serializer);
				}

				// binaryWriter.Flush()
				gen.Emit(OpCodes.Ldloc, binaryWriter);
				gen.Emit(OpCodes.Callvirt, Methods.BinaryWriterFlush);

				// stream.Position = 0
				gen.Emit(OpCodes.Ldloc, stream);
				gen.Emit(OpCodes.Ldc_I8, (long)0);
				gen.Emit(OpCodes.Call, Methods.StreamSetPosition);
			}
			else
			{
				gen.Emit(OpCodes.Ldnull);
				gen.Emit(OpCodes.Stloc, stream);
			}

			// _channel.CallRemoteMethod(_objectId, "get_XXX", stream);
			gen.Emit(OpCodes.Ldarg_0);
			gen.Emit(OpCodes.Ldfld, _channel);
			gen.Emit(OpCodes.Ldarg_0);
			gen.Emit(OpCodes.Ldfld, _objectId);
			gen.Emit(OpCodes.Ldstr, methodName);
			gen.Emit(OpCodes.Ldloc, stream);
			gen.Emit(OpCodes.Callvirt, Methods.ChannelCallRemoteMethod);

			if (originalMethod.ReturnType == typeof (void))
			{
				gen.Emit(OpCodes.Pop);
			}
			else
			{
				// reader = new BinaryReader(...)
				gen.Emit(OpCodes.Newobj, Methods.BinaryReaderCtor);
				gen.Emit(OpCodes.Stloc, binaryReader);

				// return _serializer.DeserializeXXX(reader);
				DeserializeValue(gen, binaryReader, originalMethod.ReturnType);
			}

			gen.Emit(OpCodes.Ret);

			_typeBuilder.DefineMethodOverride(method, originalMethod);
		}

		private void DeserializeValue(ILGenerator gen, LocalBuilder binaryReader, Type propertyType)
		{
			gen.Emit(OpCodes.Ldloc, binaryReader);

			if (!gen.EmitReadPod(propertyType))
			{
				throw new NotImplementedException();
			}
		}
	}
}