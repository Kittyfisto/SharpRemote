using System;
using System.Collections.Generic;
using System.Reflection.Emit;

// ReSharper disable CheckNamespace
namespace SharpRemote
// ReSharper restore CheckNamespace
{
	public partial class Serializer
	{
		private void EmitReadQueue(ILGenerator gen,
			Action loadReader,
			Action loadSerializer,
			Action loadRemotingEndPoint,
			TypeInformation typeInformation)
		{
			var elementType = typeInformation.ElementType;
			var ctor = typeInformation.Type.GetConstructor(new[] { typeof(IEnumerable<>).MakeGenericType(elementType) });

			EmitReadArray(gen,
			              loadReader,
			              loadSerializer,
			              loadRemotingEndPoint,
			              typeInformation);

			gen.Emit(OpCodes.Newobj, ctor);
		}

		private void EmitWriteQueue(ILGenerator gen,
			TypeInformation typeInformation,
			Action loadWriter,
			Action loadValue,
			Action loadSerializer,
			Action loadRemotingEndPoint)
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
				loadRemotingEndPoint
				);
		}
	}
}