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

		private readonly XmlWriterSettings _writerSettings;
		private readonly XmlReaderSettings _readerSettings;

		/// <summary>
		/// 
		/// </summary>
		internal const string FieldElementName = "Field";

		/// <summary>
		/// 
		/// </summary>
		internal const string PropertyElementName = "Property";

		/// <summary>
		/// 
		/// </summary>
		internal const string NameAttributeName = "Name";

		/// <summary>
		/// 
		/// </summary>
		internal const string ValueAttributeName = "Value";

		/// <summary>
		/// 
		/// </summary>
		internal const string ReturnValueElementName = "ReturnValue";

		/// <summary>
		/// 
		/// </summary>
		internal const string ExceptionElementName = "Exception";

		/// <summary>
		/// </summary>
		/// <param name="writerSettings">The settings used to create xml documents</param>
		public XmlSerializer(XmlWriterSettings writerSettings = null)
			: this(CreateModule(), writerSettings)
		{
		}

		/// <summary>
		/// </summary>
		/// <param name="moduleBuilder"></param>
		/// <param name="writerSettings">The settings used to create xml documents</param>
		public XmlSerializer(ModuleBuilder moduleBuilder, XmlWriterSettings writerSettings = null)
		{
			if (moduleBuilder == null)
				throw new ArgumentNullException(nameof(moduleBuilder));

			_methodCompiler = new XmlSerializationCompiler(moduleBuilder);
			_methodStorage = new SerializationMethodStorage<XmlMethodsCompiler>("XmlSerializer", _methodCompiler);
			_writerSettings = writerSettings ?? new XmlWriterSettings
			{
				Indent = true,
				NewLineHandling = NewLineHandling.Replace,
				NewLineChars = "\n"
			};
			_readerSettings = new XmlReaderSettings
			{
				IgnoreWhitespace = true
			};
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
			return new XmlMethodCallWriter(this, _writerSettings, stream, grainId, methodName, rpcId);
		}

		/// <inheritdoc />
		public IMethodResultWriter CreateMethodResultWriter(Stream stream, ulong rpcId, IRemotingEndPoint endPoint = null)
		{
			return new XmlMethodResultWriter(this, _writerSettings, stream, rpcId, endPoint);
		}

		/// <inheritdoc />
		public void CreateMethodReader(Stream stream,
		                               out IMethodCallReader callReader,
		                               out IMethodResultReader resultReader,
		                               IRemotingEndPoint endPoint = null)
		{
			var textReader = new StreamReader(stream, _writerSettings.Encoding, detectEncodingFromByteOrderMarks: true,
			                                  bufferSize: 4096, leaveOpen: true);
			var reader = XmlReader.Create(textReader, _readerSettings);
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

		#region Writing

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
		public static void WriteValue(XmlWriter writer, sbyte value)
		{
			writer.WriteAttributeString(ValueAttributeName, value.ToString(CultureInfo.InvariantCulture));
		}

		/// <summary>
		/// </summary>
		/// <param name="writer"></param>
		/// <param name="value"></param>
		public static void WriteValue(XmlWriter writer, byte value)
		{
			writer.WriteAttributeString(ValueAttributeName, value.ToString(CultureInfo.InvariantCulture));
		}

		/// <summary>
		/// </summary>
		/// <param name="writer"></param>
		/// <param name="value"></param>
		public static void WriteValue(XmlWriter writer, ushort value)
		{
			writer.WriteAttributeString(ValueAttributeName, value.ToString(CultureInfo.InvariantCulture));
		}

		/// <summary>
		/// </summary>
		/// <param name="writer"></param>
		/// <param name="value"></param>
		public static void WriteValue(XmlWriter writer, short value)
		{
			writer.WriteAttributeString(ValueAttributeName, value.ToString(CultureInfo.InvariantCulture));
		}

		/// <summary>
		/// </summary>
		/// <param name="writer"></param>
		/// <param name="value"></param>
		public static void WriteValue(XmlWriter writer, uint value)
		{
			writer.WriteAttributeString(ValueAttributeName, value.ToString(CultureInfo.InvariantCulture));
		}

		/// <summary>
		/// </summary>
		/// <param name="writer"></param>
		/// <param name="value"></param>
		public static void WriteValue(XmlWriter writer, int value)
		{
			writer.WriteAttributeString(ValueAttributeName, value.ToString(CultureInfo.InvariantCulture));
		}

		/// <summary>
		/// </summary>
		/// <param name="writer"></param>
		/// <param name="value"></param>
		public static void WriteValue(XmlWriter writer, ulong value)
		{
			writer.WriteAttributeString(ValueAttributeName, value.ToString(CultureInfo.InvariantCulture));
		}

		/// <summary>
		/// </summary>
		/// <param name="writer"></param>
		/// <param name="value"></param>
		public static void WriteValue(XmlWriter writer, long value)
		{
			writer.WriteAttributeString(ValueAttributeName, value.ToString(CultureInfo.InvariantCulture));
		}

		/// <summary>
		/// </summary>
		/// <param name="writer"></param>
		/// <param name="value"></param>
		public static void WriteValue(XmlWriter writer, float value)
		{
			writer.WriteAttributeString(ValueAttributeName, value.ToString("R", CultureInfo.InvariantCulture));
		}

		/// <summary>
		/// </summary>
		/// <param name="writer"></param>
		/// <param name="value"></param>
		public static void WriteValue(XmlWriter writer, double value)
		{
			writer.WriteAttributeString(ValueAttributeName, value.ToString("R", CultureInfo.InvariantCulture));
		}

		/// <summary>
		/// </summary>
		/// <param name="writer"></param>
		/// <param name="value"></param>
		public static void WriteValue(XmlWriter writer, decimal value)
		{
			writer.WriteAttributeString(ValueAttributeName, value.ToString(CultureInfo.InvariantCulture));
		}

		/// <summary>
		/// </summary>
		/// <param name="writer"></param>
		/// <param name="value"></param>
		public static void WriteValue(XmlWriter writer, string value)
		{
			if (value != null)
			{
				writer.WriteAttributeString(ValueAttributeName, value);
			}
		}

		/// <summary>
		/// </summary>
		/// <param name="writer"></param>
		/// <param name="value"></param>
		public static void WriteValue(XmlWriter writer, byte[] value)
		{
			if (value != null)
			{
				writer.WriteAttributeString("Value", HexFromBytes(value));
			}
		}

		#endregion

		#region Reading

		/// <summary>
		/// 
		/// </summary>
		/// <param name="reader"></param>
		/// <returns></returns>
		public static sbyte ReadValueAsSByte(XmlReader reader)
		{
			var value = ReadValue(reader);
			return sbyte.Parse(value, CultureInfo.InvariantCulture);
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="reader"></param>
		/// <returns></returns>
		public static byte ReadValueAsByte(XmlReader reader)
		{
			var value = ReadValue(reader);
			return byte.Parse(value, CultureInfo.InvariantCulture);
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="reader"></param>
		/// <returns></returns>
		public static ushort ReadValueAsUInt16(XmlReader reader)
		{
			var value = ReadValue(reader);
			return ushort.Parse(value, CultureInfo.InvariantCulture);
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="reader"></param>
		/// <returns></returns>
		public static short ReadValueAsInt16(XmlReader reader)
		{
			var value = ReadValue(reader);
			return short.Parse(value, CultureInfo.InvariantCulture);
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="reader"></param>
		/// <returns></returns>
		public static uint ReadValueAsUInt32(XmlReader reader)
		{
			var value = ReadValue(reader);
			return uint.Parse(value, CultureInfo.InvariantCulture);
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="reader"></param>
		/// <returns></returns>
		public static int ReadValueAsInt32(XmlReader reader)
		{
			var value = ReadValue(reader);
			return int.Parse(value, CultureInfo.InvariantCulture);
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="reader"></param>
		/// <returns></returns>
		public static long ReadValueAsInt64(XmlReader reader)
		{
			var value = ReadValue(reader);
			return long.Parse(value, CultureInfo.InvariantCulture);
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="reader"></param>
		/// <returns></returns>
		public static ulong ReadValueAsUInt64(XmlReader reader)
		{
			var value = ReadValue(reader);
			return ulong.Parse(value, CultureInfo.InvariantCulture);
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="reader"></param>
		/// <returns></returns>
		public static float ReadValueAsFloat(XmlReader reader)
		{
			var value = ReadValue(reader);
			return float.Parse(value, CultureInfo.InvariantCulture);
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="reader"></param>
		/// <returns></returns>
		public static double ReadValueAsDouble(XmlReader reader)
		{
			var value = ReadValue(reader);
			return double.Parse(value, CultureInfo.InvariantCulture);
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="reader"></param>
		/// <returns></returns>
		public static decimal ReadValueAsDecimal(XmlReader reader)
		{
			var value = ReadValue(reader);
			return decimal.Parse(value, CultureInfo.InvariantCulture);
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="reader"></param>
		/// <returns></returns>
		public static string ReadValueAsString(XmlReader reader)
		{
			var value = ReadValue(reader, allowNull: true);
			return value;
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="reader"></param>
		/// <param name="allowNull"></param>
		/// <returns></returns>
		private static string ReadValue(XmlReader reader, bool allowNull = false)
		{
			var attributeCount = reader.AttributeCount;
			for (var i = 0; i < attributeCount; ++i)
			{
				reader.MoveToNextAttribute();

				switch (reader.Name)
				{
					case ValueAttributeName:
						var value = reader.Value;
						reader.MoveToElement();
						return value;
				}
			}

			if (!allowNull)
			{
				var info = (IXmlLineInfo) reader;
				throw new XmlParseException(string.Format("Expected to find attribute '{0}'",  ValueAttributeName),
				                            info.LineNumber, info.LinePosition);
			}

			return null;
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="reader"></param>
		/// <returns></returns>
		public static byte[] ReadValueAsBytes(XmlReader reader)
		{
			var value = ReadValue(reader, allowNull: true);
			return BytesFromHex(value);
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