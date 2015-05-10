using System.Collections.Generic;
using System.IO;
using SharpRemote.CodeGeneration;
using SharpRemote.Hosting;

namespace SharpRemote.Test
{
	internal class Test
	{
		private ISubjectHost _subject;

		public void CreateSubject(BinaryReader reader, BinaryWriter writer)
		{
			writer.Write(_subject.CreateSubject1(Methods.GetType(reader.ReadString()), Methods.GetType(reader.ReadString())));
		}

		public static void Write(BinaryWriter writer, string value)
		{
			if (value != null)
			{
				writer.Write(true);
				writer.Write(value);
			}
			else
			{
				writer.Write(false);
			}
		}

		public static int[] Read(BinaryReader reader, ISerializer serializer)
		{
			var count = reader.ReadInt32();
			var value = new int[count];

			for (int i = 0; i < count; ++i)
			{
				value[i] = reader.ReadInt32();
			}

			return value;
		}

		public static void Write(BinaryWriter writer, int[] value, ISerializer serializer)
		{
			writer.Write(value.GetType().AssemblyQualifiedName);
			var enumerator = ((IEnumerable<int>)value).GetEnumerator();
			while (enumerator.MoveNext())
			{
				writer.Write(enumerator.Current);
			}
		}
	}
}