using System;
using System.IO;
using System.Text;
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
		public IMethodCallWriter CreateMethodCallWriter(Stream stream, ulong rpcId, ulong grainId, string methodName, IRemotingEndPoint endPoint = null)
		{
			return new BinaryMethodCallWriter(stream, grainId, methodName, rpcId);
		}

		/// <inheritdoc />
		public IMethodResultWriter CreateMethodResultWriter(Stream stream, ulong rpcId, IRemotingEndPoint endPoint = null)
		{
			return new BinaryMethodResultWriter(stream, rpcId);
		}

		/// <inheritdoc />
		public void CreateMethodReader(Stream stream,
		                               out IMethodCallReader callReader,
		                               out IMethodResultReader resultReader,
		                               IRemotingEndPoint endPoint = null)
		{
			var reader = new BinaryReader(stream, Encoding.UTF8, true);
			var type = (MessageType2)reader.ReadByte();
			switch (type)
			{
				case MessageType2.Call:
					callReader = new BinaryMethodCallReader(reader);
					resultReader = null;
					break;

				case MessageType2.Result:
					callReader = null;
					resultReader = new BinaryMethodResultReader(reader);
					break;

				default:
					throw new NotImplementedException();
			}
		}
	}
}