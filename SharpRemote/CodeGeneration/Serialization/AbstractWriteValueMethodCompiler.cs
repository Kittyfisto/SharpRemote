using System;
using System.Reflection;
using System.Reflection.Emit;

namespace SharpRemote.CodeGeneration.Serialization
{
	/// <summary>
	///     Responsible for emitting a method with the following signature:
	///     static void (WriterType, Type, <see cref="ISerializer2" />, <see cref="IRemotingEndPoint" />);
	///     The method writes a tag to the writer which either states that the value is null or that it isn't
	///     and then calls the method compiled by <see cref="AbstractWriteValueNotNullMethodCompiler" />.
	/// </summary>
	/// <remarks>
	///     This method doesn't need to write type information to the writer as it will only ever be called on
	///     value / sealed types.
	/// </remarks>
	public abstract class AbstractWriteValueMethodCompiler
		: AbstractMethodCompiler
	{
		private readonly Type _type;

		/// <summary>
		/// </summary>
		/// <param name="context"></param>
		protected AbstractWriteValueMethodCompiler(CompilationContext context)
		{
			_type = context.Type;
			Method = context.TypeBuilder.DefineMethod("WriteValue", MethodAttributes.Public | MethodAttributes.Static,
			                                          CallingConventions.Standard, typeof(void), new[]
			                                          {
				                                          context.WriterType,
				                                          context.Type,
				                                          typeof(ISerializer2),
				                                          typeof(IRemotingEndPoint)
			                                          });
		}

		/// <inheritdoc />
		public override MethodBuilder Method { get; }

		/// <inheritdoc />
		public override void Compile(AbstractMethodsCompiler methods1, ISerializationMethodStorage<AbstractMethodsCompiler> methodStorage)
		{
			//var methods = methodStorage.GetOrAdd(_type);
			//var writeValueNotNull = methods.WriteValueNotNullMethod;
			//
			var gen = Method.GetILGenerator();
			//var result = gen.DeclareLocal(typeof(int));
			//
			//// if (object == null)
			//var @true = gen.DefineLabel();
			//gen.Emit(OpCodes.Ldarg_1);
			//gen.Emit(OpCodes.Ldnull);
			//gen.Emit(OpCodes.Ceq);
			//gen.Emit(OpCodes.Ldc_I4_0);
			//gen.Emit(OpCodes.Ceq);
			//gen.Emit(OpCodes.Stloc, result);
			//gen.Emit(OpCodes.Ldloc, result);
			//gen.Emit(OpCodes.Brtrue, @true);
			//
			//// WriteNoValue()
			//// goto end
			//EmitWriteNoValue(gen);
			//var end = gen.DefineLabel();
			//gen.Emit(OpCodes.Br, end);
			//
			//// WriteHasValue()
			//gen.MarkLabel(@true);
			//EmitWriteHasValue(gen);
			//
			//// WriteValueNotNull(writer, value, serializer, remotingEndPoint)
			//gen.Emit(OpCodes.Ldarg_0);
			//gen.Emit(OpCodes.Ldarg_1);
			//gen.Emit(OpCodes.Ldarg_2);
			//gen.Emit(OpCodes.Ldarg_3);
			//gen.Emit(OpCodes.Call, writeValueNotNull);
			//
			//gen.MarkLabel(end);
			//gen.Emit(OpCodes.Ret);
			gen.Emit(OpCodes.Nop);
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="generator"></param>
		protected abstract void EmitWriteHasValue(ILGenerator generator);

		/// <summary>
		/// 
		/// </summary>
		/// <param name="generator"></param>
		protected abstract void EmitWriteNoValue(ILGenerator generator);
	}
}