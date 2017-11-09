using System;
using System.Reflection.Emit;
using System.Xml;

namespace SharpRemote.CodeGeneration.Serialization.Xml
{
	/// <summary>
	///     Compiles methods to serialize/deserialize one .NET type,
	///     <see cref="Compile" />.
	/// </summary>
	internal sealed class XmlSerializationMethods
		: AbstractSerializationMethods
	{
		public XmlSerializationMethods(TypeBuilder typeBuilder, TypeDescription typeDescription)
			: base(typeBuilder, typeDescription)
		{}

		protected override Type WriterType => typeof(XmlWriter);

		protected override Type ReaderType => typeof(XmlReader);

		public Action<XmlWriter, object, ISerializer, IRemotingEndPoint> WriteDelegate { get; private set; }

		public Func<XmlReader, ISerializer, IRemotingEndPoint, object> ReadObjectDelegate { get; private set; }

		public void Compile(ISerializationMethodStorage<XmlSerializationMethods> storage)
		{
			base.Compile(storage);

			WriteDelegate =
				(Action<XmlWriter, object, ISerializer, IRemotingEndPoint>)
				WriteObjectMethod
				            .CreateDelegate(typeof(Action<XmlWriter, object, ISerializer, IRemotingEndPoint>));

			ReadObjectDelegate =
				(Func<XmlReader, ISerializer, IRemotingEndPoint, object>)
				ReadObjectMethod
				            .CreateDelegate(typeof(Func<XmlReader, ISerializer, IRemotingEndPoint, object>));
		}
		
	}
}