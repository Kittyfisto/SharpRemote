using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using SharpRemote.CodeGeneration.Serialization;

namespace SharpRemote.CodeGeneration
{
	public sealed class ServantCreator
	{
		private readonly IEndPointChannel _channel;
		private readonly Serializer _serializer;
		private readonly Dictionary<Type, Type> _interfaceToSubject;
		private readonly ModuleBuilder _module;

		public ServantCreator(ModuleBuilder module, Serializer serializer, IEndPointChannel channel)
		{
			if (module == null) throw new ArgumentNullException("module");
			if (serializer == null) throw new ArgumentNullException("serializer");
			if (channel == null) throw new ArgumentNullException("channel");

			_channel = channel;
			_module = module;
			_serializer = serializer;
			_interfaceToSubject= new Dictionary<Type, Type>();
		}

		public ServantCreator(ModuleBuilder module, IEndPointChannel channel)
			: this(module, new Serializer(module), channel)
		{}

		public ServantCreator(IEndPointChannel channel)
			: this(CreateModule(), channel)
		{}

		private static ModuleBuilder CreateModule()
		{
			var assemblyName = new AssemblyName("SharpRemote.GeneratedCode.Servants");
			var assembly = AppDomain.CurrentDomain.DefineDynamicAssembly(assemblyName,
																		 AssemblyBuilderAccess.RunAndSave);
			var moduleName = assembly.FullName + ".dll";
			var module = assembly.DefineDynamicModule(moduleName);
			return module;
		}

		public ISerializer Serializer
		{
			get { return _serializer; }
		}

		public Type GenerateSubject<T>()
		{
			var interfaceType = typeof(T);
			if (!interfaceType.IsInterface)
				throw new ArgumentException(string.Format("Proxies can only be created for interfaces: {0} is not an interface", interfaceType));

			var proxyTypeName = GetSubjectTypeName(interfaceType);

			var generator = new ServantCompiler(_serializer, _module, proxyTypeName, interfaceType);
			var proxyType = generator.Generate();

			//generator.Save();
			//_assembly.Save(_moduleName);

			_interfaceToSubject.Add(interfaceType, proxyType);
			return proxyType;
		}

		public IServant CreateServant<T>(ulong objectId, T subject)
		{
			var interfaceType = typeof(T);
			Type subjectType;
			if (!_interfaceToSubject.TryGetValue(interfaceType, out subjectType))
			{
				subjectType = GenerateSubject<T>();
			}

			ConstructorInfo ctor = subjectType.GetConstructor(new[]
				{
					typeof(ulong),
					typeof (IEndPointChannel),
					typeof (ISerializer),
					interfaceType
				});
			if (ctor == null)
				throw new NotImplementedException(string.Format("Could not find ctor of servant for type '{0}'", interfaceType));

			return (IServant)ctor.Invoke(new object[]
				{
					objectId,
					_channel,
					_serializer,
					subject
				});
		}

		private string GetSubjectTypeName(Type interfaceType)
		{
			return string.Format("{0}.{1}.Servant", interfaceType.Namespace, interfaceType.Name);
		}
	}
}