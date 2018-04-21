using System;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

namespace SharpRemote.CodeGeneration.FaultTolerance.Fallback
{
	/// <summary>
	///     Responsible for creating objects which implement a given interface.
	///     Methods and properties don't do anything besides returning default values.
	/// </summary>
	/// <typeparam name="T"></typeparam>
	internal sealed class DefaultFallbackCreator<T>
		: IDefaultFallbackCreator
	{
		private readonly ITypeDescription _description;
		private readonly Func<T> _factoryMethod;
		private readonly ModuleBuilder _moduleBuilder;

		public DefaultFallbackCreator(ModuleBuilder moduleBuilder, ITypeDescription description)
		{
			_moduleBuilder = moduleBuilder;
			_description = description;

			var compiler = new FallbackCompiler(moduleBuilder, description);
			_factoryMethod = compiler.Compile();
		}

		public object Create()
		{
			return _factoryMethod();
		}

		private sealed class FallbackCompiler
		{
			private readonly ITypeDescription _interfaceDescription;
			private readonly Type _interfaceType;
			private readonly TypeBuilder _typeBuilder;

			public FallbackCompiler(ModuleBuilder moduleBuilder, ITypeDescription interfaceDescription)
			{
				_interfaceType = typeof(T);
				var proxyTypeName = string.Format("SharpRemote.FaultTolerance.DefaultFallback.{0}", _interfaceType.FullName);
				_typeBuilder = moduleBuilder.DefineType(proxyTypeName, TypeAttributes.Sealed | TypeAttributes.Class);
				_typeBuilder.AddInterfaceImplementation(interfaceDescription.Type);

				_interfaceDescription = interfaceDescription;
			}

			public Func<T> Compile()
			{
				var constructor = CreateConstructor();
				foreach (var method in _interfaceDescription.Methods) CreateMethod(method);

				CreateFactoryMethod(constructor);

				var type = _typeBuilder.CreateType();
				var factoryMethod = type.GetMethod("Create", BindingFlags.Static | BindingFlags.Public);
				return (Func<T>) factoryMethod.CreateDelegate(typeof(Func<T>));
			}

			private ConstructorInfo CreateConstructor()
			{
				var constructor = _typeBuilder.DefineConstructor(MethodAttributes.Public,
				                                                 CallingConventions.Standard | CallingConventions.HasThis,
				                                                 new Type[0]);

				var gen = constructor.GetILGenerator();
				gen.Emit(OpCodes.Ret);

				return constructor;
			}

			private void CreateMethod(IMethodDescription methodDescription)
			{
				var returnType = methodDescription.ReturnType.Type;

				var method = _typeBuilder.DefineMethod(methodDescription.Name,
				                                       MethodAttributes.Public | MethodAttributes.Virtual,
				                                       CallingConventions.Standard | CallingConventions.HasThis,
				                                       returnType,
				                                       methodDescription.Parameters.Select(x => x.ParameterType.Type).ToArray());

				var gen = method.GetILGenerator();

				if (returnType != typeof(void))
				{
					if (returnType.IsValueType)
					{
						var local = gen.DeclareLocal(returnType);
						gen.Emit(OpCodes.Ldloca, local);
						gen.Emit(OpCodes.Initobj, returnType);
						gen.Emit(OpCodes.Ldloc, local);
					}
					else
					{
						gen.Emit(OpCodes.Ldnull);
					}
				}

				gen.Emit(OpCodes.Ret);
			}

			private void CreateFactoryMethod(ConstructorInfo constructor)
			{
				var method = _typeBuilder.DefineMethod("Create", MethodAttributes.Static | MethodAttributes.Public,
				                                       _interfaceType,
				                                       new Type[0]);

				var gen = method.GetILGenerator();
				gen.Emit(OpCodes.Newobj, constructor);
				gen.Emit(OpCodes.Ret);
			}
		}
	}
}