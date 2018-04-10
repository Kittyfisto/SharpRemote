using System;
using System.IO;
using System.Reflection.Emit;

namespace SharpRemote.CodeGeneration.Serialization.Binary
{
	internal sealed class BinaryMethodsCompiler
		: AbstractMethodsCompiler
	{
		private readonly CompilationContext _context;

		public BinaryMethodsCompiler(TypeBuilder typeBuilder,
		                             TypeDescription typeDescription,
		                             CompilationContext context,
		                             BinaryWriteValueMethodCompiler writeValueMethodCompiler,
		                             BinaryWriteObjectMethodCompiler writeObjectMethodCompiler,
		                             BinaryReadValueMethodCompiler readValueMethodCompiler,
		                             BinaryReadObjectMethodCompiler readObjectMethodCompiler)
			: base(typeBuilder,
			       typeDescription,
			       writeValueMethodCompiler,
			       writeObjectMethodCompiler,
			       readValueMethodCompiler,
			       readObjectMethodCompiler)
		{
			_context = context;
		}

		protected override Type WriterType => typeof(BinaryWriter);

		protected override Type ReaderType => typeof(BinaryReader);

		public Action<BinaryWriter, object, BinarySerializer2, IRemotingEndPoint> WriteDelegate { get; private set; }

		public Func<BinaryReader, BinarySerializer2, IRemotingEndPoint, object> ReadObjectDelegate { get; private set; }

		public static BinaryMethodsCompiler Create(TypeBuilder typeBuilder, TypeDescription typeDescription)
		{
			var context = new CompilationContext
			{
				TypeDescription = typeDescription,
				SerializerType = typeof(BinarySerializer2),
				ReaderType = typeof(BinaryReader),
				WriterType = typeof(BinaryWriter),
				TypeBuilder = typeBuilder
			};

			return new BinaryMethodsCompiler(typeBuilder,
			                                 typeDescription,
			                                 context,
			                                 new BinaryWriteValueMethodCompiler(context),
			                                 new BinaryWriteObjectMethodCompiler(context),
			                                 new BinaryReadValueMethodCompiler(context),
			                                 new BinaryReadObjectMethodCompiler(context));
		}

		public void Compile(ISerializationMethodStorage<BinaryMethodsCompiler> storage)
		{
			base.Compile(storage);

			WriteDelegate =
				(Action<BinaryWriter, object, BinarySerializer2, IRemotingEndPoint>)
				_context.TypeBuilder.GetMethod("WriteObject")
				        .CreateDelegate(typeof(Action<BinaryWriter, object, BinarySerializer2, IRemotingEndPoint>));

			ReadObjectDelegate =
				(Func<BinaryReader, BinarySerializer2, IRemotingEndPoint, object>)
				_context.TypeBuilder.GetMethod("ReadObject")
				        .CreateDelegate(typeof(Func<BinaryReader, BinarySerializer2, IRemotingEndPoint, object>));
		}
	}
}