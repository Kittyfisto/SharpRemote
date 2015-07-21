using System;
using FluentAssertions;
using NUnit.Framework;
using SharpRemote.Test.Types.Classes;
using SharpRemote.Test.Types.Structs;

namespace SharpRemote.Test.CodeGeneration.Serialization
{
	[TestFixture]
	public sealed class SerializationConstraintsTest
	{
		private Serializer _serializer;

		[TestFixtureSetUp]
		public void TestFixtureSetUp()
		{
			_serializer = new Serializer();
		}

		private void TestFailRegister<T>(string reason)
		{
			new Action(() => _serializer.RegisterType<T>())
				.ShouldThrow<ArgumentException>()
				.WithMessage(reason);
			_serializer.IsTypeRegistered<T>().Should().BeFalse();
		}

		[Test]
		[Description("Verifies that registering a type without a [DataContract] attribute is not allowed")]
		public void TestNoDataContractStruct()
		{
			TestFailRegister<MissingDataContractStruct>(
				"The type 'SharpRemote.Test.Types.Structs.MissingDataContractStruct' is missing the [DataContract] or [ByReference] attribute, nor is there a custom-serializer available for this type");
		}

		[Test]
		[Description("Verifies that registering a type that contains a [DataMember] readonly field is not allowed")]
		public void TestReadOnlyDataMemberFieldStruct()
		{
			TestFailRegister<ReadOnlyDataMemberFieldStruct>(
				"The field 'SharpRemote.Test.Types.Structs.ReadOnlyDataMemberFieldStruct.Value' is marked with the [DataMember] attribute but is readonly - this is not supported");
		}

		[Test]
		[Description("Verifies that registering a type that contains a [DataMember] static field is not allowed")]
		public void TestStaticDataMemberFieldStruct()
		{
			TestFailRegister<StaticDataMemberFieldStruct>(
				"The field 'SharpRemote.Test.Types.Structs.StaticDataMemberFieldStruct.Value' is marked with the [DataMember] attribute but is static - this is not supported");
		}

		[Test]
		[Description("Verifies that an class may not be both marked with the [ByReference] and [DataContract] attribute")]
		public void TestInterfaceWithDataContractAndByReference()
		{
			TestFailRegister<ByReferenceAndDataContract>(
				"The type 'SharpRemote.Test.Types.Classes.ByReferenceAndDataContract' is marked with the [DataContract] as well as [ByReference] attribute, but these are mutually exclusive");
		}
	}
}