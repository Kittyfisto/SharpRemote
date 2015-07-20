using System;
using System.Diagnostics.Contracts;
using System.Reflection.Emit;
using SharpRemote.CodeGeneration;

// ReSharper disable CheckNamespace
namespace SharpRemote
// ReSharper restore CheckNamespace
{
	/// <summary>
	/// Base class for <see cref="ITypeSerializer"/> implementations.
	/// Adds methods to read / write nullable values.
	/// </summary>
	internal abstract class AbstractTypeSerializer
		: ITypeSerializer
	{
		protected static void EmitReadNullableValue(ILGenerator gen,
												  Action loadReader,
												  Action loadValue,
			bool valueCanBeNull)
		{
			if (valueCanBeNull)
			{
				var read = gen.DefineLabel();
				var end = gen.DefineLabel();

				loadReader();
				gen.Emit(OpCodes.Call, Methods.ReadBool);
				gen.Emit(OpCodes.Brtrue, read);
				gen.Emit(OpCodes.Ldnull);
				gen.Emit(OpCodes.Br, end);

				gen.MarkLabel(read);
				loadValue();

				gen.MarkLabel(end);
			}
			else
			{
				loadValue();
			}
		}

		protected static void EmitWriteNullableValue(ILGenerator gen,
		                                             Action loadWriter,
		                                             Action loadValue,
		                                             Action writeValue,
		                                             bool valueCanBeNull)
		{
			if (valueCanBeNull)
			{
				var write = gen.DefineLabel();
				var end = gen.DefineLabel();

				// if (value != null) goto write
				loadValue();
				gen.Emit(OpCodes.Ldnull);
				gen.Emit(OpCodes.Ceq);
				gen.Emit(OpCodes.Brfalse, write);

				// writer.Write(true)
				loadWriter();
				gen.Emit(OpCodes.Ldc_I4_0);
				gen.Emit(OpCodes.Call, Methods.WriteBool);
				// goto end
				gen.Emit(OpCodes.Br, end);

				// write:
				gen.MarkLabel(write);
				// writer.Write(false);
				loadWriter();
				gen.Emit(OpCodes.Ldc_I4_1);
				gen.Emit(OpCodes.Call, Methods.WriteBool);

				// writer.Write(value)
				writeValue();

				// end:
				gen.MarkLabel(end);
			}
			else
			{
				// writer.Write(value)
				writeValue();
			}
		}

		[Pure]
		public abstract bool Supports(Type type);

		public abstract void EmitWriteValue(ILGenerator gen,
			Serializer serializerCompiler,
			Action loadWriter,
			Action loadValue,
			Action loadValueAddress,
			Action loadSerializer,
			Action loadRemotingEndPoint,
			Type type,
			bool valueCanBeNull = true);

		public abstract void EmitReadValue(ILGenerator gen,
			Serializer serializerCompiler,
			Action loadReader,
			Action loadSerializer,
			Action loadRemotingEndPoint,
			Type type,
			bool valueCanBeNull = true);
	}
}