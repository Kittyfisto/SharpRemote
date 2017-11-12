using System;
using System.Reflection;
using System.Reflection.Emit;

namespace SharpRemote.CodeGeneration.Serialization
{
	/// <summary>
	///     Responsible for compiling and providing methods which take care of serializing/deserializing values.
	/// </summary>
	public abstract class AbstractMethodsCompiler
		: ISerializationMethods
	{
		private readonly IMethodCompiler[] _compilers;
		private readonly TypeBuilder _typeBuilder;
		private readonly TypeDescription _typeDescription;

		/// <summary>
		///     Initializes this object.
		/// </summary>
		/// <param name="typeBuilder"></param>
		/// <param name="typeDescription"></param>
		/// <param name="writeValueMethodCompiler"></param>
		/// <param name="writeObjectMethodCompiler"></param>
		/// <param name="readValueMethodCompiler"></param>
		/// <param name="readObjectMethodCompiler"></param>
		protected AbstractMethodsCompiler(TypeBuilder typeBuilder,
		                                 TypeDescription typeDescription,
		                                 AbstractWriteValueMethodCompiler writeValueMethodCompiler,
		                                 AbstractWriteObjectMethodCompiler writeObjectMethodCompiler,
		                                 AbstractReadValueMethodCompiler readValueMethodCompiler,
		                                 AbstractReadObjectMethodCompiler readObjectMethodCompiler)
		{
			if (typeBuilder == null)
				throw new ArgumentNullException(nameof(typeBuilder));
			if (writeValueMethodCompiler == null)
				throw new ArgumentNullException(nameof(writeValueMethodCompiler));
			if (writeObjectMethodCompiler == null)
				throw new ArgumentNullException(nameof(writeObjectMethodCompiler));
			if (readValueMethodCompiler == null)
				throw new ArgumentNullException(nameof(readValueMethodCompiler));
			if (readObjectMethodCompiler == null)
				throw new ArgumentNullException(nameof(readObjectMethodCompiler));

			_typeBuilder = typeBuilder;
			_typeDescription = typeDescription;

			_compilers = new IMethodCompiler[]
			{
				writeObjectMethodCompiler,
				writeValueMethodCompiler,
				readObjectMethodCompiler,
				readValueMethodCompiler
			};
			WriteObjectMethod = writeObjectMethodCompiler.Method;
			WriteValueMethod = writeValueMethodCompiler.Method;
			ReadObjectMethod = readObjectMethodCompiler.Method;
			ReadValueMethod = readValueMethodCompiler.Method;
		}

		/// <summary>
		/// </summary>
		protected abstract Type WriterType { get; }

		/// <summary>
		/// </summary>
		protected abstract Type ReaderType { get; }

		/// <inheritdoc />
		public ITypeDescription TypeDescription => _typeDescription;

		/// <inheritdoc />
		public MethodInfo WriteValueMethod { get; }

		/// <inheritdoc />
		public MethodInfo WriteObjectMethod { get; }

		/// <inheritdoc />
		public MethodInfo ReadValueMethod { get; }

		/// <inheritdoc />
		public MethodInfo ReadObjectMethod { get; }

		/// <summary>
		///     Emits il-code for all methods.
		/// </summary>
		protected void Compile(ISerializationMethodStorage<AbstractMethodsCompiler> methodStorage)
		{
			foreach (var compiler in _compilers)
			{
				compiler.Compile(this, methodStorage);
			}
			_typeBuilder.CreateType();
		}
	}
}