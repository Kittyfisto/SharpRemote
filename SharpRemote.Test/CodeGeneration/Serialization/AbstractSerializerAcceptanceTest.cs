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
		protected abstract void Save();

		public static IEnumerable<ulong> GrainIds => new ulong[]
		{
			ulong.MinValue,
			1,
			uint.MaxValue,
			ulong.MaxValue
		};

		public static IEnumerable<ulong> RpcIds => new ulong[]
		{
			ulong.MinValue,
			42,
			1337,
			ulong.MaxValue
		};

		public static IEnumerable<string> MethodNames => new[]
		{
			"",
			"Foo",
			"汉字",
			"昨夜のコンサートは最高でした。"
		};

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
					writer.WriteArgument(sbyte.MaxValue);
				}

				PrintAndRewind(stream);

				using (var reader = serializer.CreateMethodInvocationReader(stream))
				{
					reader.RpcId.Should().Be(1);
					reader.GrainId.Should().Be(2);
					reader.MethodName.Should().Be("Foo");

					sbyte value;
					reader.ReadNextArgumentAsSByte(out value).Should()
					      .BeTrue("because we've written one argument to the message and should be able to read it back");
					value.Should().Be(sbyte.MaxValue);

					reader.ReadNextArgumentAsSByte(out value).Should()
					      .BeFalse("because we've written only one argument and thus shouldn't be able to read back a 2nd one");
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
					writer.WriteArgument(byte.MaxValue);
				}

				PrintAndRewind(stream);

				using (var reader = serializer.CreateMethodInvocationReader(stream))
				{
					reader.RpcId.Should().Be(3);
					reader.GrainId.Should().Be(4);
					reader.MethodName.Should().Be("Bar");

					byte value;
					reader.ReadNextArgumentAsByte(out value).Should().BeTrue();
					value.Should().Be(byte.MaxValue);

					reader.ReadNextArgumentAsByte(out value).Should().BeFalse();
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
					writer.WriteArgument(short.MaxValue);
				}

				PrintAndRewind(stream);

				using (var reader = serializer.CreateMethodInvocationReader(stream))
				{
					reader.RpcId.Should().Be(5);
					reader.GrainId.Should().Be(6);
					reader.MethodName.Should().Be("Bar");

					short value;
					reader.ReadNextArgumentAsInt16(out value).Should().BeTrue();
					value.Should().Be(short.MaxValue);

					reader.ReadNextArgumentAsInt16(out value).Should().BeFalse();
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
					writer.WriteArgument(ushort.MaxValue);
				}

				PrintAndRewind(stream);

				using (var reader = serializer.CreateMethodInvocationReader(stream))
				{
					reader.RpcId.Should().Be(5);
					reader.GrainId.Should().Be(6);
					reader.MethodName.Should().Be("Bar");

					ushort value;
					reader.ReadNextArgumentAsUInt16(out value).Should().BeTrue();
					value.Should().Be(ushort.MaxValue);

					reader.ReadNextArgumentAsUInt16(out value).Should().BeFalse();
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
					writer.WriteArgument(int.MaxValue);
				}

				PrintAndRewind(stream);

				using (var reader = serializer.CreateMethodInvocationReader(stream))
				{
					reader.RpcId.Should().Be(5);
					reader.GrainId.Should().Be(6);
					reader.MethodName.Should().Be("Bar");

					int value;
					reader.ReadNextArgumentAsInt32(out value).Should().BeTrue();
					value.Should().Be(int.MaxValue);

					reader.ReadNextArgumentAsInt32(out value).Should().BeFalse();
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
					writer.WriteArgument(uint.MaxValue);
				}

				PrintAndRewind(stream);

				using (var reader = serializer.CreateMethodInvocationReader(stream))
				{
					reader.RpcId.Should().Be(5);
					reader.GrainId.Should().Be(6);
					reader.MethodName.Should().Be("Bar");

					uint value;
					reader.ReadNextArgumentAsUInt32(out value).Should().BeTrue();
					value.Should().Be(uint.MaxValue);

					reader.ReadNextArgumentAsUInt32(out value).Should().BeFalse();
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
					writer.WriteArgument(long.MaxValue);
				}

				PrintAndRewind(stream);

				using (var reader = serializer.CreateMethodInvocationReader(stream))
				{
					reader.RpcId.Should().Be(5);
					reader.GrainId.Should().Be(6);
					reader.MethodName.Should().Be("Bar");

					long value;
					reader.ReadNextArgumentAsInt64(out value).Should().BeTrue();
					value.Should().Be(long.MaxValue);

					reader.ReadNextArgumentAsInt64(out value).Should().BeFalse();
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
					writer.WriteArgument(ulong.MaxValue);
				}

				PrintAndRewind(stream);

				using (var reader = serializer.CreateMethodInvocationReader(stream))
				{
					reader.RpcId.Should().Be(5);
					reader.GrainId.Should().Be(6);
					reader.MethodName.Should().Be("Bar");

					ulong value;
					reader.ReadNextArgumentAsUInt64(out value).Should().BeTrue();
					value.Should().Be(ulong.MaxValue);

					reader.ReadNextArgumentAsUInt64(out value).Should().BeFalse();
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
					writer.WriteArgument(value);
				}

				PrintAndRewind(stream);

				using (var reader = serializer.CreateMethodInvocationReader(stream))
				{
					reader.RpcId.Should().Be(5);
					reader.GrainId.Should().Be(6);
					reader.MethodName.Should().Be("Bar");

					float actualValue;
					reader.ReadNextArgumentAsFloat(out actualValue).Should().BeTrue();
					actualValue.Should().Be(value);

					reader.ReadNextArgumentAsFloat(out actualValue).Should().BeFalse();
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
					writer.WriteArgument(value);
				}

				PrintAndRewind(stream);

				using (var reader = serializer.CreateMethodInvocationReader(stream))
				{
					reader.RpcId.Should().Be(5);
					reader.GrainId.Should().Be(6);
					reader.MethodName.Should().Be("Bar");

					double actualValue;
					reader.ReadNextArgumentAsDouble(out actualValue).Should().BeTrue();
					actualValue.Should().BeApproximately(value, 0.00000000000001);

					reader.ReadNextArgumentAsDouble(out actualValue).Should().BeFalse();
					actualValue.Should().Be(double.MinValue);
				}
			}
		}

		public static IEnumerable<decimal> DecimalValues => new[]
		{
			decimal.MinValue,
			decimal.MinusOne,
			0,
			decimal.One,
			new decimal(Math.PI),
			decimal.MaxValue
		};

		[Test]
		public void TestMethodCallDecimal([ValueSource("DecimalValues")] decimal value)
		{
			var serializer = Create();
			using (var stream = new MemoryStream())
			{
				using (var writer = serializer.CreateMethodInvocationWriter(stream, 5, 6, "Bar"))
				{
					writer.WriteArgument(value);
				}

				PrintAndRewind(stream);

				using (var reader = serializer.CreateMethodInvocationReader(stream))
				{
					reader.RpcId.Should().Be(5);
					reader.GrainId.Should().Be(6);
					reader.MethodName.Should().Be("Bar");

					decimal actualValue;
					reader.ReadNextArgumentAsDecimal(out actualValue).Should().BeTrue();
					actualValue.Should().Be(value);

					reader.ReadNextArgumentAsDecimal(out actualValue).Should().BeFalse();
					actualValue.Should().Be(decimal.MinValue);
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
					writer.WriteArgument("3.14159");
				}

				PrintAndRewind(stream);

				using (var reader = serializer.CreateMethodInvocationReader(stream))
				{
					reader.RpcId.Should().Be(5);
					reader.GrainId.Should().Be(6);
					reader.MethodName.Should().Be("GetValue");

					string value;
					reader.ReadNextArgumentAsString(out value).Should().BeTrue();
					value.Should().Be("3.14159");

					reader.ReadNextArgumentAsString(out value).Should().BeFalse();
					value.Should().Be(null);
				}
			}
		}

		[Test]
		public void TestMethodCallFieldDecimal([ValueSource(nameof(DecimalValues))] decimal value)
		{
			var serializer = Create();
			using (var stream = new MemoryStream())
			{
				serializer.RegisterType<FieldDecimal>();
				Save();

				using (var writer = serializer.CreateMethodInvocationWriter(stream, 5, 6, "GetValue"))
				{
					writer.WriteArgument(new FieldDecimal {Value = value });
				}

				PrintAndRewind(stream);

				using (var reader = serializer.CreateMethodInvocationReader(stream))
				{
					reader.RpcId.Should().Be(5);
					reader.GrainId.Should().Be(6);
					reader.MethodName.Should().Be("GetValue");

					FieldDecimal actualValue;
					reader.ReadNextArgumentAsStruct(out actualValue).Should().BeTrue();
					actualValue.Value.Should().Be(value);

					reader.ReadNextArgumentAsStruct(out actualValue).Should().BeFalse();
					actualValue.Should().Be(default(FieldDecimal));
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