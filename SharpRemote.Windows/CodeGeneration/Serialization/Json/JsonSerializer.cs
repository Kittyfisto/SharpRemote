using System.IO;

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
		public IMethodInvocationWriter CreateMethodInvocationWriter(Stream stream,
		                                                            ulong grainId,
		                                                            string methodName,
		                                                            ulong rpcId)
		{
			return new JsonMethodInvocationWriter(stream, grainId, methodName, rpcId);
		}

		/// <inheritdoc />
		public IMethodInvocationReader CreateMethodInvocationReader(Stream stream)
		{
			return new JsonMethodInvocationReader(stream);
		}

		/// <inheritdoc />
		public IMethodResultWriter CreateMethodResultWriter(Stream stream, ulong rpcId)
		{
			return new JsonMethodResultWriter(stream, rpcId);
		}

		/// <inheritdoc />
		public IMethodResultReader CreateMethodResultReader(Stream stream)
		{
			return new JsonMethodResultReader(stream);
		}
	}
}