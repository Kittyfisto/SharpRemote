using System;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Xml;
using log4net;
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

		private readonly ITypeResolver _customTypeResolver;
		private readonly ModuleBuilder _module;

		private readonly XmlWriterSettings _settings;
		private readonly TypeModel _typeModel;

		/// <summary>
		/// </summary>
		/// <param name="settings">The settings used to create xml documents</param>
		public XmlSerializer(XmlWriterSettings settings = null)
		{
			_settings = settings ?? new XmlWriterSettings();
			_typeModel = new TypeModel();
		}

		/// <inheritdoc />
		public void RegisterType<T>()
		{
			_typeModel.Add<T>();
		}

		/// <inheritdoc />
		public void RegisterType(Type type)
		{
			_typeModel.Add(type);
		}

		/// <inheritdoc />
		public bool IsTypeRegistered<T>()
		{
			return _typeModel.Contains<T>();
		}

		/// <inheritdoc />
		public bool IsTypeRegistered(Type type)
		{
			return _typeModel.Contains(type);
		}

		/// <inheritdoc />
		public IMethodInvocationWriter CreateMethodInvocationWriter(Stream stream, ulong rpcId, ulong grainId, string methodName, IRemotingEndPoint endPoint = null)
		{
			return new XmlMethodInvocationWriter(this, _settings, stream, grainId, methodName, rpcId);
		}

		/// <inheritdoc />
		public IMethodInvocationReader CreateMethodInvocationReader(Stream stream, IRemotingEndPoint endPoint = null)
		{
			return new XmlMethodInvocationReader(this, stream);
		}

		/// <inheritdoc />
		public IMethodResultWriter CreateMethodResultWriter(Stream stream, ulong rpcId, IRemotingEndPoint endPoint = null)
		{
			return new XmlMethodResultWriter(this, _settings, stream, rpcId, endPoint);
		}

		/// <inheritdoc />
		public IMethodResultReader CreateMethodResultReader(Stream stream, IRemotingEndPoint endPoint = null)
		{
			return new XmlMethodResultReader(this, stream);
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="writer"></param>
		/// <param name="value"></param>
		/// <param name="endPoint"></param>
		/// <exception cref="NotImplementedException"></exception>
		public void WriteObject(XmlWriter writer, object value, IRemotingEndPoint endPoint)
		{
			throw new NotImplementedException();
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="writer"></param>
		/// <param name="value"></param>
		public void WriteBytes(XmlWriter writer, byte[] value)
		{
			writer.WriteAttributeString("Type", typeof(byte).AssemblyQualifiedName);
			if (value == null)
			{
				writer.WriteAttributeString("IsNull", "True");
			}
			else
			{
				// TODO: Replace with a fast version sometime in the future...
				var stringBuilder = new StringBuilder(value.Length * 2);
				foreach (var b in value)
					stringBuilder.AppendFormat("{0:x2}", b);
				writer.WriteAttributeString("Value", stringBuilder.ToString());
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
	}
}