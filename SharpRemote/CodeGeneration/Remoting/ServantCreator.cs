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
		private readonly BinarySerializer _serializer;
		private readonly Dictionary<Type, Type> _interfaceToSubject;
		private readonly ModuleBuilder _module;

		public ServantCreator(ModuleBuilder module, BinarySerializer binarySerializer)
		{
			if (module == null) throw new ArgumentNullException(nameof(module));
			if (binarySerializer == null) throw new ArgumentNullException(nameof(binarySerializer));
			
			_module = module;
			_serializer = binarySerializer;
			_interfaceToSubject= new Dictionary<Type, Type>();
		}

		public ServantCreator(ModuleBuilder module)
			: this(module, new BinarySerializer(module))
		{}

		public ServantCreator()
			: this(CreateModule())
		{}

		private static ModuleBuilder CreateModule()
		{
			var assemblyName = new AssemblyName("SharpRemote.GeneratedCode.Servants");

#if NET6_0
			var access = AssemblyBuilderAccess.Run;
#else
			var access = AssemblyBuilderAccess.RunAndSave;
#endif

			var assembly = AssemblyBuilder.DefineDynamicAssembly(assemblyName, access);
			var moduleName = assembly.FullName + ".dll";
			var module = assembly.DefineDynamicModule(moduleName);
			return module;
		}

		public ISerializer BinarySerializer => _serializer;

		public Type GenerateServant<T>()
		{
			var interfaceType = typeof(T);
			return GenerateServant(interfaceType);
		}

		public Type GenerateServant(Type interfaceType)
		{
			if (!interfaceType.IsInterface)
				throw new ArgumentException(string.Format("Proxies can only be created for interfaces: {0} is not an interface", interfaceType));

			lock (_interfaceToSubject)
			{
				Type proxyType;
				if (!_interfaceToSubject.TryGetValue(interfaceType, out proxyType))
				{
					try
					{
						var proxyTypeName = GetSubjectTypeName(interfaceType);

						var generator = new ServantCompiler(_serializer, _module, proxyTypeName, interfaceType);
						proxyType = generator.Generate();

						_interfaceToSubject.Add(interfaceType, proxyType);
					}
					catch (Exception e)
					{
						var message = string.Format("Unable to create servant for type '{0}': {1}",
						                            interfaceType.Name,
						                            e.Message);
						throw new ArgumentException(message, e);
					}
				}
				return proxyType;
			}
		}

		public IServant CreateServant<T>(IRemotingEndPoint endPoint, IEndPointChannel channel, ulong objectId, T subject)
		{
			var interfaceType = typeof(T);
			var subjectType = GenerateServant<T>();

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
					endPoint,
					channel,
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