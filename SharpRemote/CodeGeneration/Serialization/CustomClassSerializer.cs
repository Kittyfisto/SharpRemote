using System;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.Serialization;

namespace SharpRemote.CodeGeneration.Serialization
{
	public partial class Serializer
	{
		/// <summary>
		///     Write an object who's type is not sealed and thus the instance-type could differ from the compile-time type,
		///     requring us to *always* emit type information into the stream.
		/// </summary>
		/// <param name="gen"></param>
		/// <param name="type"></param>
		private void WriteUnsealedObject(ILGenerator gen, Type type)
		{
			EmitWriteTypeInformationOrNull(gen, () => WriteFields(gen, type));
		}

		/// <summary>
		///     Write an object who's type is sealed (and thus the final type is known at compile time).
		/// </summary>
		/// <param name="gen"></param>
		/// <param name="type"></param>
		private void WriteSealedObject(ILGenerator gen, Type type)
		{
			LocalBuilder result = gen.DeclareLocal(typeof (int));

			// if (object == null)
			Label @true = gen.DefineLabel();
			gen.Emit(OpCodes.Ldarg_1);
			gen.Emit(OpCodes.Ldnull);
			gen.Emit(OpCodes.Ceq);
			gen.Emit(OpCodes.Ldc_I4_0);
			gen.Emit(OpCodes.Ceq);
			gen.Emit(OpCodes.Stloc, result);
			gen.Emit(OpCodes.Ldloc, result);
			gen.Emit(OpCodes.Brtrue, @true);

			// { writer.Write(false); }
			gen.Emit(OpCodes.Ldarg_0);
			gen.Emit(OpCodes.Ldc_I4_0);
			gen.Emit(OpCodes.Call, Methods.WriteBool);

			Label @end = gen.DefineLabel();
			gen.Emit(OpCodes.Br, @end);

			// else { writer.Write(true); <Serialize Fields> }
			gen.MarkLabel(@true);
			gen.Emit(OpCodes.Ldarg_0);
			gen.Emit(OpCodes.Ldc_I4_1);
			gen.Emit(OpCodes.Call, Methods.WriteBool);
			WriteFields(gen, type);

			gen.MarkLabel(@end);
			gen.Emit(OpCodes.Ret);
		}

		/// <summary>
		/// Writes all fields of the given type into the 
		/// </summary>
		/// <param name="gen"></param>
		/// <param name="type"></param>
		private void WriteFields(ILGenerator gen, Type type)
		{
			var allFields =
				type.GetFields(BindingFlags.Public | BindingFlags.Instance)
					.Where(x => x.GetCustomAttribute<DataMemberAttribute>() != null)
					.ToArray();

			foreach (var field in allFields)
			{
				if (!gen.EmitWritePod(() => gen.Emit(OpCodes.Ldarg_0),
											  () =>
											  {
												  if (type.IsValueType)
												  {
													  gen.Emit(OpCodes.Ldarga, 1);
												  }
												  else
												  {
													  gen.Emit(OpCodes.Ldarg_1);
												  }
												  gen.Emit(OpCodes.Ldfld, field);
											  }, field.FieldType))
				{
					var writeObject = GetWriteValueMethodInfo(field.FieldType);

					gen.Emit(OpCodes.Ldarg_0);
					gen.Emit(OpCodes.Ldarg_2);

					gen.Emit(OpCodes.Call, writeObject);
				}
			}
		}
	}
}