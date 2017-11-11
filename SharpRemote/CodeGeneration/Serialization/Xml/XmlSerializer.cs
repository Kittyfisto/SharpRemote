using System;
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

		private readonly XmlWriterSettings _settings;
		private readonly XmlSerializationCompiler _methodCompiler;
		private readonly SerializationMethodStorage<XmlMethodsCompiler> _methodStorage;

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
		public IMethodInvocationWriter CreateMethodInvocationWriter(Stream stream, ulong rpcId, ulong grainId, string methodName, IRemotingEndPoint endPoint = null)
		{
			return new XmlMethodInvocationWriter(this, _settings, stream, grainId, methodName, rpcId);
		}

		/// <inheritdoc />
		public IMethodResultWriter CreateMethodResultWriter(Stream stream, ulong rpcId, IRemotingEndPoint endPoint = null)
		{
			return new XmlMethodResultWriter(this, _settings, stream, rpcId, endPoint);
		}

		/// <inheritdoc />
		public void CreateMethodReader(Stream stream,
		                               out IMethodInvocationReader invocationReader,
		                               out IMethodResultReader resultReader,
		                               IRemotingEndPoint endPoint = null)
		{
			var textReader = new StreamReader(stream, _settings.Encoding, true, 4096, true);
			var reader = XmlReader.Create(textReader);
			reader.MoveToContent();
			switch (reader.Name)
			{
				case XmlMethodInvocationWriter.RpcElementName:
					invocationReader = new XmlMethodInvocationReader(this, textReader, reader, _methodStorage, endPoint);
					resultReader = null;
					break;

				case XmlMethodResultWriter.RpcElementName:
					invocationReader = null;
					resultReader = new XmlMethodResultReader(this, textReader, reader, _methodStorage, endPoint);
					break;

				default:
					throw new NotImplementedException();
			}
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
			var methods = _methodStorage.GetOrAdd(value.GetType());
			methods.WriteDelegate(writer, value, this, endPoint);
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="writer"></param>
		/// <param name="value"></param>
		/// <param name="endPoint"></param>
		/// <exception cref="NotImplementedException"></exception>
		public void WriteStruct<T>(XmlWriter writer, T value, IRemotingEndPoint endPoint) where T : struct
		{
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


		private static ModuleBuilder CreateModule()
		{
			var assemblyName = new AssemblyName("SharpRemote.GeneratedCode.Serializer");
			AssemblyBuilder assembly = AppDomain.CurrentDomain.DefineDynamicAssembly(assemblyName,
			                                                                         AssemblyBuilderAccess.RunAndSave);
			string moduleName = assemblyName.Name + ".dll";
			ModuleBuilder module = assembly.DefineDynamicModule(moduleName);
			return module;
		}

	}
}