using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Reflection.Emit;

namespace SharpRemote.CodeGeneration.Serialization
{
	public partial class Serializer
	{
		/// <summary>
		///     Emits the code necessary to write an enumeration into a <see cref="BinaryWriter" />.
		/// </summary>
		/// <param name="gen"></param>
		/// <param name="typeInformation"></param>
		private void EmitWriteEnumeration(ILGenerator gen, TypeInformation typeInformation)
		{
			Type elementType = typeInformation.ElementType;
			Type enumerableType = typeof (IEnumerable<>).MakeGenericType(elementType);
			Type enumeratorType = typeof (IEnumerator<>).MakeGenericType(elementType);
			MethodInfo getEnumerator = enumerableType.GetMethod("GetEnumerator");
			MethodInfo moveNext = typeof (IEnumerator).GetMethod("MoveNext");
			MethodInfo getCurrent = enumeratorType.GetProperty("Current").GetMethod;

			// var enumerator = value.GetEnumerator()
			LocalBuilder enumerator = gen.DeclareLocal(enumeratorType);
			gen.Emit(OpCodes.Ldarg_1);
			gen.Emit(OpCodes.Castclass, enumerableType);
			gen.Emit(OpCodes.Callvirt, getEnumerator);
			gen.Emit(OpCodes.Stloc, enumerator);

			Label loop = gen.DefineLabel();
			Label end = gen.DefineLabel();

			// loop:
			gen.MarkLabel(loop);
			// if (!enumerator.MoveNext()) goto end
			gen.Emit(OpCodes.Ldloc, enumerator);
			gen.Emit(OpCodes.Callvirt, moveNext);
			gen.Emit(OpCodes.Brfalse, end);

			EmitWriteValue(gen,
				() => gen.Emit(OpCodes.Ldarg_0),
				() =>
				{
					gen.Emit(OpCodes.Ldloc, enumerator);
					gen.Emit(OpCodes.Callvirt, getCurrent);
				},
				() => gen.Emit(OpCodes.Ldarg_2),
				elementType);

			// goto loop
			gen.Emit(OpCodes.Br, loop);

			// end:
			gen.MarkLabel(end);
		}
	}
}