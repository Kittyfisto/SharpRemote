using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

namespace SharpRemote.CodeGeneration.FaultTolerance
{
	/// <summary>
	/// Responsible for creating proxy objects implementing a specified interface,
	/// forwarding method calls to another object while attempting to hide failures
	/// from the caller.
	/// </summary>
	public sealed class FaultTolerantProxyCreator
	{
		private readonly ModuleBuilder _moduleBuilder;
		private readonly TypeModel _typeModel;
		private readonly Dictionary<Type, IFallbackProxyCreator> _fallbackProxyCreators;
		private readonly object _syncRoot;

		interface IFallbackProxyCreator
		{
			object Create(object subject, object fallback);
		}

		sealed class FallbackProxyCompiler<T>
		{
			private readonly Type _interfaceType;
			private readonly TypeBuilder _typeBuilder;
			private readonly ITypeDescription _interfaceDescription;
			private readonly FieldBuilder _subject;
			private readonly FieldBuilder _fallback;

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
				foreach (var method in _interfaceDescription.Methods)
				{
					CreateMethod(method);
				}

				foreach (var property in _interfaceDescription.Properties)
				{
					CreateProperty(property);
				}

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
				bool hasReturnValue = method.ReturnType != typeof(void);
				if (hasReturnValue)
					returnValue = gen.DeclareLocal(method.ReturnType);

				gen.BeginExceptionBlock();

				gen.Emit(OpCodes.Ldarg_0);
				gen.Emit(OpCodes.Ldfld, _subject);
				for (int i = 0; i < parameters.Count; ++i)
				{
					gen.Emit(OpCodes.Ldarg, i + 1);
				}
				gen.Emit(OpCodes.Callvirt, methodDescription.Method);
				if (hasReturnValue)
					gen.Emit(OpCodes.Stloc, returnValue);

				gen.BeginCatchBlock(typeof(Exception));
				gen.Emit(OpCodes.Ldarg_0);
				gen.Emit(OpCodes.Ldfld, _fallback);
				for (int i = 0; i < parameters.Count; ++i)
				{
					gen.Emit(OpCodes.Ldarg, i + 1);
				}
				gen.Emit(OpCodes.Callvirt, methodDescription.Method);
				if (hasReturnValue)
					gen.Emit(OpCodes.Stloc, returnValue);

				gen.EndExceptionBlock();

				if (hasReturnValue)
					gen.Emit(OpCodes.Ldloc, returnValue);
				gen.Emit(OpCodes.Ret);

				_typeBuilder.DefineMethodOverride(methodBuilder, methodDescription.Method);
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

		sealed class FallbackProxyCreator<T>
			: IFallbackProxyCreator
		{
			private readonly Func<T, T, T> _factoryMethod;

			public FallbackProxyCreator(ModuleBuilder moduleBuilder, ITypeDescription interfaceDescription)
			{
				var compiler = new FallbackProxyCompiler<T>(moduleBuilder, interfaceDescription);
				_factoryMethod = compiler.Compile();
			}

			public object Create(object subject, object fallback)
			{
				return _factoryMethod((T) subject, (T) fallback);
			}
		}

		/// <summary>
		/// 
		/// </summary>
		public FaultTolerantProxyCreator()
			: this(CreateModule())
		{

		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="moduleBuilder"></param>
		/// <exception cref="ArgumentNullException"></exception>
		public FaultTolerantProxyCreator(ModuleBuilder moduleBuilder)
		{
			if (moduleBuilder == null)
				throw new ArgumentNullException(nameof(moduleBuilder));

			_moduleBuilder = moduleBuilder;
			_syncRoot = new object();
			_typeModel = new TypeModel();
			_fallbackProxyCreators = new Dictionary<Type, IFallbackProxyCreator>();
		}

		private static ModuleBuilder CreateModule()
		{
			var assemblyName = new AssemblyName("SharpRemote.GeneratedCode.FaultTolerance");
			var assembly = AppDomain.CurrentDomain.DefineDynamicAssembly(assemblyName,
			                                                             AssemblyBuilderAccess.RunAndSave);
			var moduleName = assemblyName.Name + ".dll";
			var module = assembly.DefineDynamicModule(moduleName);
			return module;
		}

		/// <summary>
		/// Creates a proxy which delegates all method calls to the given subject.
		/// In case any method call throws an exception, the exception is caught and the fallback
		/// is queried.
		/// </summary>
		/// <param name="subject"></param>
		/// <param name="fallback"></param>
		/// <typeparam name="T"></typeparam>
		/// <returns></returns>
		public T CreateProxyWithFallback<T>(T subject, T fallback)
			where T : class
		{
			if (subject == null)
				throw new ArgumentNullException(nameof(subject));
			if (fallback == null)
				throw new ArgumentNullException(nameof(fallback));

			var creator = GetOrCreateFallbackProxyCreator<T>();
			return (T)creator.Create(subject, fallback);
		}

		private IFallbackProxyCreator GetOrCreateFallbackProxyCreator<T>()
		{
			var type = typeof(T);
			if (!type.IsInterface)
				throw new ArgumentException();

			lock (_syncRoot)
			{
				IFallbackProxyCreator creator;
				if (!_fallbackProxyCreators.TryGetValue(type, out creator))
				{
					var description = _typeModel.Add(type, assumeProxy: true);
					creator = new FallbackProxyCreator<T>(_moduleBuilder, description);
					_fallbackProxyCreators.Add(typeof(T), creator);
				}

				return creator;
			}
		}
	}
}
