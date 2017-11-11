using System;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using SharpRemote.Attributes;
using SharpRemote.Exceptions;

namespace SharpRemote.CodeGeneration.Serialization
{
	/// <summary>
	///     Responsible for emitting a method with the following signature:
	///     static void (WriterType, Type, <see cref="ISerializer2" />, <see cref="IRemotingEndPoint" />);
	///     The method can always assume that the passed value can never be null.
	/// </summary>
	public abstract class AbstractWriteValueNotNullMethodCompiler
		: AbstractMethodCompiler
	{
		private readonly CompilationContext _context;

		/// <summary>
		/// </summary>
		protected AbstractWriteValueNotNullMethodCompiler(CompilationContext context)
		{
			_context = context;
			Method = context.TypeBuilder.DefineMethod("WriteValueNotNull",
			                                          MethodAttributes.Public | MethodAttributes.Static,
			                                          CallingConventions.Standard, typeof(void), new[]
			                                          {
				                                          context.WriterType,
				                                          context.Type,
				                                          typeof(ISerializer2),
				                                          typeof(IRemotingEndPoint)
			                                          });
		}

		/// <inheritdoc />
		public override MethodBuilder Method { get; }

		/// <inheritdoc />
		public override void Compile(AbstractMethodsCompiler methods, ISerializationMethodStorage<AbstractMethodsCompiler> methodStorage)
		{
			var serializationType = _context.TypeDescription.SerializationType;
			switch (serializationType)
			{
				case SerializationType.ByValue:
					if (_context.TypeDescription.IsBuiltIn)
						EmitWriteBuiltInType();
					else
						EmitWriteByValue(methodStorage);
					break;

				case SerializationType.ByReference:
					EmitWriteByReference();
					break;

				case SerializationType.Singleton:
					break;

				case SerializationType.NotSerializable:
					throw new NotImplementedException();

				default:
					throw new InvalidEnumArgumentException("", (int) serializationType, typeof(SerializationType));
			}
		}

		private void EmitWriteBuiltInType()
		{
			throw new NotImplementedException();
		}

		private void EmitWriteByValue(ISerializationMethodStorage<AbstractMethodsCompiler> methodStorage)
		{
			var gen = Method.GetILGenerator();

			// The very first thing we want to do is to call the PreDeserializationCallback, if available.
			EmitCallBeforeSerialization(gen);

			// Then we want to write the contents of the base type to the stream, if available.
			var baseType = _context.TypeDescription.BaseType;
			if (baseType != null)
			{
				var methods = methodStorage.GetOrAdd(baseType.Type);
				gen.Emit(OpCodes.Ldarg_0);
				gen.Emit(OpCodes.Ldarg_1);
				gen.Emit(OpCodes.Ldarg_2);
				gen.Emit(OpCodes.Ldarg_3);
				gen.Emit(OpCodes.Call, methods.WriteValueNotNullMethod);
			}

			//Followed by the list of serializable fields
			EmitWriteFields(gen);
			// Then the serializable properties
			EmitWriteProperties(gen);

			// And finally call the PostDeserializationCallback, if available.
			EmitCallAfterSerialization(gen);

			gen.Emit(OpCodes.Ret);
		}

		private void EmitCallBeforeSerialization(ILGenerator gen)
		{
			var method = _context.Type.GetMethods()
			                     .FirstOrDefault(x => x.GetCustomAttribute<BeforeSerializeAttribute>() != null);
			if (method != null)
			{
				gen.Emit(OpCodes.Ldarg_1);
				gen.EmitCall(OpCodes.Call, method, optionalParameterTypes: null);
			}
		}

		private void EmitWriteFields(ILGenerator gen)
		{
			foreach (var field in _context.TypeDescription.Fields)
				try
				{
					EmitWriteValue(gen, field.FieldType, field.Name, () =>
					{
						if (_context.TypeDescription.IsValueType)
						{
							gen.Emit(OpCodes.Ldarga_S, 1);
						}
						else
						{
							gen.Emit(OpCodes.Ldarg_1);
						}

						if (field.FieldType.IsValueType)
						{
							gen.Emit(OpCodes.Ldflda, field.Field);
						}
						else
						{
							gen.Emit(OpCodes.Ldfld, field.Field);
						}
					});
				}
				catch (SerializationException)
				{
					throw;
				}
				catch (Exception e)
				{
					var message = string.Format("There was a problem generating the code to serialize field '{0} {1}' of type '{2}' ",
					                            field.FieldType,
					                            field.Name,
					                            _context.Type.FullName);
					throw new SerializationException(message, e);
				}
		}

		private void EmitWriteProperties(ILGenerator gen)
		{
			foreach (var property in _context.TypeDescription.Properties)
				try
				{
					EmitWriteValue(gen, property.PropertyType, property.Name, () =>
					{
						if (_context.TypeDescription.IsValueType)
						{
							gen.Emit(OpCodes.Ldarga_S, 1);
						}
						else
						{
							gen.Emit(OpCodes.Ldarg_1);
						}
						
						gen.Emit(OpCodes.Call, property.GetMethod.Method);
					});
				}
				catch (SerializationException)
				{
					throw;
				}
				catch (Exception e)
				{
					var message =
						string.Format("There was a problem generating the code to serialize property '{0} {1}' of type '{2}' ",
						              property.PropertyType,
						              property.Name,
						              _context.Type.FullName
						             );
					throw new SerializationException(message, e);
				}
		}

		private void EmitWriteValue(ILGenerator gen, TypeDescription valueType, string name, Action loadValue)
		{
			EmitBeginWriteFieldOrProperty(gen, valueType, name);
			var type = valueType.Type;
			if (type == typeof(byte))
				EmitWriteByte(gen, loadValue);
			else if (type == typeof(sbyte))
				EmitWriteSByte(gen, loadValue);
			else if (type == typeof(ushort))
				EmitWriteUShort(gen, loadValue);
			else if (type == typeof(short))
				EmitWriteShort(gen, loadValue);
			else if (type == typeof(uint))
				EmitWriteUInt(gen, loadValue);
			else if (type == typeof(int))
				EmitWriteInt(gen, loadValue);
			else if (type == typeof(ulong))
				EmitWriteULong(gen, loadValue);
			else if (type == typeof(long))
				EmitWriteLong(gen, loadValue);
			else if (type == typeof(decimal))
				EmitWriteDecimal(gen, loadValue);
			else if (type == typeof(float))
				EmitWriteFloat(gen, loadValue);
			else if (type == typeof(double))
				EmitWriteDouble(gen, loadValue);
			else if (type == typeof(string))
				EmitWriteString(gen, loadValue);
			else
				throw new NotImplementedException();
			EmitEndWriteFieldOrProperty(gen, valueType, name);
		}

		/// <summary>
		/// </summary>
		/// <param name="generator"></param>
		/// <param name="valueType"></param>
		/// <param name="name"></param>
		protected abstract void EmitBeginWriteFieldOrProperty(ILGenerator generator, TypeDescription valueType, string name);

		/// <summary>
		/// </summary>
		/// <param name="generator"></param>
		/// <param name="valueType"></param>
		/// <param name="name"></param>
		protected abstract void EmitEndWriteFieldOrProperty(ILGenerator generator, TypeDescription valueType, string name);

		/// <summary>
		/// </summary>
		/// <param name="gen"></param>
		/// <param name="loadValue">
		///     A method which, when executed, emits opcodes to push a value of type <see cref="byte" /> to be
		///     serialized onto the evaluation stack
		/// </param>
		protected abstract void EmitWriteByte(ILGenerator gen, Action loadValue);

		/// <summary>
		/// </summary>
		/// <param name="gen"></param>
		/// <param name="loadValue">
		///     A method which, when executed, emits opcodes to push a value of type <see cref="sbyte" /> to be
		///     serialized onto the evaluation stack
		/// </param>
		protected abstract void EmitWriteSByte(ILGenerator gen, Action loadValue);

		/// <summary>
		/// </summary>
		/// <param name="gen"></param>
		/// <param name="loadValue">
		///     A method which, when executed, emits opcodes to push a value of type <see cref="ushort" /> to
		///     be serialized onto the evaluation stack
		/// </param>
		protected abstract void EmitWriteUShort(ILGenerator gen, Action loadValue);

		/// <summary>
		/// </summary>
		/// <param name="gen"></param>
		/// <param name="loadValue">
		///     A method which, when executed, emits opcodes to push a value of type <see cref="short" /> to be
		///     serialized onto the evaluation stack
		/// </param>
		protected abstract void EmitWriteShort(ILGenerator gen, Action loadValue);

		/// <summary>
		/// </summary>
		/// <param name="gen"></param>
		/// <param name="loadValue">
		///     A method which, when executed, emits opcodes to push a value of type <see cref="uint" /> to be
		///     serialized onto the evaluation stack
		/// </param>
		protected abstract void EmitWriteUInt(ILGenerator gen, Action loadValue);

		/// <summary>
		/// </summary>
		/// <param name="gen"></param>
		/// <param name="loadValue">
		///     A method which, when executed, emits opcodes to push a value of type <see cref="int" /> to be
		///     serialized onto the evaluation stack
		/// </param>
		protected abstract void EmitWriteInt(ILGenerator gen, Action loadValue);

		/// <summary>
		/// </summary>
		/// <param name="gen"></param>
		/// <param name="loadValue">
		///     A method which, when executed, emits opcodes to push a value of type <see cref="ulong" /> to be
		///     serialized onto the evaluation stack
		/// </param>
		protected abstract void EmitWriteULong(ILGenerator gen, Action loadValue);

		/// <summary>
		/// </summary>
		/// <param name="gen"></param>
		/// <param name="loadValue">
		///     A method which, when executed, emits opcodes to push a value of type <see cref="long" /> to be
		///     serialized onto the evaluation stack
		/// </param>
		protected abstract void EmitWriteLong(ILGenerator gen, Action loadValue);

		/// <summary>
		/// </summary>
		/// <param name="gen"></param>
		/// <param name="loadValue">
		///     A method which, when executed, emits opcodes to push a value of type <see cref="decimal" /> to
		///     be serialized onto the evaluation stack
		/// </param>
		protected abstract void EmitWriteDecimal(ILGenerator gen, Action loadValue);

		/// <summary>
		/// </summary>
		/// <param name="gen"></param>
		/// <param name="loadValue">
		///     A method which, when executed, emits opcodes to push a value of type <see cref="float" /> to be
		///     serialized onto the evaluation stack
		/// </param>
		protected abstract void EmitWriteFloat(ILGenerator gen, Action loadValue);

		/// <summary>
		/// </summary>
		/// <param name="gen"></param>
		/// <param name="loadValue">
		///     A method which, when executed, emits opcodes to push a value of type <see cref="double" /> to
		///     be serialized onto the evaluation stack
		/// </param>
		protected abstract void EmitWriteDouble(ILGenerator gen, Action loadValue);

		/// <summary>
		/// </summary>
		/// <param name="gen"></param>
		/// <param name="loadValue">
		///     A method which, when executed, emits opcodes to push a value of type <see cref="string" /> to
		///     be serialized onto the evaluation stack
		/// </param>
		protected abstract void EmitWriteString(ILGenerator gen, Action loadValue);

		private void EmitCallAfterSerialization(ILGenerator gen)
		{
			var method = _context.Type.GetMethods().FirstOrDefault(x => x.GetCustomAttribute<AfterSerializeAttribute>() != null);
			if (method != null)
			{
				gen.Emit(OpCodes.Ldarg_1);
				gen.EmitCall(OpCodes.Call, method, optionalParameterTypes: null);
			}
		}

		private void EmitWriteByReference()
		{
			// The type should be serialized by reference, e.g. we create a servant for it
			// (or retrieve an existing one) and then write its (unique) object id to the stream.
			var proxyInterface = _context.TypeDescription.ByReferenceInterfaceType;

			var gen = Method.GetILGenerator();
			var grainWritten = gen.DefineLabel();
			var writeServant = gen.DefineLabel();

			// We have two possibilities however:
			// If we're dealing with an object implementing IProxy that is ALSO registered with this endpoint,
			// then we KNOW that both this endpoint and the connected counterpart have already negotiated a Grain-Id,
			// in which case we can emit that id. The reason we want to do this is as follows:
			// If a by-reference type is passed from endpoints A to B, then a servant exists on A and a proxy has been created
			// on B. If said proxy is then forwarded from B to A, then we definately want the object given to A to be
			// the ORIGINAL object, and not another proxy that performs two RPC invocations, when used (if A were
			// to use a proxy, then we would be generating calls A->B->A, when in fact no RPC invocations should be required).

			var grain = gen.DeclareLocal(typeof(IGrain));

			// var proxy = arg1 as IProxy
			gen.Emit(OpCodes.Ldarg_1);
			gen.Emit(OpCodes.Isinst, typeof(IProxy));
			gen.Emit(OpCodes.Stloc, grain);
			// if proxy == null, goto writeServant
			gen.Emit(OpCodes.Ldloc, grain);
			gen.Emit(OpCodes.Brfalse_S, writeServant);
			// if proxy.EndPoint != _endPoint, goto writeServant
			gen.Emit(OpCodes.Ldloc, grain);
			gen.Emit(OpCodes.Callvirt, Methods.GrainGetEndPoint);
			gen.Emit(OpCodes.Ldarg_2);
			gen.Emit(OpCodes.Ceq);
			gen.Emit(OpCodes.Brfalse_S, writeServant);

			// writer.WriteByte(RetrieveSubject)
			EmitWriteHint(gen, ByReferenceHint.RetrieveSubject);
			// writer.WriteLong(grain.ObjectId)
			EmitWriteObjectId(gen, grain);
			// goto grainWritten
			gen.Emit(OpCodes.Br, grainWritten);


			// If we're dealing with a proxy that is not registered OR we're not dealing with a proxy at all,
			// then we want to create a servant 
			// writer.Write(_remotingEndPoint.GetExistingOrCreateNewServant<T>(value).ObjectId);
			gen.MarkLabel(writeServant);

			// writer.WriteByte(CreateProxy) //< When the other side reads this message, it should create a proxy
			EmitWriteHint(gen, ByReferenceHint.CreateProxy);

			// grain = endPoint.GetOrCreateServant<T>(value);
			gen.Emit(OpCodes.Ldarg_3);
			gen.Emit(OpCodes.Ldarg_1);
			var method = Methods.RemotingEndPointGetOrCreateServant.MakeGenericMethod(proxyInterface);
			gen.Emit(OpCodes.Callvirt, method);
			gen.Emit(OpCodes.Stloc, grain);
			// writer.Write(grain.ObjectId);
			EmitWriteObjectId(gen, grain);

			gen.MarkLabel(grainWritten);
		}

		/// <summary>
		/// </summary>
		/// <param name="generator"></param>
		/// <param name="grain"></param>
		protected abstract void EmitWriteObjectId(ILGenerator generator, LocalBuilder grain);

		/// <summary>
		/// </summary>
		/// <param name="generator"></param>
		/// <param name="hint"></param>
		protected abstract void EmitWriteHint(ILGenerator generator, ByReferenceHint hint);
	}
}