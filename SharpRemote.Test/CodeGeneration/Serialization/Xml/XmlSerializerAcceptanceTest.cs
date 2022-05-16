﻿using System.IO;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using NUnit.Framework;

namespace SharpRemote.Test.CodeGeneration.Serialization.Xml
{
	[TestFixture]
	public sealed class XmlSerializerAcceptanceTest
		: AbstractSerializerAcceptanceTest
	{
		private AssemblyBuilder _assembly;
		private ModuleBuilder _module;

		[SetUp]
		public void Setup()
		{
			var assemblyName = new AssemblyName("SharpRemote.GeneratedCode.Serializer");
			_assembly = AssemblyBuilder.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.Run);
			string moduleName = assemblyName.Name + ".dll";
			_module = _assembly.DefineDynamicModule(moduleName);
		}

		protected override ISerializer2 Create()
		{
			return new XmlSerializer(_module);
		}

		protected override string Format(MemoryStream stream)
		{
			using (var reader = new StreamReader(stream, Encoding.Default, detectEncodingFromByteOrderMarks: true,
			                                     bufferSize: 4096, leaveOpen: true))
			{
				return reader.ReadToEnd();
			}
		}
	}
}