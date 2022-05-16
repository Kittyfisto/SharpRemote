﻿using System;
using System.Linq;
using FluentAssertions;
using NUnit.Framework;
using SharpRemote.Test.Types.Structs;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using log4net.Core;
using SharpRemote.Test.Types.Classes;
using SharpRemote.Test.Types.Enums;
using SharpRemote.Test.Types.Interfaces;
using SharpRemote.Test.Types.Interfaces.PrimitiveTypes;

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
		public void TestIReturnsTask()
		{
			var model = new SharpRemote.TypeModel();
			var type = model.Add<IReturnsTask>(assumeByReference: true);
			type.Methods.Should().HaveCount(1);
			var method = type.Methods[0];
			method.Name.Should().Be(nameof(IReturnsTask.DoStuff));
			method.IsAsync.Should().BeTrue();

			var returnType = method.ReturnType;
			returnType.Type.Should().Be<Task>();
			returnType.IsGenericType.Should().BeFalse();
			returnType.GenericArguments.Should().BeEmpty();
		}

		[Test]
		public void TestIReturnsIntTask()
		{
			var model = new SharpRemote.TypeModel();
			var type = model.Add<IReturnsIntTask>(assumeByReference: true);
			type.Methods.Should().HaveCount(1);
			var method = type.Methods[0];
			method.Name.Should().Be(nameof(IReturnsIntTask.DoStuff));
			method.IsAsync.Should().BeTrue();

			var returnType = method.ReturnType;
			returnType.Type.Should().Be<Task<int>>();
			returnType.IsGenericType.Should().BeTrue();
			returnType.GenericArguments.Should().HaveCount(1);

			var argument = returnType.GenericArguments[0];
			argument.Type.Should().Be<int>();
		}

		[Test]
		public void TestIInt32Method()
		{
			var model = new SharpRemote.TypeModel();
			var type = model.Add<IInt32Method>(assumeByReference: true);
			type.SerializationType.Should().Be(SerializationType.ByReference);
			type.Methods.Should().HaveCount(1);
			var method = type.Methods[0];
			method.Name.Should().Be(nameof(IInt32Method.DoStuff));
			method.IsAsync.Should().BeFalse();
			method.Parameters.Should().BeEmpty();
			method.ReturnType.Type.Should().Be<int>();
		}

		[Test]
		public void TestIVoidMethodInt64Parameter()
		{
			var model = new SharpRemote.TypeModel();
			var type = model.Add<IVoidMethodInt64Parameter>(assumeByReference: true);
			type.SerializationType.Should().Be(SerializationType.ByReference);
			type.Methods.Should().HaveCount(1);
			var method = type.Methods[0];
			method.Name.Should().Be(nameof(IVoidMethodInt64Parameter.Do));
			method.ReturnType.Type.Should().Be(typeof(void));
			method.Parameters.Should().HaveCount(1);
			method.Parameters[0].Name.Should().Be("value");
			method.Parameters[0].IsIn.Should().BeFalse();
			method.Parameters[0].IsOut.Should().BeFalse();
			method.Parameters[0].IsRetval.Should().BeFalse();
			method.Parameters[0].Position.Should().Be(0);
			method.Parameters[0].ParameterType.Type.Should().Be<Int64>();
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
			typeof(decimal),
			typeof(DateTime),
			typeof(DateTimeOffset),
			typeof(TimeSpan),
			typeof(Uri),
			typeof(IPAddress),
			typeof(Version),
			typeof(Guid),
			typeof(Level)
			// TODO: Add all other built in types
			// 
		};

		[Test]
		public void TestHierarchy()
		{
			var model = new SharpRemote.TypeModel();
			model.Add<BaseClass>();
			model.Add<Tree>();
			model.Add<Birke>();

			model.Types.Should().Contain(x => x.Type == typeof(BaseClass));
			model.Types.Should().Contain(x => x.Type == typeof(Tree));
			model.Types.Should().Contain(x => x.Type == typeof(Birke));
		}

		[Test]
		public void TestAddVoid()
		{
			var model = new SharpRemote.TypeModel();
			var description = model.Add(typeof(void));
			description.Should().NotBeNull();
			description.AssemblyQualifiedName.Should().Be(typeof(void).AssemblyQualifiedName);
			((TypeDescription) description).Id.Should().BeGreaterThan(0);
			model.GetId(typeof(void)).Should().Be(((TypeDescription) description).Id);
			model.Types.Should().Contain((TypeDescription) description);
		}

		[Test]
		[Description("Verifies that inheritance is correctly modeled for the int type")]
		public void TestAddInt()
		{
			var model = new SharpRemote.TypeModel();
			var intType = model.Add<int>();
			intType.Should().NotBeNull();

			var valueType = intType.BaseType;
			valueType.Should().NotBeNull();
			valueType.Type.Should().Be<ValueType>();

			var objectType = valueType.BaseType;
			objectType.Should().NotBeNull();
			objectType.Type.Should().Be<object>();

			((TypeDescription)intType).Id.Should().BeGreaterThan(0);
			((TypeDescription)valueType).Id.Should().BeGreaterThan(0);
			((TypeDescription)objectType).Id.Should().BeGreaterThan(0);

			((TypeDescription)intType).BaseTypeId.Should().Be(((TypeDescription)valueType).Id);
			((TypeDescription)valueType).BaseTypeId.Should().Be(((TypeDescription)objectType).Id);

			model.Get<int>().Should().BeSameAs(intType);
			model.Get<ValueType>().Should().BeSameAs(valueType);
			model.Get<object>().Should().BeSameAs(objectType);
		}

		[Test]
		public void TestAddIVoidMethodNoParameters()
		{
			var model = new SharpRemote.TypeModel();
			var description = model.Add<IVoidMethodNoParameters>(assumeByReference: true);
			description.Should().NotBeNull();
			description.AssemblyQualifiedName.Should().Be(typeof(IVoidMethodNoParameters).AssemblyQualifiedName);
			description.Methods.Should().HaveCount(1);

			var method = description.Methods[0];
			method.Should().NotBeNull();
			method.Parameters.Should().HaveCount(0);

			var returnParameter = method.ReturnParameter;
			returnParameter.ParameterType.Should().NotBeNull();
			returnParameter.ParameterType.Type.Should().Be(typeof(void));

			model.Contains(typeof(void)).Should().BeTrue();
			model.GetId(typeof(void)).Should().Be(((TypeDescription)returnParameter.ParameterType).Id);
		}

		[Test]
		public void TestAddObject()
		{
			var model = new SharpRemote.TypeModel();
			var description = model.Add<object>();
			description.Should().NotBeNull();
			description.AssemblyQualifiedName.Should().Be(typeof(object).AssemblyQualifiedName);

			model.Types.Should().HaveCount(1);
			model.Types.Should().BeEquivalentTo(new[] {description});
		}

		[Test]
		public void TestBuiltInType([ValueSource(nameof(BuiltInTypes))] Type type)
		{
			var model = new SharpRemote.TypeModel();
			var description = model.Add(type);
			description.SerializationType.Should().Be(SerializationType.ByValue);
			description.BaseType.Should().NotBeNull();
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
		public void TestEnum()
		{
			var model = new SharpRemote.TypeModel();
			model.Add<uint>();
			new Action(() => model.Add<UInt32Enum>()).Should().NotThrow();
			model.Types.Should().Contain(x => x.Type == typeof(uint));
			model.Types.Should().Contain(x => x.Type == typeof(UInt32Enum));
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
		public void TestSByteEnum()
		{
			var model = new SharpRemote.TypeModel();
			var type = model.Add<SbyteEnum>();
			type.IsEnum.Should().BeTrue();
			type.StorageType.Type.Should().Be<SByte>();

			type.EnumValues.Should().HaveCount(3);

			type.EnumValues[0].Value.Should().Be(SbyteEnum.B);
			type.EnumValues[0].Name.Should().Be("B");
			type.EnumValues[0].NumericValue.Should().Be((long) SbyteEnum.B);

			type.EnumValues[1].Value.Should().Be(SbyteEnum.C);
			type.EnumValues[1].Name.Should().Be("C");
			type.EnumValues[1].NumericValue.Should().Be((long) SbyteEnum.C);

			type.EnumValues[2].Value.Should().Be(SbyteEnum.A);
			type.EnumValues[2].Name.Should().Be("A");
			type.EnumValues[2].NumericValue.Should().Be((long) SbyteEnum.A);
		}

		[Test]
		public void TestByteEnum()
		{
			var model = new SharpRemote.TypeModel();
			var type = model.Add<ByteEnum>();
			type.IsEnum.Should().BeTrue();
			type.StorageType.Type.Should().Be<byte>();

			type.EnumValues.Should().HaveCount(3);

			type.EnumValues[0].Value.Should().Be(ByteEnum.A);
			type.EnumValues[0].Name.Should().Be("A");
			type.EnumValues[0].NumericValue.Should().Be((long) ByteEnum.A);

			type.EnumValues[1].Value.Should().Be(ByteEnum.B);
			type.EnumValues[1].Name.Should().Be("B");
			type.EnumValues[1].NumericValue.Should().Be((long) ByteEnum.B);

			type.EnumValues[2].Value.Should().Be(ByteEnum.C);
			type.EnumValues[2].Name.Should().Be("C");
			type.EnumValues[2].NumericValue.Should().Be((long) ByteEnum.C);
		}
		
		[Test]
		public void TestInt16Enum()
		{
			var model = new SharpRemote.TypeModel();
			var type = model.Add<Int16Enum>();
			type.IsEnum.Should().BeTrue();
			type.StorageType.Type.Should().Be<Int16>();

			type.EnumValues.Should().HaveCount(3);

			type.EnumValues[0].Value.Should().Be(Int16Enum.B);
			type.EnumValues[0].Name.Should().Be("B");
			type.EnumValues[0].NumericValue.Should().Be((long) Int16Enum.B);

			type.EnumValues[1].Value.Should().Be(Int16Enum.C);
			type.EnumValues[1].Name.Should().Be("C");
			type.EnumValues[1].NumericValue.Should().Be((long) Int16Enum.C);

			type.EnumValues[2].Value.Should().Be(Int16Enum.A);
			type.EnumValues[2].Name.Should().Be("A");
			type.EnumValues[2].NumericValue.Should().Be((long) Int16Enum.A);
		}

		[Test]
		public void TestUInt16Enum()
		{
			var model = new SharpRemote.TypeModel();
			var type = model.Add<UInt16Enum>();
			type.IsEnum.Should().BeTrue();
			type.StorageType.Type.Should().Be<UInt16>();

			type.EnumValues.Should().HaveCount(3);

			type.EnumValues[0].Value.Should().Be(UInt16Enum.A);
			type.EnumValues[0].Name.Should().Be("A");
			type.EnumValues[0].NumericValue.Should().Be((long) UInt16Enum.A);

			type.EnumValues[1].Value.Should().Be(UInt16Enum.B);
			type.EnumValues[1].Name.Should().Be("B");
			type.EnumValues[1].NumericValue.Should().Be((long) UInt16Enum.B);

			type.EnumValues[2].Value.Should().Be(UInt16Enum.C);
			type.EnumValues[2].Name.Should().Be("C");
			type.EnumValues[2].NumericValue.Should().Be((long) UInt16Enum.C);
		}

		[Test]
		public void TestInt32Enum()
		{
			var model = new SharpRemote.TypeModel();
			var type = model.Add<Int32Enum>();
			type.IsEnum.Should().BeTrue();
			type.StorageType.Type.Should().Be<Int32>();

			type.EnumValues.Should().HaveCount(3);

			type.EnumValues[0].Value.Should().Be(Int32Enum.B);
			type.EnumValues[0].Name.Should().Be("B");
			type.EnumValues[0].NumericValue.Should().Be((long) Int32Enum.B);

			type.EnumValues[1].Value.Should().Be(Int32Enum.C);
			type.EnumValues[1].Name.Should().Be("C");
			type.EnumValues[1].NumericValue.Should().Be((long) Int32Enum.C);

			type.EnumValues[2].Value.Should().Be(Int32Enum.A);
			type.EnumValues[2].Name.Should().Be("A");
			type.EnumValues[2].NumericValue.Should().Be((long) Int32Enum.A);
		}

		[Test]
		public void TestUInt32Enum()
		{
			var model = new SharpRemote.TypeModel();
			var type = model.Add<UInt32Enum>();
			type.IsEnum.Should().BeTrue();
			type.StorageType.Type.Should().Be<UInt32>();

			type.EnumValues.Should().HaveCount(3);

			type.EnumValues[0].Value.Should().Be(UInt32Enum.A);
			type.EnumValues[0].Name.Should().Be("A");
			type.EnumValues[0].NumericValue.Should().Be((long) UInt32Enum.A);

			type.EnumValues[1].Value.Should().Be(UInt32Enum.B);
			type.EnumValues[1].Name.Should().Be("B");
			type.EnumValues[1].NumericValue.Should().Be((long) UInt32Enum.B);

			type.EnumValues[2].Value.Should().Be(UInt32Enum.C);
			type.EnumValues[2].Name.Should().Be("C");
			type.EnumValues[2].NumericValue.Should().Be((long) UInt32Enum.C);
		}
		
		[Test]
		public void TestInt64Enum()
		{
			var model = new SharpRemote.TypeModel();
			var type = model.Add<Int64Enum>();
			type.IsEnum.Should().BeTrue();
			type.StorageType.Type.Should().Be<Int64>();

			type.EnumValues.Should().HaveCount(3);

			type.EnumValues[0].Value.Should().Be(Int64Enum.B);
			type.EnumValues[0].Name.Should().Be("B");
			type.EnumValues[0].NumericValue.Should().Be((long) Int64Enum.B);

			type.EnumValues[1].Value.Should().Be(Int64Enum.C);
			type.EnumValues[1].Name.Should().Be("C");
			type.EnumValues[1].NumericValue.Should().Be((long) Int64Enum.C);

			type.EnumValues[2].Value.Should().Be(Int64Enum.A);
			type.EnumValues[2].Name.Should().Be("A");
			type.EnumValues[2].NumericValue.Should().Be((long) Int64Enum.A);
		}

		[Test]
		public void TestUInt64Enum()
		{
			var model = new SharpRemote.TypeModel();
			var type = model.Add<UInt64Enum>();
			type.IsEnum.Should().BeTrue();
			type.StorageType.Type.Should().Be<UInt64>();

			type.EnumValues.Should().HaveCount(3);

			type.EnumValues[0].Value.Should().Be(UInt64Enum.A);
			type.EnumValues[0].Name.Should().Be("A");
			type.EnumValues[0].NumericValue.Should().Be(unchecked((long) UInt64Enum.A));

			type.EnumValues[1].Value.Should().Be(UInt64Enum.B);
			type.EnumValues[1].Name.Should().Be("B");
			type.EnumValues[1].NumericValue.Should().Be(unchecked((long) UInt64Enum.B));

			type.EnumValues[2].Value.Should().Be(UInt64Enum.C);
			type.EnumValues[2].Name.Should().Be("C");
			type.EnumValues[2].NumericValue.Should().Be(unchecked((long) UInt64Enum.C));
		}

		[Test]
		public void TestFieldEnum()
		{
			var model = new SharpRemote.TypeModel();
			var type = model.Add<FieldInt32Enum>();
			type.Properties.Should().BeEmpty("because the type doesn't have any properties");
			type.Methods.Should().BeEmpty("because the methods of DataContract types are uninteresting");
			type.IsClass.Should().BeFalse("because the type is a struct");
			type.IsEnum.Should().BeFalse("because the type is a struct");
			type.IsValueType.Should().BeTrue("because the type is a struct");
			type.IsInterface.Should().BeFalse("because the type is a struct");
			type.IsSealed.Should().BeTrue("because structs are always sealed");
			type.Fields.Should().HaveCount(1);

			var field1 = type.Fields[0];
			field1.Name.Should().Be(nameof(FieldInt32Enum.Value));
			field1.FieldType.AssemblyQualifiedName.Should().Be(typeof(Int32Enum).AssemblyQualifiedName);
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
			((FieldDescription) field1).FieldTypeId.Should().Be(model.GetId<double>());

			var field2 = type.Fields[1];
			field2.Name.Should().Be(nameof(FieldStruct.B));
			field2.FieldType.AssemblyQualifiedName.Should().Be(typeof(int).AssemblyQualifiedName);
			field2.FieldType.SerializationType.Should().Be(SerializationType.ByValue);
			field2.FieldType.IsBuiltIn.Should().BeTrue();
			((FieldDescription)field2).FieldTypeId.Should().Be(model.GetId<int>());

			var field3 = type.Fields[2];
			field3.Name.Should().Be(nameof(FieldStruct.C));
			field3.FieldType.AssemblyQualifiedName.Should().Be(typeof(string).AssemblyQualifiedName);
			field3.FieldType.SerializationType.Should().Be(SerializationType.ByValue);
			field3.FieldType.IsBuiltIn.Should().BeTrue();
			((FieldDescription)field3).FieldTypeId.Should().Be(model.GetId<string>());

			model.Types.Should().Contain(new[]
			{
				(TypeDescription)type, //< FieldStruct
				(TypeDescription)field1.FieldType, //< double
				(TypeDescription)field2.FieldType, //< int
				(TypeDescription)field3.FieldType //< string
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
			type.BaseType.Should().NotBeNull();
			type.Properties.Should().HaveCount(1);
			property = type.Properties[0];
			property.Name.Should().Be("Value1");
			property.PropertyType.AssemblyQualifiedName.Should().Be(typeof(string).AssemblyQualifiedName);

			var field = type.Fields[0];
			field.Name.Should().Be("Value2");
			field.FieldType.AssemblyQualifiedName.Should().Be(typeof(bool).AssemblyQualifiedName);
		}

		[Test]
		public void TestByteArray()
		{
			var model = new SharpRemote.TypeModel();
			var type = model.Add<byte[]>();
			type.IsGenericType.Should().BeFalse("because arrays were invented before generics and therefore don't count as being generic");
			type.Fields.Should().BeEmpty();
			type.Properties.Should().BeEmpty();
			type.Methods.Should().BeEmpty();
			type.GenericArguments.Should().HaveCount(1);

			var elementType = type.GenericArguments[0];
			elementType.Should().NotBeNull();
			elementType.AssemblyQualifiedName.Should().Be(typeof(byte).AssemblyQualifiedName);
			elementType.Type.Should().Be<byte>();
		}

		[Test]
		public void TestListString()
		{
			var model = new SharpRemote.TypeModel();
			var type = model.Add<List<string>>();
			type.IsGenericType.Should().BeTrue();
			type.Fields.Should().BeEmpty();
			type.Properties.Should().BeEmpty();
			type.Methods.Should().BeEmpty();
			type.GenericArguments.Should().HaveCount(1);

			var elementType = type.GenericArguments[0];
			elementType.Should().NotBeNull();
			elementType.AssemblyQualifiedName.Should().Be(typeof(string).AssemblyQualifiedName);
			elementType.Type.Should().Be<string>();
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
			new Action(() => model.Add(null)).Should().Throw<ArgumentNullException>();
		}

		[Test]
		[Description("Verifies that adding the same type again is a NOP")]
		public void TestAddTwice()
		{
			var model = new SharpRemote.TypeModel();
			var type1 = model.Add<string>();
			var type2 = model.Add<string>();
			type2.Should().BeSameAs(type1);
			model.Types.Count(x => x.AssemblyQualifiedName == typeof(string).AssemblyQualifiedName).Should().Be(1);
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
