using System;
using System.Diagnostics.Contracts;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Xml;
using log4net;
using SharpRemote.CodeGeneration.Serialization;
using SharpRemote.CodeGeneration.Serialization.Xml;

// ReSharper disable once CheckNamespace
namespace SharpRemote
{
	/// <summary>
	///     A serializer implementation which writes and reads xml documents which carry method call invocations or results.
	/// </summary>
	public sealed class XmlSerializer
		: ISerializer2
	{
		private static readonly ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
		private readonly XmlSerializationCompiler _methodCompiler;
		private readonly SerializationMethodStorage<XmlMethodsCompiler> _methodStorage;

		private readonly XmlWriterSettings _settings;

		/// <summary>
		/// </summary>
		/// <param name="settings">The settings used to create xml documents</param>
		public XmlSerializer(XmlWriterSettings settings = null)
			: this(CreateModule(), settings)
		{
		}

		/// <summary>
		/// </summary>
		/// <param name="moduleBuilder"></param>
		/// <param name="settings">The settings used to create xml documents</param>
		public XmlSerializer(ModuleBuilder moduleBuilder, XmlWriterSettings settings = null)
		{
			if (moduleBuilder == null)
				throw new ArgumentNullException(nameof(moduleBuilder));

			_methodCompiler = new XmlSerializationCompiler(moduleBuilder);
			_methodStorage = new SerializationMethodStorage<XmlMethodsCompiler>("XmlSerializer", _methodCompiler);
			_settings = settings ?? new XmlWriterSettings();
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
		public IMethodCallWriter CreateMethodCallWriter(Stream stream,
		                                                ulong rpcId,
		                                                ulong grainId,
		                                                string methodName,
		                                                IRemotingEndPoint endPoint = null)
		{
			return new XmlMethodCallWriter(this, _settings, stream, grainId, methodName, rpcId);
		}

		/// <inheritdoc />
		public IMethodResultWriter CreateMethodResultWriter(Stream stream, ulong rpcId, IRemotingEndPoint endPoint = null)
		{
			return new XmlMethodResultWriter(this, _settings, stream, rpcId, endPoint);
		}

		/// <inheritdoc />
		public void CreateMethodReader(Stream stream,
		                               out IMethodCallReader callReader,
		                               out IMethodResultReader resultReader,
		                               IRemotingEndPoint endPoint = null)
		{
			var textReader = new StreamReader(stream, _settings.Encoding, detectEncodingFromByteOrderMarks: true,
			                                  bufferSize: 4096, leaveOpen: true);
			var reader = XmlReader.Create(textReader);
			reader.MoveToContent();
			switch (reader.Name)
			{
				case XmlMethodCallWriter.RpcElementName:
					callReader = new XmlMethodCallReader(this, textReader, reader, _methodStorage, endPoint);
					resultReader = null;
					break;

				case XmlMethodResultWriter.RpcElementName:
					callReader = null;
					resultReader = new XmlMethodResultReader(this, textReader, reader, _methodStorage, endPoint);
					break;

				default:
					throw new NotImplementedException();
			}
		}

		/// <summary>
		/// </summary>
		/// <param name="writer"></param>
		/// <param name="value"></param>
		/// <param name="endPoint"></param>
		/// <exception cref="NotImplementedException"></exception>
		public void WriteObject(XmlWriter writer, object value, IRemotingEndPoint endPoint)
		{
			var methods = _methodStorage.GetOrAdd(value.GetType());
			methods.WriteDelegate(writer, value, this, endPoint);
		}

		/// <summary>
		/// </summary>
		/// <param name="writer"></param>
		/// <param name="value"></param>
		public void WriteBytes(XmlWriter writer, byte[] value)
		{
			if (value != null)
			{
				writer.WriteAttributeString("Value", HexFromBytes(value));
			}
		}

		/// <summary>
		/// </summary>
		/// <param name="writer"></param>
		/// <param name="value"></param>
		public void WriteSByte(XmlWriter writer, sbyte value)
		{
			writer.WriteValue(value);
		}

		/// <summary>
		/// </summary>
		/// <param name="writer"></param>
		/// <param name="value"></param>
		public void WriteByte(XmlWriter writer, byte value)
		{
			writer.WriteValue(value);
		}

		/// <summary>
		/// </summary>
		/// <param name="writer"></param>
		/// <param name="value"></param>
		public void WriteUInt16(XmlWriter writer, ushort value)
		{
			writer.WriteValue(value);
		}

		/// <summary>
		/// </summary>
		/// <param name="writer"></param>
		/// <param name="value"></param>
		public void WriteInt16(XmlWriter writer, short value)
		{
			writer.WriteValue(value);
		}

		/// <summary>
		/// </summary>
		/// <param name="writer"></param>
		/// <param name="value"></param>
		public void WriteUInt32(XmlWriter writer, uint value)
		{
			writer.WriteValue(value);
		}

		/// <summary>
		/// </summary>
		/// <param name="writer"></param>
		/// <param name="value"></param>
		public void WriteInt32(XmlWriter writer, int value)
		{
			writer.WriteValue(value);
		}

		/// <summary>
		/// </summary>
		/// <param name="writer"></param>
		/// <param name="value"></param>
		public void WriteUInt64(XmlWriter writer, ulong value)
		{
			writer.WriteValue(value.ToString(CultureInfo.InvariantCulture));
		}

		/// <summary>
		/// </summary>
		/// <param name="writer"></param>
		/// <param name="value"></param>
		public void WriteInt64(XmlWriter writer, long value)
		{
			writer.WriteValue(value);
		}

		/// <summary>
		/// </summary>
		/// <param name="writer"></param>
		/// <param name="value"></param>
		public void WriteFloat(XmlWriter writer, float value)
		{
			writer.WriteValue(value);
		}

		/// <summary>
		/// </summary>
		/// <param name="writer"></param>
		/// <param name="value"></param>
		public void WriteDouble(XmlWriter writer, double value)
		{
			writer.WriteValue(value);
		}

		/// <summary>
		/// </summary>
		/// <param name="writer"></param>
		/// <param name="value"></param>
		public void WriteString(XmlWriter writer, string value)
		{
			writer.WriteValue(value);
		}

		private static ModuleBuilder CreateModule()
		{
			var assemblyName = new AssemblyName("SharpRemote.GeneratedCode.Serializer");
			var assembly = AppDomain.CurrentDomain.DefineDynamicAssembly(assemblyName,
			                                                             AssemblyBuilderAccess.RunAndSave);
			var moduleName = assemblyName.Name + ".dll";
			var module = assembly.DefineDynamicModule(moduleName);
			return module;
		}

		#region Hexadecimal Conversion

		private static readonly string[] LookupTable;

		static XmlSerializer()
		{
			LookupTable = new string[256];
			for (var i = 0; i < 256; i++)
			{
				LookupTable[i] = i.ToString("X2");
			}
		}

		/// <summary>
		///     Returns a byte array with the given hex-coded content
		///     or null if <paramref name="value" /> is null.
		/// </summary>
		/// <param name="value"></param>
		/// <returns></returns>
		[Pure]
		public static byte[] BytesFromHex(string value)
		{
			if (value == null)
				return null;

			if ((value.Length %2) != 0)
				throw new ArgumentException("The given array must have a multiple length of 2");

			var ret = new byte[value.Length / 2];
			for (int i = 0; i < value.Length; i += 2)
			{
				var upper = GetValue(value[i]);
				var lower = GetValue(value[i + 1]);
				ret[i/2] = (byte) (upper * 16 + lower);
			}
			return ret;
		}

		[Pure]
		private static byte GetValue(char value)
		{
			switch (value)
			{
				case '0': return 0;
				case '1': return 1;
				case '2': return 2;
				case '3': return 3;
				case '4': return 4;
				case '5': return 5;
				case '6': return 6;
				case '7': return 7;
				case '8': return 8;
				case '9': return 9;
				case 'A': return 10;
				case 'B': return 11;
				case 'C': return 12;
				case 'D': return 13;
				case 'E': return 14;
				case 'F': return 15;
				default: throw new ArgumentException(string.Format("Invalid value: '{0}'", value));
			}
		}

		/// <summary>
		///     Returns a hex-string with the given content or null
		///     if <paramref name="value" /> is null.
		/// </summary>
		/// <param name="value"></param>
		/// <returns></returns>
		[Pure]
		public static string HexFromBytes(byte[] value)
		{
			if (value == null)
				return null;

			var builder = new StringBuilder(value.Length * 2);
			for (var i = 0; i < value.Length; ++i)
				builder.Append(LookupTable[value[i]]);
			return builder.ToString();
		}

		#endregion
	}
}