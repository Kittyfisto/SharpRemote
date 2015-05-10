using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using SharpRemote.CodeGeneration.Serialization;

namespace SharpRemote.CodeGeneration
{
	public sealed class ProxyCreator
	{
		private readonly Serializer _serializer;
		private readonly IEndPointChannel _channel;
		private readonly Dictionary<Type, Type> _interfaceToProxy;
		private readonly ModuleBuilder _module;

		public ProxyCreator(ModuleBuilder module, Serializer serializer, IEndPointChannel channel)
		{
			if (module == null) throw new ArgumentNullException("module");
			if (serializer == null) throw new ArgumentNullException("serializer");
			if (channel == null) throw new ArgumentNullException("channel");

			_channel = channel;
			_module = module;
			_serializer = serializer;

			_interfaceToProxy = new Dictionary<Type, Type>();
		}

		public ProxyCreator(ModuleBuilder module, IEndPointChannel channel)
			: this(module, new Serializer(module), channel)
		{}

		public ProxyCreator(IEndPointChannel channel)
			: this (CreateModule(), channel)
		{
		}

		private static ModuleBuilder CreateModule()
		{
			var assemblyName = new AssemblyName("SharpRemote.GeneratedCode.Proxies");
			var assembly = AppDomain.CurrentDomain.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.RunAndSave);
			var moduleName = assemblyName.Name + ".dll";
			var module = assembly.DefineDynamicModule(moduleName);
			return module;
		}

		public Type GenerateProxy<T>()
		{
			var interfaceType = typeof (T);
			if (!interfaceType.IsInterface)
				throw new ArgumentException(string.Format("Proxies can only be created for interfaces: {0} is not an interface", interfaceType));

			var assemblyName = GetProxyAssemblyName(interfaceType);
			var proxyTypeName = GetProxyTypeName(interfaceType);

			Assembly proxyAssembly;
			if (TryLoadAssembly(assemblyName, out proxyAssembly))
			{
				throw new NotImplementedException();
			}

			var generator = new ProxyCompiler(_serializer, _module, proxyTypeName, interfaceType);
			var proxyType = generator.Generate();

			_interfaceToProxy.Add(interfaceType, proxyType);
			return proxyType;
		}

		public T CreateProxy<T>(ulong objectId)
		{
			var interfaceType = typeof (T);
			Type proxyType;
			if (!_interfaceToProxy.TryGetValue(interfaceType, out proxyType))
			{
				proxyType = GenerateProxy<T>();
			}

			ConstructorInfo ctor = proxyType.GetConstructor(new[]
				{
					typeof(ulong),
					typeof (IEndPointChannel),
					typeof (ISerializer)
				});
			if (ctor == null)
				throw new Exception();

			return (T)ctor.Invoke(new object[]
				{
					objectId,
					_channel,
					_serializer
				});
		}

		private string GetProxyTypeName(Type interfaceType)
		{
			return string.Format("Corba.{0}.Proxy", interfaceType.Name);
		}

		private bool TryLoadAssembly(AssemblyName assemblyName, out Assembly proxyAssembly)
		{
			proxyAssembly = null;
			return false;
		}

		private AssemblyName GetProxyAssemblyName(Type type)
		{
			var fileName = string.Format("{0}.{1}.Proxy", type.Namespace, type.Name);
			return new AssemblyName(fileName);
		}
	}
}