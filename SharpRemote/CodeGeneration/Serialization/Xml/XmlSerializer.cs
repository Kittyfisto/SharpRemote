using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.Serialization;
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
		private static readonly IReadOnlyDictionary<Type, string> BuiltInTypeToTypeName;
		private static readonly IReadOnlyDictionary<string, Type> TypeNameTobuiltInType;

		private readonly XmlSerializationCompiler _methodCompiler;
		private readonly SerializationMethodStorage<XmlMethodsCompiler> _methodStorage;

		private readonly ITypeResolver _typeResolver;
		private readonly XmlWriterSettings _writerSettings;
		private readonly XmlReaderSettings _readerSettings;

		/// <summary>
		/// Name of the XML element which represents a method call.
		/// </summary>
		internal const string MethodCallElementName = "Call";

		/// <summary>
		/// Name of the XML element which represents a method result.
		/// </summary>
		internal const string MethodResultElementName = "Result";

		/// <summary>
		/// Name of the XML attribute which carries the id of the remote procedure call.
		/// </summary>
		internal const string RpcIdAttributeName = "ID";

		/// <summary>
		/// Name of the XML attribute which carries the id of the grain (proxy/servant).
		/// </summary>
		internal const string GrainIdAttributeName = "Grain";

		/// <summary>
		/// 
		/// </summary>
		internal const string MethodAttributeName = "Method";

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
		/// Name of the XML element (or attribute) which holds the return value or that of an argument.
		/// The value of built-in types is stored in an attribute and stored as child-elements in case for any
		/// other <see cref="DataContractAttribute"/> types.
		/// </summary>
		internal const string ValueName = "Value";

		/// <summary>
		/// 
		/// </summary>
		internal const string ArgumentElementName = "Argument";

		/// <summary>
		/// 
		/// </summary>
		internal const string ReturnValueElementName = "ReturnValue";

		/// <summary>
		/// 
		/// </summary>
		internal const string ExceptionElementName = "Exception";

		/// <summary>
		///     The name of the attribute to hold the .NET type of a value, often
		///     the <see cref="Type.AssemblyQualifiedName" /> unless it happens to be a built-in
		///     type for which shorter names are used.
		/// </summary>
		internal const string TypeAttributeName = "Type";

		static XmlSerializer()
		{
			var forward = new Dictionary<Type, string>();
			var reverse = new Dictionary<string, Type>();

			AddBuiltInType<byte>("Byte", forward, reverse);
			AddBuiltInType<sbyte>("SByte", forward, reverse);
			AddBuiltInType<ushort>("UInt16", forward, reverse);
			AddBuiltInType<short>("Int16", forward, reverse);
			AddBuiltInType<uint>("UInt32", forward, reverse);
			AddBuiltInType<int>("Int32", forward, reverse);
			AddBuiltInType<ulong>("UInt64", forward, reverse);
			AddBuiltInType<long>("Int64", forward, reverse);
			AddBuiltInType<float>("Single", forward, reverse);
			AddBuiltInType<double>("Double", forward, reverse);
			AddBuiltInType<string>("String", forward, reverse);
			AddBuiltInType<decimal>("Decimal", forward, reverse);

			BuiltInTypeToTypeName = forward;
			TypeNameTobuiltInType = reverse;

			LookupTable = new string[256];
			for (var i = 0; i < 256; i++)
			{
				LookupTable[i] = i.ToString("X2");
			}
		}

		private static void AddBuiltInType<T>(string name, Dictionary<Type, string> builtInTypeToTypeName, Dictionary<string, Type> typeNameTobuiltInType)
		{
			builtInTypeToTypeName.Add(typeof(T), name);
			typeNameTobuiltInType.Add(name, typeof(T));
		}

		/// <summary>
		/// </summary>
		/// <param name="typeResolver"></param>
		/// <param name="writerSettings">The settings used to create xml documents</param>
		public XmlSerializer(ITypeResolver typeResolver = null, XmlWriterSettings writerSettings = null)
			: this(CreateModule(), typeResolver, writerSettings)
		{
		}

		/// <summary>
		/// </summary>
		/// <param name="moduleBuilder"></param>
		/// <param name="typeResolver"></param>
		/// <param name="writerSettings">The settings used to create xml documents</param>
		public XmlSerializer(ModuleBuilder moduleBuilder, ITypeResolver typeResolver = null, XmlWriterSettings writerSettings = null)
		{
			if (moduleBuilder == null)
				throw new ArgumentNullException(nameof(moduleBuilder));

			_typeResolver = typeResolver;
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
				case MethodCallElementName:
					callReader = new XmlMethodCallReader(this, textReader, reader, _methodStorage, endPoint);
					resultReader = null;
					break;

				case MethodResultElementName:
					callReader = null;
					resultReader = new XmlMethodResultReader(this, textReader, reader, _methodStorage, endPoint);
					break;

				default:
					throw new NotImplementedException();
			}
		}

		#region Writing

		/// <summary>
		/// 
		/// </summary>
		/// <param name="writer"></param>
		/// <param name="value"></param>
		/// <param name="endPoint"></param>
		public void WriteObject(XmlWriter writer, object value, IRemotingEndPoint endPoint)
		{
			if (value != null)
			{
				writer.WriteAttributeString(TypeAttributeName, value.GetType().AssemblyQualifiedName);
				writer.WriteStartElement(ValueName);
				WriteObjectNotNull(writer, value, endPoint);
				writer.WriteEndElement();
			}
		}

		/// <summary>
		/// </summary>
		/// <param name="writer"></param>
		/// <param name="value"></param>
		/// <param name="endPoint"></param>
		/// <exception cref="NotImplementedException"></exception>
		public void WriteObjectNotNull(XmlWriter writer, object value, IRemotingEndPoint endPoint)
		{
			// TODO: Built-in types should not be denoted using their full assembly qualified name but instead
			//       by using custom names, such as "UInt16", "SByte" or "String" for short.
			writer.WriteAttributeString(TypeAttributeName, GetTypeName(value.GetType()));
			var methods = _methodStorage.GetOrAdd(value.GetType());
			methods.WriteDelegate(writer, value, this, endPoint);
		}

		/// <summary>
		///     Returns the typename for the given type.
		/// </summary>
		/// <remarks>
		///     Exists because this serializer reserves very short names for built-in types
		///     so they are easier to read and take up less space in a message.
		/// </remarks>
		[Pure]
		internal static string GetTypeName(Type type)
		{
			string typeName;
			if (BuiltInTypeToTypeName.TryGetValue(type, out typeName))
			{
				return typeName;
			}

			return type.AssemblyQualifiedName;
		}

		/// <summary>
		/// </summary>
		/// <param name="writer"></param>
		/// <param name="value"></param>
		public static void WriteValue(XmlWriter writer, sbyte value)
		{
			writer.WriteAttributeString(ValueName, value.ToString(CultureInfo.InvariantCulture));
		}

		/// <summary>
		/// </summary>
		/// <param name="writer"></param>
		/// <param name="value"></param>
		public static void WriteValue(XmlWriter writer, byte value)
		{
			writer.WriteAttributeString(ValueName, value.ToString(CultureInfo.InvariantCulture));
		}

		/// <summary>
		/// </summary>
		/// <param name="writer"></param>
		/// <param name="value"></param>
		public static void WriteValue(XmlWriter writer, ushort value)
		{
			writer.WriteAttributeString(ValueName, value.ToString(CultureInfo.InvariantCulture));
		}

		/// <summary>
		/// </summary>
		/// <param name="writer"></param>
		/// <param name="value"></param>
		public static void WriteValue(XmlWriter writer, short value)
		{
			writer.WriteAttributeString(ValueName, value.ToString(CultureInfo.InvariantCulture));
		}

		/// <summary>
		/// </summary>
		/// <param name="writer"></param>
		/// <param name="value"></param>
		public static void WriteValue(XmlWriter writer, uint value)
		{
			writer.WriteAttributeString(ValueName, value.ToString(CultureInfo.InvariantCulture));
		}

		/// <summary>
		/// </summary>
		/// <param name="writer"></param>
		/// <param name="value"></param>
		public static void WriteValue(XmlWriter writer, int value)
		{
			writer.WriteAttributeString(ValueName, value.ToString(CultureInfo.InvariantCulture));
		}

		/// <summary>
		/// </summary>
		/// <param name="writer"></param>
		/// <param name="value"></param>
		public static void WriteValue(XmlWriter writer, ulong value)
		{
			writer.WriteAttributeString(ValueName, value.ToString(CultureInfo.InvariantCulture));
		}

		/// <summary>
		/// </summary>
		/// <param name="writer"></param>
		/// <param name="value"></param>
		public static void WriteValue(XmlWriter writer, long value)
		{
			writer.WriteAttributeString(ValueName, value.ToString(CultureInfo.InvariantCulture));
		}

		/// <summary>
		/// </summary>
		/// <param name="writer"></param>
		/// <param name="value"></param>
		public static void WriteValue(XmlWriter writer, float value)
		{
			writer.WriteAttributeString(ValueName, value.ToString("R", CultureInfo.InvariantCulture));
		}

		/// <summary>
		/// </summary>
		/// <param name="writer"></param>
		/// <param name="value"></param>
		public static void WriteValue(XmlWriter writer, double value)
		{
			writer.WriteAttributeString(ValueName, value.ToString("R", CultureInfo.InvariantCulture));
		}

		/// <summary>
		/// </summary>
		/// <param name="writer"></param>
		/// <param name="value"></param>
		public static void WriteValue(XmlWriter writer, decimal value)
		{
			writer.WriteAttributeString(ValueName, value.ToString(CultureInfo.InvariantCulture));
		}

		/// <summary>
		/// </summary>
		/// <param name="writer"></param>
		/// <param name="value"></param>
		public static void WriteValue(XmlWriter writer, DateTime value)
		{
			writer.WriteAttributeString(ValueName, value.ToString("o", CultureInfo.InvariantCulture));
		}

		/// <summary>
		/// </summary>
		/// <param name="writer"></param>
		/// <param name="value"></param>
		public static void WriteValue(XmlWriter writer, string value)
		{
			if (value != null)
			{
				writer.WriteAttributeString(ValueName, value);
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
		
		/// <summary>
		///     Writes the given <paramref name="exception" /> to the given <paramref name="writer" />.
		/// </summary>
		/// <param name="writer"></param>
		/// <param name="serializer"></param>
		/// <param name="exception"></param>
		public static void WriteException(XmlWriter writer, XmlSerializer serializer, Exception exception)
		{
			if (writer == null)
				throw new ArgumentNullException(nameof(writer));
			if (exception == null)
				throw new ArgumentNullException(nameof(exception));

			var type = exception.GetType();
			var info = new SerializationInfo(type, new XmlFormatterConverter());
			var context = new StreamingContext(StreamingContextStates.CrossMachine |
			                                   StreamingContextStates.CrossProcess |
			                                   StreamingContextStates.CrossAppDomain);
			exception.GetObjectData(info, context);

			writer.WriteStartElement(ValueName);

			var it = info.GetEnumerator();
			while (it.MoveNext())
			{
				var entry = it.Current;
				var name = entry.Name;
				var value = entry.Value;
				WriteValue(writer, serializer, name, value);
			}

			writer.WriteEndElement();
		}

		private static void WriteValue(XmlWriter writer, XmlSerializer serializer, string name, object value)
		{
			writer.WriteStartElement(name);
			if (value != null)
			{
				serializer.WriteObjectNotNull(writer, value, null);
			}
			writer.WriteEndElement();
		}

		#endregion

		#region Reading

		/// <summary>
		/// 
		/// </summary>
		/// <param name="reader"></param>
		/// <returns></returns>
		public object ReadObject(XmlReader reader)
		{
			var attributeCount = reader.AttributeCount;
			string typeName = null;
			string value = null;
			for (var i = 0; i < attributeCount; ++i)
			{
				reader.MoveToNextAttribute();

				switch (reader.Name)
				{
					case TypeAttributeName:
						typeName = reader.Value;
						break;
					
					case ValueName:
						value = reader.Value;
						break;
				}
			}

			if (typeName == null)
				return null;
			
			var type = ResolveTypeName(typeName);
			if (value != null)
			{
				return ReadValue(type, value);
			}

			reader.MoveToElement();
			var content = reader.ReadSubtree();
			while (content.Read())
			{
				if (content.Name == ValueName)
				{
					// It might not have the value embedded in an attribute, so
					// it's probably in the value element.
					var methods = _methodStorage.GetOrAdd(type);
					return methods.ReadObjectDelegate(reader, this, null);
				}
			}

			return null;
		}

		/// <summary>
		///     Tries to resolve the given type-name to a .NET <see cref="Type" />.
		/// </summary>
		/// <param name="typeName"></param>
		/// <returns></returns>
		[Pure]
		private Type ResolveTypeName(string typeName)
		{
			Type type;
			if (TypeNameTobuiltInType.TryGetValue(typeName, out type))
				return type;

			type = _typeResolver?.GetType(typeName) ?? Type.GetType(typeName);
			return type;
		}

		private static object ReadValue(Type type, string value)
		{
			if (type == typeof(string))
				return value;
			if (type == typeof(byte))
				return ParseByte(value);
			if (type == typeof(sbyte))
				return ParseSByte(value);
			if (type == typeof(ushort))
				return ParseUInt16(value);
			if (type == typeof(short))
				return ParseInt16(value);
			if (type == typeof(uint))
				return ParseUInt32(value);
			if (type == typeof(int))
				return ParseInt32(value);
			if (type == typeof(ulong))
				return ParseUInt64(value);
			if (type == typeof(long))
				return ParseInt64(value);
			if (type == typeof(float))
				return ParseSingle(value);
			if (type == typeof(double))
				return ParseDouble(value);
			if (type == typeof(decimal))
				return ParseDecimal(value);
			throw new NotImplementedException();
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="reader"></param>
		/// <returns></returns>
		public static sbyte ReadValueAsSByte(XmlReader reader)
		{
			var value = ReadValue(reader);
			return ParseSByte(value);
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="reader"></param>
		/// <returns></returns>
		public static byte ReadValueAsByte(XmlReader reader)
		{
			var value = ReadValue(reader);
			return ParseByte(value);
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="reader"></param>
		/// <returns></returns>
		public static ushort ReadValueAsUInt16(XmlReader reader)
		{
			var value = ReadValue(reader);
			return ParseUInt16(value);
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="reader"></param>
		/// <returns></returns>
		public static short ReadValueAsInt16(XmlReader reader)
		{
			var value = ReadValue(reader);
			return ParseInt16(value);
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="reader"></param>
		/// <returns></returns>
		public static uint ReadValueAsUInt32(XmlReader reader)
		{
			var value = ReadValue(reader);
			return ParseUInt32(value);
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="reader"></param>
		/// <returns></returns>
		public static int ReadValueAsInt32(XmlReader reader)
		{
			var value = ReadValue(reader);
			return ParseInt32(value);
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="reader"></param>
		/// <returns></returns>
		public static long ReadValueAsInt64(XmlReader reader)
		{
			var value = ReadValue(reader);
			return ParseInt64(value);
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="reader"></param>
		/// <returns></returns>
		public static ulong ReadValueAsUInt64(XmlReader reader)
		{
			var value = ReadValue(reader);
			return ParseUInt64(value);
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="reader"></param>
		/// <returns></returns>
		public static float ReadValueAsSingle(XmlReader reader)
		{
			var value = ReadValue(reader);
			return ParseSingle(value);
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="reader"></param>
		/// <returns></returns>
		public static double ReadValueAsDouble(XmlReader reader)
		{
			var value = ReadValue(reader);
			return ParseDouble(value);
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="reader"></param>
		/// <returns></returns>
		public static decimal ReadValueAsDecimal(XmlReader reader)
		{
			var value = ReadValue(reader);
			return ParseDecimal(value);
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="reader"></param>
		/// <returns></returns>
		public static DateTime ReadValueAsDateTime(XmlReader reader)
		{
			var value = ReadValue(reader);
			return ParseDateTime(value);
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
					case ValueName:
						var value = reader.Value;
						reader.MoveToElement();
						return value;
				}
			}

			if (!allowNull)
			{
				var info = (IXmlLineInfo) reader;
				throw new XmlParseException(string.Format("Expected to find attribute '{0}'",  ValueName),
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

		/// <summary>
		/// 
		/// </summary>
		/// <remarks>
		/// TODO: Lookup exception in generated code only once, makes it easier!
		/// </remarks>
		/// <param name="exceptionType"></param>
		/// <param name="reader"></param>
		/// <param name="serializer"></param>
		public static Exception ReadException(Type exceptionType, XmlReader reader, XmlSerializer serializer)
		{
			if (reader == null)
				throw new ArgumentNullException(nameof(reader));

			try
			{
				if (!typeof(Exception).IsAssignableFrom(exceptionType))
					throw new UnserializableException(string.Format("Unable to find type '{0}'", exceptionType));

				var info = new SerializationInfo(exceptionType, new XmlFormatterConverter());
				int depth = reader.Depth;
				while (reader.Read() && reader.Depth >= depth)
				{
					ReadValue(reader, serializer, info);
				}

				var context = new StreamingContext(StreamingContextStates.CrossMachine |
				                                   StreamingContextStates.CrossProcess |
				                                   StreamingContextStates.CrossAppDomain);
				var tmp = GetConstructor(exceptionType);
				if (tmp == null)
					throw new UnserializableException(string.Format("The type '{0}' is missing a deserialization constructor", exceptionType));

				var exception = tmp.Invoke(new object[] {info, context});
				return (Exception) exception;
			}
			catch (UnserializableException)
			{
				throw;
			}
			catch (Exception e)
			{
				var message = string.Format("Caught unexpected exception while trying to deserialize an exception: {0}", e);
				Log.ErrorFormat(message);
				throw new UnserializableException(message, e);
			}
		}

		private static ConstructorInfo GetConstructor(Type type)
		{
			var ctor = type.GetConstructor(new[]
			{
				typeof(SerializationInfo),
				typeof(StreamingContext)
			});
			if (ctor != null)
				return ctor;

			ctor = type.GetConstructors(BindingFlags.NonPublic | BindingFlags.Instance).FirstOrDefault();
			return ctor;
		}

		private static void ReadValue(XmlReader reader, XmlSerializer serializer, SerializationInfo info)
		{
			var name = reader.Name;
			var value = serializer.ReadObject(reader);
			info.AddValue(name, value);
		}

		#endregion

		#region Parsing

		[Pure]
		private static sbyte ParseSByte(string value)
		{
			return sbyte.Parse(value, CultureInfo.InvariantCulture);
		}

		[Pure]
		private static byte ParseByte(string value)
		{
			return byte.Parse(value, CultureInfo.InvariantCulture);
		}

		[Pure]
		private static ushort ParseUInt16(string value)
		{
			return ushort.Parse(value, CultureInfo.InvariantCulture);
		}

		private static short ParseInt16(string value)
		{
			return short.Parse(value, CultureInfo.InvariantCulture);
		}

		[Pure]
		private static uint ParseUInt32(string value)
		{
			return uint.Parse(value, CultureInfo.InvariantCulture);
		}

		[Pure]
		private static int ParseInt32(string value)
		{
			return int.Parse(value, CultureInfo.InvariantCulture);
		}

		[Pure]
		private static ulong ParseUInt64(string value)
		{
			return ulong.Parse(value, CultureInfo.InvariantCulture);
		}

		[Pure]
		private static long ParseInt64(string value)
		{
			return long.Parse(value, CultureInfo.InvariantCulture);
		}

		[Pure]
		private static float ParseSingle(string value)
		{
			return float.Parse(value, CultureInfo.InvariantCulture);
		}

		[Pure]
		private static double ParseDouble(string value)
		{
			return double.Parse(value, CultureInfo.InvariantCulture);
		}

		[Pure]
		private static decimal ParseDecimal(string value)
		{
			return decimal.Parse(value, CultureInfo.InvariantCulture);
		}

		[Pure]
		private static DateTime ParseDateTime(string value)
		{
			return DateTime.ParseExact(value, "O", CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind);
		}

		#endregion

		private static ModuleBuilder CreateModule()
		{
			var assemblyName = new AssemblyName("SharpRemote.GeneratedCode.Serializer");

#if NET6_0
			var access = AssemblyBuilderAccess.Run;
#else
			var access = AssemblyBuilderAccess.RunAndSave;
#endif

			var assembly = AssemblyBuilder.DefineDynamicAssembly(assemblyName, access);
			var moduleName = assemblyName.Name + ".dll";
			var module = assembly.DefineDynamicModule(moduleName);
			return module;
		}

		#region Hexadecimal Conversion

		private static readonly string[] LookupTable;

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