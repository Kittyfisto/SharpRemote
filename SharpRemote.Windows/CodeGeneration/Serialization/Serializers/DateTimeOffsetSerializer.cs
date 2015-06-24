using System;
using System.Reflection;
using System.Reflection.Emit;

namespace SharpRemote.CodeGeneration.Serialization.Serializers
{
	internal sealed class DateTimeOffsetSerializer
		: AbstractTypeSerializer
	{
		private readonly ConstructorInfo _ctor;
		private readonly MethodInfo _getDateTime;
		private readonly MethodInfo _getOffset;

		public DateTimeOffsetSerializer()
		{
			_ctor = typeof (DateTimeOffset).GetConstructor(new[] {typeof (DateTime), typeof (TimeSpan)});
			_getDateTime = typeof (DateTimeOffset).GetProperty("DateTime").GetMethod;
			_getOffset = typeof (DateTimeOffset).GetProperty("Offset").GetMethod;
		}

		public override bool Supports(Type type)
		{
			return type == typeof (DateTimeOffset);
		}

		public override void EmitWriteValue(ILGenerator gen, Serializer serializerCompiler, Action loadWriter,
		                                    Action loadValue,
		                                    Action loadValueAddress, Action loadSerializer, Type type,
		                                    bool valueCanBeNull = true)
		{
			LocalBuilder dateTime = null;

			serializerCompiler.EmitWriteValue(gen,
			                                  loadWriter,
			                                  () =>
				                                  {
					                                  loadValueAddress();
					                                  gen.Emit(OpCodes.Call, _getDateTime);
				                                  },
			                                  () =>
				                                  {
					                                  if (dateTime == null)
													  {
														  loadValueAddress();
						                                  dateTime = gen.DeclareLocal(typeof (DateTime));
						                                  gen.Emit(OpCodes.Call, _getDateTime);
						                                  gen.Emit(OpCodes.Stloc, dateTime);
					                                  }

					                                  gen.Emit(OpCodes.Ldloca, dateTime);
				                                  },
			                                  loadSerializer,
			                                  typeof (DateTime));

			LocalBuilder offset = null;

			serializerCompiler.EmitWriteValue(gen,
											  loadWriter,
											  () =>
											  {
												  loadValueAddress();
												  gen.Emit(OpCodes.Call, _getOffset);
											  },
											  () =>
											  {
												  if (offset == null)
												  {
													  loadValueAddress();
													  offset = gen.DeclareLocal(typeof(TimeSpan));
													  gen.Emit(OpCodes.Call, _getOffset);
													  gen.Emit(OpCodes.Stloc, offset);
												  }

												  gen.Emit(OpCodes.Ldloca, offset);
											  },
											  loadSerializer,
											  typeof(TimeSpan));
		}

		public override void EmitReadValue(ILGenerator gen, Serializer serializerCompiler, Action loadReader,
		                                   Action loadSerializer, Type type,
		                                   bool valueCanBeNull = true)
		{
			serializerCompiler.EmitReadValue(gen,
				loadReader,
				loadSerializer,
				typeof(DateTime));
			serializerCompiler.EmitReadValue(gen,
				loadReader,
				loadSerializer,
				typeof(TimeSpan));
			gen.Emit(OpCodes.Newobj, _ctor);
		}
	}
}