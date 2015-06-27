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
		private readonly IRemotingEndPoint _endPoint;
		private readonly IEndPointChannel _channel;
		private readonly Dictionary<Type, Type> _interfaceToProxy;
		private readonly ModuleBuilder _module;

		public ProxyCreator(ModuleBuilder module, Serializer serializer, IRemotingEndPoint endPoint, IEndPointChannel channel)
		{
			if (module == null) throw new ArgumentNullException("module");
			if (serializer == null) throw new ArgumentNullException("serializer");
			if (endPoint == null) throw new ArgumentNullException("endPoint");
			if (channel == null) throw new ArgumentNullException("channel");

			_endPoint = endPoint;
			_channel = channel;
			_module = module;
			_serializer = serializer;

			_interfaceToProxy = new Dictionary<Type, Type>();
		}

		public ProxyCreator(ModuleBuilder module, IRemotingEndPoint endPoint, IEndPointChannel channel)
			: this(module, new Serializer(module), endPoint, channel)
		{}

		public ProxyCreator(IRemotingEndPoint endPoint, IEndPointChannel channel)
			: this (CreateModule(), endPoint, channel)
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

			Type proxyType;
			if (!_interfaceToProxy.TryGetValue(interfaceType, out proxyType))
			{
				try
				{
					var proxyTypeName = GetProxyTypeName(interfaceType);

					var generator = new ProxyCompiler(_serializer, _module, proxyTypeName, interfaceType);
					proxyType = generator.Generate();
				}
				catch (Exception e)
				{
					var message = string.Format("Unable to create proxy for type '{0}': {1}",
					                            interfaceType.Name,
					                            e.Message);
					throw new ArgumentException(message, e);
				}
				_interfaceToProxy.Add(interfaceType, proxyType);
			}
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
					typeof (IRemotingEndPoint),
					typeof (IEndPointChannel),
					typeof (ISerializer)
				});
			if (ctor == null)
				throw new Exception();

			return (T)ctor.Invoke(new object[]
				{
					objectId,
					_endPoint,
					_channel,
					_serializer
				});
		}

		private string GetProxyTypeName(Type interfaceType)
		{
			return string.Format("{0}.{1}.Proxy", interfaceType.Namespace, interfaceType.Name);
		}
	}
}