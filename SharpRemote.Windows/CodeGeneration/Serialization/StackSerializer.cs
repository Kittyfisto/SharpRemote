using System;
using System.Collections.Generic;
using System.Reflection.Emit;

namespace SharpRemote.CodeGeneration.Serialization
{
	public partial class Serializer
	{
		private void EmitReadStack(ILGenerator gen, TypeInformation typeInformation)
		{
			var elementType = typeInformation.ElementType;
			var ctor = typeInformation.Type.GetConstructor(new[] {typeof (IEnumerable<>).MakeGenericType(elementType)});

			EmitReadArray(gen, typeInformation);
			gen.Emit(OpCodes.Newobj, ctor);
		}

		private void EmitWriteStack(ILGenerator gen, TypeInformation typeInformation, Action loadWriter, Action loadValue, Action loadSerializer)
		{
			var type = typeInformation.Type;
			var toArray = type.GetMethod("ToArray");

			var tmp = gen.DeclareLocal(type.MakeArrayType());
			loadValue();
			gen.Emit(OpCodes.Call, toArray);
			gen.Emit(OpCodes.Stloc, tmp);

			EmitWriteArray(gen,
				typeInformation,
				loadWriter,
				() => gen.Emit(OpCodes.Ldloc, tmp),
				loadSerializer,
				ArrayOrder.Reverse
				);
		}
	}
}