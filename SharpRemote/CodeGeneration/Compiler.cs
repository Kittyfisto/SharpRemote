using System;
using System.Reflection;
using System.Reflection.Emit;
using SharpRemote.CodeGeneration.Serialization;

namespace SharpRemote.CodeGeneration
{
	public abstract class Compiler
	{
		protected readonly Serializer _serializerCompiler;
		protected FieldBuilder Serializer;

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
	}
}