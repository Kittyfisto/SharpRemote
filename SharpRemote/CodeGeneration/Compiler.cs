using System;
using System.IO;
using System.Reflection;
using System.Reflection.Emit;
using SharpRemote.CodeGeneration.Serialization;

namespace SharpRemote.CodeGeneration
{
	public abstract class Compiler
	{
		protected readonly Serializer SerializerCompiler;
		protected FieldBuilder Serializer;
		protected FieldBuilder EndPoint;
		protected FieldBuilder Channel;
		protected FieldBuilder ObjectId;

		protected Compiler(Serializer serializer)
		{
			SerializerCompiler = serializer;
		}

		protected void ExtractArgumentsAndCallMethod(ILGenerator gen,
			MethodInfo methodInfo,
			Action loadReader,
			Action loadWriter)
		{
			Action loadSerializer = () =>
				{
					gen.Emit(OpCodes.Ldarg_0);
					gen.Emit(OpCodes.Ldfld, Serializer);
				};

			var allParameters = methodInfo.GetParameters();
			foreach (var parameter in allParameters)
			{
				var parameterType = parameter.ParameterType;

				if (parameter.GetCustomAttribute<ByReferenceAttribute>() != null)
				{
					VerifyParameterConstraints(parameter);

					// _endPoint.GetOrCreateProxy(reader.ReadUlong());
					var getOrCreateProxy = Methods.RemotingEndPointGetOrCreateProxy.MakeGenericMethod(parameterType);
					gen.Emit(OpCodes.Ldarg_0);
					gen.Emit(OpCodes.Ldfld, EndPoint);
					loadReader();
					gen.Emit(OpCodes.Call, Methods.ReadULong);
					gen.Emit(OpCodes.Callvirt, getOrCreateProxy);
				}
				else
				{
					SerializerCompiler.EmitReadValue(
						gen,
						loadReader,
						loadSerializer,
						parameterType
						);
				}
			}

			var returnType = methodInfo.ReturnType;
			if (returnType != typeof (void))
			{
				var tmp = gen.DeclareLocal(returnType);
				gen.Emit(OpCodes.Callvirt, methodInfo);
				gen.Emit(OpCodes.Stloc, tmp);

				SerializerCompiler.EmitWriteValue(gen,
					loadWriter,
					() => gen.Emit(OpCodes.Ldloc, tmp),
					() => gen.Emit(OpCodes.Ldloca, tmp),
					() => gen.Emit(OpCodes.Ldfld, Serializer),
					returnType);
			}
			else
			{
				gen.Emit(OpCodes.Callvirt, methodInfo);
			}
		}

		protected void GenerateMethodInvocation(MethodBuilder method, string remoteMethodName, ParameterInfo[] parameters, Type returnType)
		{
			var gen = method.GetILGenerator();

			var stream = gen.DeclareLocal(typeof(MemoryStream));
			var binaryWriter = gen.DeclareLocal(typeof(StreamWriter));
			var binaryReader = gen.DeclareLocal(typeof(StreamReader));

			if (parameters.Length > 0)
			{
				// var stream = new MemoryStream();
				gen.Emit(OpCodes.Newobj, Methods.MemoryStreamCtor);
				gen.Emit(OpCodes.Stloc, stream);

				// var binaryWriter = new BinaryWriter(stream);
				gen.Emit(OpCodes.Ldloc, stream);
				gen.Emit(OpCodes.Newobj, Methods.BinaryWriterCtor);
				gen.Emit(OpCodes.Stloc, binaryWriter);

				Action loadWriter = () => gen.Emit(OpCodes.Ldloc, binaryWriter);
				Action loadSerializer =
					() =>
					{
						gen.Emit(OpCodes.Ldarg_0);
						gen.Emit(OpCodes.Ldfld, Serializer);
					};

				for (int i = 0; i < parameters.Length; ++i)
				{
					var parameter = parameters[i];
					var parameterType = parameter.ParameterType;
					int currentIndex = i+1;

					Action loadValue = () => gen.Emit(OpCodes.Ldarg, currentIndex);

					// If the parameter is attributed with [ByReference] then we don't want to serialize the object but ensure
					// that there's a servant for it on this end-point and then only serialize its object id.
					if (parameter.GetCustomAttribute<ByReferenceAttribute>() != null)
					{
						VerifyParameterConstraints(parameter);

						gen.Emit(OpCodes.Ldloc, binaryWriter);

						// _endPoint.GetOrCreateServant(arg[y])
						var getOrCreateServant = Methods.RemotingEndPointGetOrCreateServant.MakeGenericMethod(parameterType);
						gen.Emit(OpCodes.Ldarg_0);
						gen.Emit(OpCodes.Ldfld, EndPoint);
						loadValue();
						gen.Emit(OpCodes.Callvirt, getOrCreateServant);

						// binaryWriter.Write(servant.ObjectId);
						gen.Emit(OpCodes.Callvirt, Methods.GrainGetObjectId);
						gen.Emit(OpCodes.Call, Methods.WriteULong);
					}
					else
					{
						Action loadValueAddress = () => gen.Emit(OpCodes.Ldarga, currentIndex);

						//WriteXXX(_serializer, arg[y], binaryWriter);
						SerializerCompiler.EmitWriteValue(
							gen,
							loadWriter,
							loadValue,
							loadValueAddress,
							loadSerializer,
							parameterType);
					}
				}

				// binaryWriter.Flush()
				gen.Emit(OpCodes.Ldloc, binaryWriter);
				gen.Emit(OpCodes.Callvirt, Methods.BinaryWriterFlush);

				// stream.Position = 0
				gen.Emit(OpCodes.Ldloc, stream);
				gen.Emit(OpCodes.Ldc_I8, (long)0);
				gen.Emit(OpCodes.Call, Methods.StreamSetPosition);
			}
			else
			{
				gen.Emit(OpCodes.Ldnull);
				gen.Emit(OpCodes.Stloc, stream);
			}

			// _channel.CallRemoteMethod(_objectId, "get_XXX", stream);
			gen.Emit(OpCodes.Ldarg_0);
			gen.Emit(OpCodes.Ldfld, Channel);
			gen.Emit(OpCodes.Ldarg_0);
			gen.Emit(OpCodes.Ldfld, ObjectId);
			gen.Emit(OpCodes.Ldstr, remoteMethodName);
			gen.Emit(OpCodes.Ldloc, stream);
			gen.Emit(OpCodes.Callvirt, Methods.ChannelCallRemoteMethod);

			if (returnType == typeof(void))
			{
				gen.Emit(OpCodes.Pop);
			}
			else
			{
				// reader = new BinaryReader(...)
				gen.Emit(OpCodes.Newobj, Methods.BinaryReaderCtor);
				gen.Emit(OpCodes.Stloc, binaryReader);

				// return _serializer.DeserializeXXX(reader);
				SerializerCompiler.EmitReadValue(
					gen,
					() => gen.Emit(OpCodes.Ldloc, binaryReader),
					() =>
					{
						gen.Emit(OpCodes.Ldarg_0);
						gen.Emit(OpCodes.Ldfld, Serializer);
					},
					returnType
					);
			}

			gen.Emit(OpCodes.Ret);
		}

		private void VerifyParameterConstraints(ParameterInfo parameter)
		{
			if (parameter.ParameterType.IsValueType)
				throw new ArgumentException(string.Format("The parameter '{0}' of method '{1}' is marked as [ByReference] but is a valuetype - this is not supported", parameter.Name, parameter.Member.Name));
		}
	}
}