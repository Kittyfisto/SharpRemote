using System;
using FluentAssertions;
using NUnit.Framework;
using SharpRemote.Test.Types.Classes;
using SharpRemote.Test.Types.Interfaces;
using SharpRemote.Test.Types.Structs;

namespace SharpRemote.Test.CodeGeneration.Serialization
{
	[TestFixture]
	public sealed class SerializationConstraintsTest
	{
		private BinarySerializer _serializer;

		[TestFixtureSetUp]
		public void TestFixtureSetUp()
		{
			_serializer = new BinarySerializer();
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
		[Description("Verifies that a class may not be both marked with the [ByReference] and [DataContract] attribute")]
		public void TestInterfaceWithDataContractAndByReference()
		{
			TestFailRegister<ByReferenceAndDataContract>(
				"The type 'SharpRemote.Test.Types.Classes.ByReferenceAndDataContract' is marked with the [DataContract] as well as [ByReference] attribute, but these are mutually exclusive");
		}

		[Test]
		[Description("Verifies that a property without a setter may not be serialized")]
		public void TestStructPropertyWithoutSetter()
		{
			TestFailRegister<MissingPropertySetterStruct>(
				"The property 'SharpRemote.Test.Types.Structs.MissingPropertySetterStruct.Value' is marked with the [DataMember] attribute but has no setter - this is not supported");
		}

		[Test]
		[Description("Verifies that a property without a getter may not be serialized")]
		public void TestStructPropertyWithoutGetter()
		{
			TestFailRegister<MissingPropertyGetterStruct>(
				"The property 'SharpRemote.Test.Types.Structs.MissingPropertyGetterStruct.Value' is marked with the [DataMember] attribute but has no getter - this is not supported");
		}

		#region Struct with callbacks not allowed

		[Test]
		[Description("Verifies that structures may not have serialization callbacks")]
		public void TestStructWithBeforeSerializeCallback()
		{
			TestFailRegister<StructWithBeforeSerialize>(
				"The type 'SharpRemote.Test.Types.Structs.StructWithBeforeSerialize' may not contain methods marked with the [BeforeSerialize] attribute: Only classes may have these callbacks"
				);
		}

		[Test]
		[Description("Verifies that structures may not have serialization callbacks")]
		public void TestStructWithAfterSerializeCallback()
		{
			TestFailRegister<StructWithAfterSerialize>(
				"The type 'SharpRemote.Test.Types.Structs.StructWithAfterSerialize' may not contain methods marked with the [AfterSerialize] attribute: Only classes may have these callbacks"
			);
		}

		[Test]
		[Description("Verifies that structures may not have serialization callbacks")]
		public void TestStructWithBeforeDeserializeCallback()
		{
			TestFailRegister<StructWithBeforeDeserialize>(
				"The type 'SharpRemote.Test.Types.Structs.StructWithBeforeDeserialize' may not contain methods marked with the [BeforeDeserialize] attribute: Only classes may have these callbacks"
			);
		}

		[Test]
		[Description("Verifies that structures may not have serialization callbacks")]
		public void TestStructWithAfterDeserializeCallback()
		{
			TestFailRegister<StructWithAfterDeserialize>(
				"The type 'SharpRemote.Test.Types.Structs.StructWithAfterDeserialize' may not contain methods marked with the [AfterDeserialize] attribute: Only classes may have these callbacks"
			);
		}

		#endregion

		#region By reference type callbacks not allowed

		[Test]
		[Description("Verifies that types with the ByReferenceAttribute may not have serialization callbacks")]
		public void TestByReferenceWithBeforeDeserializeCallback()
		{
			TestFailRegister<IByReferenceWithBeforeDeserializeCallback>(
				"The type 'SharpRemote.Test.Types.Interfaces.IByReferenceWithBeforeDeserializeCallback' is marked with the [ByReference] attribute and thus may not contain methods marked with the [BeforeDeserialize] attribute"
			);
		}

		[Test]
		[Description("Verifies that types with the ByReferenceAttribute may not have serialization callbacks")]
		public void TestByReferenceWithAfterDeserializeCallback()
		{
			TestFailRegister<IByReferenceWithAfterDeserializeCallback>(
				"The type 'SharpRemote.Test.Types.Interfaces.IByReferenceWithAfterDeserializeCallback' is marked with the [ByReference] attribute and thus may not contain methods marked with the [AfterDeserialize] attribute"
				);
		}

		[Test]
		[Description("Verifies that types with the ByReferenceAttribute may not have serialization callbacks")]
		public void TestByReferenceWithBeforeSerializeCallback()
		{
			TestFailRegister<IByReferenceWithBeforeSerializeCallback>(
				"The type 'SharpRemote.Test.Types.Interfaces.IByReferenceWithBeforeSerializeCallback' is marked with the [ByReference] attribute and thus may not contain methods marked with the [BeforeSerialize] attribute"
			);
		}

		[Test]
		[Description("Verifies that types with the ByReferenceAttribute may not have serialization callbacks")]
		public void TestByReferenceWithAfterSerializeCallback()
		{
			TestFailRegister<IByReferenceWithAfterSerializeCallback>(
				"The type 'SharpRemote.Test.Types.Interfaces.IByReferenceWithAfterSerializeCallback' is marked with the [ByReference] attribute and thus may not contain methods marked with the [AfterSerialize] attribute"
			);
		}

		#endregion

		#region Multiple callbacks of the same type not allowed

		[Test]
		[Description("Verifies that types may not be registered if they contain more than one serialization callback of the same type")]
		public void TestTooManyAfterDeserializeCallbacks()
		{
			TestFailRegister<TooManyAfterDeserializeCallbacks>(
				"The type 'SharpRemote.Test.Types.Classes.TooManyAfterDeserializeCallbacks' contains too many methods with the [AfterDeserialize] attribute: There may not be more than one"
			);
		}

		[Test]
		[Description("Verifies that types may not be registered if they contain more than one serialization callback of the same type")]
		public void TestTooManyBeforeDeserializeCallbacks()
		{
			TestFailRegister<TooManyBeforeDeserializeCallbacks>(
				"The type 'SharpRemote.Test.Types.Classes.TooManyBeforeDeserializeCallbacks' contains too many methods with the [BeforeDeserialize] attribute: There may not be more than one"
			);
		}

		[Test]
		[Description("Verifies that types may not be registered if they contain more than one serialization callback of the same type")]
		public void TestTooManyAfterSerializeCallbacks()
		{
			TestFailRegister<TooManyAfterSerializeCallbacks>(
				"The type 'SharpRemote.Test.Types.Classes.TooManyAfterSerializeCallbacks' contains too many methods with the [AfterSerialize] attribute: There may not be more than one"
			);
		}

		[Test]
		[Description("Verifies that types may not be registered if they contain more than one serialization callback of the same type")]
		public void TestTooManyBeforeSerializeCallbacks()
		{
			TestFailRegister<TooManyBeforeSerializeCallbacks>(
				"The type 'SharpRemote.Test.Types.Classes.TooManyBeforeSerializeCallbacks' contains too many methods with the [BeforeSerialize] attribute: There may not be more than one"
			);
		}

		#endregion

		#region Non-public callbacks not allowed

		[Test]
		public void TestNonPublicBeforeSerializeCallback()
		{
			TestFailRegister<NonPublicBeforeSerializeCallback>(
				"The method 'SharpRemote.Test.Types.Classes.NonPublicBeforeSerializeCallback.BeforeSerialize()' is marked with the [BeforeSerialize] attribute and must therefore be publicly accessible"
			);
		}

		[Test]
		public void TestNonPublicAfterSerializeCallback()
		{
			TestFailRegister<NonPublicAfterSerializeCallback>(
				"The method 'SharpRemote.Test.Types.Classes.NonPublicAfterSerializeCallback.AfterSerialize()' is marked with the [AfterSerialize] attribute and must therefore be publicly accessible"
			);
		}

		[Test]
		public void TestNonPublicBeforeDeserializeCallback()
		{
			TestFailRegister<NonPublicBeforeDeserializeCallback>(
				"The method 'SharpRemote.Test.Types.Classes.NonPublicBeforeDeserializeCallback.BeforeDeserialize()' is marked with the [BeforeDeserialize] attribute and must therefore be publicly accessible"
			);
		}

		[Test]
		public void TestNonPublicAfterDeserializeCallback()
		{
			TestFailRegister<NonPublicAfterDeserializeCallback>(
				"The method 'SharpRemote.Test.Types.Classes.NonPublicAfterDeserializeCallback.AfterDeserialize()' is marked with the [AfterDeserialize] attribute and must therefore be publicly accessible"
			);
		}

		#endregion

		#region Static Callbacks not allowed

		[Test]
		public void TestStaticAfterDeserializeCallback()
		{
			TestFailRegister<StaticAfterDeserializeCallback>(
				"The method 'SharpRemote.Test.Types.Classes.StaticAfterDeserializeCallback.AfterDeserialize()' is marked with the [AfterDeserialize] attribute and must therefore be non-static"
			);
		}

		[Test]
		public void TestStaticBeforeDeserializeCallback()
		{
			TestFailRegister<StaticBeforeDeserializeCallback>(
				"The method 'SharpRemote.Test.Types.Classes.StaticBeforeDeserializeCallback.BeforeDeserialize()' is marked with the [BeforeDeserialize] attribute and must therefore be non-static"
			);
		}

		[Test]
		public void TestStaticAfterSerializeCallback()
		{
			TestFailRegister<StaticAfterSerializeCallback>(
				"The method 'SharpRemote.Test.Types.Classes.StaticAfterSerializeCallback.AfterSerialize()' is marked with the [AfterSerialize] attribute and must therefore be non-static"
			);
		}

		[Test]
		public void TestStaticBeforeSerializeCallback()
		{
			TestFailRegister<StaticBeforeSerializeCallback>(
				"The method 'SharpRemote.Test.Types.Classes.StaticBeforeSerializeCallback.BeforeSerialize()' is marked with the [BeforeSerialize] attribute and must therefore be non-static"
			);
		}

		#endregion

		#region Callbacks with parameters not allowed

		[Test]
		public void TestAfterSerializeWithParameters()
		{
			TestFailRegister<AfterSerializeCallbackWithParameters>(
				"The method 'SharpRemote.Test.Types.Classes.AfterSerializeCallbackWithParameters.AfterSerialize()' is marked with the [AfterSerialize] attribute and must therefore be parameterless"
			);
		}

		[Test]
		public void TestBeforeSerializeWithParameters()
		{
			TestFailRegister<BeforeSerializeCallbackWithParameters>(
				"The method 'SharpRemote.Test.Types.Classes.BeforeSerializeCallbackWithParameters.BeforeSerialize()' is marked with the [BeforeSerialize] attribute and must therefore be parameterless"
			);
		}

		[Test]
		public void TestAfterDeserializeWithParameters()
		{
			TestFailRegister<AfterDeserializeCallbackWithParameters>(
				"The method 'SharpRemote.Test.Types.Classes.AfterDeserializeCallbackWithParameters.AfterDeserialize()' is marked with the [AfterDeserialize] attribute and must therefore be parameterless"
			);
		}

		[Test]
		public void TestBeforeDeserializeWithParameters()
		{
			TestFailRegister<BeforeDeserializeCallbackWithParameters>(
				"The method 'SharpRemote.Test.Types.Classes.BeforeDeserializeCallbackWithParameters.BeforeDeserialize()' is marked with the [BeforeDeserialize] attribute and must therefore be parameterless"
			);
		}

		#endregion

		#region Generic Callbacks not allowed

		[Test]
		public void TestGenericAfterDeserializeCallback()
		{
			TestFailRegister<GenericAfterDeserializeCallback>(
				"The method 'SharpRemote.Test.Types.Classes.GenericAfterDeserializeCallback.AfterDeserialize()' is marked with the [AfterDeserialize] attribute and must therefore be non-generic"
			);
		}

		[Test]
		public void TestGenericBeforeDeserializeCallback()
		{
			TestFailRegister<GenericBeforeDeserializeCallback>(
				"The method 'SharpRemote.Test.Types.Classes.GenericBeforeDeserializeCallback.BeforeDeserialize()' is marked with the [BeforeDeserialize] attribute and must therefore be non-generic"
			);
		}

		[Test]
		public void TestGenericAfterSerializeCallback()
		{
			TestFailRegister<GenericAfterSerializeCallback>(
				"The method 'SharpRemote.Test.Types.Classes.GenericAfterSerializeCallback.AfterSerialize()' is marked with the [AfterSerialize] attribute and must therefore be non-generic"
			);
		}

		[Test]
		public void TestGenericBeforeSerializeCallback()
		{
			TestFailRegister<GenericBeforeSerializeCallback>(
				"The method 'SharpRemote.Test.Types.Classes.GenericBeforeSerializeCallback.BeforeSerialize()' is marked with the [BeforeSerialize] attribute and must therefore be non-generic"
			);
		}

		#endregion

		#region Singletons with callbacks not allowed

		[Test]
		public void TestSingletonWithBeforeDeserialize()
		{
			TestFailRegister<SingletonWithBeforeDeserializeCallback>(
				"The Type 'SharpRemote.Test.Types.Classes.SingletonWithBeforeDeserializeCallback' is a singleton and thus may not contain any serialization callbacks"
			);
		}

		[Test]
		public void TestSingletonWithAfterDeserialize()
		{
			TestFailRegister<SingletonWithAfterDeserializeCallback>(
				"The Type 'SharpRemote.Test.Types.Classes.SingletonWithAfterDeserializeCallback' is a singleton and thus may not contain any serialization callbacks"
			);
		}

		[Test]
		public void TestSingletonWithBeforeSerialize()
		{
			TestFailRegister<SingletonWithBeforeSerializeCallback>(
				"The Type 'SharpRemote.Test.Types.Classes.SingletonWithBeforeSerializeCallback' is a singleton and thus may not contain any serialization callbacks"
			);
		}

		[Test]
		public void TestSingletonWithAfterSerialize()
		{
			TestFailRegister<SingletonWithAfterSerializeCallback>(
				"The Type 'SharpRemote.Test.Types.Classes.SingletonWithAfterSerializeCallback' is a singleton and thus may not contain any serialization callbacks"
			);
		}

		#endregion

		#region Singletons with ByReference not allowed

		[Test]
		public void TestSingletonsWithByReferenceNotAllowed()
		{
			TestFailRegister<SingletonByReference>(
				"The type 'SharpRemote.Test.Types.Classes.SingletonByReference' both has a method marked with the SingletonFactoryMethod attribute and also implements an interface 'SharpRemote.Test.Types.Interfaces.IByReferenceType' which has the ByReference attribute: This is not allowed; they are mutually exclusive");
		}

		#endregion
	}
}