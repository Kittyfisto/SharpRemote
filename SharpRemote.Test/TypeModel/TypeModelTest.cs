using System;
using System.Linq;
using FluentAssertions;
using NUnit.Framework;
using SharpRemote.Test.Types.Structs;
using System.Collections.Generic;
using SharpRemote.Test.Types.Classes;
using SharpRemote.Test.Types.Enums;
using SharpRemote.Test.Types.Interfaces;

namespace SharpRemote.Test.TypeModel
{
	[TestFixture]
	public sealed class TypeModelTest
	{
		[Test]
		public void TestCtor()
		{
			var model = new SharpRemote.TypeModel();
			model.Types.Should().NotBeNull();
		}

		[Test]
		public void TestAddType1()
		{
			var model = new SharpRemote.TypeModel();
			model.Add(typeof(void));
			model.Types.Should().HaveCount(1);
			var type = model.Types.First();
			type.AssemblyQualifiedName.Should().Be(typeof(void).AssemblyQualifiedName);
		}

		public static IEnumerable<Type> BuiltInTypes => new[]
		{
			typeof(byte),
			typeof(sbyte),
			typeof(ushort),
			typeof(short),
			typeof(uint),
			typeof(int),
			typeof(ulong),
			typeof(long),
			typeof(float),
			typeof(double),
			typeof(string),
			typeof(decimal)
			// TODO: Add all other built in types
			// typeof(IpAddress)
		};

		[Test]
		public void TestBuiltInType([ValueSource(nameof(BuiltInTypes))] Type type)
		{
			var model = new SharpRemote.TypeModel();
			var description = model.Add(type);
			description.SerializationType.Should().Be(SerializationType.ByValue);
			description.IsBuiltIn.Should().BeTrue();
			description.IsClass.Should().Be(type.IsClass);
			description.IsSealed.Should().Be(type.IsSealed);
			description.IsEnum.Should().Be(type.IsEnum);
			description.IsInterface.Should().Be(type.IsInterface);
			description.IsValueType.Should().Be(type.IsValueType);
		}

		[Test]
		[Description("Verifies that the TypeModel can describe itself")]
		public void TestTypeModel()
		{
			var model = new SharpRemote.TypeModel();
			model.Add<SharpRemote.TypeModel>();
		}

		[Test]
		public void TestFieldObjectStruct()
		{
			var model = new SharpRemote.TypeModel();
			var description = model.Add<FieldObjectStruct>();

			var field = description.Fields[0];
			field.Name.Should().Be(nameof(FieldObjectStruct.Value));
			field.FieldType.SerializationType.Should().Be(SerializationType.Unknown, "because the serialization is not known due to the field-type to be non-closed (i.e. System.Object)");
			field.FieldType.IsEnum.Should().BeFalse();
			field.FieldType.IsEnumerable.Should().BeFalse();
			field.FieldType.IsSealed.Should().BeFalse();
			field.FieldType.IsGenericType.Should().BeFalse();
			field.FieldType.IsInterface.Should().BeFalse();
			field.FieldType.IsClass.Should().BeTrue();
			field.FieldType.IsBuiltIn.Should().BeFalse();
			field.FieldType.Type.Should().Be<object>();
			field.FieldType.AssemblyQualifiedName.Should().Be(typeof(object).AssemblyQualifiedName);
			field.FieldType.Properties.Should().BeEmpty();
			field.FieldType.Fields.Should().BeEmpty();
			field.FieldType.Methods.Should().BeEmpty();
		}

		[Test]
		public void TestFieldIEnumerable()
		{
			var model = new SharpRemote.TypeModel();
			var description = model.Add<FieldIEnumerable>();

			var field = description.Fields[0];
			field.Name.Should().Be(nameof(FieldIEnumerable.Values));
			field.FieldType.SerializationType.Should().Be(SerializationType.ByValue, "because all enumerations are serialized by value");
			field.FieldType.IsEnum.Should().BeFalse();
			field.FieldType.IsEnumerable.Should().BeTrue();
		}

