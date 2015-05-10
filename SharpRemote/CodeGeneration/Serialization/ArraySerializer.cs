using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection.Emit;

namespace SharpRemote.CodeGeneration.Serialization
{
	public partial class Serializer
	{
		private void WriteArray(ILGenerator gen, TypeInformation typeInformation)
		{
			var type = typeInformation.Type;
			var elementType = typeInformation.ElementType;
			var enumerableType = typeof(IEnumerable<>).MakeGenericType(elementType);
			var enumeratorType = typeof(IEnumerator<>).MakeGenericType(elementType);
			var getLength = type.GetProperty("Length").GetMethod;
			var getEnumerator = enumerableType.GetMethod("GetEnumerator");
			var moveNext = typeof(IEnumerator).GetMethod("MoveNext");
			var getCurrent = enumeratorType.GetProperty("Current").GetMethod;

			// writer.Write(value.Length) OR writer.Write(value.Count)
			gen.Emit(OpCodes.Ldarg_0);
			gen.Emit(OpCodes.Ldarg_1);
			gen.Emit(OpCodes.Call, getLength);
			gen.Emit(OpCodes.Call, Methods.WriteInt);

			// var enumerator = value.GetEnumerator()
			var enumerator = gen.DeclareLocal(enumeratorType);
			gen.Emit(OpCodes.Ldarg_1);
			gen.Emit(OpCodes.Castclass, enumerableType);
			gen.Emit(OpCodes.Callvirt, getEnumerator);
			gen.Emit(OpCodes.Stloc, enumerator);

			var loop = gen.DefineLabel();
			var end = gen.DefineLabel();

			// loop:
			gen.MarkLabel(loop);
			// if (!enumerator.MoveNext()) goto end
			gen.Emit(OpCodes.Ldloc, enumerator);
			gen.Emit(OpCodes.Callvirt, moveNext);
			gen.Emit(OpCodes.Brfalse, end);

			if (gen.EmitWritePod(
				() => gen.Emit(OpCodes.Ldarg_0),
				() =>
				{
					gen.Emit(OpCodes.Ldloc, enumerator);
					gen.Emit(OpCodes.Callvirt, getCurrent);
				},
				elementType))
			{

			}
			else if (elementType.IsValueType)
			{
				WriteMethod write;
				ReadMethod unused;
				RegisterType(elementType, out write, out unused);
				var writeMethod = write.ValueMethod;

				gen.Emit(OpCodes.Ldarg_0);
				gen.Emit(OpCodes.Ldloc, enumerator);
				gen.Emit(OpCodes.Callvirt, getCurrent);
				gen.Emit(OpCodes.Ldarg_2);
				gen.Emit(OpCodes.Call, writeMethod);
			}
			else
			{
				throw new NotImplementedException();
			}

			// goto loop
			gen.Emit(OpCodes.Br, loop);

			// end:
			gen.MarkLabel(end);
			// return
			gen.Emit(OpCodes.Ret);
		}

		private void ReadArray(ILGenerator gen, TypeInformation typeInformation)
		{
			var elementType = typeInformation.ElementType;

			var value = gen.DeclareLocal(typeInformation.Type);
			var count = gen.DeclareLocal(typeof(int));
			var i = gen.DeclareLocal(typeof(int));

			// count = reader.ReadInt32()
			gen.Emit(OpCodes.Ldarg_0);
			gen.Emit(OpCodes.Call, Methods.ReadInt);
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
			if (gen.EmitReadNativeType(
				() => gen.Emit(OpCodes.Ldarg_0),
				elementType
				))
			{

			}
			else if (elementType.IsValueType)
			{
				WriteMethod unused;
				ReadMethod read;
				RegisterType(elementType, out unused, out read);
				var readMethod = read.MethodInfo;

				gen.Emit(OpCodes.Ldarg_0);
				gen.Emit(OpCodes.Ldarg_1);
				gen.Emit(OpCodes.Call, readMethod);
			}
			else
			{
				throw new NotImplementedException();
			}

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
