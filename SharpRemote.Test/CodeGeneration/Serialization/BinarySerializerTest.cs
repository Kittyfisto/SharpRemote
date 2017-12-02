using System;
using FluentAssertions;
using Moq;
using NUnit.Framework;
using SharpRemote.Test.Types.Classes;
using SharpRemote.Test.Types.Interfaces;

namespace SharpRemote.Test.CodeGeneration.Serialization
{
	[TestFixture]
	public sealed class BinarySerializerTest
	{
		[Test]
		[Description("Verifies that the caching-mechanism for RegisterType works and returns the same serialization methods for [ByReference] types")]
		public void TestRegisterType1()
		{
			var serializer = new BinarySerializer();

			BinarySerializer.SerializationMethods methods;
			serializer.RegisterType<IByReferenceType>(out methods);
			methods.Should().NotBeNull();

			BinarySerializer.SerializationMethods methods2;
			serializer.RegisterType<ByReferenceClass>(out methods2);
			methods2.Should().BeSameAs(methods);
		}

		[Test]
		[Description("Verifies that the caching-mechanism for RegisterType works and returns the same serialization methods for typeof(Type) and typeof(Type).GetType()")]
		public void TestRegisterType2()
		{
			var serializer = new BinarySerializer();

			BinarySerializer.SerializationMethods methods;
			serializer.RegisterType<Type>(out methods);
			methods.Should().NotBeNull();

			BinarySerializer.SerializationMethods methods2;
			serializer.RegisterType(typeof(Type).GetType(), out methods2);
			methods2.Should().BeSameAs(methods);
		}

		[Test]
		[Description("Verifies that ReadObject throws a TypeLoadException when the type resolver returns null")]
		public void TestReadObject1()
		{
			var resolver = new Mock<ITypeResolver>();
			resolver.Setup(x => x.GetType(It.IsAny<string>())).Returns((Type) null);
			var serializer = new BinarySerializer(resolver.Object);
			new Action(() => serializer.Roundtrip("Foobar"))
				.ShouldThrow<TypeLoadException>()
				.WithMessage(string.Format("Unable to load '{0}': The type resolver returned null",
				                           typeof(string).AssemblyQualifiedName));
		}

		[Test]
		[Description("Verifies that ReadObject throws a TypeLoadException when the type resolver throws another exception")]
		public void TestReadObject2()
		{
			var resolver = new Mock<ITypeResolver>();
			resolver.Setup(x => x.GetType(It.IsAny<string>())).Throws<InvalidOperationException>();
			var serializer = new BinarySerializer(resolver.Object);
			new Action(() => serializer.Roundtrip("Foobar"))
				.ShouldThrow<TypeLoadException>()
				.WithMessage(string.Format("Unable to load '{0}': The type resolver threw an exception while resolving the type",
				                           typeof(string).AssemblyQualifiedName))
				.WithInnerException<InvalidOperationException>();
		}

		[Test]
		[Description("Verifies that ReadObject throws a TypeLoadException when the type resolver throws another exception")]
		public void TestReadObject3()
		{
			var resolver = new Mock<ITypeResolver>();
			resolver.Setup(x => x.GetType(It.IsAny<string>())).Throws<TypeLoadException>();
			var serializer = new BinarySerializer(resolver.Object);
			new Action(() => serializer.Roundtrip("Foobar"))
				.ShouldThrow<TypeLoadException>();
		}
	}
}