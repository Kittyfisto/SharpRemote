using System;
using System.IO;
using System.Reflection.Emit;

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
			throw new NotImplementedException();
		}

		/// <inheritdoc />
		public IMethodInvocationReader CreateMethodInvocationReader(Stream stream)
		{
			throw new NotImplementedException();
		}

		/// <inheritdoc />
		public IMethodResultWriter CreateMethodResultWriter(Stream stream, ulong grainId, string methodName, ulong rpcId)
		{
			throw new NotImplementedException();
		}

		/// <inheritdoc />
		public IMethodResultReader CreateMethodResultReader(Stream stream)
		{
			throw new NotImplementedException();
		}
	}
}