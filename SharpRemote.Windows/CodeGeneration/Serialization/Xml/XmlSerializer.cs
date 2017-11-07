using System.IO;
using System.Reflection.Emit;
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
		private readonly ITypeResolver _customTypeResolver;
		private readonly ModuleBuilder _module;
		private readonly TypeModel _typeModel;

		/// <inheritdoc />
		public IMethodInvocationWriter CreateMethodInvocationWriter(Stream stream, ulong grainId, string methodName, ulong rpcId)
		{
			return new XmlMethodInvocationWriter(stream, grainId, methodName, rpcId);
		}

		/// <inheritdoc />
		public IMethodInvocationReader CreateMethodInvocationReader(Stream stream)
		{
			return new XmlMethodInvocationReader(stream);
		}

		/// <inheritdoc />
		public IMethodResultWriter CreateMethodResultWriter(Stream stream, ulong rpcId)
		{
			return new XmlMethodResultWriter(stream, rpcId);
		}

		/// <inheritdoc />
		public IMethodResultReader CreateMethodResultReader(Stream stream)
		{
			return new XmlMethodResultReader(stream);
		}
	}
}