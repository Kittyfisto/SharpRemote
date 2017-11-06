using System;
using System.ComponentModel;
using System.Reflection.Emit;
using SharpRemote.CodeGeneration;

// ReSharper disable CheckNamespace
namespace SharpRemote
// ReSharper restore CheckNamespace
{
	public partial class BinarySerializer
	{
		private enum ArrayOrder
		{
			Forward,
			Reverse,
		}

		private void EmitWriteArray(ILGenerator gen,
			TypeInformation typeInformation,
			Action loadWriter,
			Action loadValue,
			Action loadSerializer,
			Action loadRemotingEndPoint,
			ArrayOrder order = ArrayOrder.Forward)
		{
			var elementType = typeInformation.ElementType;
			var length = gen.DeclareLocal(typeof (int));
			var i = gen.DeclareLocal(typeof (int));
			var loop = gen.DefineLabel();
			var end = gen.DefineLabel();

			// writer.Write(value.Length)
			loadValue();
			gen.Emit(OpCodes.Ldlen);
			gen.Emit(OpCodes.Stloc, length);

			loadWriter();
			gen.Emit(OpCodes.Ldloc, length);
			gen.Emit(OpCodes.Call, Methods.WriteInt32);

			// i = 0
			// OR
			// i = length-1
			switch (order)
			{
				case ArrayOrder.Forward:
					gen.Emit(OpCodes.Ldc_I4_0);
					gen.Emit(OpCodes.Stloc, i);
					break;

				case ArrayOrder.Reverse:
					gen.Emit(OpCodes.Ldloc, length);
					gen.Emit(OpCodes.Ldc_I4_1);
					gen.Emit(OpCodes.Sub);
					gen.Emit(OpCodes.Stloc, i);
					break;

				default:
					throw new InvalidEnumArgumentException(nameof(order), (int)order, typeof(ArrayOrder));
			}

			gen.MarkLabel(loop);

			// while i != length
			// OR
			// while i != -1

			switch (order)
			{
				case ArrayOrder.Forward:
					gen.Emit(OpCodes.Ldloc, length);
					gen.Emit(OpCodes.Ldloc, i);
					gen.Emit(OpCodes.Ceq);
					gen.Emit(OpCodes.Brtrue, end);
					break;

				default:
					gen.Emit(OpCodes.Ldc_I4_0);
					gen.Emit(OpCodes.Ldloc, i);
					gen.Emit(OpCodes.Cgt);
					gen.Emit(OpCodes.Brtrue, end);
					break;
			}

			Action loadCurrentValue = () =>
			{
				loadValue();
				gen.Emit(OpCodes.Ldloc, i);
				gen.Emit(OpCodes.Ldelem, elementType);
			};
			Action loadCurrentValueAddress = () =>
			{
				loadValue();
				gen.Emit(OpCodes.Ldloc, i);
				gen.Emit(OpCodes.Ldelema, elementType);
			};

			EmitWriteValue(gen,
				loadWriter,
				loadCurrentValue,
				loadCurrentValueAddress,
				loadSerializer,
				loadRemotingEndPoint,
				elementType);

			switch (order)
			{
				case ArrayOrder.Forward:
					gen.Emit(OpCodes.Ldloc, i);
					gen.Emit(OpCodes.Ldc_I4_1);
					gen.Emit(OpCodes.Add);
					gen.Emit(OpCodes.Stloc, i);
					break;

				default:
					gen.Emit(OpCodes.Ldloc, i);
					gen.Emit(OpCodes.Ldc_I4_1);
					gen.Emit(OpCodes.Sub);
					gen.Emit(OpCodes.Stloc, i);
					break;
			}

			// goto loop
			gen.Emit(OpCodes.Br, loop);

			gen.MarkLabel(end);
		}

		private void EmitReadArray(ILGenerator gen,
			Action loadReader,
			Action loadSerializer,
			Action loadRemotingEndPoint,
			TypeInformation typeInformation)
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
				loadReader,
				loadSerializer,
				loadRemotingEndPoint,
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
