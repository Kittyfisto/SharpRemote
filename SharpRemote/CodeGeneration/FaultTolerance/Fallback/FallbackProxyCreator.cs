using System;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Threading.Tasks;

namespace SharpRemote.CodeGeneration.FaultTolerance.Fallback
{
	internal sealed class FallbackProxyCreator<T>
		: IFallbackProxyCreator
	{
		private readonly Func<T, T, T> _factoryMethod;

		public FallbackProxyCreator(ModuleBuilder moduleBuilder, ITypeDescription interfaceDescription)
		{
			var compiler = new FallbackProxyCompiler(moduleBuilder, interfaceDescription);
			_factoryMethod = compiler.Compile();
		}

		public object Create(object subject, object fallback)
		{
			return _factoryMethod((T) subject, (T) fallback);
		}

		sealed class FallbackProxyCompiler
		{
			private readonly FieldBuilder _fallback;
			private readonly ITypeDescription _interfaceDescription;
			private readonly Type _interfaceType;
			private readonly FieldBuilder _subject;
			private readonly TypeBuilder _typeBuilder;

			public FallbackProxyCompiler(ModuleBuilder moduleBuilder, ITypeDescription interfaceDescription)
			{
				_interfaceType = typeof(T);
				var proxyTypeName = string.Format("SharpRemote.FaultTolerance.Fallback.{0}", _interfaceType.FullName);
				_typeBuilder = moduleBuilder.DefineType(proxyTypeName, TypeAttributes.Sealed | TypeAttributes.Class);
				_typeBuilder.AddInterfaceImplementation(interfaceDescription.Type);

				_interfaceDescription = interfaceDescription;
				_subject = _typeBuilder.DefineField("_subject", _interfaceType,
				                                    FieldAttributes.InitOnly | FieldAttributes.Private);
				_fallback = _typeBuilder.DefineField("_fallback", _interfaceType,
				                                     FieldAttributes.InitOnly | FieldAttributes.Private);
			}

			public Func<T, T, T> Compile()
			{
				var constructor = CreateConstructor();
				foreach (var method in _interfaceDescription.Methods) CreateMethod(method);

				foreach (var property in _interfaceDescription.Properties) CreateProperty(property);

				CreateFactoryMethod(constructor);
				var type = _typeBuilder.CreateType();

				var factoryMethod = type.GetMethod("Create", BindingFlags.Static | BindingFlags.Public);
				var d = factoryMethod.CreateDelegate(typeof(Func<T, T, T>));
				return (Func<T, T, T>) d;
			}

			private ConstructorInfo CreateConstructor()
			{
				var constructor = _typeBuilder.DefineConstructor(MethodAttributes.Public,
				                                                 CallingConventions.HasThis,
				                                                 new[]
				                                                 {
					                                                 _interfaceType,
					                                                 _interfaceType
				                                                 });

				var gen = constructor.GetILGenerator();
				gen.Emit(OpCodes.Ldarg_0);
				gen.Emit(OpCodes.Ldarg_1);
				gen.Emit(OpCodes.Stfld, _subject);
				gen.Emit(OpCodes.Ldarg_0);
				gen.Emit(OpCodes.Ldarg_2);
				gen.Emit(OpCodes.Stfld, _fallback);
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

				LocalBuilder returnValue = null;
				var hasReturnValue = method.ReturnType != typeof(void);
				if (hasReturnValue)
					returnValue = gen.DeclareLocal(method.ReturnType);

				gen.BeginExceptionBlock();

				gen.Emit(OpCodes.Ldarg_0);
				gen.Emit(OpCodes.Ldfld, _subject);
				for (var i = 0; i < parameters.Count; ++i) gen.Emit(OpCodes.Ldarg, i + 1);
				gen.Emit(OpCodes.Callvirt, methodDescription.Method);

				if (hasReturnValue)
				{
					gen.Emit(OpCodes.Stloc, returnValue);

					if (methodDescription.IsAsync)
					{
						GenerateStartTaskStateMachine(gen, methodDescription, returnValue);
					}
				}

				gen.BeginCatchBlock(typeof(Exception));
				gen.Emit(OpCodes.Ldarg_0);
				gen.Emit(OpCodes.Ldfld, _fallback);
				for (var i = 0; i < parameters.Count; ++i) gen.Emit(OpCodes.Ldarg, i + 1);
				gen.Emit(OpCodes.Callvirt, methodDescription.Method);
				if (hasReturnValue)
					gen.Emit(OpCodes.Stloc, returnValue);

				gen.EndExceptionBlock();

				if (hasReturnValue)
					gen.Emit(OpCodes.Ldloc, returnValue);
				gen.Emit(OpCodes.Ret);

				_typeBuilder.DefineMethodOverride(methodBuilder, methodDescription.Method);
			}

			private void GenerateStartTaskStateMachine(ILGenerator gen,
			                                  IMethodDescription methodDescription,
			                                  LocalBuilder returnedTask)
			{
				var compiler = new AsyncStateMachineCompiler(_typeBuilder,
				                                        _interfaceDescription,
				                                        methodDescription);
				FieldInfo taskField;
				var constructor = compiler.Compile(out taskField);

				gen.Emit(OpCodes.Ldloc, returnedTask);
				gen.Emit(OpCodes.Ldarg_0);
				gen.Emit(OpCodes.Ldfld, _fallback);
				gen.Emit(OpCodes.Newobj, constructor);
				gen.Emit(OpCodes.Ldfld, taskField);
				gen.Emit(OpCodes.Stloc, returnedTask);

				//var returnTypeDescription = methodDescription.ReturnType;
				//
				//ConstructorInfo delegateConstructor;
				//MethodInfo continueWith;
				//var method = GenerateCallbackMethod(methodDescription,
				//                                    returnTypeDescription,
				//                                    out delegateConstructor,
				//                                    out continueWith);
				//
				//gen.Emit(OpCodes.Ldloc, returnedTask);
				//gen.Emit(OpCodes.Ldarg_0);
				//gen.Emit(OpCodes.Ldftn, method);
				//gen.Emit(OpCodes.Newobj, delegateConstructor);
				//gen.Emit(OpCodes.Callvirt, continueWith);
				//gen.Emit(OpCodes.Stloc, returnedTask);
			}

			private MethodBuilder GenerateCallbackMethod(IMethodDescription methodDescription,
			                                             ITypeDescription returnTypeDescription,
			                                             out ConstructorInfo delegateConstructor,
			                                             out MethodInfo continueWith)
			{
				var taskType = returnTypeDescription.Type;
				Type callbackReturnType;
				bool hasReturnValue;
				if (returnTypeDescription.IsGenericType)
				{
					callbackReturnType = returnTypeDescription.GenericArguments[0].Type;
					var delegateType = typeof(Func<,>).MakeGenericType(taskType,
					                                                   callbackReturnType);
					delegateConstructor = delegateType.GetConstructor(new[]
					{
						typeof(object),
						typeof(IntPtr)
					});
					continueWith = taskType.GetMethod(nameof(Task<int>.ContinueWith),
					                                  new[] {delegateType});

					hasReturnValue = true;
				}
				else
				{
					callbackReturnType = typeof(void);
					var delegateType = typeof(Action<Task>);
					delegateConstructor = delegateType.GetConstructor(new []
					{
						typeof(object),
						typeof(IntPtr)
					});
					continueWith = taskType.GetMethod(nameof(Task<int>.ContinueWith),
					                                  new[] {delegateType});
					hasReturnValue = false;
				}

				var methodName = string.Format("On{0}", methodDescription.Name);
				var method = _typeBuilder.DefineMethod(methodName,
				                                       MethodAttributes.Private,
				                                       callbackReturnType,
				                                       new[]
				                                       {
					                                       taskType
				                                       });
				
				var gen = method.GetILGenerator();

				LocalBuilder returnValue;
				if (hasReturnValue)
					returnValue = gen.DeclareLocal(callbackReturnType);
				else
					returnValue = null;

				gen.BeginExceptionBlock();
				gen.Emit(OpCodes.Stloc, returnValue);

				gen.BeginCatchBlock(typeof(Exception));
				//gen.B
				gen.EndExceptionBlock();

				if (hasReturnValue)
					gen.Emit(OpCodes.Ldloc, returnValue);

				gen.Emit(OpCodes.Ret);

				return method;
			}

			private void CreateProperty(IPropertyDescription propertyDescription)
			{
				var property = _typeBuilder.DefineProperty(propertyDescription.Name,
				                                           PropertyAttributes.None,
				                                           propertyDescription.PropertyType.Type,
				                                           new Type[0]);
				if (propertyDescription.GetMethod != null)
				{
				}

				if (propertyDescription.SetMethod != null)
				{
				}
			}

			private MethodInfo CreateFactoryMethod(ConstructorInfo constructor)
			{
				var method = _typeBuilder.DefineMethod("Create", MethodAttributes.Static | MethodAttributes.Public,
				                                       CallingConventions.Standard,
				                                       typeof(T),
				                                       new[] {typeof(T), typeof(T)});
				var gen = method.GetILGenerator();
				gen.Emit(OpCodes.Ldarg_0);
				gen.Emit(OpCodes.Ldarg_1);
				gen.Emit(OpCodes.Newobj, constructor);
				gen.Emit(OpCodes.Ret);

				return method;
			}
		}
	}
}