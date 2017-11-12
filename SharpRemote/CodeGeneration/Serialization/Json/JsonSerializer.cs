using System;
using System.IO;
using SharpRemote.CodeGeneration.Serialization.Json;

// ReSharper disable once CheckNamespace
namespace SharpRemote
{
	/// <summary>
	///     A serializer implementation which writes and reads json documents which carry method call invocations or results.
	/// </summary>
	public sealed class JsonSerializer
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
		public IMethodCallWriter CreateMethodCallWriter(Stream stream, ulong rpcId, ulong grainId, string methodName, IRemotingEndPoint endPoint = null)
		{
			return new JsonMethodCallWriter(stream, grainId, methodName, rpcId);
		}

		/// <inheritdoc />
		public IMethodResultWriter CreateMethodResultWriter(Stream stream, ulong rpcId, IRemotingEndPoint endPoint = null)
		{
			return new JsonMethodResultWriter(stream, rpcId);
		}

		/// <inheritdoc />
		public void CreateMethodReader(Stream stream,
		                               out IMethodCallReader callReader,
		                               out IMethodResultReader resultReader,
		                               IRemotingEndPoint endPoint = null)
		{
			throw new NotImplementedException();
		}
	}
}