using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using FluentAssertions;
using NUnit.Framework;
using SharpRemote.Test.Types.Classes;

namespace SharpRemote.Test.CodeGeneration.Serialization.Binary
{
	[TestFixture]
	public sealed class BinarySerializerAcceptanceTest
		: AbstractSerializerAcceptanceTest
	{
		private AssemblyBuilder _assembly;
		private ModuleBuilder _module;

		[SetUp]
		public void Setup()
		{
			var assemblyName = new AssemblyName("SharpRemote.GeneratedCode.Serializer");
			_assembly = AppDomain.CurrentDomain.DefineDynamicAssembly(assemblyName,
			                                                          AssemblyBuilderAccess.RunAndSave);
			string moduleName = assemblyName.Name + ".dll";
			_module = _assembly.DefineDynamicModule(moduleName);
		}

		protected override ISerializer2 Create()
		{
			return new BinarySerializer2(_module);
		}

		protected override void Save()
		{
			var fname = "SharpRemote.GeneratedCode.Serializer.dll";
			try
			{
				_assembly.Save(fname);
				TestContext.Out.WriteLine("Assembly written to: {0}", Path.Combine(Directory.GetCurrentDirectory(), fname));
			}
			catch (Exception e)
			{
				TestContext.Out.WriteLine("Couldn't write assembly: {0}", e);
			}
		}

		public static IEnumerable<ProtocolVersion> SupportedVersions => new[]
		{
			ProtocolVersion.None,
			ProtocolVersion.Version1
		};

		public static IEnumerable<Serializer> SupportedSerializer => new[]
		{
			Serializer.None,
			Serializer.BinarySerializer,
			Serializer.XmlSerializer,
			Serializer.BinarySerializer | Serializer.XmlSerializer
		};

		[Test]
		[Ignore("Not implemented yet")]
		public void TestSerializeHandshakeSync(
			[ValueSource(nameof(SupportedVersions))] ProtocolVersion supportedVersions,
			[ValueSource(nameof(SupportedSerializer))] Serializer supportedSerializers,
			[ValueSource(nameof(ObjectValues))] object challenge
			)
		{
			var message = new HandshakeSyn
			{
				SupportedVersions = supportedVersions,
				SupportedSerializers = supportedSerializers,
				Challenge = challenge
			};
			var actualMessage = Roundtrip(message);
			actualMessage.SupportedVersions.Should().Be(supportedVersions);
			actualMessage.SupportedSerializers.Should().Be(supportedSerializers);
			actualMessage.Challenge.Should().Be(challenge);
		}

		private T Roundtrip<T>(T message)
		{
			var serializer = (BinarySerializer2)Create();
			var serializedMessage = serializer.SerializeWithoutTypeInformation(message);
			var actualMessage = serializer.Deserialize<T>(serializedMessage);
			return actualMessage;
		}

		protected override string Format(MemoryStream stream)
		{
			var value = stream.ToArray();
			var stringBuilder = new StringBuilder(value.Length * 2);
			foreach (var b in value)
				stringBuilder.AppendFormat("{0:x2}", b);
			return stringBuilder.ToString();
		}
	}
}