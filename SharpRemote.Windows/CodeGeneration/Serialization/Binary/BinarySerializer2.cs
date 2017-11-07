using System;
using System.IO;
using SharpRemote.CodeGeneration.Serialization.Binary;

// ReSharper disable once CheckNamespace
namespace SharpRemote
{
	/// <summary>
	///     Shall replace <see cref="BinarySerializer" />.
	/// </summary>
	public sealed class BinarySerializer2
		: ISerializer2
	{
		/// <inheritdoc />
		public void RegisterType<T>()
		{
			throw new NotImplementedException();
		}

		/// <inheritdoc />
		public void RegisterType(Type type)
		{
			throw new NotImplementedException();
		}

		/// <inheritdoc />
		public bool IsTypeRegistered<T>()
		{
			throw new NotImplementedException();
		}

		/// <inheritdoc />
		public bool IsTypeRegistered(Type type)
		{
			throw new NotImplementedException();
		}

		/// <inheritdoc />
		public IMethodInvocationWriter CreateMethodInvocationWriter(Stream stream,
		                                                            ulong grainId,
		                                                            string methodName,
		                                                            ulong rpcId,
		                                                            IRemotingEndPoint endPoint = null)
		{
			return new BinaryMethodInvocationWriter(stream, grainId, methodName, rpcId);
		}

		/// <inheritdoc />
		public IMethodInvocationReader CreateMethodInvocationReader(Stream stream,
		                                                            IRemotingEndPoint endPoint = null)
		{
			return new BinaryMethodInvocationReader(stream);
		}

		/// <inheritdoc />
		public IMethodResultWriter CreateMethodResultWriter(Stream stream, ulong rpcId, IRemotingEndPoint endPoint = null)
		{
			return new BinaryMethodResultWriter(stream, rpcId);
		}

		/// <inheritdoc />
		public IMethodResultReader CreateMethodResultReader(Stream stream, IRemotingEndPoint endPoint = null)
		{
			return new BinaryMethodResultReader(stream);
		}
	}
}