using System;
using System.Collections;
using System.IO;
using System.Text;
using FluentAssertions;
using SharpRemote.Test.Types.Classes;

namespace SharpRemote.Test.CodeGeneration.Serialization
{
	internal static class SerializerExtensions
	{
		#region Static Methods

		public static T ShouldRoundtripEnumeration<T>(this ISerializer serializer, T value)
			where T : class, IEnumerable
		{
			using (var stream = new MemoryStream())
			{
				var writer = new BinaryWriter(stream, Encoding.UTF8);
				serializer.WriteObject(writer, value, null);
				writer.Flush();
				stream.Position = 0;

				var reader = new BinaryReader(stream, Encoding.UTF8);
				var actualValue = (T) serializer.ReadObject(reader, null);

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

		public static object ShouldRoundtrip(this ISerializer serializer, object value, IRemotingEndPoint endPoint = null)
		{
			using (var stream = new MemoryStream())
			{
				var writer = new BinaryWriter(stream, Encoding.UTF8);
				serializer.WriteObject(writer, value, endPoint);
				writer.Flush();
				stream.Position = 0;

				var reader = new BinaryReader(stream, Encoding.UTF8);
				object actualValue = serializer.ReadObject(reader, endPoint);

				actualValue.Should()
				           .Be(value, "because serialization should preserve all those members attributing to value equality");

				if (value != null)
				{
					if (!Equals(actualValue, string.Empty) && !(actualValue is Type) && !(actualValue is Singleton))
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