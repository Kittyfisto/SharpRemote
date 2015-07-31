using System;
using FluentAssertions;
using NUnit.Framework;
using SharpRemote.Test.Types.Classes;
using SharpRemote.Test.Types.Interfaces;

namespace SharpRemote.Test.CodeGeneration.Serialization
{
	[TestFixture]
	public sealed class SerializerTest
	{
		[Test]
		[Description("Verifies that the caching-mechanism for RegisterType works and returns the same serialization methods for [ByReference] types")]
		public void TestRegisterType1()
		{
			var serializer = new Serializer();

			Serializer.SerializationMethods methods;
			serializer.RegisterType<IByReferenceType>(out methods);
			methods.Should().NotBeNull();

			Serializer.SerializationMethods methods2;
			serializer.RegisterType<ByReferenceClass>(out methods2);
			methods2.Should().BeSameAs(methods);
		}

		[Test]
		[Description("Verifies that the caching-mechanism for RegisterType works and returns the same serialization methods for typeof(Type) and typeof(Type).GetType()")]
		public void TestRegisterType2()
		{
			var serializer = new Serializer();

			Serializer.SerializationMethods methods;
			serializer.RegisterType<Type>(out methods);
			methods.Should().NotBeNull();

			Serializer.SerializationMethods methods2;
			serializer.RegisterType(typeof(Type).GetType(), out methods2);
			methods2.Should().BeSameAs(methods);
		}
	}
}