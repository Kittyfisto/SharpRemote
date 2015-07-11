using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;

namespace SharpRemote.CodeGeneration.Remoting
{
	/// <summary>
	/// Compiler capable of compiling <see cref="IServant"/> implementations that forward calls from
	/// an <see cref="IEndPointChannel"/> to a subject.
	/// </summary>
	internal sealed class ServantCreator
	{
		private readonly IRemotingEndPoint _endPoint;
		private readonly IEndPointChannel _channel;
		private readonly Serializer _serializer;
		private readonly Dictionary<Type, Type> _interfaceToSubject;
		private readonly ModuleBuilder _module;

		public ServantCreator(ModuleBuilder module, Serializer serializer, IRemotingEndPoint endPoint, IEndPointChannel channel)
		{
			if (module == null) throw new ArgumentNullException("module");
			if (serializer == null) throw new ArgumentNullException("serializer");
			if (endPoint == null) throw new ArgumentNullException("endPoint");
			if (channel == null) throw new ArgumentNullException("channel");

			_endPoint = endPoint;
			_channel = channel;
			_module = module;
			_serializer = serializer;
			_interfaceToSubject= new Dictionary<Type, Type>();
		}

		public ServantCreator(ModuleBuilder module, IRemotingEndPoint endPoint, IEndPointChannel channel)
			: this(module, new Serializer(module), endPoint, channel)
		{}

		public ServantCreator(IRemotingEndPoint endPoint, IEndPointChannel channel)
			: this(CreateModule(), endPoint, channel)
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

		public Type GenerateServant<T>()
		{
			var interfaceType = typeof(T);
			if (!interfaceType.IsInterface)
				throw new ArgumentException(string.Format("Proxies can only be created for interfaces: {0} is not an interface", interfaceType));

			lock (_interfaceToSubject)
			{
				Type proxyType;
				if (!_interfaceToSubject.TryGetValue(interfaceType, out proxyType))
				{
					var proxyTypeName = GetSubjectTypeName(interfaceType);

					var generator = new ServantCompiler(_serializer, _module, proxyTypeName, interfaceType);
					proxyType = generator.Generate();

					//generator.Save();
					//_assembly.Save(_moduleName);

					_interfaceToSubject.Add(interfaceType, proxyType);
				}
				return proxyType;
			}
		}

		public IServant CreateServant<T>(ulong objectId, T subject)
		{
			var interfaceType = typeof(T);
			Type subjectType;
			if (!_interfaceToSubject.TryGetValue(interfaceType, out subjectType))
			{
				subjectType = GenerateServant<T>();
			}

			ConstructorInfo ctor = subjectType.GetConstructor(new[]
				{
					typeof(ulong),
					typeof (IRemotingEndPoint),
					typeof (IEndPointChannel),
					typeof (ISerializer),
					interfaceType
				});
			if (ctor == null)
				throw new NotImplementedException(string.Format("Could not find ctor of servant for type '{0}'", interfaceType));

			return (IServant)ctor.Invoke(new object[]
				{
					objectId,
					_endPoint,
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