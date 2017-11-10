using System;
using System.Reflection;
using System.Reflection.Emit;

namespace SharpRemote.CodeGeneration.Serialization
{
	/// <summary>
	/// </summary>
	public abstract class AbstractReadValueNotNullMethodCompiler
		: IMethodCompiler
	{
		private readonly MethodBuilder _method;

		/// <summary>
		/// 
		/// </summary>
		protected AbstractReadValueNotNullMethodCompiler(CompilationContext context)
		{
			_method = context.TypeBuilder.DefineMethod("ReadValueNotNull",
			                                           MethodAttributes.Public | MethodAttributes.Static,
			                                           CallingConventions.Standard,
			                                           context.Type,
			                                           new[]
			                                           {
				                                           context.ReaderType,
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