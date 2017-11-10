using System;
using System.Collections.Generic;
using System.IO;
using FluentAssertions;
using NUnit.Framework;
using SharpRemote.Test.Types.Structs;

namespace SharpRemote.Test.CodeGeneration.Serialization
{
	[TestFixture]
	public abstract class AbstractSerializerAcceptanceTest
	{
		protected abstract ISerializer2 Create();

		[Test]
		public void TestEmptyMethodCall([ValueSource("RpcIds")] ulong rpcId,
		                                [ValueSource("GrainIds")] ulong grainId,
		                                [ValueSource("MethodNames")] string methodName)
		{
			var serializer = Create();
			using (var stream = new MemoryStream())
			{
				using (serializer.CreateMethodInvocationWriter(stream, rpcId, grainId, methodName))
				{
				}

				PrintAndRewind(stream);

				using (var reader = serializer.CreateMethodInvocationReader(stream))
				{
					reader.RpcId.Should().Be(rpcId);
					reader.GrainId.Should().Be(grainId);
					reader.MethodName.Should().Be(methodName);
				}
			}
		}

		[Test]
		public void TestMethodCallSByte()
		{
			var serializer = Create();
			using (var stream = new MemoryStream())
			{
				using (var writer = serializer.CreateMethodInvocationWriter(stream, 1, 2, "Foo"))
				{
					writer.WriteNamedArgument("someValue", sbyte.MaxValue);
				}

				PrintAndRewind(stream);

				using (var reader = serializer.CreateMethodInvocationReader(stream))
				{
					reader.RpcId.Should().Be(1);
					reader.GrainId.Should().Be(2);
					reader.MethodName.Should().Be("Foo");

					string name;
					sbyte value;
					reader.ReadNextArgumentAsSByte(out name, out value).Should()
					      .BeTrue("because we've written one argument to the message and should be able to read it back");
					name.Should().Be("someValue");
					value.Should().Be(sbyte.MaxValue);

					reader.ReadNextArgumentAsSByte(out name, out value).Should()
					      .BeFalse("because we've written only one argument and thus shouldn't be able to read back a 2nd one");
					name.Should().BeNull();
					value.Should().Be(sbyte.MinValue);
				}
			}
		}

		[Test]
		public void TestMethodCallByte()
		{
			var serializer = Create();
			using (var stream = new MemoryStream())
			{
				using (var writer = serializer.CreateMethodInvocationWriter(stream, 3, 4, "Bar"))
				{
					writer.WriteNamedArgument("pi", byte.MaxValue);
				}

				PrintAndRewind(stream);

				using (var reader = serializer.CreateMethodInvocationReader(stream))
				{
					reader.RpcId.Should().Be(3);
					reader.GrainId.Should().Be(4);
					reader.MethodName.Should().Be("Bar");

					string name;
					byte value;
					reader.ReadNextArgumentAsByte(out name, out value).Should().BeTrue();
					name.Should().Be("pi");
					value.Should().Be(byte.MaxValue);

					reader.ReadNextArgumentAsByte(out name, out value).Should().BeFalse();
					name.Should().BeNull();
					value.Should().Be(byte.MinValue);
				}
			}
		}

		[Test]
		public void TestMethodCallInt16()
		{
			var serializer = Create();
			using (var stream = new MemoryStream())
			{
				using (var writer = serializer.CreateMethodInvocationWriter(stream, 5, 6, "Bar"))
				{
					writer.WriteNamedArgument("pi", short.MaxValue);
				}

				PrintAndRewind(stream);

				using (var reader = serializer.CreateMethodInvocationReader(stream))
				{
					reader.RpcId.Should().Be(5);
					reader.GrainId.Should().Be(6);
					reader.MethodName.Should().Be("Bar");

					string name;
					short value;
					reader.ReadNextArgumentAsInt16(out name, out value).Should().BeTrue();
					name.Should().Be("pi");
					value.Should().Be(short.MaxValue);

					reader.ReadNextArgumentAsInt16(out name, out value).Should().BeFalse();
					name.Should().BeNull();
					value.Should().Be(short.MinValue);
				}
			}
		}

		[Test]
		public void TestMethodCallUInt16()
		{
			var serializer = Create();
			using (var stream = new MemoryStream())
			{
				using (var writer = serializer.CreateMethodInvocationWriter(stream, 5, 6, "Bar"))
				{
					writer.WriteNamedArgument("pi", ushort.MaxValue);
				}

				PrintAndRewind(stream);

				using (var reader = serializer.CreateMethodInvocationReader(stream))
				{
					reader.RpcId.Should().Be(5);
					reader.GrainId.Should().Be(6);
					reader.MethodName.Should().Be("Bar");

					string name;
					ushort value;
					reader.ReadNextArgumentAsUInt16(out name, out value).Should().BeTrue();
					name.Should().Be("pi");
					value.Should().Be(ushort.MaxValue);

					reader.ReadNextArgumentAsUInt16(out name, out value).Should().BeFalse();
					name.Should().BeNull();
					value.Should().Be(ushort.MinValue);
				}
			}
		}

