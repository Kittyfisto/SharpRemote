using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using log4net.Core;

namespace SharpRemote.CodeGeneration.Serialization.Binary.Serializers
{
	internal sealed class LevelSerializer
		: AbstractTypeSerializer
	{
		private static readonly MethodInfo LevelGetValue;
		private static readonly IReadOnlyList<Info> HardcodedLevels;

		struct Info
		{
			public readonly FieldInfo Field;
			
			/// <summary>
			/// The actual value which is serialized as a substitute for a
			/// particular well known instance, such as <see cref="log4net.Core.Level.Debug"/>.
			/// </summary>
			public readonly byte SerializedValue;

			private Info(FieldInfo field, byte serializedValue)
			{
				Field = field;
				SerializedValue = serializedValue;
			}

			public static Info Create(string name, byte value)
			{
				var field = typeof(Level).GetField(name, BindingFlags.Static | BindingFlags.Public);
				return new Info(field, value);
			}
		}

		static LevelSerializer()
		{
			LevelGetValue = typeof(Level).GetProperty(nameof(Level.Value)).GetMethod;
			
			// The following table needs to start at 0.
			// NEVER change the hardcoded values, only append new values.
			// Every subsequent entry must increment the value by 1, if not,
			// then the jump table below won't work.
			HardcodedLevels = new[]
			{
				Info.Create(nameof(Level.Debug), 0),
				Info.Create(nameof(Level.Alert), 1),
				Info.Create(nameof(Level.All), 2),
				Info.Create(nameof(Level.Critical), 3),
				Info.Create(nameof(Level.Emergency), 4),
				Info.Create(nameof(Level.Error), 5),
				Info.Create(nameof(Level.Fatal), 6),
				Info.Create(nameof(Level.Fine), 7),
				Info.Create(nameof(Level.Finer), 8),
				Info.Create(nameof(Level.Finest), 9),
				Info.Create(nameof(Level.Info), 10),
				Info.Create(nameof(Level.Log4Net_Debug), 11),
				Info.Create(nameof(Level.Notice), 12),
				Info.Create(nameof(Level.Off), 13),
				Info.Create(nameof(Level.Severe), 14),
				Info.Create(nameof(Level.Trace), 15),
				Info.Create(nameof(Level.Verbose), 16),
				Info.Create(nameof(Level.Warn), 17)
			};
		}

		public override bool Supports(Type type)
		{
			return type == typeof(Level);
		}

		public override void EmitWriteValue(ILGenerator gen,
		                                    ISerializerCompiler serializerCompiler,
		                                    Action loadWriter,
		                                    Action loadValue,
		                                    Action loadValueAddress,
		                                    Action loadSerializer,
		                                    Action loadRemotingEndPoint,
		                                    Type type,
		                                    bool valueCanBeNull = true)
		{
			EmitWriteNullableValue(gen, loadWriter, loadValue, () =>
			                       {
				                       var end = gen.DefineLabel();

									   // For now we only allow serialization of "special"
									   // values, hence we always place 1 in the stream, however
									   // in the future we could allow arbitrary Level values
									   // to be serialized, requiring us to store the
				                       // (Level.Value, Level.Name, Level.DisplayName) tuple.
				                       // For that case, we would have to write 0 here.
									   loadWriter();
				                       gen.Emit(OpCodes.Ldc_I4_1);
				                       gen.Emit(OpCodes.Callvirt, Methods.WriteBool);

				                       for (int i = 0; i < HardcodedLevels.Count; ++i)
				                       {
					                       var next = gen.DefineLabel();

					                       // if (ReferenceEquals(value, <fld>))
										   loadValue();
										   gen.Emit(OpCodes.Ldsfld, HardcodedLevels[i].Field);
										   gen.Emit(OpCodes.Call, Methods.ObjectReferenceEquals);
										   gen.Emit(OpCodes.Brfalse, next);

					                       // writer.WriteByte(<constant>)
					                       loadWriter();
					                       gen.Emit(OpCodes.Ldc_I4, (int)HardcodedLevels[i].SerializedValue);
					                       gen.Emit(OpCodes.Callvirt, Methods.WriteByte);
										   gen.Emit(OpCodes.Br, end);
					                       
					                       gen.MarkLabel(next);
				                       }

									   gen.Emit(OpCodes.Newobj, Methods.NotImplementedCtor);
				                       gen.Emit(OpCodes.Throw);

									   gen.MarkLabel(end);
			                       },
			                       valueCanBeNull);
		}

		public override void EmitReadValue(ILGenerator gen,
		                                   ISerializerCompiler serializerCompiler,
		                                   Action loadReader,
		                                   Action loadSerializer,
		                                   Action loadRemotingEndPoint,
		                                   Type type,
		                                   bool valueCanBeNull = true)
		{
			EmitReadNullableValue(gen, loadReader, () =>
			{
				var readSpecialValue = gen.DefineLabel();
				
				// if (!reader.ReadBool())
				loadReader();
				gen.Emit(OpCodes.Callvirt, Methods.ReadBool);
				gen.Emit(OpCodes.Ldc_I4_1);
				gen.Emit(OpCodes.Ceq);
				gen.Emit(OpCodes.Brtrue_S, readSpecialValue);
				// throw new NotImplementedException
				gen.Emit(OpCodes.Newobj, Methods.NotImplementedCtor);
				gen.Emit(OpCodes.Throw);
				
				// else ...
				gen.MarkLabel(readSpecialValue);
				loadReader();
				gen.Emit(OpCodes.Callvirt, Methods.ReadByte);

				var jumpTable = HardcodedLevels.Select(x => gen.DefineLabel()).ToArray();
				var end = gen.DefineLabel();

				gen.Emit(OpCodes.Switch, jumpTable);
				// Default case => we throw here - new serializer will implement it...
				gen.Emit(OpCodes.Newobj, Methods.NotImplementedCtor);
				gen.Emit(OpCodes.Throw);

				for (int i = 0; i < jumpTable.Length; ++i)
				{
					gen.MarkLabel(jumpTable[i]);
					gen.Emit(OpCodes.Ldsfld, HardcodedLevels[i].Field);
					gen.Emit(OpCodes.Br, end);
				}
				gen.MarkLabel(end);
			}, valueCanBeNull);
		}
	}
}