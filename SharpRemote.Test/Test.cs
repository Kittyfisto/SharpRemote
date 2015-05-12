using System;
using System.Collections.Generic;
using System.IO;
using SharpRemote.Hosting;
using SharpRemote.Test.Types.Classes;
using SharpRemote.Test.Types.Structs;

namespace SharpRemote.Test
{
	internal class Test
	{
		private ISubjectHost _subject;

		public KeyValuePair<int, string> Read()
		{
			return new KeyValuePair<int, string>(42, "foo");
		}

		public static void WriteValue(BinaryWriter writer, KeyValuePair<int, string> value, ISerializer serializer)
		{
			writer.Write(value.Key);
			if (value.Value != null)
			{
				writer.Write(true);
				writer.Write(value.Value);
			}
			else
			{
				writer.Write(false);
			}
		}

		public FieldSealedClass Read(BinaryReader reader)
		{
			var tmp = new FieldSealedClass();
			tmp.A = reader.ReadDouble();
			return tmp;
		}

		public static void WriteValue(BinaryWriter writer, FieldStruct value, ISerializer serializer)
		{
			
		}

		public static void WriteValue(BinaryWriter writer, PropertySealedClass value, ISerializer serializer)
		{
			
		}

		public static FieldObjectStruct ReadValueNotNull(BinaryReader reader, ISerializer serializer)
		{
			var tmp = new FieldObjectStruct();
			tmp.Value = serializer.ReadObject(reader);
			return tmp;
		}

		public static void WriteValueNotNull(BinaryWriter writer, NestedFieldStruct value, ISerializer serializer)
		{
			//WriteValue(writer, value.N1, serializer);
			//WriteValue(writer, value.N2, serializer);
		}

		public static void WriteValueNotNull(BinaryWriter writer, FieldStruct[] value, ISerializer serializer)
		{
			writer.Write(value.Length);
			var it = ((IEnumerable<FieldStruct>) value).GetEnumerator();
			while (it.MoveNext())
			{
				WriteValue(writer, it.Current, serializer);
			}
		}

		public void CreateSubject(BinaryReader reader, BinaryWriter writer)
		{
			writer.Write(_subject.CreateSubject1(Type.GetType(reader.ReadString()), Type.GetType(reader.ReadString())));
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