		[Test]
		public void TestFieldEnum()
		{
			var model = new SharpRemote.TypeModel();
			var type = model.Add<FieldEnum>();
			type.Properties.Should().BeEmpty("because the type doesn't have any properties");
			type.Methods.Should().BeEmpty("because the methods of DataContract types are uninteresting");
			type.IsClass.Should().BeFalse("because the type is a struct");
			type.IsEnum.Should().BeFalse("because the type is a struct");
			type.IsValueType.Should().BeTrue("because the type is a struct");
			type.IsInterface.Should().BeFalse("because the type is a struct");
			type.IsSealed.Should().BeTrue("because structs are always sealed");
			type.Fields.Should().HaveCount(1);

			var field1 = type.Fields[0];
			field1.Name.Should().Be(nameof(FieldEnum.Value));
			field1.FieldType.AssemblyQualifiedName.Should().Be(typeof(DataContractEnum).AssemblyQualifiedName);
			field1.FieldType.SerializationType.Should().Be(SerializationType.ByValue);
			field1.FieldType.IsEnum.Should().BeTrue();
		}

		[Test]
		public void TestFieldStruct()
		{
			var model = new SharpRemote.TypeModel();
			var type = model.Add<FieldStruct>();
			type.AssemblyQualifiedName.Should().Be(typeof(FieldStruct).AssemblyQualifiedName);
			type.SerializationType.Should().Be(SerializationType.ByValue);
			type.Properties.Should().BeEmpty("because the type doesn't have any properties");
			type.Methods.Should().BeEmpty("because the methods of DataContract types are uninteresting");
			type.IsClass.Should().BeFalse("because the type is a struct");
			type.IsEnum.Should().BeFalse("because the type is a struct");
			type.IsValueType.Should().BeTrue("because the type is a struct");
			type.IsInterface.Should().BeFalse("because the type is a struct");
			type.IsSealed.Should().BeTrue("because structs are always sealed");
			type.Fields.Should().HaveCount(3);

			var field1 = type.Fields[0];
			field1.Name.Should().Be(nameof(FieldStruct.A));
			field1.FieldType.AssemblyQualifiedName.Should().Be(typeof(double).AssemblyQualifiedName);
			field1.FieldType.SerializationType.Should().Be(SerializationType.ByValue);
			field1.FieldType.IsBuiltIn.Should().BeTrue();

			var field2 = type.Fields[1];
			field2.Name.Should().Be(nameof(FieldStruct.B));
			field2.FieldType.AssemblyQualifiedName.Should().Be(typeof(int).AssemblyQualifiedName);
			field2.FieldType.SerializationType.Should().Be(SerializationType.ByValue);
			field2.FieldType.IsBuiltIn.Should().BeTrue();

			var field3 = type.Fields[2];
			field3.Name.Should().Be(nameof(FieldStruct.C));
			field3.FieldType.AssemblyQualifiedName.Should().Be(typeof(string).AssemblyQualifiedName);
			field3.FieldType.SerializationType.Should().Be(SerializationType.ByValue);
			field3.FieldType.IsBuiltIn.Should().BeTrue();

			model.Types.Should().BeEquivalentTo(new object[]
			{
				type, //< FieldStruct
				field1.FieldType, //< double
				field2.FieldType, //< int
				field3.FieldType //< string
			});
		}

		[Test]
		public void TestFieldDecimal()
		{
			var model = new SharpRemote.TypeModel();
			var type = model.Add<FieldDecimal>();
			type.AssemblyQualifiedName.Should().Be(typeof(FieldDecimal).AssemblyQualifiedName);
			type.SerializationType.Should().Be(SerializationType.ByValue);
			type.Properties.Should().BeEmpty("because the type doesn't have any properties");
			type.Methods.Should().BeEmpty("because the methods of DataContract types are uninteresting");
			type.IsClass.Should().BeFalse("because the type is a struct");
			type.IsEnum.Should().BeFalse("because the type is a struct");
			type.IsValueType.Should().BeTrue("because the type is a struct");
			type.IsInterface.Should().BeFalse("because the type is a struct");
			type.IsSealed.Should().BeTrue("because structs are always sealed");
			type.Fields.Should().HaveCount(1);
			var field = type.Fields[0];
			field.Name.Should().Be("Value");
			field.FieldType.AssemblyQualifiedName.Should().Be(typeof(decimal).AssemblyQualifiedName);
			field.FieldType.SerializationType.Should().Be(SerializationType.ByValue);
			field.FieldType.IsBuiltIn.Should().BeTrue();
		}

