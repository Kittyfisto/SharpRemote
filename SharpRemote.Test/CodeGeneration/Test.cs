using System;
using System.IO;
using SharpRemote.Test.CodeGeneration.Types.Interfaces.PrimitiveTypes;
using SharpRemote.Test.CodeGeneration.Types.Structs;

namespace SharpRemote.Test.CodeGeneration
{
	public class Test
		: IServant
	{
		private IVoidMethodDoubleParameter _subject;

		public Test(IVoidMethodDoubleParameter subject)
		{
			_subject = subject;
		}

		public ulong ObjectId
		{
			get { throw new NotImplementedException(); }
		}

		public ISerializer Serializer
		{
			get { throw new NotImplementedException(); }
		}

		public object Subject
		{
			get { throw new NotImplementedException(); }
		}

		public FieldStruct Read()
		{
			var tmp = new FieldStruct();
			tmp.A = 42;
			return tmp;
		}

		public static FieldStruct ReadValue(BinaryReader reader, ISerializer serializer)
		{
			var tmp = new FieldStruct();
			tmp.A = reader.ReadDouble();
			tmp.B = reader.ReadInt32();
			tmp.C = reader.ReadString();
			return tmp;
		}

		public static void WriteObject(BinaryWriter writer, FieldStruct value, ISerializer serializer)
		{
			writer.Write(value.A);
			writer.Write(value.B);
			writer.Write(value.C);
		}

		public static void WriteObject(BinaryWriter writer, object value, ISerializer serializer)
		{
			if (value == null)
			{
				writer.Write(string.Empty);
			}
			else
			{
				writer.Write(value.GetType().AssemblyQualifiedName);
				WriteObject(writer, (FieldStruct)value, serializer);
			}
		}

		public void Invoke(string methodName, BinaryReader reader, BinaryWriter writer)
		{
			switch (methodName)
			{
				case "Do":
					_subject.Do(reader.ReadDouble());
					break;

				case "Bar":
					_subject.Do(reader.ReadDouble());
					break;

				default:
					throw new ArgumentException(string.Format("Function {0} not found", methodName));
			}
		}
	}
}