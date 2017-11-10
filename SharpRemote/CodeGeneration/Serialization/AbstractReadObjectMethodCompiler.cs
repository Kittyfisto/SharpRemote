using System.Reflection;
using System.Reflection.Emit;

namespace SharpRemote.CodeGeneration.Serialization
{
	/// <summary>
	/// 
	/// </summary>
	public abstract class AbstractReadObjectMethodCompiler
		: AbstractMethodCompiler
	{
		private readonly MethodBuilder _method;
		private readonly CompilationContext _context;

		/// <summary>
		/// </summary>
		/// <param name="context"></param>
		protected AbstractReadObjectMethodCompiler(CompilationContext context)
		{
			_context = context;
			_method = context.TypeBuilder.DefineMethod("ReadObject", MethodAttributes.Public | MethodAttributes.Static,
			                                           CallingConventions.Standard, typeof(object), new[]
			                                           {
				                                           context.ReaderType,
				                                           typeof(ISerializer2),
				                                           typeof(IRemotingEndPoint)
			                                           });
		}

		/// <inheritdoc />
		public override MethodBuilder Method => _method;

		/// <inheritdoc />
		public override void Compile(AbstractMethodsCompiler methods, ISerializationMethodStorage<AbstractMethodsCompiler> methodStorage)
		{
			var requiresBoxing = _context.Type.IsPrimitive || _context.Type.IsValueType;
			var gen = _method.GetILGenerator();

			// return ReadValueNotNull(reader, serializer, remoteEndPoint);
			gen.Emit(OpCodes.Ldarg_0);
			gen.Emit(OpCodes.Ldarg_1);
			gen.Emit(OpCodes.Ldarg_2);
			gen.Emit(OpCodes.Call, methods.ReadValueNotNullMethod);

			if (requiresBoxing)
				gen.Emit(OpCodes.Box, _context.Type);

			gen.Emit(OpCodes.Ret);
		}
	}
}