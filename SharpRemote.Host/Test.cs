using System;
using System.IO;
using SharpRemote.CodeGeneration;
using SharpRemote.Hosting;

namespace SharpRemote.Host
{
	public sealed class Test
		: IServant
	{
		private readonly ISubjectHost _subject;

		public ulong ObjectId
		{
			get { throw new System.NotImplementedException(); }
		}

		public ISerializer Serializer
		{
			get { throw new System.NotImplementedException(); }
		}

		public object Subject
		{
			get { throw new System.NotImplementedException(); }
		}

		public void InvokeMethod(string methodName, BinaryReader reader, BinaryWriter writer)
		{
			switch (methodName)
			{
				case "CreateSubject":
					writer.Write(_subject.CreateSubject(Methods.GetType(reader.ReadString()), Methods.GetType(reader.ReadString())));
					break;

				case "Dispose":
					_subject.Dispose();
					break;

				default:
					throw new ArgumentException(string.Format("Method '{0}' not found", methodName));
			}

			writer.Flush();
		}
	}
}