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
		public IMethodInvocationWriter CreateMethodInvocationWriter(Stream stream,
		                                                            ulong grainId,
		                                                            string methodName,
		                                                            ulong rpcId)
		{
			return new BinaryMethodInvocationWriter(stream, grainId, methodName, rpcId);
		}

		/// <inheritdoc />
		public IMethodInvocationReader CreateMethodInvocationReader(Stream stream)
		{
			return new BinaryMethodInvocationReader(stream);
		}

		/// <inheritdoc />
		public IMethodResultWriter CreateMethodResultWriter(Stream stream, ulong rpcId)
		{
			return new BinaryMethodResultWriter(stream, rpcId);
		}

		/// <inheritdoc />
		public IMethodResultReader CreateMethodResultReader(Stream stream)
		{
			return new BinaryMethodResultReader(stream);
		}
	}
}