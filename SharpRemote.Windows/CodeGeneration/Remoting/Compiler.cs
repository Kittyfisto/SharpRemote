using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Threading.Tasks;

namespace SharpRemote.CodeGeneration.Remoting
{
	internal abstract class Compiler
	{
		protected readonly Serializer SerializerCompiler;
		protected readonly Type InterfaceType;
		protected FieldBuilder Channel;
		protected FieldBuilder EndPoint;
		protected FieldBuilder ObjectId;
		protected FieldBuilder Serializer;

		protected Compiler(Serializer serializer, Type interfaceType)
		{
			if (interfaceType == null) throw new ArgumentNullException("interfaceType");

			SerializerCompiler = serializer;
			InterfaceType = interfaceType;
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
			Action loadRemotingEndPoint = () =>
			{
				gen.Emit(OpCodes.Ldarg_0);
				gen.Emit(OpCodes.Ldfld, EndPoint);
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
						loadRemotingEndPoint,
						parameterType
						);
				}
			}

			Type returnType = methodInfo.ReturnType;
			bool isAsync = returnType == typeof (Task) ||
			               returnType.IsGenericType && returnType.GetGenericTypeDefinition() == typeof (Task<>);

			if (isAsync)
			{
				LocalBuilder task = gen.DeclareLocal(returnType);
				Type taskReturnType = returnType != typeof(Task) ? returnType.GetGenericArguments()[0] : typeof(void);
				if (taskReturnType != typeof (void))
				{
					LocalBuilder taskResult = gen.DeclareLocal(taskReturnType);

					gen.Emit(OpCodes.Callvirt, methodInfo);
					gen.Emit(OpCodes.Stloc, task);
					EmitVerifyTaskConstraints(methodInfo, gen, () => gen.Emit(OpCodes.Ldloc, task));

					gen.Emit(OpCodes.Ldloc, task);
					var getResult = returnType.GetProperty("Result").GetMethod;
					gen.Emit(OpCodes.Call, getResult);
					gen.Emit(OpCodes.Stloc, taskResult);

					SerializerCompiler.EmitWriteValue(gen,
						loadWriter,
						() => gen.Emit(OpCodes.Ldloc, taskResult),
						() => gen.Emit(OpCodes.Ldloca, taskResult),
						loadSerializer,
						loadRemotingEndPoint,
						taskReturnType);
				}
				else
				{
					gen.Emit(OpCodes.Callvirt, methodInfo);
					gen.Emit(OpCodes.Stloc, task);
					EmitVerifyTaskConstraints(methodInfo, gen, () => gen.Emit(OpCodes.Ldloc, task));

					gen.Emit(OpCodes.Ldloc, task);
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
				                                  loadRemotingEndPoint,
				                                  returnType);
			}
			else
			{
				gen.Emit(OpCodes.Callvirt, methodInfo);
			}
		}

		private void EmitVerifyTaskConstraints(MethodInfo method, ILGenerator gen, Action loadTask)
		{
			loadTask();
			gen.Emit(OpCodes.Callvirt, Methods.TaskGetStatus);
			gen.Emit(OpCodes.Ldc_I4, (int)TaskStatus.Created);
			gen.Emit(OpCodes.Ceq);

			var taskNotStarted = gen.DefineLabel();
			var taskStarted = gen.DefineLabel();

			gen.Emit(OpCodes.Brtrue, taskNotStarted);
			gen.Emit(OpCodes.Br, taskStarted);

			gen.MarkLabel(taskNotStarted);

			gen.Emit(OpCodes.Ldstr, "{0}.{1} of servant #{2} returned a non-started task - this is not supported");
			gen.Emit(OpCodes.Ldstr, InterfaceType.Name);
			gen.Emit(OpCodes.Ldstr, method.Name);
			gen.Emit(OpCodes.Ldarg_0);
			gen.Emit(OpCodes.Ldfld, ObjectId);
			gen.Emit(OpCodes.Box, typeof(ulong));
			gen.Emit(OpCodes.Call, Methods.StringFormat3Objects);
			gen.Emit(OpCodes.Newobj, Methods.NotSupportedExceptionCtor);
			gen.Emit(OpCodes.Throw);

			gen.MarkLabel(taskStarted);
		}

		protected void GenerateMethodInvocation(MethodBuilder method, string interfaceType, string remoteMethodName, ParameterInfo[] parameters,
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
				Action loadRemotingEndPoint =
					() =>
						{
							gen.Emit(OpCodes.Ldarg_0);
							gen.Emit(OpCodes.Ldfld, EndPoint);
						};

				for (int i = 0; i < parameters.Length; ++i)
				{
					ParameterInfo parameter = parameters[i];
					Type parameterType = parameter.ParameterType;
					int currentIndex = i + 1;

					Action loadValue = () => gen.Emit(OpCodes.Ldarg, currentIndex);
					Action loadValueAddress = () => gen.Emit(OpCodes.Ldarga, currentIndex);

					//WriteXXX(_serializer, arg[y], binaryWriter);
					SerializerCompiler.EmitWriteValue(
						gen,
						loadWriter,
						loadValue,
						loadValueAddress,
						loadSerializer,
						loadRemotingEndPoint,
						parameterType);
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

				gen.Emit(OpCodes.Ldloc, stream);

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
				invokeGen.Emit(OpCodes.Ldstr, interfaceType);
				invokeGen.Emit(OpCodes.Ldstr, remoteMethodName);
				invokeGen.Emit(OpCodes.Ldarg_1);
				invokeGen.Emit(OpCodes.Castclass, typeof(MemoryStream));

				// _channel.CallRemoteMethod(_objectId, "IFoo", "get_XXX", stream);
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
				// _channel.CallRemoteMethod(_objectId, "IFoo", "get_XXX", stream);
				gen.Emit(OpCodes.Ldarg_0);
				gen.Emit(OpCodes.Ldfld, Channel);
				gen.Emit(OpCodes.Ldarg_0);
				gen.Emit(OpCodes.Ldfld, ObjectId);
				gen.Emit(OpCodes.Ldstr, interfaceType);
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
					() =>
						{
							gen.Emit(OpCodes.Ldarg_0);
							gen.Emit(OpCodes.Ldfld, EndPoint);
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