		[Test]
		[Description("Verifies that the type model can describe a 'by reference' type")]
		public void TestByReferenceType()
		{
			var model = new SharpRemote.TypeModel();
			var type = model.Add<IByReferenceType>();
			type.AssemblyQualifiedName.Should().Be(typeof(IByReferenceType).AssemblyQualifiedName);
			type.SerializationType.Should().Be(SerializationType.ByReference);
			type.IsClass.Should().BeFalse();
			type.IsBuiltIn.Should().BeFalse();
			type.IsEnum.Should().BeFalse();
			type.IsInterface.Should().BeTrue();
			type.IsSealed.Should().BeFalse();
			type.IsValueType.Should().BeFalse();
			type.Fields.Should().BeEmpty("because by reference types cannot have fields");
			type.Methods.Should().BeEmpty("because special methods can only be accessed via their property");

			type.Properties.Should().HaveCount(1);
			var property = type.Properties[0];
			property.Name.Should().Be("Value");
			property.GetMethod.Should().NotBeNull();
			property.GetMethod.Name.Should().Be("get_Value");
			property.GetMethod.ReturnParameter.Should().NotBeNull();
			property.SetMethod.Should().BeNull();
			property.PropertyType.AssemblyQualifiedName.Should().Be(typeof(int).AssemblyQualifiedName);
		}

		[Test]
		[Description("Verifies that the type model can describe a type which itself inherits from another serializable type")]
		public void TestInheritedClass()
		{
			var model = new SharpRemote.TypeModel();
			var type = model.Add<InheritedClass>();
			type.Properties.Should().HaveCount(1);
			var property = type.Properties[0];
			property.Name.Should().Be("Value1");
			property.PropertyType.AssemblyQualifiedName.Should().Be(typeof(long).AssemblyQualifiedName);

			type.BaseType.Should().NotBeNull();
			type = type.BaseType;
			type.BaseType.Should().BeNull("because the type model only concentrates the serializable aspect of types, something which the base type object doesn't have any impact on");
			type.Properties.Should().HaveCount(1);
			property = type.Properties[0];
			property.Name.Should().Be("Value1");
			property.PropertyType.AssemblyQualifiedName.Should().Be(typeof(string).AssemblyQualifiedName);

			var field = type.Fields[0];
			field.Name.Should().Be("Value2");
			field.FieldType.AssemblyQualifiedName.Should().Be(typeof(bool).AssemblyQualifiedName);
		}

		[Test]
		public void TestRecursiveClass()
		{
			var model = new SharpRemote.TypeModel();
			var type = model.Add<RecursiveClass>();
			type.Fields.Should().HaveCount(2);
			var field = type.Fields[0];
			field.Name.Should().Be("Left");
			field.FieldType.Should().BeSameAs(type, "because a recursive type model shall reference the very same object");

			field = type.Fields[1];
			field.Name.Should().Be("Right");
			field.FieldType.Should().BeSameAs(type, "because a recursive type model shall reference the very same object");
		}

		[Test]
		public void TestAddNull()
		{
			var model = new SharpRemote.TypeModel();
			new Action(() => model.Add(null)).ShouldThrow<ArgumentNullException>();
		}

		[Test]
		[Description("Verifies that adding the same type again is a NOP")]
		public void TestAddTwice()
		{
			var model = new SharpRemote.TypeModel();
			var type1 = model.Add<string>();
			var type2 = model.Add<string>();
			type2.Should().BeSameAs(type1);
			model.Types.Should().HaveCount(1);
		}

		[Test]
		public void TestContains1()
		{
			var model = new SharpRemote.TypeModel();
			model.Contains<string>().Should().BeFalse();
			model.Add<string>();
			model.Contains<string>().Should().BeTrue();
		}
	}
}
