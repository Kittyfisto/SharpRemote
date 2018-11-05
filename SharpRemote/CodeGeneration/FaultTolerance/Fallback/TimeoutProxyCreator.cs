using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

namespace SharpRemote.CodeGeneration.FaultTolerance.Fallback
{
	/// <summary>
	/// 
	/// </summary>
	internal sealed class TimeoutProxyCreator<T>
		: ITimeoutProxyCreator
		where T : class
	{
		private readonly ModuleBuilder _moduleBuilder;
		private readonly ITypeDescription _typeDescription;
		private readonly Func<T, TimeSpan, T> _factoryMethod;

		public TimeoutProxyCreator(ModuleBuilder moduleBuilder, ITypeDescription typeDescription)
		{
			_moduleBuilder = moduleBuilder;
			_typeDescription = typeDescription;

			var compiler = new TimeoutProxyCompiler(moduleBuilder, typeDescription);
			_factoryMethod = compiler.Compile();
		}

		public object Create(object subject, TimeSpan maximumMethodLatency)
		{
			return _factoryMethod((T) subject, maximumMethodLatency);
		}

		sealed class TimeoutProxyCompiler
		{
			private readonly TypeBuilder _typeBuilder;
			private readonly Type _interfaceType;
			private readonly ITypeDescription _interfaceDescription;
			private readonly FieldBuilder _subject;
			private readonly FieldBuilder _timeout;

			public TimeoutProxyCompiler(ModuleBuilder moduleBuilder, ITypeDescription interfaceDescription)
			{
				_interfaceType = typeof(T);
				var proxyTypeName = string.Format("SharpRemote.FaultTolerance.Timeout.{0}", _interfaceType.FullName);
				_typeBuilder = moduleBuilder.DefineType(proxyTypeName, TypeAttributes.Sealed | TypeAttributes.Class);
				_typeBuilder.AddInterfaceImplementation(interfaceDescription.Type);

				_interfaceDescription = interfaceDescription;
				_subject = _typeBuilder.DefineField("_subject", _interfaceType,
				                                    FieldAttributes.InitOnly | FieldAttributes.Private);

				_timeout = _typeBuilder.DefineField("_timeout", typeof(TimeSpan),
				                                    FieldAttributes.InitOnly | FieldAttributes.Private);
			}

			public Func<T, TimeSpan, T> Compile()
			{
				var constructor = CreateConstructor();
				foreach (var method in _interfaceDescription.Methods) CreateMethod(method);

				CreateFactoryMethod(constructor);
				var type = _typeBuilder.CreateType();

				var factoryMethod = type.GetMethod("Create", BindingFlags.Static | BindingFlags.Public);
				var d = factoryMethod.CreateDelegate(typeof(Func<T, TimeSpan, T>));
				return (Func<T, TimeSpan, T>) d;
			}

			private void CreateFactoryMethod(ConstructorInfo constructor)
			{
				var method = _typeBuilder.DefineMethod("Create", MethodAttributes.Public | MethodAttributes.Static,
				                                       CallingConventions.Standard,
				                                       _interfaceType,
				                                       new[]
				                                       {
					                                       _interfaceType,
					                                       typeof(TimeSpan)
				                                       });

				var gen = method.GetILGenerator();
				gen.Emit(OpCodes.Ldarg_0);
				gen.Emit(OpCodes.Ldarg_1);
				gen.Emit(OpCodes.Newobj, constructor);
				gen.Emit(OpCodes.Ret);
			}

			private ConstructorInfo CreateConstructor()
			{
				var constructor = _typeBuilder.DefineConstructor(MethodAttributes.Public,
				                                                 CallingConventions.HasThis,
				                                                 new[]
				                                                 {
					                                                 _interfaceType,
					                                                 typeof(TimeSpan)
				                                                 });

				var gen = constructor.GetILGenerator();
				gen.Emit(OpCodes.Ldarg_0);
				gen.Emit(OpCodes.Ldarg_1);
				gen.Emit(OpCodes.Stfld, _subject);
				gen.Emit(OpCodes.Ldarg_0);
				gen.Emit(OpCodes.Ldarg_2);
				gen.Emit(OpCodes.Stfld, _timeout);
				gen.Emit(OpCodes.Ret);

				return constructor;
			}

			private void CreateMethod(IMethodDescription methodDescription)
			{
				var method = methodDescription.Method;
				var parameters = methodDescription.Parameters;
				var methodAttributes = MethodAttributes.Public | MethodAttributes.Virtual;

				var methodBuilder = _typeBuilder.DefineMethod(methodDescription.Name,
				                                              methodAttributes,
				                                              method.ReturnType,
				                                              parameters.Select(x => x.ParameterType.Type).ToArray());
				var gen = methodBuilder.GetILGenerator();

				if (methodDescription.IsAsync)
				{
					gen.Emit(OpCodes.Ldarg_0);
					gen.Emit(OpCodes.Ldfld, _subject);
					for (int i = 0; i < methodDescription.Parameters.Count; ++i)
						gen.Emit(OpCodes.Ldarg, i + 1);
					gen.Emit(OpCodes.Callvirt, methodDescription.Method);
				}
				else
				{
					ConstructorInfo constructor;
					MethodInfo run;
					List<FieldInfo> fields;
					var lambdaType = CreateLambdaStorageClass(methodDescription,
															  out fields,
					                                          out constructor,
					                                          out run);

					var lambda = gen.DeclareLocal(lambdaType);
					gen.Emit(OpCodes.Newobj, constructor);
					gen.Emit(OpCodes.Stloc, lambda);

					for (int i = 0; i < methodDescription.Parameters.Count; ++i)
					{
						gen.Emit(OpCodes.Ldloc, lambda);
						gen.Emit(OpCodes.Ldarg, i + 1);
						gen.Emit(OpCodes.Stfld, fields[i]);
					}

					gen.Emit(OpCodes.Ldloc, lambda);
					gen.Emit(OpCodes.Ldftn, run);
					gen.Emit(OpCodes.Newobj, Methods.ActionIntPtrCtor);

					gen.Emit(OpCodes.Call, Methods.TaskFactoryStartNew);
				}

				gen.Emit(OpCodes.Ldarg_0);
				gen.Emit(OpCodes.Ldfld, _timeout);

				gen.Emit(OpCodes.Call, Methods.TaskExTimeoutAfter);

				gen.Emit(OpCodes.Ret);

				_typeBuilder.DefineMethodOverride(methodBuilder, methodDescription.Method);
			}

			private Type CreateLambdaStorageClass(IMethodDescription methodDescription,
												  out List<FieldInfo> fields,
							out ConstructorInfo constructor,
			                                      out MethodInfo run)
			{
				var name = string.Format("{0}_State", methodDescription.Name);
				var lambdaType = _typeBuilder.DefineNestedType(name);

				fields = new List<FieldInfo>(methodDescription.Parameters.Count);
				foreach (var parameter in methodDescription.Parameters)
				{
					var field = lambdaType.DefineField(parameter.Name,
					                                   parameter.ParameterType.Type,
					                                   FieldAttributes.Public);
					fields.Add(field);
				}

				var ctor = lambdaType.DefineConstructor(MethodAttributes.Public,
				                                        CallingConventions.Standard | CallingConventions.HasThis,
				                                        new Type[0]);
				var gen = ctor.GetILGenerator();
				gen.Emit(OpCodes.Ret);
				constructor = ctor;

				var method = lambdaType.DefineMethod("Run",
				                                     MethodAttributes.Public,
				                                     CallingConventions.Standard | CallingConventions.HasThis,
				                                     typeof(void),
				                                     new Type[0]);
				gen = method.GetILGenerator();
				gen.Emit(OpCodes.Ret);
				run = method;

				return lambdaType.CreateType();
			}
		}
	}
}