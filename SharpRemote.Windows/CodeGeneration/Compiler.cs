using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Threading.Tasks;
using SharpRemote.CodeGeneration.Serialization;

namespace SharpRemote.CodeGeneration
{
	public abstract class Compiler
	{
		protected readonly Serializer SerializerCompiler;
		protected FieldBuilder Channel;
		protected FieldBuilder EndPoint;
		protected FieldBuilder ObjectId;
		protected FieldBuilder Serializer;

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

			ParameterInfo[] allParameters = methodInfo.GetParameters();
			foreach (ParameterInfo parameter in allParameters)
			{
				Type parameterType = parameter.ParameterType;

				if (parameter.GetCustomAttribute<ByReferenceAttribute>() != null)
				{
					VerifyParameterConstraints(parameter);

					// _endPoint.GetOrCreateProxy(reader.ReadUlong());
					MethodInfo getOrCreateProxy = Methods.RemotingEndPointGetOrCreateProxy.MakeGenericMethod(parameterType);
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

			Type returnType = methodInfo.ReturnType;
			bool isAsync = returnType == typeof (Task) ||
			               returnType.IsGenericType && returnType.GetGenericTypeDefinition() == typeof (Task<>);

			if (isAsync)
			{
				Type taskReturnType = returnType != typeof(Task) ? returnType.GetGenericArguments()[0] : typeof(void);
				if (taskReturnType != typeof (void))
				{
					
				}
				else
				{
					gen.Emit(OpCodes.Callvirt, methodInfo);
					gen.Emit(OpCodes.Call, Methods.TaskWait);
				}
			}
			else if (returnType != typeof (void))
			{
				LocalBuilder tmp = gen.DeclareLocal(returnType);
				gen.Emit(OpCodes.Callvirt, methodInfo);
				gen.Emit(OpCodes.Stloc, tmp);

				SerializerCompiler.EmitWriteValue(gen,
					loadWriter,
					() => gen.Emit(OpCodes.Ldloc, tmp),
					() => gen.Emit(OpCodes.Ldloca, tmp),
					loadSerializer,
					returnType);
			}
			else
			{
				gen.Emit(OpCodes.Callvirt, methodInfo);
			}
		}

		protected void GenerateMethodInvocation(MethodBuilder method, string remoteMethodName, ParameterInfo[] parameters,
			MethodInfo remoteMethod)
		{
			ILGenerator gen = method.GetILGenerator();

			LocalBuilder stream = gen.DeclareLocal(typeof (MemoryStream));
			LocalBuilder binaryWriter = gen.DeclareLocal(typeof (StreamWriter));

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
					ParameterInfo parameter = parameters[i];
					Type parameterType = parameter.ParameterType;
					int currentIndex = i + 1;

					Action loadValue = () => gen.Emit(OpCodes.Ldarg, currentIndex);

					// If the parameter is attributed with [ByReference] then we don't want to serialize the object but ensure
					// that there's a servant for it on this end-point and then only serialize its object id.
					if (parameter.GetCustomAttribute<ByReferenceAttribute>() != null)
					{
						VerifyParameterConstraints(parameter);

						gen.Emit(OpCodes.Ldloc, binaryWriter);

						// _endPoint.GetOrCreateServant(arg[y])
						MethodInfo getOrCreateServant = Methods.RemotingEndPointGetOrCreateServant.MakeGenericMethod(parameterType);
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
				gen.Emit(OpCodes.Ldc_I8, (long) 0);
				gen.Emit(OpCodes.Call, Methods.StreamSetPosition);
			}
			else
			{
				gen.Emit(OpCodes.Ldnull);
				gen.Emit(OpCodes.Stloc, stream);
			}

			Type returnType = method.ReturnType;
			ICustomAttributeProvider returnAttributes = remoteMethod.ReturnTypeCustomAttributes;
			bool isAsync = returnType == typeof (Task) ||
			               returnType.IsGenericType && returnType.GetGenericTypeDefinition() == typeof (Task<>);

			if (isAsync)
			{
				Type taskReturnType = returnType != typeof (Task) ? returnType.GetGenericArguments()[0] : typeof (void);

				var type = (TypeBuilder) method.DeclaringType;

				string name = string.Format("Invoke_{0}", method.Name);
				MethodBuilder invokeMethod = type.DefineMethod(name, MethodAttributes.Private,
					CallingConventions.Standard,
					taskReturnType,
					new[] {typeof (object)}
					);

				// Task.Factory_get()
				gen.Emit(OpCodes.Call, Methods.TaskGetFactory);

				if (taskReturnType == typeof (void))
				{
					// new Action<object>(this, Invoke_XXX)
					gen.Emit(OpCodes.Ldarg_0);
					gen.Emit(OpCodes.Ldftn, invokeMethod);
					gen.Emit(OpCodes.Newobj, Methods.ActionIntPtrCtor);
				}
				else
				{
					// new Func<object, T>(this, Invoke_XXX)
					Type func = typeof (Func<,>).MakeGenericType(typeof (object), taskReturnType);
					ConstructorInfo funcCtor = func.GetConstructor(new[] {typeof (object), typeof (IntPtr)});
					gen.Emit(OpCodes.Ldarg_0);
					gen.Emit(OpCodes.Ldftn, invokeMethod);
					gen.Emit(OpCodes.Newobj, funcCtor);
				}

				// new TaskParameters(_channel, _objectId, "get_XXX", stream);
				gen.Emit(OpCodes.Ldstr, remoteMethodName);
				gen.Emit(OpCodes.Ldloc, stream);
				gen.Emit(OpCodes.Newobj, Methods.NewTaskParameters);

				if (taskReturnType == typeof (void))
				{
					// return TaskFactory.StartNew(Action<object>, object);
					gen.Emit(OpCodes.Callvirt, Methods.TaskFactoryStartNew);
					gen.Emit(OpCodes.Ret);
				}
				else
				{
					// return TaskFactory.StartNew<T>(Func<object, T>, object);
					MethodInfo startNew = typeof (TaskFactory).GetMethods()
						.Where(x => x.Name == "StartNew")
						.Where(x => x.GetParameters().Length == 2 &&
						            x.GetParameters()[1].ParameterType == typeof (object))
						.First(x => x.IsGenericMethod)
						.MakeGenericMethod(taskReturnType);

					gen.Emit(OpCodes.Callvirt, startNew);
					gen.Emit(OpCodes.Ret);
				}

				ILGenerator invokeGen = invokeMethod.GetILGenerator();
				invokeGen.Emit(OpCodes.Ldarg_0);
				invokeGen.Emit(OpCodes.Ldfld, Channel);
				invokeGen.Emit(OpCodes.Ldarg_0);
				invokeGen.Emit(OpCodes.Ldfld, ObjectId);
				invokeGen.Emit(OpCodes.Ldarg_1);
				invokeGen.Emit(OpCodes.Ldfld, Methods.TaskParametersMethodName);
				invokeGen.Emit(OpCodes.Ldarg_1);
				invokeGen.Emit(OpCodes.Ldfld, Methods.TaskParametersStream);

				// _channel.CallRemoteMethod(_objectId, "get_XXX", stream);
				invokeGen.Emit(OpCodes.Callvirt, Methods.ChannelCallRemoteMethod);

				if (taskReturnType == typeof (void))
				{
					invokeGen.Emit(OpCodes.Pop);
				}
				else
				{
					LocalBuilder binaryReader = invokeGen.DeclareLocal(typeof (StreamReader));
					ReadValueFromStream(method, invokeGen, binaryReader, null, taskReturnType);
				}

				invokeGen.Emit(OpCodes.Ret);
			}
			else
			{
				// _channel.CallRemoteMethod(_objectId, "get_XXX", stream);
				gen.Emit(OpCodes.Ldarg_0);
				gen.Emit(OpCodes.Ldfld, Channel);
				gen.Emit(OpCodes.Ldarg_0);
				gen.Emit(OpCodes.Ldfld, ObjectId);
				gen.Emit(OpCodes.Ldstr, remoteMethodName);
				gen.Emit(OpCodes.Ldloc, stream);
				gen.Emit(OpCodes.Callvirt, Methods.ChannelCallRemoteMethod);

				if (returnType == typeof (void))
				{
					gen.Emit(OpCodes.Pop);
				}
				else
				{
					LocalBuilder binaryReader = gen.DeclareLocal(typeof (StreamReader));
					ReadValueFromStream(method, gen, binaryReader, returnAttributes, returnType);
				}

				gen.Emit(OpCodes.Ret);
			}
		}

		private void ReadValueFromStream(MethodBuilder method,
			ILGenerator gen,
			LocalBuilder binaryReader,
			ICustomAttributeProvider returnAttributes,
			Type returnType)
		{
			// reader = new BinaryReader(...)
			gen.Emit(OpCodes.Newobj, Methods.BinaryReaderCtor);
			gen.Emit(OpCodes.Stloc, binaryReader);

			Action loadReader = () => gen.Emit(OpCodes.Ldloc, binaryReader);

			if (returnAttributes != null && returnAttributes.GetCustomAttributes(typeof (ByReferenceAttribute), true).Length > 0)
			{
				// _endPoint.GetOrCreateProxy(reader.ReadUlong());
				VerifyReturnParameterConstraints(method);

				// _endPoint.GetOrCreateProxy(reader.ReadUlong());
				MethodInfo getOrCreateProxy = Methods.RemotingEndPointGetOrCreateProxy.MakeGenericMethod(returnType);
				gen.Emit(OpCodes.Ldarg_0);
				gen.Emit(OpCodes.Ldfld, EndPoint);
				loadReader();
				gen.Emit(OpCodes.Call, Methods.ReadULong);
				gen.Emit(OpCodes.Callvirt, getOrCreateProxy);
			}
			else
			{
				// return _serializer.DeserializeXXX(reader);
				SerializerCompiler.EmitReadValue(
					gen,
					loadReader,
					() =>
					{
						gen.Emit(OpCodes.Ldarg_0);
						gen.Emit(OpCodes.Ldfld, Serializer);
					},
					returnType
					);
			}
		}

		private void VerifyParameterConstraints(ParameterInfo parameter)
		{
			if (parameter.ParameterType.IsValueType)
				throw new ArgumentException(
					string.Format(
						"The parameter '{0}' of method '{1}' is marked as [ByReference] but is a valuetype - this is not supported",
						parameter.Name, parameter.Member.Name));
		}

		private void VerifyReturnParameterConstraints(MethodInfo method)
		{
			if (method.ReturnType.IsValueType)
				throw new ArgumentException(
					string.Format(
						"The return parameter of method '{0}' is marked as [ByReference] but is a valuetype - this is not supported",
						method.Name));
		}
	}
}