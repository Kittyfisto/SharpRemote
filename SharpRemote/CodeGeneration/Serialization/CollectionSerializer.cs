using System.Collections;
using System.Reflection.Emit;

namespace SharpRemote.CodeGeneration.Serialization
{
	public partial class Serializer
	{
		private void EmitWriteCollection(ILGenerator gen, TypeInformation typeInformation)
		{
			var getCount = typeof(ICollection).GetProperty("Count").GetMethod;

			// writer.Write(value.Count)
			gen.Emit(OpCodes.Ldarg_0);
			gen.Emit(OpCodes.Ldarg_1);
			gen.Emit(OpCodes.Callvirt, getCount);
			gen.Emit(OpCodes.Call, Methods.WriteInt);

			EmitWriteEnumeration(gen, typeInformation);
		}
	}
}