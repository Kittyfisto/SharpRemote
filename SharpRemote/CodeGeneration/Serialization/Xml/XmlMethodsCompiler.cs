﻿using System;
using System.Reflection.Emit;
using System.Xml;

namespace SharpRemote.CodeGeneration.Serialization.Xml
{
	/// <summary>
	///     Compiles methods to serialize/deserialize one .NET type,
	///     <see cref="Compile" />.
	/// </summary>
	internal sealed class XmlMethodsCompiler
		: AbstractMethodsCompiler
	{
		private readonly CompilationContext _context;

		private XmlMethodsCompiler(TypeBuilder typeBuilder,
		                           TypeDescription typeDescription,
		                           CompilationContext context,
		                           XmlWriteValueMethodCompiler writeValueMethodCompiler,
		                           XmlWriteObjectMethodCompiler writeObjectMethodCompiler,
		                           XmlReadValueMethodCompiler readValueMethodCompiler,
		                           XmlReadObjectMethodCompiler readObjectMethodCompiler)
			: base(typeBuilder,
			       typeDescription,
			       writeValueMethodCompiler,
			       writeObjectMethodCompiler,
			       readValueMethodCompiler,
			       readObjectMethodCompiler)
		{
			_context = context;
		}

		protected override Type WriterType => typeof(XmlWriter);

		protected override Type ReaderType => typeof(XmlReader);

		public Action<XmlWriter, object, ISerializer2, IRemotingEndPoint> WriteDelegate { get; private set; }

		public Func<XmlReader, ISerializer2, IRemotingEndPoint, object> ReadObjectDelegate { get; private set; }

		public static XmlMethodsCompiler Create(TypeBuilder typeBuilder, TypeDescription typeDescription)
		{
			var context = new CompilationContext
			{
				TypeDescription = typeDescription,
				ReaderType = typeof(XmlReader),
				WriterType = typeof(XmlWriter),
				TypeBuilder = typeBuilder
			};

			return new XmlMethodsCompiler(typeBuilder,
			                              typeDescription,
			                              context,
			                              new XmlWriteValueMethodCompiler(context),
			                              new XmlWriteObjectMethodCompiler(context),
			                              new XmlReadValueMethodCompiler(context),
			                              new XmlReadObjectMethodCompiler(context));
		}

		public void Compile(ISerializationMethodStorage<XmlMethodsCompiler> storage)
		{
			base.Compile(storage);

			WriteDelegate =
				(Action<XmlWriter, object, ISerializer2, IRemotingEndPoint>)
				_context.TypeBuilder.GetMethod("WriteObject")
				        .CreateDelegate(typeof(Action<XmlWriter, object, ISerializer2, IRemotingEndPoint>));

			ReadObjectDelegate =
				(Func<XmlReader, ISerializer2, IRemotingEndPoint, object>)
				_context.TypeBuilder.GetMethod("ReadObject")
				        .CreateDelegate(typeof(Func<XmlReader, ISerializer2, IRemotingEndPoint, object>));
		}
	}
}