using System;
using System.ComponentModel;
using System.IO;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.Serialization.Formatters.Binary;
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
	/// <remarks>
	///     This class is meant to be internal, but I don't have a clue how to access
	///     internal class from within generated code, so it's public for now...
	/// </remarks>
	public sealed class BinarySerializer2
		: ISerializer2
	{
		private static readonly ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

		private readonly SerializationMethodStorage<BinaryMethodsCompiler> _methodStorage;
		private readonly BinarySerializationCompiler _methodCompiler;
		private readonly ITypeResolver _typeResolver;

		/// <summary>
		/// </summary>
		public BinarySerializer2(ITypeResolver typeResolver = null)
			: this(CreateModule(), typeResolver)
		{
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="moduleBuilder"></param>
		/// <param name="typeResolver"></param>
		public BinarySerializer2(ModuleBuilder moduleBuilder, ITypeResolver typeResolver = null)
		{
			_methodCompiler = new BinarySerializationCompiler(moduleBuilder);
			_methodStorage = new SerializationMethodStorage<BinaryMethodsCompiler>("BinarySerializer",
			                                                                       _methodCompiler);
			_typeResolver = typeResolver;
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
			return new BinaryMethodCallWriter(this, stream, grainId, methodName, rpcId, endPoint);
		}

		/// <inheritdoc />
		public IMethodResultWriter CreateMethodResultWriter(Stream stream, ulong rpcId, IRemotingEndPoint endPoint = null)
		{
			return new BinaryMethodResultWriter(this, stream, rpcId, endPoint);
		}

		/// <inheritdoc />
		public void CreateMethodReader(Stream stream,
		                               out IMethodCallReader callReader,
		                               out IMethodResultReader resultReader,
		                               IRemotingEndPoint endPoint = null)
		{
			var reader = new BinaryReader(stream, Encoding.UTF8, true);
			var type = (MessageType2)reader.ReadByte();
			if (type == MessageType2.Call)
			{
				callReader = new BinaryMethodCallReader(this, reader);
				resultReader = null;
			}
			else if ((type & MessageType2.Result) == MessageType2.Result)
			{
				callReader = null;
				resultReader = new BinaryMethodResultReader(this, reader);
			}
			else
			{
				throw new InvalidEnumArgumentException("type", (int) type, typeof(MessageType2));
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
			var methods = _methodStorage.GetOrAdd(message.GetType());

			using (var stream = new MemoryStream())
			using (var writer = new BinaryWriter(stream))
			{
				methods.WriteDelegate(writer, message, this, null);
				writer.Flush();
				return stream.ToArray();
			}
		}

		/// <summary>
		///     Deserializes the given message into an object graph of type <typeparamref name="T"/>.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="serializedMessage"></param>
		/// <returns></returns>
		public T Deserialize<T>(byte[] serializedMessage)
		{
			var methods = _methodStorage.GetOrAdd(typeof(T));

			using (var stream = new MemoryStream(serializedMessage))
			using (var reader = new BinaryReader(stream))
			{
				var value = methods.ReadObjectDelegate(reader, this, null);
				return (T) value;
			}
		}

		#region Write Methods

		/// <summary>
		/// 
		/// </summary>
		/// <param name="writer"></param>
		/// <param name="value"></param>
		/// <param name="endPoint"></param>
		public void WriteObject(BinaryWriter writer, object value, IRemotingEndPoint endPoint)
		{
			if (value != null)
			{
				WriteValue(writer, true);
				WriteObjectNotNull(writer, value, endPoint);
			}
			else
			{
				WriteValue(writer, false);
			}
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="writer"></param>
		/// <param name="value"></param>
		/// <param name="endPoint"></param>
		public void WriteObjectNotNull(BinaryWriter writer, object value, IRemotingEndPoint endPoint)
		{
			var type = value.GetType();
			WriteTypeInformation(writer, type);
			var methods = _methodStorage.GetOrAdd(value.GetType());
			methods.WriteDelegate(writer, value, this, endPoint);
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="writer"></param>
		/// <param name="value"></param>
		public static void WriteValue(BinaryWriter writer, bool value)
		{
			writer.Write(value);
		}
		
		/// <summary>
		/// 
		/// </summary>
		/// <param name="writer"></param>
		/// <param name="value"></param>
		public static void WriteValue(BinaryWriter writer, sbyte value)
		{
			writer.Write(value);
		}
		
		/// <summary>
		/// 
		/// </summary>
		/// <param name="writer"></param>
		/// <param name="value"></param>
		public static void WriteValue(BinaryWriter writer, byte value)
		{
			writer.Write(value);
		}
		
		/// <summary>
		/// 
		/// </summary>
		/// <param name="writer"></param>
		/// <param name="value"></param>
		public static void WriteValue(BinaryWriter writer, ushort value)
		{
			writer.Write(value);
		}
		
		/// <summary>
		/// 
		/// </summary>
		/// <param name="writer"></param>
		/// <param name="value"></param>
		public static void WriteValue(BinaryWriter writer, uint value)
		{
			writer.Write(value);
		}
		
		/// <summary>
		/// 
		/// </summary>
		/// <param name="writer"></param>
		/// <param name="value"></param>
		public static void WriteValue(BinaryWriter writer, short value)
		{
			writer.Write(value);
		}
		
		/// <summary>
		/// 
		/// </summary>
		/// <param name="writer"></param>
		/// <param name="value"></param>
		public static void WriteValue(BinaryWriter writer, int value)
		{
			writer.Write(value);
		}
		
		/// <summary>
		/// 
		/// </summary>
		/// <param name="writer"></param>
		/// <param name="value"></param>
		public static void WriteValue(BinaryWriter writer, ulong value)
		{
			writer.Write(value);
		}
		
		/// <summary>
		/// 
		/// </summary>
		/// <param name="writer"></param>
		/// <param name="value"></param>
		public static void WriteValue(BinaryWriter writer, long value)
		{
			writer.Write(value);
		}
		
		/// <summary>
		/// 
		/// </summary>
		/// <param name="writer"></param>
		/// <param name="value"></param>
		public static void WriteValue(BinaryWriter writer, float value)
		{
			writer.Write(value);
		}
		
		/// <summary>
		/// 
		/// </summary>
		/// <param name="writer"></param>
		/// <param name="value"></param>
		public static void WriteValue(BinaryWriter writer, double value)
		{
			writer.Write(value);
		}
		
		/// <summary>
		/// 
		/// </summary>
		/// <param name="writer"></param>
		/// <param name="value"></param>
		public static void WriteValue(BinaryWriter writer, string value)
		{
			if (value != null)
			{
				writer.Write(true);
				writer.Write(value);
			}
			else
			{
				writer.Write(false);
			}
		}
		
		/// <summary>
		/// 
		/// </summary>
		/// <param name="writer"></param>
		/// <param name="value"></param>
		public static void WriteValue(BinaryWriter writer, decimal value)
		{
			writer.Write(value);
		}
		
		/// <summary>
		/// 
		/// </summary>
		/// <param name="writer"></param>
		/// <param name="value"></param>
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

		/// <summary>
		/// 
		/// </summary>
		/// <param name="writer"></param>
		/// <param name="value"></param>
		public static void WriteValue(BinaryWriter writer, DateTime value)
		{
			writer.Write(value.ToBinary());
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="writer"></param>
		/// <param name="exception"></param>
		/// <exception cref="ArgumentNullException"></exception>
		public static void WriteValue(BinaryWriter writer, Exception exception)
		{
			if (writer == null)
				throw new ArgumentNullException(nameof(writer));
			if (exception == null)
				throw new ArgumentNullException(nameof(exception));

			var formatter = new BinaryFormatter();
			writer.Flush();
			formatter.Serialize(writer.BaseStream, exception);
		}

		#endregion

		#region Read Methods
		
		/// <summary>
		/// 
		/// </summary>
		/// <param name="reader"></param>
		/// <returns></returns>
		public static byte ReadValueAsByte(BinaryReader reader)
		{
			return reader.ReadByte();
		}
		
		/// <summary>
		/// 
		/// </summary>
		/// <param name="reader"></param>
		/// <returns></returns>
		public static sbyte ReadValueAsSByte(BinaryReader reader)
		{
			return reader.ReadSByte();
		}
		
		/// <summary>
		/// 
		/// </summary>
		/// <param name="reader"></param>
		/// <returns></returns>
		public static bool ReadValueAsBoolean(BinaryReader reader)
		{
			return reader.ReadBoolean();
		}
		
		/// <summary>
		/// 
		/// </summary>
		/// <param name="reader"></param>
		/// <returns></returns>
		public static ushort ReadValueAsUInt16(BinaryReader reader)
		{
			return reader.ReadUInt16();
		}
		
		/// <summary>
		/// 
		/// </summary>
		/// <param name="reader"></param>
		/// <returns></returns>
		public static short ReadValueAsInt16(BinaryReader reader)
		{
			return reader.ReadInt16();
		}
		
		/// <summary>
		/// 
		/// </summary>
		/// <param name="reader"></param>
		/// <returns></returns>
		public static int ReadValueAsInt32(BinaryReader reader)
		{
			return reader.ReadInt32();
		}
		
		/// <summary>
		/// 
		/// </summary>
		/// <param name="reader"></param>
		/// <returns></returns>
		public static uint ReadValueAsUInt32(BinaryReader reader)
		{
			return reader.ReadUInt32();
		}
		
		/// <summary>
		/// 
		/// </summary>
		/// <param name="reader"></param>
		/// <returns></returns>
		public static long ReadValueAsInt64(BinaryReader reader)
		{
			return reader.ReadInt64();
		}
		
		/// <summary>
		/// 
		/// </summary>
		/// <param name="reader"></param>
		/// <returns></returns>
		public static ulong ReadValueAsUInt64(BinaryReader reader)
		{
			return reader.ReadUInt64();
		}
		
		/// <summary>
		/// 
		/// </summary>
		/// <param name="reader"></param>
		/// <returns></returns>
		public static float ReadValueAsSingle(BinaryReader reader)
		{
			return reader.ReadSingle();
		}
		
		/// <summary>
		/// 
		/// </summary>
		/// <param name="reader"></param>
		/// <returns></returns>
		public static double ReadValueAsDouble(BinaryReader reader)
		{
			return reader.ReadDouble();
		}
		
		/// <summary>
		/// 
		/// </summary>
		/// <param name="reader"></param>
		/// <returns></returns>
		public static decimal ReadValueAsDecimal(BinaryReader reader)
		{
			return reader.ReadDecimal();
		}
		
		/// <summary>
		/// 
		/// </summary>
		/// <param name="reader"></param>
		/// <returns></returns>
		public static DateTime ReadValueAsDateTime(BinaryReader reader)
		{
			var value = reader.ReadInt64();
			return DateTime.FromBinary(value);
		}
		
		/// <summary>
		/// 
		/// </summary>
		/// <param name="reader"></param>
		/// <returns></returns>
		public static string ReadValueAsString(BinaryReader reader)
		{
			if (!reader.ReadBoolean())
				return null;

			return reader.ReadString();
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="reader"></param>
		/// <returns></returns>
		public static Exception ReadValueAsException(BinaryReader reader)
		{
			var formatter = new BinaryFormatter();
			var exception = formatter.Deserialize(reader.BaseStream);
			return (Exception)exception;
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="reader"></param>
		/// <returns></returns>
		public object ReadObject(BinaryReader reader)
		{
			if (!ReadValueAsBoolean(reader))
			{
				return null;
			}

			var type = ReadTypeInformation(reader);
			var methods = _methodStorage.GetOrAdd(type);
			return methods.ReadObjectDelegate(reader, this, null);
		}

		#endregion

		private static ModuleBuilder CreateModule()
		{
			var assemblyName = new AssemblyName("SharpRemote.GeneratedCode.Serializer");

#if DOTNETCORE
			var access = AssemblyBuilderAccess.Run;
#else
			var access = AssemblyBuilderAccess.RunAndSave;
#endif

			var assembly = AssemblyBuilder.DefineDynamicAssembly(assemblyName, access);
			var moduleName = assemblyName.Name + ".dll";
			var module = assembly.DefineDynamicModule(moduleName);
			return module;
		}

		private static void WriteTypeInformation(BinaryWriter writer, Type type)
		{
			WriteValue(writer, type.AssemblyQualifiedName);
		}

		private Type ReadTypeInformation(BinaryReader reader)
		{
			var typeName = ReadValueAsString(reader);
			var type = _typeResolver?.GetType(typeName) ?? Type.GetType(typeName);
			return type;
		}
	}
}