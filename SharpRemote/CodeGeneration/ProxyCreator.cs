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
		private readonly AssemblyBuilder _assembly;
		private readonly string _moduleName;

		public ProxyCreator(IEndPointChannel channel)
		{
			if (channel == null) throw new ArgumentNullException("channel");

			_channel = channel;

			var assemblyName = new AssemblyName("SharpRemote.CodeGeneration.Proxies");
			_assembly = AppDomain.CurrentDomain.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.RunAndSave);
			_moduleName = assemblyName.Name + ".dll";
			var module = _assembly.DefineDynamicModule(_moduleName);
			_serializer = new Serializer(module);

			_interfaceToProxy = new Dictionary<Type, Type>();
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

			var generator = new ProxyCompiler(_serializer, assemblyName, proxyTypeName, interfaceType);
			var proxyType = generator.Generate();

			//generator.Save();
			//_assembly.Save(_moduleName);

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