using System.Reflection;
using System.Reflection.Emit;

namespace SharpRemote.CodeGeneration.Serialization
{
	/// <summary>
	/// </summary>
	public abstract class AbstractReadValueMethodCompiler
		: IMethodCompiler
	{
		private readonly MethodBuilder _method;

		/// <summary>
		/// 
		/// </summary>
		protected AbstractReadValueMethodCompiler(CompilationContext context)
		{
			_method = context.TypeBuilder.DefineMethod("ReadValue", MethodAttributes.Public | MethodAttributes.Static,
			                                           CallingConventions.Standard, context.Type, new[]
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
			var gen = _method.GetILGenerator();

			gen.Emit(OpCodes.Ldarg_0);
			gen.Emit(OpCodes.Call, Methods.ReadBool);
			var end = gen.DefineLabel();
			var @null = gen.DefineLabel();
			gen.Emit(OpCodes.Brfalse, @null);

			// ReadValueNotNull(reader, serializer, remotingEndPoint);
			gen.Emit(OpCodes.Ldarg_0);
			gen.Emit(OpCodes.Ldarg_1);
			gen.Emit(OpCodes.Ldarg_2);
			gen.Emit(OpCodes.Call, methods.ReadValueNotNullMethod);
			gen.Emit(OpCodes.Br_S, end);

			gen.MarkLabel(@null);
			gen.Emit(OpCodes.Ldnull);

			gen.MarkLabel(end);
			gen.Emit(OpCodes.Ret);
		}
	}
}