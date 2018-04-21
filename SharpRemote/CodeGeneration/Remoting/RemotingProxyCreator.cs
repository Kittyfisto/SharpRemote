using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;

namespace SharpRemote.CodeGeneration.Remoting
{
	/// <summary>
	/// Compiler capable of compiling <see cref="IProxy"/> implementations that implement
	/// one additional interface, and forwards all calls to that interface to an <see cref="IEndPointChannel"/>.
	/// </summary>
	internal sealed class RemotingProxyCreator
	{
		private readonly ISerializerCompiler _serializer;
		private readonly Dictionary<Type, Type> _interfaceToProxy;
		private readonly ModuleBuilder _module;

		public RemotingProxyCreator(ModuleBuilder module, ISerializerCompiler serializer)
		{
			if (module == null) throw new ArgumentNullException(nameof(module));
			if (serializer == null) throw new ArgumentNullException(nameof(serializer));

			_module = module;
			_serializer = serializer;

			_interfaceToProxy = new Dictionary<Type, Type>();
		}

		public RemotingProxyCreator(ModuleBuilder module)
			: this(module, new BinarySerializer(module))
		{}

		public RemotingProxyCreator()
			: this (CreateModule())
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

		/// <summary>
		/// Generates the class for a proxy of the given type <typeparamref name="T"/>.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <returns></returns>
		public Type GenerateProxy<T>()
		{
			var interfaceType = typeof (T);
			if (!interfaceType.IsInterface)
				throw new ArgumentException(string.Format("Proxies can only be created for interfaces: {0} is not an interface", interfaceType));

			lock (_interfaceToProxy)
			{
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
		}

		/// <summary>
		/// Creates a new proxy instance that implements the given type <typeparamref name="T"/>
		/// of the given id.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="endPoint"></param>
		/// <param name="channel"></param>
		/// <param name="objectId"></param>
		/// <returns></returns>
		public T CreateProxy<T>(IRemotingEndPoint endPoint, IEndPointChannel channel, ulong objectId)
		{
			var proxyType = GenerateProxy<T>();

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
					endPoint,
					channel,
					_serializer
				});
		}

		private string GetProxyTypeName(Type interfaceType)
		{
			return string.Format("{0}.{1}.Proxy", interfaceType.Namespace, interfaceType.Name);
		}
	}
}