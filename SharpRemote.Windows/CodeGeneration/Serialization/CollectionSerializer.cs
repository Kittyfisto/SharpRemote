using System;
using System.Reflection.Emit;
using SharpRemote.CodeGeneration;

// ReSharper disable CheckNamespace
namespace SharpRemote
// ReSharper restore CheckNamespace
{
	public partial class Serializer
	{
		private void EmitWriteCollection(ILGenerator gen,
			TypeInformation typeInformation,
			Action loadWriter,
			Action loadValue,
			Action loadSerializer,
			Action loadRemotingEndPoint)
		{
			var type = typeInformation.CollectionType;
			var getCount = type.GetProperty("Count").GetMethod;

			// writer.Write(value.Count)
			gen.Emit(OpCodes.Ldarg_0);
			gen.Emit(OpCodes.Ldarg_1);
			gen.Emit(OpCodes.Callvirt, getCount);
			gen.Emit(OpCodes.Call, Methods.WriteInt32);

			EmitWriteEnumeration(gen,
				typeInformation,
				loadWriter,
				loadValue,
				loadSerializer,
				loadRemotingEndPoint);
		}

		private void EmitReadCollection(ILGenerator gen,
			Action loadReader,
			Action loadSerializer,
			Action loadRemotingEndPoint,
			TypeInformation typeInformation)
		{
			var ctor = typeInformation.Type.GetConstructor(new Type[0]);
			var collectionType = typeInformation.CollectionType;
			var add = collectionType.GetMethod("Add", new[] {typeInformation.ElementType});
			var elementType = typeInformation.ElementType;
			var result = gen.DeclareLocal(typeInformation.Type);
			var count = gen.DeclareLocal(typeof (int));
			var i = gen.DeclareLocal(typeof (int));
			var start = gen.DefineLabel();
			var end = gen.DefineLabel();

			gen.Emit(OpCodes.Newobj, ctor);
			gen.Emit(OpCodes.Stloc, result);

			gen.Emit(OpCodes.Ldarg_0);
			gen.Emit(OpCodes.Call, Methods.ReadInt32);
			gen.Emit(OpCodes.Stloc, count);

			gen.Emit(OpCodes.Ldc_I4_0);
			gen.Emit(OpCodes.Stloc, i);

			// start:
			gen.MarkLabel(start);
			// if i == count goto end
			gen.Emit(OpCodes.Ldloc, i);
			gen.Emit(OpCodes.Ldloc, count);
			gen.Emit(OpCodes.Ceq);
			gen.Emit(OpCodes.Brtrue, end);

			gen.Emit(OpCodes.Ldloc, result);
			EmitReadValue(gen,
				loadReader,
				loadSerializer,
				loadRemotingEndPoint,
				elementType);
			gen.Emit(OpCodes.Callvirt, add);

			// ++i
			gen.Emit(OpCodes.Ldloc, i);
			gen.Emit(OpCodes.Ldc_I4_1);
			gen.Emit(OpCodes.Add);
			gen.Emit(OpCodes.Stloc, i);
			// goto start
			gen.Emit(OpCodes.Br, start);

			// end:
			gen.MarkLabel(end);
			gen.Emit(OpCodes.Ldloc, result);
		}
	}
}