		[Test]
		public void TestMethodCallInt32()
		{
			var serializer = Create();
			using (var stream = new MemoryStream())
			{
				using (var writer = serializer.CreateMethodInvocationWriter(stream, 5, 6, "Bar"))
				{
					writer.WriteNamedArgument("pi", int.MaxValue);
				}

				PrintAndRewind(stream);

				using (var reader = serializer.CreateMethodInvocationReader(stream))
				{
					reader.RpcId.Should().Be(5);
					reader.GrainId.Should().Be(6);
					reader.MethodName.Should().Be("Bar");

					string name;
					int value;
					reader.ReadNextArgumentAsInt32(out name, out value).Should().BeTrue();
					name.Should().Be("pi");
					value.Should().Be(int.MaxValue);

					reader.ReadNextArgumentAsInt32(out name, out value).Should().BeFalse();
					name.Should().BeNull();
					value.Should().Be(int.MinValue);
				}
			}
		}

		[Test]
		public void TestMethodCallUInt32()
		{
			var serializer = Create();
			using (var stream = new MemoryStream())
			{
				using (var writer = serializer.CreateMethodInvocationWriter(stream, 5, 6, "Bar"))
				{
					writer.WriteNamedArgument("pi", uint.MaxValue);
				}

				PrintAndRewind(stream);

				using (var reader = serializer.CreateMethodInvocationReader(stream))
				{
					reader.RpcId.Should().Be(5);
					reader.GrainId.Should().Be(6);
					reader.MethodName.Should().Be("Bar");

					string name;
					uint value;
					reader.ReadNextArgumentAsUInt32(out name, out value).Should().BeTrue();
					name.Should().Be("pi");
					value.Should().Be(uint.MaxValue);

					reader.ReadNextArgumentAsUInt32(out name, out value).Should().BeFalse();
					name.Should().BeNull();
					value.Should().Be(uint.MinValue);
				}
			}
		}

		[Test]
		public void TestMethodCallInt64()
		{
			var serializer = Create();
			using (var stream = new MemoryStream())
			{
				using (var writer = serializer.CreateMethodInvocationWriter(stream, 5, 6, "Bar"))
				{
					writer.WriteNamedArgument("pi", long.MaxValue);
				}

				PrintAndRewind(stream);

				using (var reader = serializer.CreateMethodInvocationReader(stream))
				{
					reader.RpcId.Should().Be(5);
					reader.GrainId.Should().Be(6);
					reader.MethodName.Should().Be("Bar");

					string name;
					long value;
					reader.ReadNextArgumentAsInt64(out name, out value).Should().BeTrue();
					name.Should().Be("pi");
					value.Should().Be(long.MaxValue);

					reader.ReadNextArgumentAsInt64(out name, out value).Should().BeFalse();
					name.Should().BeNull();
					value.Should().Be(long.MinValue);
				}
			}
		}

		[Test]
		public void TestMethodCallUInt64()
		{
			var serializer = Create();
			using (var stream = new MemoryStream())
			{
				using (var writer = serializer.CreateMethodInvocationWriter(stream, 5, 6, "Bar"))
				{
					writer.WriteNamedArgument("pi", ulong.MaxValue);
				}

				PrintAndRewind(stream);

				using (var reader = serializer.CreateMethodInvocationReader(stream))
				{
					reader.RpcId.Should().Be(5);
					reader.GrainId.Should().Be(6);
					reader.MethodName.Should().Be("Bar");

					string name;
					ulong value;
					reader.ReadNextArgumentAsUInt64(out name, out value).Should().BeTrue();
					name.Should().Be("pi");
					value.Should().Be(ulong.MaxValue);

					reader.ReadNextArgumentAsUInt64(out name, out value).Should().BeFalse();
					name.Should().BeNull();
					value.Should().Be(ulong.MinValue);
				}
			}
		}

		public static IEnumerable<float> FloatValues => new[]
		{
			float.MinValue,
			0,
			(float)Math.PI,
			float.MaxValue
		};

