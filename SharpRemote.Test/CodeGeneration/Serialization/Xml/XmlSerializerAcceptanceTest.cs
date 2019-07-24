using System;
using System.IO;
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
			_assembly = AssemblyBuilder.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.RunAndSave);
			string moduleName = assemblyName.Name + ".dll";
			_module = _assembly.DefineDynamicModule(moduleName);
		}

		protected override ISerializer2 Create()
		{
			return new XmlSerializer(_module);
		}

		protected override void Save()
		{
			//var fname = "SharpRemote.GeneratedCode.Serializer.dll";
			//try
			//{
			//	_assembly.Save(fname);
			//	TestContext.Out.WriteLine("Assembly written to: {0}", Path.Combine(Directory.GetCurrentDirectory(), fname));
			//}
			//catch (Exception e)
			//{
			//	TestContext.Out.WriteLine("Couldn't write assembly: {0}", e);
			//}
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