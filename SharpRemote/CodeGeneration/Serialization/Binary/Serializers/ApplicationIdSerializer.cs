﻿using System;
using System.Reflection;
using System.Reflection.Emit;

namespace SharpRemote.CodeGeneration.Serialization.Binary.Serializers
{
	internal sealed class ApplicationIdSerializer
		: AbstractTypeSerializer
	{
		private readonly ConstructorInfo _ctor;
		private readonly MethodInfo _getKey;
		private readonly MethodInfo _getName;
		private readonly MethodInfo _getVersion;
		private readonly MethodInfo _getProcessorArchitecture;
		private readonly MethodInfo _getCulture;

		public ApplicationIdSerializer()
		{
			_ctor = typeof (ApplicationId).GetConstructor(new[]
				{
					typeof(byte[]),
					typeof(string),
					typeof(Version),
					typeof(string),
					typeof(string)
				});
			_getKey = typeof (ApplicationId).GetProperty("PublicKeyToken").GetMethod;
			_getName = typeof(ApplicationId).GetProperty("Name").GetMethod;
			_getVersion = typeof(ApplicationId).GetProperty("Version").GetMethod;
			_getProcessorArchitecture = typeof(ApplicationId).GetProperty("ProcessorArchitecture").GetMethod;
			_getCulture = typeof(ApplicationId).GetProperty("Culture").GetMethod;
		}

		public override bool Supports(Type type)
		{
			return type == typeof (ApplicationId);
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
					                                  gen.Emit(OpCodes.Call, _getKey);
				                                  },
			                                  null,
			                                  loadSerializer,
											  loadRemotingEndPoint,
			                                  typeof (byte[]));

			serializerCompiler.EmitWriteValue(gen,
			                                  loadWriter,
			                                  () =>
				                                  {
					                                  loadValue();
					                                  gen.Emit(OpCodes.Call, _getName);
				                                  },
			                                  null,
			                                  loadSerializer,
											  loadRemotingEndPoint,
			                                  typeof (string));

			serializerCompiler.EmitWriteValue(gen,
			                                  loadWriter,
			                                  () =>
				                                  {
					                                  loadValue();
					                                  gen.Emit(OpCodes.Call, _getVersion);
				                                  },
			                                  null,
			                                  loadSerializer,
											  loadRemotingEndPoint,
			                                  typeof (Version));

			serializerCompiler.EmitWriteValue(gen,
			                                  loadWriter,
			                                  () =>
				                                  {
					                                  loadValue();
					                                  gen.Emit(OpCodes.Call, _getProcessorArchitecture);
				                                  },
			                                  null,
			                                  loadSerializer,
											  loadRemotingEndPoint,
			                                  typeof (string));

			serializerCompiler.EmitWriteValue(gen,
			                                  loadWriter,
			                                  () =>
				                                  {
					                                  loadValue();
					                                  gen.Emit(OpCodes.Call, _getCulture);
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
			                                 loadRemotingEndPoint,
			                                 typeof (byte[]));

			serializerCompiler.EmitReadValue(gen,
			                                 loadReader,
			                                 loadSerializer,
			                                 loadRemotingEndPoint,
			                                 typeof (string));

			serializerCompiler.EmitReadValue(gen,
			                                 loadReader,
			                                 loadSerializer,
			                                 loadRemotingEndPoint,
			                                 typeof (Version));

			serializerCompiler.EmitReadValue(gen,
			                                 loadReader,
			                                 loadSerializer,
			                                 loadRemotingEndPoint,
			                                 typeof (string));

			serializerCompiler.EmitReadValue(gen,
			                                 loadReader,
			                                 loadSerializer,
			                                 loadRemotingEndPoint,
			                                 typeof (string));

			gen.Emit(OpCodes.Newobj, _ctor);
		}
	}
}