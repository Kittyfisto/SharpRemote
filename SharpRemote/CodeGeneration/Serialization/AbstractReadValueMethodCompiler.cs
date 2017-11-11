using System;
using System.Reflection;
using System.Reflection.Emit;

namespace SharpRemote.CodeGeneration.Serialization
{
	/// <summary>
	/// </summary>
	public abstract class AbstractReadValueMethodCompiler
		: AbstractMethodCompiler
	{
		private readonly CompilationContext _context;
		private readonly MethodBuilder _method;

		/// <summary>
		/// 
		/// </summary>
		protected AbstractReadValueMethodCompiler(CompilationContext context)
		{
			_context = context;
			_method = context.TypeBuilder.DefineMethod("ReadValue", MethodAttributes.Public | MethodAttributes.Static,
			                                           CallingConventions.Standard, context.Type, new[]
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
			var gen = _method.GetILGenerator();
			var tmp = gen.DeclareLocal(_context.Type);
			if (_context.Type.IsValueType)
			{
				gen.Emit(OpCodes.Ldloca, tmp);
				gen.Emit(OpCodes.Initobj, _context.Type);
			}
			else
			{
				var ctor = _context.Type.GetConstructor(new Type[0]);
				if (ctor == null)
					throw new ArgumentException(string.Format("Type '{0}' is missing a parameterless constructor", _context.Type));

				gen.Emit(OpCodes.Newobj, ctor);
				gen.Emit(OpCodes.Stloc, tmp);
			}
			gen.Emit(OpCodes.Ldloc);
			gen.Emit(OpCodes.Ret);

			//gen.Emit(OpCodes.Ldarg_0);
			//gen.Emit(OpCodes.Call, Methods.ReadBool);
			//var end = gen.DefineLabel();
			//var @null = gen.DefineLabel();
			//gen.Emit(OpCodes.Brfalse, @null);
			//
			//// ReadValueNotNull(reader, serializer, remotingEndPoint);
			//gen.Emit(OpCodes.Ldarg_0);
			//gen.Emit(OpCodes.Ldarg_1);
			//gen.Emit(OpCodes.Ldarg_2);
			//gen.Emit(OpCodes.Call, methods.ReadValueNotNullMethod);
			//gen.Emit(OpCodes.Br_S, end);
			//
			//gen.MarkLabel(@null);
			//gen.Emit(OpCodes.Ldnull);
			//
			//gen.MarkLabel(end);
			//gen.Emit(OpCodes.Ret);
		}
	}
}