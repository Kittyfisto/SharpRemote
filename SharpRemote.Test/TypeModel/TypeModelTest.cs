using System;
using System.Linq;
using FluentAssertions;
using NUnit.Framework;
using SharpRemote.Test.Types.Structs;
using System.Collections.Generic;

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
			typeof(string)
			// TODO: Add all other built in types
			// typeof(IpAddress)
		};

		[Test]
		public void TestBuiltInType([ValueSource(nameof(BuiltInTypes))] Type type)
		{
			var model = new SharpRemote.TypeModel();
			var description = model.Add(type);
			description.SerializationType.Should().Be(SerializationType.BuiltIn);
		}

		[Test]
		public void TestFields1()
		{
			var model = new SharpRemote.TypeModel();
			var type = model.Add<FieldStruct>();
			type.AssemblyQualifiedName.Should().Be(typeof(FieldStruct).AssemblyQualifiedName);
			type.SerializationType.Should().Be(SerializationType.DataContract);
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

			var field2 = type.Fields[1];
			field2.Name.Should().Be(nameof(FieldStruct.B));
			field2.FieldType.AssemblyQualifiedName.Should().Be(typeof(int).AssemblyQualifiedName);

			var field3 = type.Fields[2];
			field3.Name.Should().Be(nameof(FieldStruct.C));
			field3.FieldType.AssemblyQualifiedName.Should().Be(typeof(string).AssemblyQualifiedName);

			model.Types.Should().BeEquivalentTo(new object[]
			{
				type, //< FieldStruct
				field1.FieldType, //< double
				field2.FieldType, //< int
				field3.FieldType //< string
			});
		}
	}
}
