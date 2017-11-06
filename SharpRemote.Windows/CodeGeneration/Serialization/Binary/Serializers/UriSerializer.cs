using System;
using System.Reflection;
using System.Reflection.Emit;

namespace SharpRemote.CodeGeneration.Serialization.Binary.Serializers
{
	internal sealed class UriSerializer
		: AbstractTypeSerializer
	{
		private readonly ConstructorInfo _ctor;
		private readonly MethodInfo _getOriginalString;

		public UriSerializer()
		{
			_ctor = typeof (Uri).GetConstructor(new[] {typeof(string), typeof(UriKind)});
			_getOriginalString = typeof (Uri).GetProperty("OriginalString").GetMethod;
		}

		public override bool Supports(Type type)
		{
			return type == typeof (Uri);
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
			serializerCompiler.EmitWriteValue(gen,
			                                  loadWriter,
			                                  () =>
				                                  {
					                                  loadValue();
					                                  gen.Emit(OpCodes.Call, _getOriginalString);
				                                  },
			                                  null,
			                                  loadSerializer,
			                                  loadRemotingEndPoint,
			                                  typeof (string));
		}

		public override void EmitReadValue(ILGenerator gen,
		                                   ISerializerCompiler serializerCompiler,
		                                   Action loadReader,
		                                   Action loadSerializer,
		                                   Action loadRemotingEndPoint,
		                                   Type type,
		                                   bool valueCanBeNull = true)
		{
			serializerCompiler.EmitReadValue(gen,
			                                 loadReader,
			                                 loadSerializer,
			                                 loadSerializer,
			                                 typeof (string));
			gen.Emit(OpCodes.Ldc_I4, (int) UriKind.RelativeOrAbsolute);
			gen.Emit(OpCodes.Newobj, _ctor);
		}
	}
}