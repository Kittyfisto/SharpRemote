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
		/// <param name="loadWriter"></param>
		/// <param name="loadValue"></param>
		/// <param name="loadSerializer"></param>
		private void EmitWriteEnumeration(ILGenerator gen,
			TypeInformation typeInformation,
			Action loadWriter,
			Action loadValue,
			Action loadSerializer)
		{
			Type elementType = typeInformation.ElementType;
			Type enumerableType = typeof (IEnumerable<>).MakeGenericType(elementType);
			Type enumeratorType = typeof (IEnumerator<>).MakeGenericType(elementType);
			MethodInfo getEnumerator = enumerableType.GetMethod("GetEnumerator");
			MethodInfo moveNext = typeof (IEnumerator).GetMethod("MoveNext");
			MethodInfo getCurrent = enumeratorType.GetProperty("Current").GetMethod;

			// var enumerator = value.GetEnumerator()
			LocalBuilder enumerator = gen.DeclareLocal(enumeratorType);
			loadValue();
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

			LocalBuilder current = null;
			Action loadCurrentValue = () =>
			{
				gen.Emit(OpCodes.Ldloc, enumerator);
				gen.Emit(OpCodes.Callvirt, getCurrent);
			};
			Action loadCurrentValueAddress = () =>
			{
				if (current == null)
				{
					current = gen.DeclareLocal(elementType);
					loadCurrentValue();
					gen.Emit(OpCodes.Stloc, current);
				}
				gen.Emit(OpCodes.Ldloca, current);
			};

			EmitWriteValue(gen,
				loadWriter,
				loadCurrentValue,
				loadCurrentValueAddress,
				loadSerializer,
				elementType);

			// goto loop
			gen.Emit(OpCodes.Br, loop);

			// end:
			gen.MarkLabel(end);
		}
	}
}