using System;
using System.IO;

namespace SharpRemote.CodeGeneration.Serialization.Xml
{
	public sealed class XmlSerializer
		: ISerializer
	{
		private readonly TypeModel _typeModel;

		public XmlSerializer()
		{
			
		}

		public void RegisterType<T>()
		{
			throw new NotImplementedException();
		}

		public void RegisterType(Type type)
		{
			throw new NotImplementedException();
		}

		public bool IsTypeRegistered<T>()
		{
			throw new NotImplementedException();
		}

		public bool IsTypeRegistered(Type type)
		{
			throw new NotImplementedException();
		}

		public void WriteObject(BinaryWriter writer, object value, IRemotingEndPoint endPoint)
		{
			throw new NotImplementedException();
		}

		public object ReadObject(BinaryReader reader, IRemotingEndPoint endPoint)
		{
			throw new NotImplementedException();
		}

		public Type GetType(string assemblyQualifiedTypeName)
		{
			throw new NotImplementedException();
		}
	}
}