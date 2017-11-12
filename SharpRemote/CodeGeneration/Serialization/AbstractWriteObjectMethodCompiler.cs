using System;
using System.Reflection;
using System.Reflection.Emit;

namespace SharpRemote.CodeGeneration.Serialization
{
	/// <summary>
	///     Responsible for emitting a method with the following signature:
	///     static void (WriterType, <see cref="object" />, <see cref="ISerializer2" />, <see cref="IRemotingEndPoint" />);
	///     The method casts/unboxes the given value and forwards it to the method compiled by
	///     <see cref="AbstractWriteValueMethodCompiler" />.
	/// </summary>
	public abstract class AbstractWriteObjectMethodCompiler
		: AbstractMethodCompiler
	{
		/// <summary>
		/// </summary>
		protected AbstractWriteObjectMethodCompiler(CompilationContext context)
		{
			if (context == null)
				throw new ArgumentNullException(nameof(context));
			if (!typeof(ISerializer2).IsAssignableFrom(context.SerializerType))
				throw new ArgumentException();

			Type = context.Type;
			Method = context.TypeBuilder.DefineMethod("WriteObject", MethodAttributes.Public | MethodAttributes.Static,
			                                          CallingConventions.Standard, typeof(void), new[]
			                                          {
				                                          context.WriterType,
				                                          typeof(object),
				                                          context.SerializerType,
				                                          typeof(IRemotingEndPoint)
			                                          });
		}

		/// <summary>
		///     The type for which a WriteObject method is being compiled.
		/// </summary>
		protected Type Type { get; }

		/// <inheritdoc />
		public override MethodBuilder Method { get; }

		/// <inheritdoc />
		public override void Compile(AbstractMethodsCompiler methods1,
		                             ISerializationMethodStorage<AbstractMethodsCompiler> methodStorage)
		{
			var methods = methodStorage.GetOrAdd(Type);
			var writeValueNotNull = methods.WriteValueMethod;

			var gen = Method.GetILGenerator();
			//gen.EmitWriteLine("writing type info");

			//var result = gen.DeclareLocal(typeof(bool));
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
			//// Writer.Write(null)
			//// goto end
			//EmitWriteNull(gen);
			//var end = gen.DefineLabel();
			//gen.Emit(OpCodes.Br, end);
			//
			//// Writer.Write(type)
			//gen.MarkLabel(@true);
			//EmitWriteTypeInformation(gen);

			// WriteValueNotNull(writer, value, serializer, remotingEndPoint);
			gen.Emit(OpCodes.Ldarg_0);
			gen.Emit(OpCodes.Ldarg_1);

			if (Type.IsPrimitive || Type.IsValueType)
				gen.Emit(OpCodes.Unbox_Any, Type);
			else
				gen.Emit(OpCodes.Castclass, Type);

			gen.Emit(OpCodes.Ldarg_2);
			gen.Emit(OpCodes.Ldarg_3);
			gen.Emit(OpCodes.Call, writeValueNotNull);

			//gen.MarkLabel(end);
			gen.Emit(OpCodes.Ret);
		}
	}
}