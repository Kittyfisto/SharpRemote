using System;
using System.Reflection;
using System.Reflection.Emit;

namespace SharpRemote.CodeGeneration.Serialization
{
	/// <summary>
	/// </summary>
	public abstract class AbstractReadObjectMethodCompiler
		: AbstractMethodCompiler
	{
		private readonly CompilationContext _context;
		private readonly MethodBuilder _method;

		/// <summary>
		/// </summary>
		/// <param name="context"></param>
		protected AbstractReadObjectMethodCompiler(CompilationContext context)
		{
			if (context == null)
				throw new ArgumentNullException(nameof(context));
			if (!typeof(ISerializer2).IsAssignableFrom(context.SerializerType))
				throw new ArgumentException();
			
			_context = context;
			_method = context.TypeBuilder.DefineMethod("ReadObject", MethodAttributes.Public | MethodAttributes.Static,
			                                           CallingConventions.Standard, typeof(object), new[]
			                                           {
				                                           context.ReaderType,
				                                           context.SerializerType,
				                                           typeof(IRemotingEndPoint)
			                                           });
		}

		/// <inheritdoc />
		public override MethodBuilder Method => _method;

		/// <inheritdoc />
		public override void Compile(AbstractMethodsCompiler methods,
		                             ISerializationMethodStorage<AbstractMethodsCompiler> methodStorage)
		{
			var requiresBoxing = _context.Type.IsPrimitive || _context.Type.IsValueType;
			var gen = _method.GetILGenerator();
			//var hasValue = gen.DefineLabel();
			//var end = gen.DefineLabel();
			//
			//// If value != null: goto hasValue
			//EmitReadIsNull(gen);
			//gen.Emit(OpCodes.Ldc_I4_0);
			//gen.Emit(OpCodes.Ceq);
			//gen.Emit(OpCodes.Brfalse, hasValue);
			//
			//// return null
			//gen.Emit(OpCodes.Ldnull);
			//gen.Emit(OpCodes.Br_S, end);

			// :hasValue
			// return ReadValueNotNull(reader, serializer, remoteEndPoint);
			//gen.MarkLabel(hasValue);
			gen.Emit(OpCodes.Ldarg_0);
			gen.Emit(OpCodes.Ldarg_1);
			gen.Emit(OpCodes.Ldarg_2);
			gen.Emit(OpCodes.Call, methods.ReadValueMethod);

			if (requiresBoxing)
				gen.Emit(OpCodes.Box, _context.Type);

			//gen.MarkLabel(end);
			gen.Emit(OpCodes.Ret);
		}
	}
}