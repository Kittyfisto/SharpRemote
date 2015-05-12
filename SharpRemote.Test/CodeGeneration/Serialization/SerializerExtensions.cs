using System;
using System.Collections;
using System.IO;
using System.Text;
using FluentAssertions;

namespace SharpRemote.Test.CodeGeneration.Serialization
{
	public static class SerializerExtensions
	{
		#region Static Methods

		public static T ShouldRoundtripEnumeration<T>(this ISerializer serializer, T value)
			where T : class, IEnumerable
		{
			using (var stream = new MemoryStream())
			{
				var writer = new BinaryWriter(stream, Encoding.UTF8);
				serializer.WriteObject(writer, value);
				writer.Flush();
				stream.Position = 0;

				var reader = new BinaryReader(stream, Encoding.UTF8);
				var actualValue = (T)serializer.ReadObject(reader);

				actualValue.Should().Equal(value, "because serialization should preserve the order of elements in the enumeration");
				if (value != null)
				{
					actualValue.Should().NotBeSameAs(value, "because serialization should've created a deep copy");
				}
				else
				{
					actualValue.Should().BeNull();
				}

				stream.Position.Should()
					  .Be(stream.Length,
						  "because reading the object again should've consumed everything that was written - not a single byte less");

				return actualValue;
			}
		}

		public static object ShouldRoundtrip(this ISerializer serializer, object value)
		{
			using (var stream = new MemoryStream())
			{
				var writer = new BinaryWriter(stream, Encoding.UTF8);
				serializer.WriteObject(writer, value);
				writer.Flush();
				stream.Position = 0;

				var reader = new BinaryReader(stream, Encoding.UTF8);
				object actualValue = serializer.ReadObject(reader);

				actualValue.Should().Be(value, "because serialization should preserve all those members attributing to value equality");

				if (value != null)
				{
					if (actualValue != string.Empty && !(actualValue is Type))
					{
						actualValue.Should().NotBeSameAs(value, "because serialization should've created a deep copy");
					}
				}
				else
				{
					actualValue.Should().BeNull();
				}

				stream.Length.Should().BeGreaterThan(0, "because something must have been written to the stream");
				stream.Position.Should()
				      .Be(stream.Length,
					      "because reading the object again should've consumed everything that was written - not a single byte less");

				return actualValue;
			}
		}

		#endregion
	}
}