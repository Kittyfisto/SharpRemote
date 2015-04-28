using System;
using System.IO;
using System.Reflection;
using System.Reflection.Emit;
using SharpRemote.CodeGeneration.Serialization;

namespace SharpRemote.CodeGeneration
{
	public sealed class ServantCompiler
	{
		private readonly AssemblyBuilder _assembly;
		private readonly Type _interfaceType;
		private readonly ModuleBuilder _module;
		private readonly Serializer _serializerCompiler;
		private readonly string _moduleName;
		private readonly TypeBuilder _typeBuilder;
		private readonly FieldBuilder _objectId;
		private readonly FieldBuilder _serializer;
		private readonly FieldBuilder _subject;

		public ServantCompiler(Serializer serializer,
		                       AssemblyName assemblyName,
		                       string subjectTypeName,
		                       Type interfaceType)
		{
			_serializerCompiler = serializer;
			_interfaceType = interfaceType;
			_assembly = AppDomain.CurrentDomain.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.RunAndSave);
			_moduleName = assemblyName.Name + ".dll";
			_module = _assembly.DefineDynamicModule(_moduleName);

			_typeBuilder = _module.DefineType(subjectTypeName, TypeAttributes.Class, typeof(object));
			_typeBuilder.AddInterfaceImplementation(typeof(IServant));

			_subject = _typeBuilder.DefineField("_subject", interfaceType, FieldAttributes.Private | FieldAttributes.InitOnly);
			_objectId = _typeBuilder.DefineField("_objectId", typeof(ulong), FieldAttributes.Private | FieldAttributes.InitOnly);
			_serializer = _typeBuilder.DefineField("_serializer", typeof(ISerializer),
												FieldAttributes.Private | FieldAttributes.InitOnly);
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
			var method = _typeBuilder.DefineMethod("get_Id", MethodAttributes.Public | MethodAttributes.Virtual | MethodAttributes.Final, typeof(ulong), null);
			var gen = method.GetILGenerator();

			gen.Emit(OpCodes.Ldarg_0);
			gen.Emit(OpCodes.Ldfld, _objectId);
			gen.Emit(OpCodes.Ret);

			_typeBuilder.DefineMethodOverride(method, Methods.GrainGetObjectId);
		}

		public Type Generate()
		{
			GenerateCtor();
			GenerateGetObjectId();
			GenerateGetSerializer();
			GenerateGetSubject();
			GenerateDispatchMethod();

			var proxyType = _typeBuilder.CreateType();
			return proxyType;
		}

		private void GenerateGetSubject()
		{
			var method = _typeBuilder.DefineMethod("get_Subject",
			                                       MethodAttributes.Public | MethodAttributes.Virtual | MethodAttributes.Final,
			                                       typeof (object),
			                                       null);

			var gen = method.GetILGenerator();
			gen.Emit(OpCodes.Ldarg_0);
			gen.Emit(OpCodes.Ldfld, _subject);
			gen.Emit(OpCodes.Ret);

			_typeBuilder.DefineMethodOverride(method, Methods.ServantGetSubject);
		}

		private void GenerateDispatchMethod()
		{
			var method = _typeBuilder.DefineMethod("Invoke",
			                                       MethodAttributes.Public | MethodAttributes.Virtual,
												   typeof(void),
												   new[]
													   {
														   typeof(string),
														   typeof(BinaryReader),
														   typeof(BinaryWriter)
													   });

			var gen = method.GetILGenerator();

			var name = gen.DeclareLocal(typeof (string));
			var @throw = gen.DefineLabel();
			var @ret = gen.DefineLabel();

			// if (method == null) goto ret
			gen.Emit(OpCodes.Ldarg_1);
			gen.Emit(OpCodes.Stloc, name);
			gen.Emit(OpCodes.Ldloc, name);
			gen.Emit(OpCodes.Brfalse_S, @throw);

			var allMethods = _interfaceType.GetMethods(BindingFlags.Instance | BindingFlags.Public);
			var labels = new Label[allMethods.Length];
			int index = 0;
			foreach (var methodInfo in allMethods)
			{
				gen.Emit(OpCodes.Ldloc, name);
				gen.Emit(OpCodes.Ldstr, methodInfo.Name);
				gen.Emit(OpCodes.Call, Methods.StringEquality);

				var @true = gen.DefineLabel();
				labels[index++] = @true;
				gen.Emit(OpCodes.Brtrue_S, @true);
			}

			gen.Emit(OpCodes.Br_S, @throw);

			for (int i = 0; i < allMethods.Length; ++i)
			{
				var methodInfo = allMethods[i];
				var label = labels[i];

				gen.MarkLabel(label);
				ExtractArgumentsAndCallMethod(gen, methodInfo);
				gen.Emit(OpCodes.Br_S, @ret);
			}

			gen.MarkLabel(@throw);
			gen.Emit(OpCodes.Ldstr, "Method '{0}' not found");
			gen.Emit(OpCodes.Ldarg_1);
			gen.Emit(OpCodes.Call, Methods.StringFormat);
			gen.Emit(OpCodes.Newobj, Methods.ArgumentExceptionCtor);
			gen.Emit(OpCodes.Throw);

			gen.MarkLabel(@ret);
			gen.Emit(OpCodes.Ldarg_3);
			gen.Emit(OpCodes.Call, Methods.BinaryWriterFlush);
			gen.Emit(OpCodes.Ret);

			_typeBuilder.DefineMethodOverride(method, Methods.ServantInvoke);
		}

		private void ExtractArgumentsAndCallMethod(ILGenerator gen, MethodInfo methodInfo)
		{
			gen.Emit(OpCodes.Ldarg_3);

			gen.Emit(OpCodes.Ldarg_0);
			gen.Emit(OpCodes.Ldfld, _subject);

			var allParameters = methodInfo.GetParameters();
			foreach (var parameter in allParameters)
			{
				gen.Emit(OpCodes.Ldarg_2);
				gen.EmitReadPod(parameter.ParameterType);
			}

			gen.Emit(OpCodes.Callvirt, methodInfo);
			_serializerCompiler.WriteValue(gen, methodInfo.ReturnType, _serializer);
		}

		private void GenerateCtor()
		{
			var builder = _typeBuilder.DefineConstructor(MethodAttributes.Public, CallingConventions.Standard, new[]
				{
					typeof (ulong),
					typeof (ISerializer),
					_interfaceType
				});

			var gen = builder.GetILGenerator();
			gen.Emit(OpCodes.Ldarg_0);
			gen.Emit(OpCodes.Call, Methods.ObjectCtor);

			gen.Emit(OpCodes.Ldarg_0);
			gen.Emit(OpCodes.Ldarg_1);
			gen.Emit(OpCodes.Stfld, _objectId);

			gen.Emit(OpCodes.Ldarg_0);
			gen.Emit(OpCodes.Ldarg_2);
			gen.Emit(OpCodes.Stfld, _serializer);

			gen.Emit(OpCodes.Ldarg_0);
			gen.Emit(OpCodes.Ldarg_3);
			gen.Emit(OpCodes.Stfld, _subject);
		}

		public void Save()
		{
			_assembly.Save(_moduleName);
		}
	}
}