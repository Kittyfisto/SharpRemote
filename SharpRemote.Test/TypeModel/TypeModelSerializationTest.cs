using System;
using FluentAssertions;
using NUnit.Framework;
using SharpRemote.Test.CodeGeneration.Serialization;
using SharpRemote.Test.Types.Interfaces;
using SharpRemote.Test.Types.Structs;

namespace SharpRemote.Test.TypeModel
{
	[TestFixture]
	public sealed class TypeModelSerializationTest
	{
		private BinarySerializer _serializer;

		[OneTimeSetUp]
		public void OneTimeSetUp()
		{
			_serializer = new BinarySerializer();
		}

		[Test]
		public void TestRoundtripEmpty()
		{
			var expected = new SharpRemote.TypeModel();
			var actual = Roundtrip(expected);
			actual.Should().NotBeNull();
			actual.Types.Should().BeEmpty();
		}

		[Test]
		[Description("Verifies that base type references are preserved through serialization")]
		public void TestRoundtripInt()
		{
			var expected = new SharpRemote.TypeModel();
			expected.Add<int>();

			var actual = Roundtrip(expected);
			actual.Should().NotBeNull();
			var intType = actual.Get<int>();

			var valueType = intType.BaseType;
			valueType.Should().NotBeNull();
			valueType.Type.Should().Be<ValueType>();

			var objectType = valueType.BaseType;
			objectType.Should().NotBeNull();
			objectType.Type.Should().Be<object>();

			actual.Get<ValueType>().Should().BeSameAs(valueType);
			actual.Get<object>().Should().BeSameAs(objectType);
		}

		[Test]
		public void TestRoundtripByteArray()
		{
			var expected = new SharpRemote.TypeModel();
			expected.Add<byte[]>();

			var actual = Roundtrip(expected);
			actual.Should().NotBeNull();
			actual.Contains<byte[]>().Should().BeTrue();
			actual.Contains<byte>().Should().BeTrue();
			actual.Contains<Array>().Should().BeTrue();

			var byteArrayDescription = actual.Get<byte[]>();
			byteArrayDescription.IsGenericType.Should()
				.BeFalse("because arrays are special and don't count as generic types");
			byteArrayDescription.GenericArguments.Should().HaveCount(1);
			var byteDescription = byteArrayDescription.GenericArguments[0];
			byteDescription.Should().BeSameAs(actual.Get<byte>());
		}

		[Test]
		public void TestRoundtripFieldUInt32()
		{
			var expected = new SharpRemote.TypeModel();
			expected.Add<FieldUInt32>();

			var actual = Roundtrip(expected);
			var actualDescription = actual.Get<FieldUInt32>();
			actualDescription.AssemblyQualifiedName.Should().Be(typeof(FieldUInt32).AssemblyQualifiedName);
			actualDescription.IsClass.Should().BeFalse();
			actualDescription.IsBuiltIn.Should().BeFalse();
			actualDescription.IsEnum.Should().BeFalse();
			actualDescription.IsGenericType.Should().BeFalse();
			actualDescription.IsInterface.Should().BeFalse();
			actualDescription.IsSealed.Should().BeTrue();
			actualDescription.IsValueType.Should().BeTrue();
			actualDescription.Type.Should().Be<FieldUInt32>("because the type should've been resolved");
			actualDescription.Properties.Should().BeEmpty();
			actualDescription.Methods.Should().BeEmpty();
			actualDescription.Fields.Should().HaveCount(1);
			var actualField = actualDescription.Fields[0];
			actualField.Name.Should().Be(nameof(FieldUInt32.Value));
			actualField.FieldType.Should().NotBeNull();
			actualField.FieldType.Type.Should().Be<UInt32>();
		}

		[Test]
		public void TestRoundtripPropertyStruct()
		{
			var expected = new SharpRemote.TypeModel();
			expected.Add<PropertyStruct>();

			var actual = Roundtrip(expected);
			var actualDescription = actual.Get<PropertyStruct>();
			actualDescription.AssemblyQualifiedName.Should().Be(typeof(PropertyStruct).AssemblyQualifiedName);
			actualDescription.Type.Should().Be<PropertyStruct>("because the type should've been resolved");
			actualDescription.Fields.Should().BeEmpty();
			actualDescription.Methods.Should().BeEmpty();
			actualDescription.Properties.Should().HaveCount(1);
			var actualProperty = actualDescription.Properties[0];
			actualProperty.Name.Should().Be(nameof(PropertyStruct.Value));
			actualProperty.PropertyType.Should().NotBeNull("because the type of the property should've been reserved");
			actualProperty.PropertyType.Type.Should().Be<string>();
		}

		[Test]
		public void TestRoundtripIVoidMethodNoParameters()
		{
			var expected = new SharpRemote.TypeModel();
			expected.Add<IVoidMethodNoParameters>(assumeByReference: true);

			var actual = Roundtrip(expected);
			var actualDescription = actual.Get<IVoidMethodNoParameters>();
			actualDescription.AssemblyQualifiedName.Should().Be(typeof(IVoidMethodNoParameters).AssemblyQualifiedName);
			actualDescription.IsBuiltIn.Should().BeFalse();
			actualDescription.IsClass.Should().BeFalse();
			actualDescription.IsInterface.Should().BeTrue();
			actualDescription.IsValueType.Should().BeFalse();
			actualDescription.IsEnum.Should().BeFalse();
			actualDescription.IsEnumerable.Should().BeFalse();
			actualDescription.Type.Should().Be<IVoidMethodNoParameters>("because the type should've been resolved");
			actualDescription.Fields.Should().BeEmpty();
			actualDescription.Properties.Should().BeEmpty();
			actualDescription.Methods.Should().HaveCount(1);

			var method = actualDescription.Methods[0];
			method.Name.Should().Be(nameof(IVoidMethodNoParameters.Do));
			method.Parameters.Should().BeEmpty();
			method.ReturnParameter.Should().NotBeNull();

			var returnParameter = method.ReturnParameter;
			returnParameter.ParameterType.Should().NotBeNull();
			returnParameter.ParameterType.Type.Should().Be(typeof(void));
		}

		[Test]
		public void TestRoundtripFieldObjectStruct()
		{
			var expected = new SharpRemote.TypeModel();
			expected.Add<FieldObjectStruct>();

			var actual = Roundtrip(expected);
			var actualDescription = actual.Get<FieldObjectStruct>();
			actualDescription.AssemblyQualifiedName.Should().Be(typeof(FieldObjectStruct).AssemblyQualifiedName);
			actualDescription.Type.Should().Be<FieldObjectStruct>("because the type should've been resolved");
			actualDescription.Properties.Should().BeEmpty();
			actualDescription.Methods.Should().BeEmpty();
			actualDescription.Fields.Should().HaveCount(1);

			var field = actualDescription.Fields[0];
			field.Name.Should().Be(nameof(FieldObjectStruct.Value));
			field.FieldType.Should().NotBeNull();
			field.FieldType.AssemblyQualifiedName.Should().Be(typeof(object).AssemblyQualifiedName);
			field.FieldType.Type.Should().Be<object>();
		}

		private SharpRemote.TypeModel Roundtrip(SharpRemote.TypeModel expected)
		{
			var actual = _serializer.Roundtrip(expected);
			actual.TryResolveTypes();
			return actual;
		}
	}
}
