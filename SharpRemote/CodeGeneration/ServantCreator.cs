﻿using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using SharpRemote.CodeGeneration.Serialization;

namespace SharpRemote.CodeGeneration
{
	public sealed class ServantCreator
	{
		private readonly Serializer _serializer;
		private readonly AssemblyBuilder _assembly;
		private readonly string _moduleName;
		private readonly Dictionary<Type, Type> _interfaceToSubject;

		public ServantCreator()
		{
			var assemblyName = new AssemblyName("SharpRemote.CodeGeneration.Servants");
			_assembly = AppDomain.CurrentDomain.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.RunAndSave);
			_moduleName = assemblyName.Name + ".dll";
			var module = _assembly.DefineDynamicModule(_moduleName);
			_serializer = new Serializer(module);
			_interfaceToSubject= new Dictionary<Type, Type>();
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

			var assemblyName = GetSubjectAssemblyName(interfaceType);
			var proxyTypeName = GetSubjectTypeName(interfaceType);

			var generator = new ServantCompiler(_serializer, assemblyName, proxyTypeName, interfaceType);
			var proxyType = generator.Generate();

			generator.Save();
			_assembly.Save(_moduleName);

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
					typeof (ISerializer),
					interfaceType
				});
			if (ctor == null)
				throw new Exception();

			return (IServant)ctor.Invoke(new object[]
				{
					objectId,
					_serializer,
					subject
				});
		}

		private AssemblyName GetSubjectAssemblyName(Type interfaceType)
		{
			var fileName = string.Format("{0}.{1}.Servant", interfaceType.Namespace, interfaceType.Name);
			return new AssemblyName(fileName);
		}

		private string GetSubjectTypeName(Type interfaceType)
		{
			return string.Format("{0}.{1}.Servant", interfaceType.Namespace, interfaceType.Name);
		}
	}
}