using System;
using System.IO;
using System.Reflection;
using System.Reflection.Emit;
using SharpRemote.CodeGeneration.Serialization;

namespace SharpRemote.CodeGeneration
{
	public abstract class Compiler
	{
		protected readonly Serializer _serializerCompiler;
		protected FieldBuilder Serializer;
		protected FieldBuilder Channel;
		protected FieldBuilder ObjectId;

		protected Compiler(Serializer serializer)
		{
			_serializerCompiler = serializer;
		}

		protected void ExtractArgumentsAndCallMethod(ILGenerator gen,
			MethodInfo methodInfo)
		{
			var allParameters = methodInfo.GetParameters();
			foreach (var parameter in allParameters)
			{
				gen.Emit(OpCodes.Ldarg_2);
				gen.EmitReadPod(parameter.ParameterType);
			}

			gen.Emit(OpCodes.Callvirt, methodInfo);
			if (methodInfo.ReturnType != typeof(void))
				_serializerCompiler.WriteValue(gen, methodInfo.ReturnType, Serializer);
		}

		protected void GenerateMethodInvocation(MethodBuilder method, string remoteMethodName, Type[] parameterTypes, Type returnType)
		{
			var gen = method.GetILGenerator();

			var stream = gen.DeclareLocal(typeof(MemoryStream));
			var binaryWriter = gen.DeclareLocal(typeof(StreamWriter));
			var binaryReader = gen.DeclareLocal(typeof(StreamReader));

			if (parameterTypes.Length > 0)
			{
				// var stream = new MemoryStream();
				gen.Emit(OpCodes.Newobj, Methods.MemoryStreamCtor);
				gen.Emit(OpCodes.Stloc, stream);

				// var binaryWriter = new BinaryWriter(stream);
				gen.Emit(OpCodes.Ldloc, stream);
				gen.Emit(OpCodes.Newobj, Methods.BinaryWriterCtor);
				gen.Emit(OpCodes.Stloc, binaryWriter);

				int index = 0;
				for (int i = 0; i < parameterTypes.Length; ++i)
				{
					//WriteXXX(_serializer, arg[y], binaryWriter);
					gen.Emit(OpCodes.Ldloc, binaryWriter);
					gen.Emit(OpCodes.Ldarg, ++index);
					_serializerCompiler.WriteValue(gen, parameterTypes[i], Serializer);
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
				DeserializeValue(gen, binaryReader, returnType);
			}

			gen.Emit(OpCodes.Ret);
		}

		private void DeserializeValue(ILGenerator gen, LocalBuilder binaryReader, Type propertyType)
		{
			gen.Emit(OpCodes.Ldloc, binaryReader);

			if (!gen.EmitReadPod(propertyType))
			{
				throw new NotImplementedException();
			}
		}
	}
}