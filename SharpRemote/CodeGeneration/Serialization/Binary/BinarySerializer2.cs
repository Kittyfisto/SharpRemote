using System;
using System.IO;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using log4net;
using SharpRemote.CodeGeneration.Serialization;
using SharpRemote.CodeGeneration.Serialization.Binary;

// ReSharper disable once CheckNamespace
namespace SharpRemote
{
	/// <summary>
	///     Shall replace <see cref="BinarySerializer" />.
	/// </summary>
	internal sealed class BinarySerializer2
		: ISerializer2
	{
		private static readonly ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

		private readonly SerializationMethodStorage<BinaryMethodsCompiler> _methodStorage;
		private readonly BinarySerializationCompiler _methodCompiler;
		
		/// <summary>
		/// </summary>
		public BinarySerializer2()
			: this(CreateModule())
		{
		}

		public BinarySerializer2(ModuleBuilder moduleBuilder)
		{
			_methodCompiler = new BinarySerializationCompiler(moduleBuilder);
			_methodStorage = new SerializationMethodStorage<BinaryMethodsCompiler>("BinarySerializer",
			                                                                       _methodCompiler);
		}

		/// <inheritdoc />
		public void RegisterType<T>()
		{
			RegisterType(typeof(T));
		}

		/// <inheritdoc />
		public void RegisterType(Type type)
		{
			Log.DebugFormat("Registering type '{0}'", type);
			_methodStorage.GetOrAdd(type);
			Log.DebugFormat("Type '{0}' successfully registered", type);
		}

		/// <inheritdoc />
		public bool IsTypeRegistered<T>()
		{
			return IsTypeRegistered(typeof(T));
		}

		/// <inheritdoc />
		public bool IsTypeRegistered(Type type)
		{
			return _methodStorage.Contains(type);
		}

		/// <inheritdoc />
		public IMethodCallWriter CreateMethodCallWriter(Stream stream, ulong rpcId, ulong grainId, string methodName, IRemotingEndPoint endPoint = null)
		{
			return new BinaryMethodCallWriter(stream, grainId, methodName, rpcId);
		}

		/// <inheritdoc />
		public IMethodResultWriter CreateMethodResultWriter(Stream stream, ulong rpcId, IRemotingEndPoint endPoint = null)
		{
			return new BinaryMethodResultWriter(stream, rpcId);
		}

		/// <inheritdoc />
		public void CreateMethodReader(Stream stream,
		                               out IMethodCallReader callReader,
		                               out IMethodResultReader resultReader,
		                               IRemotingEndPoint endPoint = null)
		{
			var reader = new BinaryReader(stream, Encoding.UTF8, true);
			var type = (MessageType2)reader.ReadByte();
			switch (type)
			{
				case MessageType2.Call:
					callReader = new BinaryMethodCallReader(reader);
					resultReader = null;
					break;

				case MessageType2.Result:
					callReader = null;
					resultReader = new BinaryMethodResultReader(reader);
					break;

				default:
					throw new NotImplementedException();
			}
		}

		/// <summary>
		///     Serializes the given object graph without any type information of <paramref name="message" />
		///     (Obviously types of other values within the object graph are allowed to be serialized).
		/// </summary>
		/// <param name="message"></param>
		/// <returns></returns>
		public byte[] SerializeWithoutTypeInformation(object message)
		{
			throw new NotImplementedException();
		}

		/// <summary>
		///     Deserializes the given message into an object graph of type <typeparamref name="T"/>.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="serializedMessage"></param>
		/// <returns></returns>
		public T Deserialize<T>(byte[] serializedMessage)
		{
			throw new NotImplementedException();
		}

		#region Write Methods

		public static void WriteValue(BinaryWriter writer, sbyte value)
		{
			writer.Write(value);
		}

		public static void WriteValue(BinaryWriter writer, byte value)
		{
			writer.Write(value);
		}

		public static void WriteValue(BinaryWriter writer, ushort value)
		{
			writer.Write(value);
		}

		public static void WriteValue(BinaryWriter writer, uint value)
		{
			writer.Write(value);
		}

		public static void WriteValue(BinaryWriter writer, short value)
		{
			writer.Write(value);
		}

		public static void WriteValue(BinaryWriter writer, int value)
		{
			writer.Write(value);
		}

		public static void WriteValue(BinaryWriter writer, ulong value)
		{
			writer.Write(value);
		}

		public static void WriteValue(BinaryWriter writer, long value)
		{
			writer.Write(value);
		}

		public static void WriteValue(BinaryWriter writer, float value)
		{
			writer.Write(value);
		}

		public static void WriteValue(BinaryWriter writer, double value)
		{
			writer.Write(value);
		}

		public static void WriteValue(BinaryWriter writer, string value)
		{
			writer.Write(value);
		}

		public static void WriteValue(BinaryWriter writer, decimal value)
		{
			writer.Write(value);
		}

		public static void WriteValue(BinaryWriter writer, byte[] value)
		{
			if (value != null)
			{
				writer.Write(true);
				writer.Write(value.Length);
				writer.Write(value);
			}
			else
			{
				writer.Write(false);
			}
		}

		public static void WriteValue(BinaryWriter writer, DateTime value)
		{
			writer.Write(value.ToBinary());
		}

		#endregion

		#region Read Methods

		public static byte ReadValueAsByte(BinaryReader reader)
		{
			return reader.ReadByte();
		}

		#endregion

		private static ModuleBuilder CreateModule()
		{
			var assemblyName = new AssemblyName("SharpRemote.GeneratedCode.Serializer");
			var assembly = AppDomain.CurrentDomain.DefineDynamicAssembly(assemblyName,
			                                                             AssemblyBuilderAccess.RunAndSave);
			var moduleName = assemblyName.Name + ".dll";
			var module = assembly.DefineDynamicModule(moduleName);
			return module;
		}
	}
}