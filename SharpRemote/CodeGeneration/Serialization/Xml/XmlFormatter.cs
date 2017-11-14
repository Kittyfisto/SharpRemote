using System;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Xml;
using log4net;

namespace SharpRemote.CodeGeneration.Serialization.Xml
{
	/// <summary>
	///     Similar to <see cref="BinaryFormatter" />, but uses <see cref="XmlReader" /> and <see cref="XmlWriter" />
	///     instead to produce human- and machine readable output.
	/// </summary>
	internal sealed class XmlFormatter
	{
		private static readonly ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

		/// <summary>
		///     Writes the given <paramref name="exception" /> to the given <paramref name="writer" />.
		/// </summary>
		/// <param name="writer"></param>
		/// <param name="serializer"></param>
		/// <param name="exception"></param>
		public static void Write(XmlWriter writer, XmlSerializer serializer, Exception exception)
		{
			if (writer == null)
				throw new ArgumentNullException(nameof(writer));
			if (exception == null)
				throw new ArgumentNullException(nameof(exception));

			var type = exception.GetType();
			writer.WriteAttributeString(XmlSerializer.TypeAttributeName, type.AssemblyQualifiedName);
			
			var info = new SerializationInfo(type, new Formatter());
			var context = new StreamingContext(StreamingContextStates.CrossMachine |
			                                   StreamingContextStates.CrossProcess |
			                                   StreamingContextStates.CrossAppDomain);
			exception.GetObjectData(info, context);
			var it = info.GetEnumerator();
			while (it.MoveNext())
			{
				var entry = it.Current;
				var name = entry.Name;
				var value = entry.Value;
				WriteValue(writer, serializer, name, value);
			}
		}

		private static void WriteValue(XmlWriter writer, XmlSerializer serializer, string name, object value)
		{
			writer.WriteStartElement(name);
			if (value != null)
			{
				serializer.WriteObject(writer, value, null);
			}
			writer.WriteEndElement();
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="reader"></param>
		/// <param name="serializer"></param>
		/// <param name="typeResolver"></param>
		public static Exception Read(XmlReader reader, XmlSerializer serializer, ITypeResolver typeResolver)
		{
			if (reader == null)
				throw new ArgumentNullException(nameof(reader));

			int count = reader.AttributeCount;
			string exceptionType = null;
			for (int i = 0; i < count; ++i)
			{
				reader.MoveToAttribute(i);
				switch (reader.Name)
				{
					case XmlSerializer.TypeAttributeName:
						exceptionType = reader.Value;
						break;
				}
			}

			if (exceptionType == null)
			{
				return new UnserializableException();
			}

			try
			{
				var type = typeResolver.GetType(exceptionType);
				if (!typeof(Exception).IsAssignableFrom(type))
					throw new UnserializableException();

				var info = new SerializationInfo(type, new Formatter());
				int depth = reader.Depth;
				while (reader.Read() && reader.Depth >= depth)
				{
					ReadValue(reader, serializer, info);
				}
				
				var context = new StreamingContext(StreamingContextStates.CrossMachine |
				                                   StreamingContextStates.CrossProcess |
				                                   StreamingContextStates.CrossAppDomain);
				var tmp = type.GetConstructors(BindingFlags.NonPublic | BindingFlags.Instance).FirstOrDefault();
				var exception = tmp.Invoke(new object[] {info, context});
				return (Exception) exception;
			}
			catch (UnserializableException)
			{
				throw;
			}
			catch (Exception e)
			{
				Log.ErrorFormat("Caught unexpected exception while trying to deserialize exception: {0}", e);
				throw new UnserializableException();
			}
		}

		private static void ReadValue(XmlReader reader, XmlSerializer serializer, SerializationInfo info)
		{
			var name = reader.Name;
			var value = serializer.ReadObject(reader);
		}

		private sealed class Formatter
			: IFormatterConverter
		{
			public object Convert(object value, Type type)
			{
				throw new NotImplementedException();
			}

			public object Convert(object value, TypeCode typeCode)
			{
				throw new NotImplementedException();
			}

			public bool ToBoolean(object value)
			{
				throw new NotImplementedException();
			}

			public char ToChar(object value)
			{
				throw new NotImplementedException();
			}

			public sbyte ToSByte(object value)
			{
				throw new NotImplementedException();
			}

			public byte ToByte(object value)
			{
				throw new NotImplementedException();
			}

			public short ToInt16(object value)
			{
				throw new NotImplementedException();
			}

			public ushort ToUInt16(object value)
			{
				throw new NotImplementedException();
			}

			public int ToInt32(object value)
			{
				throw new NotImplementedException();
			}

			public uint ToUInt32(object value)
			{
				throw new NotImplementedException();
			}

			public long ToInt64(object value)
			{
				throw new NotImplementedException();
			}

			public ulong ToUInt64(object value)
			{
				throw new NotImplementedException();
			}

			public float ToSingle(object value)
			{
				throw new NotImplementedException();
			}

			public double ToDouble(object value)
			{
				throw new NotImplementedException();
			}

			public decimal ToDecimal(object value)
			{
				throw new NotImplementedException();
			}

			public DateTime ToDateTime(object value)
			{
				throw new NotImplementedException();
			}

			public string ToString(object value)
			{
				throw new NotImplementedException();
			}
		}
	}
}