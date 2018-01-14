using System;
using System.Collections.Generic;
using System.IO;
using FluentAssertions;
using log4net.Core;
using NUnit.Framework;
using SharpRemote.Test.Types.Classes;
using SharpRemote.Test.Types.Exceptions;
using SharpRemote.Test.Types.Interfaces;
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

		public static IEnumerable<string> StringValues => new[] {"3.14159", "", null};
		public static IEnumerable<sbyte> SByteValues => new sbyte[] {sbyte.MinValue, -1, 0, 1, sbyte.MaxValue};
		public static IEnumerable<byte> ByteValues => new byte[] {byte.MinValue, 1, byte.MaxValue};
		public static IEnumerable<short> Int16Values => new short[] {short.MinValue, -1, 0, 1, short.MaxValue};
		public static IEnumerable<ushort> UInt16Values => new ushort[] {ushort.MinValue, 1, ushort.MaxValue};
		public static IEnumerable<int> Int32Values => new[] {int.MinValue, -1, 0, 1, int.MaxValue};
		public static IEnumerable<uint> UInt32Values => new uint[] {uint.MinValue, 1, uint.MaxValue};
		public static IEnumerable<long> Int64Values => new[] {long.MinValue, -1, 0, 1, long.MaxValue};
		public static IEnumerable<ulong> UInt64Values => new ulong[] {ulong.MinValue, 1, ulong.MaxValue};
		public static IEnumerable<float> SingleValues => new[] { float.MinValue, 0, (float) Math.PI, float.MaxValue };
		public static IEnumerable<double> DoubleValues => new[] { double.MinValue, 0, Math.PI, double.MaxValue };
		public static IEnumerable<Level> LevelValues => new[]
		{
			Level.Error, Level.Alert, Level.All, Level.Critical, Level.Debug,
			Level.Emergency, Level.Error, Level.Fatal, Level.Fine, Level.Finer,
			Level.Finest, Level.Info, Level.Notice, Level.Off, Level.Debug,
			Level.Severe, Level.Trace, Level.Verbose, Level.Warn,
			Level.Log4Net_Debug
		};
		public static IEnumerable<DateTime> DateTimeValues => new[]
		{
			DateTime.MinValue, DateTime.MaxValue,
			new DateTime(2017, 11, 21, 20, 27, 38, DateTimeKind.Local),
			new DateTime(2017, 11, 21, 20, 27, 38, DateTimeKind.Utc),
			new DateTime(2017, 11, 21, 20, 27, 38, DateTimeKind.Unspecified),
			DateTime.Now, DateTime.UtcNow
		};
		public static IEnumerable<decimal> DecimalValues => new[]
		{
			decimal.MinValue,
			decimal.MinusOne,
			0,
			decimal.One,
			new decimal(Math.PI),
			decimal.MaxValue
		};

		#region Method Call Roundtrips

		[Test]
		[Description("Verifies that a method call which gets passed a singleton roundtrips")]
		public void TestMethodCallSingleton()
		{
			MethodCallRoundtripSingleton(Singleton.GetInstance());
		}

		[Test]
		public void TestEmptyMethodCall([ValueSource(nameof(RpcIds))] ulong rpcId,
		                                [ValueSource(nameof(GrainIds))] ulong grainId,
		                                [ValueSource(nameof(MethodNames))] string methodName)
		{
			var serializer = Create();
			using (var stream = new MemoryStream())
			{
				using (serializer.CreateMethodCallWriter(stream, rpcId, grainId, methodName))
				{
				}

				PrintAndRewind(stream);

				using (var reader = CreateMethodCallReader(serializer, stream))
				{
					reader.RpcId.Should().Be(rpcId);
					reader.GrainId.Should().Be(grainId);
					reader.MethodName.Should().Be(methodName);
				}
			}
		}

		[Test]
		public void TestMethodCallObjectString([ValueSource(nameof(StringValues))] string value)
		{
			MethodCallRoundtripObject(value);
		}

		[Test]
		public void TestMethodCallObjectByte([ValueSource(nameof(ByteValues))] byte value)
		{
			MethodCallRoundtripObject(value);
		}

		[Test]
		public void TestMethodCallObjectSByte([ValueSource(nameof(SByteValues))] sbyte value)
		{
			MethodCallRoundtripObject(value);
		}

		[Test]
		public void TestMethodCallObjectUInt16([ValueSource(nameof(UInt16Values))] ushort value)
		{
			MethodCallRoundtripObject(value);
		}

		[Test]
		public void TestMethodCallObjectInt16([ValueSource(nameof(Int16Values))] short value)
		{
			MethodCallRoundtripObject(value);
		}

		[Test]
		public void TestMethodCallObjectUInt32([ValueSource(nameof(UInt32Values))] uint value)
		{
			MethodCallRoundtripObject(value);
		}

		[Test]
		public void TestMethodCallObjectInt32([ValueSource(nameof(Int32Values))] int value)
		{
			MethodCallRoundtripObject(value);
		}


		[Test]
		public void TestMethodCallObjectUInt64([ValueSource(nameof(UInt64Values))] ulong value)
		{
			MethodCallRoundtripObject(value);
		}

		[Test]
		public void TestMethodCallObjectInt64([ValueSource(nameof(Int64Values))] long value)
		{
			MethodCallRoundtripObject(value);
		}

		[Test]
		public void TestMethodCallObjectSingle([ValueSource(nameof(SingleValues))] float value)
		{
			MethodCallRoundtripObject(value);
		}

		[Test]
		public void TestMethodCallObjectDouble([ValueSource(nameof(DoubleValues))] double value)
		{
			MethodCallRoundtripObject(value);
		}

		[Test]
		public void TestMethodCallObjectDateTime([ValueSource(nameof(DateTimeValues))] DateTime value)
		{
			MethodCallRoundtripObject(value);
		}

		[Test]
		public void TestMethodCallByteArray1([Values(0, 1, 2)] int length)
		{
			var serializer = Create();
			using (var stream = new MemoryStream())
			{
				int seed = Environment.TickCount;
				TestContext.Out.WriteLine("Seed: {0}", seed);
				var rng = new Random(seed);
				var value = new byte[length];
				rng.NextBytes(value);

				using (var writer = serializer.CreateMethodCallWriter(stream, 1, 2, "Foo"))
				{
					writer.WriteArgument(value);
				}

				PrintAndRewind(stream);

				using (var reader = CreateMethodCallReader(serializer, stream))
				{
					reader.RpcId.Should().Be(1);
					reader.GrainId.Should().Be(2);
					reader.MethodName.Should().Be("Foo");

					byte[] actualValue;
					reader.ReadNextArgumentAsBytes(out actualValue).Should()
					      .BeTrue();
					actualValue.Should().Equal(value);

					reader.ReadNextArgumentAsBytes(out actualValue).Should()
					      .BeFalse("because there are no more arguments");
					actualValue.Should().BeNull();
				}
			}
		}

		[Test]
		public void TestMethodCallByteArray2()
		{
			var serializer = Create();
			using (var stream = new MemoryStream())
			{
				byte[] value = null;
				using (var writer = serializer.CreateMethodCallWriter(stream, 1, 2, "Foo"))
				{
					writer.WriteArgument(value);
				}

				PrintAndRewind(stream);

				using (var reader = CreateMethodCallReader(serializer, stream))
				{
					reader.RpcId.Should().Be(1);
					reader.GrainId.Should().Be(2);
					reader.MethodName.Should().Be("Foo");

					byte[] actualValue;
					reader.ReadNextArgumentAsBytes(out actualValue).Should()
					      .BeTrue();
					actualValue.Should().BeNull();

					reader.ReadNextArgumentAsBytes(out actualValue).Should()
					      .BeFalse("because there are no more arguments");
					actualValue.Should().BeNull();
				}
			}
		}

		[Test]
		public void TestMethodCallTwoParameters()
		{
			var serializer = Create();
			using (var stream = new MemoryStream())
			{
				using (var writer = serializer.CreateMethodCallWriter(stream, 1, 2, "Foo"))
				{
					writer.WriteArgument((object)null);
					writer.WriteArgument(Math.PI);
				}

				PrintAndRewind(stream);

				using (var reader = CreateMethodCallReader(serializer, stream))
				{
					reader.RpcId.Should().Be(1);
					reader.GrainId.Should().Be(2);
					reader.MethodName.Should().Be("Foo");

					object firstArgument;
					reader.ReadNextArgument(out firstArgument).Should()
					      .BeTrue();
					firstArgument.Should().Be(null);

					double secondArgument;
					reader.ReadNextArgumentAsDouble(out secondArgument).Should()
					      .BeTrue();
					secondArgument.Should().Be(Math.PI);

					reader.ReadNextArgument(out firstArgument).Should()
					      .BeFalse("because there are no more arguments");
				}
			}
		}

		[Test]
		public void TestMethodCallNullObject()
		{
			var serializer = Create();
			using (var stream = new MemoryStream())
			{
				using (var writer = serializer.CreateMethodCallWriter(stream, 1, 2, "Foo"))
				{
					writer.WriteArgument((object)null);
				}

				PrintAndRewind(stream);

				using (var reader = CreateMethodCallReader(serializer, stream))
				{
					reader.RpcId.Should().Be(1);
					reader.GrainId.Should().Be(2);
					reader.MethodName.Should().Be("Foo");

					object value;
					reader.ReadNextArgument(out value).Should()
					      .BeTrue("because we've written one argument to the message and should be able to read it back");
					value.Should().Be(null);

					reader.ReadNextArgument(out value).Should()
					      .BeFalse("because we've written only one argument and thus shouldn't be able to read back a 2nd one");
					value.Should().Be(null);
				}
			}
		}

		[Test]
		public void TestMethodCallSByte([ValueSource(nameof(SByteValues))] sbyte value)
		{
			var serializer = Create();
			using (var stream = new MemoryStream())
			{
				using (var writer = serializer.CreateMethodCallWriter(stream, 1, 2, "Foo"))
				{
					writer.WriteArgument(value);
				}

				PrintAndRewind(stream);

				using (var reader = CreateMethodCallReader(serializer, stream))
				{
					reader.RpcId.Should().Be(1);
					reader.GrainId.Should().Be(2);
					reader.MethodName.Should().Be("Foo");

					sbyte actualValue;
					reader.ReadNextArgumentAsSByte(out actualValue).Should()
					      .BeTrue("because we've written one argument to the message and should be able to read it back");
					actualValue.Should().Be(value);

					reader.ReadNextArgumentAsSByte(out actualValue).Should()
					      .BeFalse("because we've written only one argument and thus shouldn't be able to read back a 2nd one");
					actualValue.Should().Be(sbyte.MinValue);
				}
			}
		}

		[Test]
		public void TestMethodCallByte([ValueSource(nameof(ByteValues))] byte value)
		{
			var serializer = Create();
			using (var stream = new MemoryStream())
			{
				using (var writer = serializer.CreateMethodCallWriter(stream, 3, 4, "Bar"))
				{
					writer.WriteArgument(value);
				}

				PrintAndRewind(stream);

				using (var reader = CreateMethodCallReader(serializer, stream))
				{
					reader.RpcId.Should().Be(3);
					reader.GrainId.Should().Be(4);
					reader.MethodName.Should().Be("Bar");

					byte actualValue;
					reader.ReadNextArgumentAsByte(out actualValue).Should().BeTrue();
					actualValue.Should().Be(value);

					reader.ReadNextArgumentAsByte(out actualValue).Should().BeFalse();
					actualValue.Should().Be(byte.MinValue);
				}
			}
		}

		[Test]
		public void TestMethodCallInt16([ValueSource(nameof(Int16Values))] short value)
		{
			var serializer = Create();
			using (var stream = new MemoryStream())
			{
				using (var writer = serializer.CreateMethodCallWriter(stream, 5, 6, "Bar"))
				{
					writer.WriteArgument(value);
				}

				PrintAndRewind(stream);

				using (var reader = CreateMethodCallReader(serializer, stream))
				{
					reader.RpcId.Should().Be(5);
					reader.GrainId.Should().Be(6);
					reader.MethodName.Should().Be("Bar");

					short actualValue;
					reader.ReadNextArgumentAsInt16(out actualValue).Should().BeTrue();
					actualValue.Should().Be(value);

					reader.ReadNextArgumentAsInt16(out actualValue).Should().BeFalse();
					actualValue.Should().Be(short.MinValue);
				}
			}
		}

		[Test]
		public void TestMethodCallUInt16([ValueSource(nameof(UInt16Values))] ushort value)
		{
			var serializer = Create();
			using (var stream = new MemoryStream())
			{
				using (var writer = serializer.CreateMethodCallWriter(stream, 5, 6, "Bar"))
				{
					writer.WriteArgument(value);
				}

				PrintAndRewind(stream);

				using (var reader = CreateMethodCallReader(serializer, stream))
				{
					reader.RpcId.Should().Be(5);
					reader.GrainId.Should().Be(6);
					reader.MethodName.Should().Be("Bar");

					ushort actualValue;
					reader.ReadNextArgumentAsUInt16(out actualValue).Should().BeTrue();
					actualValue.Should().Be(value);

					reader.ReadNextArgumentAsUInt16(out actualValue).Should().BeFalse();
					actualValue.Should().Be(ushort.MinValue);
				}
			}
		}

		[Test]
		public void TestMethodCallInt32([ValueSource(nameof(Int32Values))] int value)
		{
			var serializer = Create();
			using (var stream = new MemoryStream())
			{
				using (var writer = serializer.CreateMethodCallWriter(stream, 5, 6, "Bar"))
				{
					writer.WriteArgument(value);
				}

				PrintAndRewind(stream);

				using (var reader = CreateMethodCallReader(serializer, stream))
				{
					reader.RpcId.Should().Be(5);
					reader.GrainId.Should().Be(6);
					reader.MethodName.Should().Be("Bar");

					int actualValue;
					reader.ReadNextArgumentAsInt32(out actualValue).Should().BeTrue();
					actualValue.Should().Be(value);

					reader.ReadNextArgumentAsInt32(out actualValue).Should().BeFalse();
					actualValue.Should().Be(int.MinValue);
				}
			}
		}

		[Test]
		public void TestMethodCallUInt32([ValueSource(nameof(UInt32Values))] uint value)
		{
			var serializer = Create();
			using (var stream = new MemoryStream())
			{
				using (var writer = serializer.CreateMethodCallWriter(stream, 5, 6, "Bar"))
				{
					writer.WriteArgument(value);
				}

				PrintAndRewind(stream);

				using (var reader = CreateMethodCallReader(serializer, stream))
				{
					reader.RpcId.Should().Be(5);
					reader.GrainId.Should().Be(6);
					reader.MethodName.Should().Be("Bar");

					uint actualValue;
					reader.ReadNextArgumentAsUInt32(out actualValue).Should().BeTrue();
					actualValue.Should().Be(value);

					reader.ReadNextArgumentAsUInt32(out actualValue).Should().BeFalse();
					actualValue.Should().Be(uint.MinValue);
				}
			}
		}

		[Test]
		public void TestMethodCallInt64([ValueSource(nameof(Int64Values))] long value)
		{
			var serializer = Create();
			using (var stream = new MemoryStream())
			{
				using (var writer = serializer.CreateMethodCallWriter(stream, 5, 6, "Bar"))
				{
					writer.WriteArgument(value);
				}

				PrintAndRewind(stream);

				using (var reader = CreateMethodCallReader(serializer, stream))
				{
					reader.RpcId.Should().Be(5);
					reader.GrainId.Should().Be(6);
					reader.MethodName.Should().Be("Bar");

					long actualValue;
					reader.ReadNextArgumentAsInt64(out actualValue).Should().BeTrue();
					actualValue.Should().Be(value);

					reader.ReadNextArgumentAsInt64(out actualValue).Should().BeFalse();
					actualValue.Should().Be(long.MinValue);
				}
			}
		}

		[Test]
		public void TestMethodCallUInt64([ValueSource(nameof(UInt64Values))] ulong value)
		{
			var serializer = Create();
			using (var stream = new MemoryStream())
			{
				using (var writer = serializer.CreateMethodCallWriter(stream, 5, 6, "Bar"))
				{
					writer.WriteArgument(value);
				}

				PrintAndRewind(stream);

				using (var reader = CreateMethodCallReader(serializer, stream))
				{
					reader.RpcId.Should().Be(5);
					reader.GrainId.Should().Be(6);
					reader.MethodName.Should().Be("Bar");

					ulong actualValue;
					reader.ReadNextArgumentAsUInt64(out actualValue).Should().BeTrue();
					actualValue.Should().Be(value);

					reader.ReadNextArgumentAsUInt64(out actualValue).Should().BeFalse();
					actualValue.Should().Be(ulong.MinValue);
				}
			}
		}

		[Test]
		public void TestMethodCallFloat([ValueSource(nameof(SingleValues))] float value)
		{
			var serializer = Create();
			using (var stream = new MemoryStream())
			{
				using (var writer = serializer.CreateMethodCallWriter(stream, 5, 6, "Bar"))
				{
					writer.WriteArgument(value);
				}

				PrintAndRewind(stream);

				using (var reader = CreateMethodCallReader(serializer, stream))
				{
					reader.RpcId.Should().Be(5);
					reader.GrainId.Should().Be(6);
					reader.MethodName.Should().Be("Bar");

					float actualValue;
					reader.ReadNextArgumentAsSingle(out actualValue).Should().BeTrue();
					actualValue.Should().Be(value);

					reader.ReadNextArgumentAsSingle(out actualValue).Should().BeFalse();
					actualValue.Should().Be(float.MinValue);
				}
			}
		}

		[Test]
		public void TestMethodCallDouble([ValueSource(nameof(DoubleValues))] double value)
		{
			var serializer = Create();
			using (var stream = new MemoryStream())
			{
				using (var writer = serializer.CreateMethodCallWriter(stream, 5, 6, "Bar"))
				{
					writer.WriteArgument(value);
				}

				PrintAndRewind(stream);

				using (var reader = CreateMethodCallReader(serializer, stream))
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

		[Test]
		public void TestMethodCallDecimal([ValueSource(nameof(DecimalValues))] decimal value)
		{
			var serializer = Create();
			using (var stream = new MemoryStream())
			{
				using (var writer = serializer.CreateMethodCallWriter(stream, 5, 6, "Bar"))
				{
					writer.WriteArgument(value);
				}

				PrintAndRewind(stream);

				using (var reader = CreateMethodCallReader(serializer, stream))
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
		public void TestMethodCallString([ValueSource(nameof(StringValues))] string value)
		{
			var serializer = Create();
			using (var stream = new MemoryStream())
			{
				using (var writer = serializer.CreateMethodCallWriter(stream, 5, 6, "GetValue"))
				{
					writer.WriteArgument(value);
				}

				PrintAndRewind(stream);

				using (var reader = CreateMethodCallReader(serializer, stream))
				{
					reader.RpcId.Should().Be(5);
					reader.GrainId.Should().Be(6);
					reader.MethodName.Should().Be("GetValue");

					string actualValue;
					reader.ReadNextArgumentAsString(out actualValue).Should().BeTrue();
					actualValue.Should().Be(value);

					reader.ReadNextArgumentAsString(out actualValue).Should().BeFalse();
					actualValue.Should().Be(null);
				}
			}
		}

		[Test]
		public void TestMethodCallDateTime([ValueSource(nameof(DateTimeValues))] DateTime value)
		{
			var serializer = Create();
			using (var stream = new MemoryStream())
			{
				using (var writer = serializer.CreateMethodCallWriter(stream, 5, 6, "GetValue"))
				{
					writer.WriteArgument(value);
				}

				PrintAndRewind(stream);

				using (var reader = CreateMethodCallReader(serializer, stream))
				{
					reader.RpcId.Should().Be(5);
					reader.GrainId.Should().Be(6);
					reader.MethodName.Should().Be("GetValue");

					DateTime actualValue;
					reader.ReadNextArgumentAsDateTime(out actualValue).Should().BeTrue();
					actualValue.Should().Be(value);

					reader.ReadNextArgumentAsDateTime(out actualValue).Should().BeFalse();
					actualValue.Should().Be(DateTime.MinValue);
				}
			}
		}

		[Test]
		[Ignore("Not yet implemented")]
		public void TestMethodCallKeyValuePair()
		{
			var serializer = Create();
			using (var stream = new MemoryStream())
			{
				using (var writer = serializer.CreateMethodCallWriter(stream, 5, 6, "GetValue"))
				{
					writer.WriteArgument(new KeyValuePair<int, string>(42, "foo"));
				}

				PrintAndRewind(stream);

				using (var reader = CreateMethodCallReader(serializer, stream))
				{
					reader.RpcId.Should().Be(5);
					reader.GrainId.Should().Be(6);
					reader.MethodName.Should().Be("GetValue");

					object actualValue;
					reader.ReadNextArgument(out actualValue).Should().BeTrue();
					actualValue.Should().BeOfType<KeyValuePair<int, string>>();
					actualValue.Should().Be(new KeyValuePair<int, string>(42, "foo"));

					reader.ReadNextArgument(out actualValue).Should().BeFalse();
					actualValue.Should().BeNull();
				}
			}
		}

		[Test]
		[Defect("https://github.com/Kittyfisto/SharpRemote/issues/44")]
		public void TestMethodCallLevel([ValueSource(nameof(LevelValues))] Level value)
		{
			MethodCallRoundtripSingleton(value);
		}

		[Test]
		public void TestMethodCallFieldDecimal([ValueSource(nameof(DecimalValues))] decimal value)
		{
			MethodCallRoundtripObject(new FieldDecimal { Value = value });
		}

		[Test]
		public void TestMethodCallFieldString([ValueSource(nameof(StringValues))] string value)
		{
			MethodCallRoundtripObject(new FieldString { Value = value });
		}

		[Test]
		public void TestMethodCallFieldInt32([ValueSource(nameof(Int32Values))] int value)
		{
			MethodCallRoundtripObject(new FieldInt32 { Value = value });
		}

		[Test]
		public void TestMethodCallFieldUInt32([ValueSource(nameof(UInt32Values))] uint value)
		{
			MethodCallRoundtripObject(new FieldUInt32 { Value = value });
		}

		#endregion

		#region Method Result callback

		[Test]
		public void TestEmptyMethodResult([ValueSource(nameof(RpcIds))] ulong rpcId)
		{
			var serializer = Create();
			using (var stream = new MemoryStream())
			{
				using (serializer.CreateMethodResultWriter(stream, rpcId))
				{ }

				PrintAndRewind(stream);

				using (var reader = CreateMethodResultReader(serializer, stream))
				{
					reader.RpcId.Should().Be(rpcId);
					Exception exception;
					reader.ReadException(out exception).Should().BeFalse("because the method call didn't return an exception");

					const string reason = "because the method call didn't return a value";
					byte byteValue;
					reader.ReadResultByte(out byteValue).Should().BeFalse(reason);
					short int16Value;
					reader.ReadResultInt16(out int16Value).Should().BeFalse(reason);
					int int32Value;
					reader.ReadResultInt32(out int32Value).Should().BeFalse(reason);
					long int64Value;
					reader.ReadResultInt64(out int64Value).Should().BeFalse(reason);
					ushort uint16Value;
					reader.ReadResultUInt16(out uint16Value).Should().BeFalse(reason);
					uint uint32Value;
					reader.ReadResultUInt32(out uint32Value).Should().BeFalse(reason);
					ulong uint64Value;
					reader.ReadResultUInt64(out uint64Value).Should().BeFalse(reason);
					float floatValue;
					reader.ReadResultSingle(out floatValue).Should().BeFalse(reason);
					double doubleValue;
					reader.ReadResultDouble(out doubleValue).Should().BeFalse(reason);
					string stringValue;
					reader.ReadResultString(out stringValue).Should().BeFalse(reason);
					object value;
					reader.ReadResult(out value).Should().BeFalse(reason);
				}
			}
		}

		[Test]
		public void TestMethodResultException1()
		{
			const int rpcId = 10;
			var exception = MethodResultRoundtripException(rpcId, new ArgumentException("You passed a wrong argument"));
			exception.Should().BeOfType<ArgumentException>();
			exception.Message.Should().Be("You passed a wrong argument");
		}

		[Test]
		public void TestMethodResultException2()
		{
			const int rpcId = 10;
			var exception = MethodResultRoundtripException(rpcId, new Exception("Oops"));
			exception.Should().BeOfType<Exception>();
			exception.Message.Should().Be("Oops");
		}

		[Test]
		[Description("Verifies that a custom Exception can be roundtripped")]
		public void TestRoundtripCustomException()
		{
			const int rpcId = 10;
			var exception = MethodResultRoundtripException(rpcId, new WellBehavedCustomException("You forgot to implement this!"));
			exception.Should().BeOfType<WellBehavedCustomException>();
			exception.Message.Should().Be("You forgot to implement this!");
		}

		[Test]
		public void TestMethodResultByte([ValueSource(nameof(ByteValues))] byte value)
		{
			var serializer = Create();
			using (var stream = new MemoryStream())
			{
				const int rpcId = 12;
				using (var writer = serializer.CreateMethodResultWriter(stream, rpcId))
				{
					writer.WriteResult(value);
				}

				PrintAndRewind(stream);

				using (var reader = CreateMethodResultReader(serializer, stream))
				{
					reader.RpcId.Should().Be(rpcId);

					byte actualValue;
					reader.ReadResultByte(out actualValue).Should().BeTrue();
					actualValue.Should().Be(value);
				}
			}
		}

		[Test]
		public void TestMethodResultSByte([ValueSource(nameof(SByteValues))] sbyte value)
		{
			var serializer = Create();
			using (var stream = new MemoryStream())
			{
				const int rpcId = 10;
				using (var writer = serializer.CreateMethodResultWriter(stream, rpcId))
				{
					writer.WriteResult(value);
				}

				PrintAndRewind(stream);

				using (var reader = CreateMethodResultReader(serializer, stream))
				{
					reader.RpcId.Should().Be(rpcId);

					sbyte actualValue;
					reader.ReadResultSByte(out actualValue).Should().BeTrue();
					actualValue.Should().Be(value);
				}
			}
		}

		[Test]
		public void TestMethodResultUInt16([ValueSource(nameof(UInt16Values))] ushort value)
		{
			var serializer = Create();
			using (var stream = new MemoryStream())
			{
				const int rpcId = 10;
				using (var writer = serializer.CreateMethodResultWriter(stream, rpcId))
				{
					writer.WriteResult(value);
				}

				PrintAndRewind(stream);

				using (var reader = CreateMethodResultReader(serializer, stream))
				{
					reader.RpcId.Should().Be(rpcId);

					ushort actualValue;
					reader.ReadResultUInt16(out actualValue).Should().BeTrue();
					actualValue.Should().Be(value);
				}
			}
		}

		[Test]
		public void TestMethodResultInt16([ValueSource(nameof(Int16Values))] short value)
		{
			var serializer = Create();
			using (var stream = new MemoryStream())
			{
				const int rpcId = 10;
				using (var writer = serializer.CreateMethodResultWriter(stream, rpcId))
				{
					writer.WriteResult(value);
				}

				PrintAndRewind(stream);

				using (var reader = CreateMethodResultReader(serializer, stream))
				{
					reader.RpcId.Should().Be(rpcId);

					short actualValue;
					reader.ReadResultInt16(out actualValue).Should().BeTrue();
					actualValue.Should().Be(value);
				}
			}
		}

		[Test]
		public void TestMethodResultUInt32([ValueSource(nameof(UInt32Values))] uint value)
		{
			var serializer = Create();
			using (var stream = new MemoryStream())
			{
				const int rpcId = 10;
				using (var writer = serializer.CreateMethodResultWriter(stream, rpcId))
				{
					writer.WriteResult(value);
				}

				PrintAndRewind(stream);

				using (var reader = CreateMethodResultReader(serializer, stream))
				{
					reader.RpcId.Should().Be(rpcId);

					uint actualValue;
					reader.ReadResultUInt32(out actualValue).Should().BeTrue();
					actualValue.Should().Be(value);
				}
			}
		}

		[Test]
		public void TestMethodResultInt32([ValueSource(nameof(Int32Values))] int value)
		{
			var serializer = Create();
			using (var stream = new MemoryStream())
			{
				const int rpcId = 10;
				using (var writer = serializer.CreateMethodResultWriter(stream, rpcId))
				{
					writer.WriteResult(value);
				}

				PrintAndRewind(stream);

				using (var reader = CreateMethodResultReader(serializer, stream))
				{
					reader.RpcId.Should().Be(rpcId);

					int actualValue;
					reader.ReadResultInt32(out actualValue).Should().BeTrue();
					actualValue.Should().Be(value);
				}
			}
		}

		[Test]
		public void TestMethodResultUInt64([ValueSource(nameof(UInt64Values))] ulong value)
		{
			var serializer = Create();
			using (var stream = new MemoryStream())
			{
				const int rpcId = 10;
				using (var writer = serializer.CreateMethodResultWriter(stream, rpcId))
				{
					writer.WriteResult(value);
				}

				PrintAndRewind(stream);

				using (var reader = CreateMethodResultReader(serializer, stream))
				{
					reader.RpcId.Should().Be(rpcId);

					ulong actualValue;
					reader.ReadResultUInt64(out actualValue).Should().BeTrue();
					actualValue.Should().Be(value);
				}
			}
		}

		[Test]
		public void TestMethodResultInt64([ValueSource(nameof(Int64Values))] long value)
		{
			var serializer = Create();
			using (var stream = new MemoryStream())
			{
				const int rpcId = 10;
				using (var writer = serializer.CreateMethodResultWriter(stream, rpcId))
				{
					writer.WriteResult(value);
				}

				PrintAndRewind(stream);

				using (var reader = CreateMethodResultReader(serializer, stream))
				{
					reader.RpcId.Should().Be(rpcId);

					long actualValue;
					reader.ReadResultInt64(out actualValue).Should().BeTrue();
					actualValue.Should().Be(value);
				}
			}
		}

		[Test]
		public void TestMethodResultFloat([ValueSource(nameof(SingleValues))] float value)
		{
			var serializer = Create();
			using (var stream = new MemoryStream())
			{
				const int rpcId = 10;
				using (var writer = serializer.CreateMethodResultWriter(stream, rpcId))
				{
					writer.WriteResult(value);
				}

				PrintAndRewind(stream);

				using (var reader = CreateMethodResultReader(serializer, stream))
				{
					reader.RpcId.Should().Be(rpcId);

					float actualValue;
					reader.ReadResultSingle(out actualValue).Should().BeTrue();
					actualValue.Should().Be(value);
				}
			}
		}

		[Test]
		public void TestMethodResultDouble([ValueSource(nameof(DoubleValues))] double value)
		{
			var serializer = Create();
			using (var stream = new MemoryStream())
			{
				const int rpcId = 10;
				using (var writer = serializer.CreateMethodResultWriter(stream, rpcId))
				{
					writer.WriteResult(value);
				}

				PrintAndRewind(stream);

				using (var reader = CreateMethodResultReader(serializer, stream))
				{
					reader.RpcId.Should().Be(rpcId);

					double actualValue;
					reader.ReadResultDouble(out actualValue).Should().BeTrue();
					actualValue.Should().Be(value);
				}
			}
		}

		[Test]
		public void TestMethodResultString([ValueSource(nameof(StringValues))] string value)
		{
			var serializer = Create();
			using (var stream = new MemoryStream())
			{
				const int rpcId = 10;
				using (var writer = serializer.CreateMethodResultWriter(stream, rpcId))
				{
					writer.WriteResult(value);
				}

				PrintAndRewind(stream);

				using (var reader = CreateMethodResultReader(serializer, stream))
				{
					reader.RpcId.Should().Be(rpcId);

					string actualValue;
					reader.ReadResultString(out actualValue).Should().BeTrue();
					actualValue.Should().Be(value);
				}
			}
		}

		#endregion

		#region Constraint violations

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
				"The type 'SharpRemote.Test.Types.Interfaces.IByReferenceWithBeforeDeserializeCallback' is marked with the [ByReference] attribute and thus may not contain serialization callback methods"
			);
		}

		[Test]
		[Description("Verifies that types with the ByReferenceAttribute may not have serialization callbacks")]
		public void TestByReferenceWithAfterDeserializeCallback()
		{
			TestFailRegister<IByReferenceWithAfterDeserializeCallback>(
				"The type 'SharpRemote.Test.Types.Interfaces.IByReferenceWithAfterDeserializeCallback' is marked with the [ByReference] attribute and thus may not contain serialization callback methods"
				);
		}

		[Test]
		[Description("Verifies that types with the ByReferenceAttribute may not have serialization callbacks")]
		public void TestByReferenceWithBeforeSerializeCallback()
		{
			TestFailRegister<IByReferenceWithBeforeSerializeCallback>(
				"The type 'SharpRemote.Test.Types.Interfaces.IByReferenceWithBeforeSerializeCallback' is marked with the [ByReference] attribute and thus may not contain serialization callback methods"
			);
		}

		[Test]
		[Description("Verifies that types with the ByReferenceAttribute may not have serialization callbacks")]
		public void TestByReferenceWithAfterSerializeCallback()
		{
			TestFailRegister<IByReferenceWithAfterSerializeCallback>(
				"The type 'SharpRemote.Test.Types.Interfaces.IByReferenceWithAfterSerializeCallback' is marked with the [ByReference] attribute and thus may not contain serialization callback methods"
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

		#endregion

		#region Helper methods

		private void MethodCallRoundtripObject<T>(T value)
		{
			var serializer = Create();
			using (var stream = new MemoryStream())
			{
				serializer.RegisterType<T>();
				Save();

				using (var writer = serializer.CreateMethodCallWriter(stream, 5, 6, "GetValue"))
				{
					writer.WriteArgument(value);
				}

				PrintAndRewind(stream);

				using (var reader = CreateMethodCallReader(serializer, stream))
				{
					reader.RpcId.Should().Be(5);
					reader.GrainId.Should().Be(6);
					reader.MethodName.Should().Be("GetValue");

					object actualValue;
					reader.ReadNextArgument(out actualValue).Should().BeTrue();
					if (value != null)
					{
						actualValue.Should().BeOfType<T>();
						if (!ReferenceEquals(value, string.Empty))
							actualValue.Should().NotBeSameAs(value);
					}
					actualValue.Should().Be(value);

					reader.ReadNextArgument(out actualValue).Should().BeFalse();
					actualValue.Should().Be(null);
				}
			}
		}

		private void MethodCallRoundtripSingleton<T>(T value) where T : class
		{
			var serializer = Create();
			using (var stream = new MemoryStream())
			{
				serializer.RegisterType<T>();
				Save();

				using (var writer = serializer.CreateMethodCallWriter(stream, 5, 6, "GetValue"))
				{
					writer.WriteArgument(value);
				}

				PrintAndRewind(stream);

				using (var reader = CreateMethodCallReader(serializer, stream))
				{
					reader.RpcId.Should().Be(5);
					reader.GrainId.Should().Be(6);
					reader.MethodName.Should().Be("GetValue");

					object actualValue;
					reader.ReadNextArgument(out actualValue).Should().BeTrue();
					actualValue.Should().BeOfType<T>();
					actualValue.Should().BeSameAs(value);

					reader.ReadNextArgument(out actualValue).Should().BeFalse();
					actualValue.Should().Be(null);
				}
			}
		}

		private Exception MethodResultRoundtripException(ulong rpcId, Exception exception)
		{
			var serializer = Create();
			using (var stream = new MemoryStream())
			{
				using (var writer = serializer.CreateMethodResultWriter(stream, rpcId))
				{
					writer.WriteException(exception);
				}

				PrintAndRewind(stream);

				using (var reader = CreateMethodResultReader(serializer, stream))
				{
					reader.RpcId.Should().Be(rpcId);

					Exception actualException;
					reader.ReadException(out actualException).Should().BeTrue();
					return actualException;
				}
			}
		}


		private void TestFailRegister<T>(string reason)
		{
			var serializer = Create();
			new Action(() => serializer.RegisterType<T>())
				.ShouldThrow<ArgumentException>("because the type '{0}' violates serialization constraints", typeof(T).Name)
				.WithMessage(reason);
			serializer.IsTypeRegistered<T>().Should().BeFalse();
		}

		protected abstract string Format(MemoryStream stream);

		private void PrintAndRewind(MemoryStream stream)
		{
			stream.Position.Should()
			      .BeGreaterThan(expected: 0, because: "because some data must've been written to the stream");
			stream.Position = 0;

			var formatted = Format(stream);
			TestContext.Progress.WriteLine("Message, {0} bytes", stream.Length);
			TestContext.Progress.Write(formatted);
			stream.Position = 0;
		}

		private static IMethodCallReader CreateMethodCallReader(ISerializer2 serializer, Stream stream)
		{
			IMethodCallReader callReader;
			IMethodResultReader unused;
			serializer.CreateMethodReader(stream, out callReader, out unused);
			return callReader;
		}

		private static IMethodResultReader CreateMethodResultReader(ISerializer2 serializer, Stream stream)
		{
			IMethodCallReader unused;
			IMethodResultReader resultReader;
			serializer.CreateMethodReader(stream, out unused, out resultReader);
			return resultReader;
		}

		#endregion
	}
}