		[Test]
		public void TestMethodCallFloat([ValueSource("FloatValues")] float value)
		{
			var serializer = Create();
			using (var stream = new MemoryStream())
			{
				using (var writer = serializer.CreateMethodInvocationWriter(stream, 5, 6, "Bar"))
				{
					writer.WriteNamedArgument("pi", value);
				}

				PrintAndRewind(stream);

				using (var reader = serializer.CreateMethodInvocationReader(stream))
				{
					reader.RpcId.Should().Be(5);
					reader.GrainId.Should().Be(6);
					reader.MethodName.Should().Be("Bar");

					string name;
					float actualValue;
					reader.ReadNextArgumentAsFloat(out name, out actualValue).Should().BeTrue();
					name.Should().Be("pi");
					actualValue.Should().Be(value);

					reader.ReadNextArgumentAsFloat(out name, out actualValue).Should().BeFalse();
					name.Should().BeNull();
					actualValue.Should().Be(float.MinValue);
				}
			}
		}

		public static IEnumerable<double> DoubleValues => new[]
		{
			double.MinValue,
			0,
			Math.PI,
			double.MaxValue
		};

		[Test]
		public void TestMethodCallDouble([ValueSource("DoubleValues")] double value)
		{
			var serializer = Create();
			using (var stream = new MemoryStream())
			{
				using (var writer = serializer.CreateMethodInvocationWriter(stream, 5, 6, "Bar"))
				{
					writer.WriteNamedArgument("pi", value);
				}

				PrintAndRewind(stream);

				using (var reader = serializer.CreateMethodInvocationReader(stream))
				{
					reader.RpcId.Should().Be(5);
					reader.GrainId.Should().Be(6);
					reader.MethodName.Should().Be("Bar");

					string name;
					double actualValue;
					reader.ReadNextArgumentAsDouble(out name, out actualValue).Should().BeTrue();
					name.Should().Be("pi");
					actualValue.Should().BeApproximately(value, 0.00000000000001);

					reader.ReadNextArgumentAsDouble(out name, out actualValue).Should().BeFalse();
					name.Should().BeNull();
					actualValue.Should().Be(double.MinValue);
				}
			}
		}

		[Test]
		public void TestMethodCallString()
		{
			var serializer = Create();
			using (var stream = new MemoryStream())
			{
				using (var writer = serializer.CreateMethodInvocationWriter(stream, 5, 6, "GetValue"))
				{
					writer.WriteNamedArgument("foobar", "3.14159");
				}

				PrintAndRewind(stream);

				using (var reader = serializer.CreateMethodInvocationReader(stream))
				{
					reader.RpcId.Should().Be(5);
					reader.GrainId.Should().Be(6);
					reader.MethodName.Should().Be("GetValue");

					string name;
					string value;
					reader.ReadNextArgumentAsString(out name, out value).Should().BeTrue();
					name.Should().Be("foobar");
					value.Should().Be("3.14159");

					reader.ReadNextArgumentAsString(out name, out value).Should().BeFalse();
					name.Should().BeNull();
					value.Should().Be(null);
				}
			}
		}

		[Test]
		public void TestMethodCallFieldDecimal()
		{
			var serializer = Create();
			using (var stream = new MemoryStream())
			{
				using (var writer = serializer.CreateMethodInvocationWriter(stream, 5, 6, "GetValue"))
				{
					writer.WriteNamedArgument("foobar", new FieldDecimal {Value = decimal.MinusOne});
				}

				PrintAndRewind(stream);

				using (var reader = serializer.CreateMethodInvocationReader(stream))
				{
					reader.RpcId.Should().Be(5);
					reader.GrainId.Should().Be(6);
					reader.MethodName.Should().Be("GetValue");

					string name;
					FieldDecimal value;
					reader.ReadNextArgumentAsStruct(out name, out value).Should().BeTrue();
					name.Should().Be("foobar");
					value.Should().Be("3.14159");

					reader.ReadNextArgumentAsStruct(out name, out value).Should().BeFalse();
					name.Should().BeNull();
					value.Should().Be(default(FieldDecimal));
				}
			}
		}

		[Test]
		public void TestEmptyMethodResult([ValueSource("RpcIds")] ulong rpcId)
		{
			var serializer = Create();
			using (var stream = new MemoryStream())
			{
				using (serializer.CreateMethodResultWriter(stream, rpcId))
				{ }

				PrintAndRewind(stream);

				using (var reader = serializer.CreateMethodResultReader(stream))
				{
					reader.RpcId.Should().Be(rpcId);
				}
			}
		}

		protected abstract string Format(MemoryStream stream);

		private void PrintAndRewind(MemoryStream stream)
		{
			stream.Position.Should()
			      .BeGreaterThan(expected: 0, because: "because some data must've been written to the stream");
			stream.Position = 0;
			
			var formatted = Format(stream);
			TestContext.Out.WriteLine("Message, {0} bytes", stream.Length);
			TestContext.Out.Write(formatted);
			stream.Position = 0;
		}
	}
}