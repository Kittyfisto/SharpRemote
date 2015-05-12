using System.Reflection.Emit;

namespace SharpRemote.CodeGeneration.Serialization
{
	public partial class Serializer
	{
		private void EmitWriteArray(ILGenerator gen, TypeInformation typeInformation)
		{
			// writer.Write(value.Length)
			gen.Emit(OpCodes.Ldarg_0);
			gen.Emit(OpCodes.Ldarg_1);
			gen.Emit(OpCodes.Ldlen);
			gen.Emit(OpCodes.Call, Methods.WriteInt);

			EmitWriteEnumeration(gen, typeInformation);
		}

		private void EmitReadArray(ILGenerator gen, TypeInformation typeInformation)
		{
			var elementType = typeInformation.ElementType;

			var value = gen.DeclareLocal(typeInformation.Type);
			var count = gen.DeclareLocal(typeof(int));
			var i = gen.DeclareLocal(typeof(int));

			// count = reader.ReadInt32()
			gen.Emit(OpCodes.Ldarg_0);
			gen.Emit(OpCodes.Call, Methods.ReadInt32);
			gen.Emit(OpCodes.Stloc, count);

			// value = new XXX[count]
			gen.Emit(OpCodes.Ldloc, count);
			gen.Emit(OpCodes.Newarr, elementType);
			gen.Emit(OpCodes.Stloc, value);

			var loop = gen.DefineLabel();
			var end = gen.DefineLabel();

			// int i = 0
			gen.Emit(OpCodes.Ldc_I4_0);
			gen.Emit(OpCodes.Stloc, i);

			// loop:
			gen.MarkLabel(loop);
			// if (i < count) goto end
			gen.Emit(OpCodes.Ldloc, i);
			gen.Emit(OpCodes.Ldloc, count);
			gen.Emit(OpCodes.Clt);
			gen.Emit(OpCodes.Brfalse, end);

			// value[i] = <ReadValue>
			gen.Emit(OpCodes.Ldloc, value);
			gen.Emit(OpCodes.Ldloc, i);

			EmitReadValue(gen,
				() => gen.Emit(OpCodes.Ldarg_0),
				() => gen.Emit(OpCodes.Ldarg_1),
				elementType);

			gen.Emit(OpCodes.Stelem, elementType);

			// ++i
			gen.Emit(OpCodes.Ldloc, i);
			gen.Emit(OpCodes.Ldc_I4_1);
			gen.Emit(OpCodes.Add);
			gen.Emit(OpCodes.Stloc, i);
			// goto loop
			gen.Emit(OpCodes.Br, loop);

			// end:
			gen.MarkLabel(end);
			gen.Emit(OpCodes.Ldloc, value);
		}
	}
}
