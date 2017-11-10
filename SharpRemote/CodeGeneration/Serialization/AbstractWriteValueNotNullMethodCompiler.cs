using System;
using System.Reflection;
using System.Reflection.Emit;

namespace SharpRemote.CodeGeneration.Serialization
{
	/// <summary>
	///     Responsible for emitting a method with the following signature:
	///     static void (WriterType, Type, <see cref="ISerializer2" />, <see cref="IRemotingEndPoint" />);
	///     The method can always assume that the passed value can never be null.
	/// </summary>
	public abstract class AbstractWriteValueNotNullMethodCompiler
		: IMethodCompiler
	{
		private readonly MethodBuilder _method;

		/// <summary>
		/// </summary>
		protected AbstractWriteValueNotNullMethodCompiler(CompilationContext context)
		{
			_method = context.TypeBuilder.DefineMethod("WriteValueNotNull",
			                                           MethodAttributes.Public | MethodAttributes.Static,
			                                           CallingConventions.Standard, typeof(void), new[]
			                                           {
				                                           context.WriterType,
				                                           context.Type,
				                                           typeof(ISerializer2),
				                                           typeof(IRemotingEndPoint)
			                                           });
		}

		/// <inheritdoc />
		public MethodBuilder Method => _method;

		/// <inheritdoc />
		public void Compile(AbstractMethodCompiler methods, ISerializationMethodStorage<AbstractMethodCompiler> methodStorage)
		{
			throw new NotImplementedException();
		}
	}
}