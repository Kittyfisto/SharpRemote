using System;
using System.IO;
using SharpRemote.Test.CodeGeneration.Types.Interfaces.PrimitiveTypes;

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