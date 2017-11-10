using System;
using System.Reflection.Emit;
using System.Xml;

namespace SharpRemote.CodeGeneration.Serialization.Xml
{
	/// <summary>
	///     Compiles methods to serialize/deserialize one .NET type,
	///     <see cref="Compile" />.
	/// </summary>
	internal sealed class XmlMethodCompiler
		: AbstractMethodCompiler
	{
		public XmlMethodCompiler(TypeBuilder typeBuilder, TypeDescription typeDescription)
			: this(typeBuilder,
			       typeDescription,
			       new CompilationContext
			{
				Type = typeDescription.Type,
				ReaderType = typeof(XmlReader),
				WriterType = typeof(XmlWriter),
				TypeBuilder = typeBuilder
			})
		{}

		private XmlMethodCompiler(TypeBuilder typeBuilder, TypeDescription typeDescription, CompilationContext context)
			: base(typeBuilder,
			       typeDescription,
			       new XmlWriteValueNotNullMethodCompiler(context),
			       new XmlWriteValueMethodCompiler(context),
			       new XmlWriteObjectMethodCompiler(context),
			       new XmlReadValueNotNullMethodCompiler(context),
			       new XmlReadValueMethodCompiler(context),
			       new XmlReadObjectMethodCompiler(context))
		{}

		protected override Type WriterType => typeof(XmlWriter);

		protected override Type ReaderType => typeof(XmlReader);

		public Action<XmlWriter, object, ISerializer2, IRemotingEndPoint> WriteDelegate { get; private set; }

		public Func<XmlReader, ISerializer2, IRemotingEndPoint, object> ReadObjectDelegate { get; private set; }

		public void Compile(ISerializationMethodStorage<XmlMethodCompiler> storage)
		{
			base.Compile(storage);

			WriteDelegate =
				(Action<XmlWriter, object, ISerializer2, IRemotingEndPoint>)
				WriteObjectMethod
					.CreateDelegate(typeof(Action<XmlWriter, object, ISerializer2, IRemotingEndPoint>));

			ReadObjectDelegate =
				(Func<XmlReader, ISerializer2, IRemotingEndPoint, object>)
				ReadObjectMethod
					.CreateDelegate(typeof(Func<XmlReader, ISerializer2, IRemotingEndPoint, object>));
		}
	}
}