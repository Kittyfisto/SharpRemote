using System;
using System.Collections.Generic;
using System.IO;
using FluentAssertions;
using NUnit.Framework;
using SharpRemote.Test.Types.Classes;
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

		[Test]
		public void TestEmptyMethodCall([ValueSource("RpcIds")] ulong rpcId,
		                                [ValueSource("GrainIds")] ulong grainId,
		                                [ValueSource("MethodNames")] string methodName)
		{
			var serializer = Create();
			using (var stream = new MemoryStream())
			{
				using (serializer.CreateMethodCallWriter(stream, rpcId, grainId, methodName))
				{
				}

				PrintAndRewind(stream);

				using (var reader = CreateMethodInvocationReader(serializer, stream))
				{
					reader.RpcId.Should().Be(rpcId);
					reader.GrainId.Should().Be(grainId);
					reader.MethodName.Should().Be(methodName);
				}
			}
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

				using (var reader = CreateMethodInvocationReader(serializer, stream))
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

				using (var reader = CreateMethodInvocationReader(serializer, stream))
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

				using (var reader = CreateMethodInvocationReader(serializer, stream))
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

				using (var reader = CreateMethodInvocationReader(serializer, stream))
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
		public void TestMethodCallSByte()
		{
			var serializer = Create();
			using (var stream = new MemoryStream())
			{
				using (var writer = serializer.CreateMethodCallWriter(stream, 1, 2, "Foo"))
				{
					writer.WriteArgument(sbyte.MaxValue);
				}

				PrintAndRewind(stream);

				using (var reader = CreateMethodInvocationReader(serializer, stream))
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
				using (var writer = serializer.CreateMethodCallWriter(stream, 3, 4, "Bar"))
				{
					writer.WriteArgument(byte.MaxValue);
				}

				PrintAndRewind(stream);

				using (var reader = CreateMethodInvocationReader(serializer, stream))
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
				using (var writer = serializer.CreateMethodCallWriter(stream, 5, 6, "Bar"))
				{
					writer.WriteArgument(short.MaxValue);
				}

				PrintAndRewind(stream);

				using (var reader = CreateMethodInvocationReader(serializer, stream))
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
				using (var writer = serializer.CreateMethodCallWriter(stream, 5, 6, "Bar"))
				{
					writer.WriteArgument(ushort.MaxValue);
				}

				PrintAndRewind(stream);

				using (var reader = CreateMethodInvocationReader(serializer, stream))
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
				using (var writer = serializer.CreateMethodCallWriter(stream, 5, 6, "Bar"))
				{
					writer.WriteArgument(int.MaxValue);
				}

				PrintAndRewind(stream);

				using (var reader = CreateMethodInvocationReader(serializer, stream))
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
				using (var writer = serializer.CreateMethodCallWriter(stream, 5, 6, "Bar"))
				{
					writer.WriteArgument(uint.MaxValue);
				}

				PrintAndRewind(stream);

				using (var reader = CreateMethodInvocationReader(serializer, stream))
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
				using (var writer = serializer.CreateMethodCallWriter(stream, 5, 6, "Bar"))
				{
					writer.WriteArgument(long.MaxValue);
				}

				PrintAndRewind(stream);

				using (var reader = CreateMethodInvocationReader(serializer, stream))
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
				using (var writer = serializer.CreateMethodCallWriter(stream, 5, 6, "Bar"))
				{
					writer.WriteArgument(ulong.MaxValue);
				}

				PrintAndRewind(stream);

				using (var reader = CreateMethodInvocationReader(serializer, stream))
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
				using (var writer = serializer.CreateMethodCallWriter(stream, 5, 6, "Bar"))
				{
					writer.WriteArgument(value);
				}

				PrintAndRewind(stream);

				using (var reader = CreateMethodInvocationReader(serializer, stream))
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
				using (var writer = serializer.CreateMethodCallWriter(stream, 5, 6, "Bar"))
				{
					writer.WriteArgument(value);
				}

				PrintAndRewind(stream);

				using (var reader = CreateMethodInvocationReader(serializer, stream))
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
				using (var writer = serializer.CreateMethodCallWriter(stream, 5, 6, "Bar"))
				{
					writer.WriteArgument(value);
				}

				PrintAndRewind(stream);

				using (var reader = CreateMethodInvocationReader(serializer, stream))
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
		public void TestMethodCallString([Values("3.14159", "", null)] string value)
		{
			var serializer = Create();
			using (var stream = new MemoryStream())
			{
				using (var writer = serializer.CreateMethodCallWriter(stream, 5, 6, "GetValue"))
				{
					writer.WriteArgument(value);
				}

				PrintAndRewind(stream);

				using (var reader = CreateMethodInvocationReader(serializer, stream))
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
		public void TestMethodCallFieldDecimal([ValueSource(nameof(DecimalValues))] decimal value)
		{
			var serializer = Create();
			using (var stream = new MemoryStream())
			{
				serializer.RegisterType<FieldDecimal>();
				Save();

				using (var writer = serializer.CreateMethodCallWriter(stream, 5, 6, "GetValue"))
				{
					writer.WriteArgument(new FieldDecimal {Value = value });
				}

				PrintAndRewind(stream);

				using (var reader = CreateMethodInvocationReader(serializer, stream))
				{
					reader.RpcId.Should().Be(5);
					reader.GrainId.Should().Be(6);
					reader.MethodName.Should().Be("GetValue");

					object actualValue;
					reader.ReadNextArgument(out actualValue).Should().BeTrue();
					actualValue.Should().BeOfType<FieldDecimal>();
					((FieldDecimal)actualValue).Value.Should().Be(value);

					reader.ReadNextArgument(out actualValue).Should().BeFalse();
					actualValue.Should().Be(null);
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

				using (var reader = CreateMethodResultReader(serializer, stream))
				{
					reader.RpcId.Should().Be(rpcId);
				}
			}
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

		private void TestMethodInvocationRoundtrip<T>(T value) where T : class
		{
			var serializer = Create();
			using (var stream = new MemoryStream())
			{
				serializer.RegisterType<T>();
				//Save();

				using (var writer = serializer.CreateMethodCallWriter(stream, 5, 6, "GetValue"))
				{
					writer.WriteArgument(value);
				}

				PrintAndRewind(stream);

				using (var reader = CreateMethodInvocationReader(serializer, stream))
				{
					reader.RpcId.Should().Be(5);
					reader.GrainId.Should().Be(6);
					reader.MethodName.Should().Be("GetValue");

					object actualValue;
					reader.ReadNextArgument(out actualValue).Should().BeTrue();
					actualValue.Should().BeOfType<T>();
					actualValue.Should().Be(value);

					reader.ReadNextArgument(out actualValue).Should().BeFalse();
					actualValue.Should().Be(null);
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
			TestContext.Out.WriteLine("Message, {0} bytes", stream.Length);
			TestContext.Out.Write(formatted);
			stream.Position = 0;
		}

		private static IMethodCallReader CreateMethodInvocationReader(ISerializer2 serializer, Stream stream)
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
	}
}