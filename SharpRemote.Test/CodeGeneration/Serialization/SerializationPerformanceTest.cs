using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.Serialization;
using Moq;
using NUnit.Framework;

namespace SharpRemote.Test.CodeGeneration.Serialization
{
	[TestFixture]
	public sealed class SerializationPerformanceTest
	{
		private ISerializer _serializer;
		private DataContractSerializer _contractSerializer;

		/// <summary>
		///     Prepares the serializers to serialize values of the given type.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		private void Prepare<T>()
		{
			_serializer = new Serializer();
			_serializer.RegisterType<T>();
			_contractSerializer = new DataContractSerializer(typeof (T));
		}

		private void Measure<T>(T value, int numSamples)
		{
			Prepare<T>();
			Warmup(value);

			using (var data = new MemoryStream())
			using (var writer = new BinaryWriter(data))
			{
				var sw = new Stopwatch();
				sw.Start();

				for (int i = 0; i < numSamples; ++i)
				{
					_serializer.WriteObject(writer, value, null);
				}

				sw.Stop();
				WriteVerdict("ISerializer", sw, data, numSamples);
			}

			using (var data = new MemoryStream())
			{
				var sw = new Stopwatch();
				sw.Start();

				for (int i = 0; i < numSamples; ++i)
				{
					_contractSerializer.WriteObject(data, value);
				}

				sw.Stop();
				WriteVerdict("DataContractSerializer", sw, data, numSamples);
			}
		}

		private static void WriteVerdict(string name, Stopwatch sw, MemoryStream data, int numSamples)
		{
			double timePerSample = sw.Elapsed.TotalMilliseconds/numSamples;
			long sizePerSample = data.Length/numSamples;
			Console.WriteLine("{0}: {1:F2}ms, {2}", name, timePerSample, TestHelpers.FormatBytes(sizePerSample));
		}

		/// <summary>
		///     Ensures that the
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="value"></param>
		private void Warmup<T>(T value)
		{
			using (var data = new MemoryStream())
			using (var writer = new BinaryWriter(data))
			{
				for (int i = 0; i < 100; ++i)
				{
					_serializer.WriteObject(writer, value, null);
					_contractSerializer.WriteObject(data, value);
				}
			}
		}

		[Test]
		[PerformanceTest]
		public void TestByteArray()
		{
			var value = new byte[12123];
			Console.WriteLine("Information: {0}", TestHelpers.FormatBytes(value.Length));

			const int numSamples = 1000;
			Measure(value, numSamples);
		}

		[Test]
		[PerformanceTest]
		public void TestIntArray()
		{
			var value = new int[1234];
			Console.WriteLine("Size: {0}", TestHelpers.FormatBytes(value.Length*4));

			const int numSamples = 1000;
			Measure(value, numSamples);
		}

		[Test]
		[PerformanceTest]
		public void TestObjectIntArray()
		{
			var value = new object[1234];
			var rng = new Random();
			for (int i = 0; i < value.Length; ++i)
			{
				value[i] = rng.Next();
			}

			Console.WriteLine("Size: {0}", TestHelpers.FormatBytes(value.Length*4));

			const int numSamples = 100;
			Measure(value, numSamples);
		}
	}
}