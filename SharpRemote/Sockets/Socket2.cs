using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;

namespace SharpRemote.Sockets
{
	/// <summary>
	///     Proxy around a <see cref="Socket" /> object.
	/// </summary>
	/// <remarks>
	///     This should be part of the System.Extensions project and be publicly available.
	/// </remarks>
	internal sealed class Socket2
		: ISocket
	{
		private readonly Socket _socket;

		/// <summary>
		///     Initializes this object.
		/// </summary>
		/// <param name="socket"></param>
		public Socket2(Socket socket)
		{
			if (socket == null)
				throw new ArgumentNullException(nameof(socket));

			_socket = socket;
		}

		/// <summary>
		///     Initializes this object.
		/// </summary>
		/// <param name="addressFamily"></param>
		/// <param name="socketType"></param>
		/// <param name="protocolType"></param>
		public Socket2(AddressFamily addressFamily, SocketType socketType, ProtocolType protocolType)
		{
			_socket = new Socket(addressFamily, socketType, protocolType);
		}

		/// <summary>
		///     Initializes this object.
		/// </summary>
		/// <param name="socketInformation"></param>
		public Socket2(SocketInformation socketInformation)
		{
			_socket = new Socket(socketInformation);
		}

		/// <summary>
		///     Initializes this object.
		/// </summary>
		/// <param name="socketType"></param>
		/// <param name="protocolType"></param>
		public Socket2(SocketType socketType, ProtocolType protocolType)
		{
			_socket = new Socket(socketType, protocolType);
		}

		/// <inheritdoc />
		public AddressFamily AddressFamily => _socket.AddressFamily;

		/// <inheritdoc />
		public int Available => _socket.Available;

		/// <inheritdoc />
		public bool Blocking
		{
			get { return _socket.Blocking; }
			set { _socket.Blocking = value; }
		}

		/// <inheritdoc />
		public bool Connected => _socket.Connected;

		/// <inheritdoc />
		public bool DontFragment
		{
			get { return _socket.DontFragment; }
			set { _socket.DontFragment = value; }
		}

		/// <inheritdoc />
		public bool DualMode
		{
			get { return _socket.DualMode; }
			set { _socket.DualMode = value; }
		}

		/// <inheritdoc />
		public bool EnableBroadcast
		{
			get { return _socket.EnableBroadcast; }
			set { _socket.EnableBroadcast = value; }
		}

		/// <inheritdoc />
		public bool ExclusiveAddressUse
		{
			get { return _socket.ExclusiveAddressUse; }
			set { _socket.ExclusiveAddressUse = value; }
		}

		/// <inheritdoc />
		public IntPtr Handle => _socket.Handle;

		/// <inheritdoc />
		public bool IsBound => _socket.IsBound;

		/// <inheritdoc />
		public LingerOption LingerState
		{
			get { return _socket.LingerState; }
			set { _socket.LingerState = value; }
		}

		/// <inheritdoc />
		public EndPoint LocalEndPoint => _socket.LocalEndPoint;

		/// <inheritdoc />
		public bool MulticastLoopback
		{
			get { return _socket.MulticastLoopback; }
			set { _socket.MulticastLoopback = value; }
		}

		/// <inheritdoc />
		public bool NoDelay
		{
			get { return _socket.NoDelay; }
			set { _socket.NoDelay = value; }
		}

		/// <inheritdoc />
		public ProtocolType ProtocolType => _socket.ProtocolType;

		/// <inheritdoc />
		public int ReceiveBufferSize
		{
			get { return _socket.ReceiveBufferSize; }
			set { _socket.ReceiveBufferSize = value; }
		}

		/// <inheritdoc />
		public int ReceiveTimeout
		{
			get { return _socket.ReceiveTimeout; }
			set { _socket.ReceiveTimeout = value; }
		}

		/// <inheritdoc />
		public EndPoint RemoteEndPoint => _socket.RemoteEndPoint;

		/// <inheritdoc />
		public int SendBufferSize
		{
			get { return _socket.SendBufferSize; }
			set { _socket.SendBufferSize = value; }
		}

		/// <inheritdoc />
		public int SendTimeout
		{
			get { return _socket.SendTimeout; }
			set { _socket.SendTimeout = value; }
		}

		/// <inheritdoc />
		public SocketType SocketType => _socket.SocketType;

		/// <inheritdoc />
		public short Ttl
		{
			get { return _socket.Ttl; }
			set { _socket.Ttl = value; }
		}

		/// <inheritdoc />
		public bool UseOnlyOverlappedIO
		{
			get { return _socket.UseOnlyOverlappedIO; }
			set { _socket.UseOnlyOverlappedIO = value; }
		}

		/// <inheritdoc />
		public ISocket Accept()
		{
			return new Socket2(_socket.Accept());
		}

		/// <inheritdoc />
		public bool AcceptAsync(SocketAsyncEventArgs e)
		{
			return _socket.AcceptAsync(e);
		}

		/// <inheritdoc />
		public IAsyncResult BeginAccept(AsyncCallback callback, object state)
		{
			return _socket.BeginAccept(callback, state);
		}

		/// <inheritdoc />
		public IAsyncResult BeginAccept(int receiveSize, AsyncCallback callback, object state)
		{
			return _socket.BeginAccept(receiveSize, callback, state);
		}

		/// <inheritdoc />
		public IAsyncResult BeginAccept(Socket acceptSocket, int receiveSize, AsyncCallback callback, object state)
		{
			return _socket.BeginAccept(acceptSocket, receiveSize, callback, state);
		}

		/// <inheritdoc />
		public IAsyncResult BeginConnect(EndPoint remoteEP, AsyncCallback callback, object state)
		{
			return _socket.BeginConnect(remoteEP, callback, state);
		}

		/// <inheritdoc />
		public IAsyncResult BeginConnect(IPAddress[] addresses, int port, AsyncCallback requestCallback, object state)
		{
			return _socket.BeginConnect(addresses, port, requestCallback, state);
		}

		/// <inheritdoc />
		public IAsyncResult BeginConnect(IPAddress address, int port, AsyncCallback requestCallback, object state)
		{
			return _socket.BeginConnect(address, port, requestCallback, state);
		}

		/// <inheritdoc />
		public IAsyncResult BeginConnect(string host, int port, AsyncCallback requestCallback, object state)
		{
			return _socket.BeginConnect(host, port, requestCallback, state);
		}

		/// <inheritdoc />
		public IAsyncResult BeginDisconnect(bool reuseSocket, AsyncCallback callback, object state)
		{
			return _socket.BeginDisconnect(reuseSocket, callback, state);
		}

		/// <inheritdoc />
		public IAsyncResult BeginReceive(IList<ArraySegment<byte>> buffers,
		                                 SocketFlags socketFlags,
		                                 AsyncCallback callback,
		                                 object state)
		{
			return _socket.BeginReceive(buffers, socketFlags, callback, state);
		}

		/// <inheritdoc />
		public IAsyncResult BeginReceive(IList<ArraySegment<byte>> buffers,
		                                 SocketFlags socketFlags,
		                                 out SocketError errorCode,
		                                 AsyncCallback callback,
		                                 object state)
		{
			return _socket.BeginReceive(buffers, socketFlags, out errorCode, callback, state);
		}

		/// <inheritdoc />
		public IAsyncResult BeginReceive(byte[] buffer,
		                                 int offset,
		                                 int size,
		                                 SocketFlags socketFlags,
		                                 AsyncCallback callback,
		                                 object state)
		{
			return _socket.BeginReceive(buffer, offset, size, socketFlags, callback, state);
		}

		/// <inheritdoc />
		public IAsyncResult BeginReceive(byte[] buffer,
		                                 int offset,
		                                 int size,
		                                 SocketFlags socketFlags,
		                                 out SocketError errorCode,
		                                 AsyncCallback callback,
		                                 object state)
		{
			return _socket.BeginReceive(buffer, offset, size, socketFlags, out errorCode, callback, state);
		}

		/// <inheritdoc />
		public IAsyncResult BeginReceiveFrom(byte[] buffer,
		                                     int offset,
		                                     int size,
		                                     SocketFlags socketFlags,
		                                     ref EndPoint remoteEP,
		                                     AsyncCallback callback,
		                                     object state)
		{
			return _socket.BeginReceiveFrom(buffer, offset, size, socketFlags, ref remoteEP, callback, state);
		}

		/// <inheritdoc />
		public IAsyncResult BeginReceiveMessageFrom(byte[] buffer,
		                                            int offset,
		                                            int size,
		                                            SocketFlags socketFlags,
		                                            ref EndPoint remoteEP,
		                                            AsyncCallback callback,
		                                            object state)
		{
			return _socket.BeginReceiveMessageFrom(buffer, offset, size, socketFlags, ref remoteEP, callback, state);
		}

		/// <inheritdoc />
		public IAsyncResult BeginSend(IList<ArraySegment<byte>> buffers,
		                              SocketFlags socketFlags,
		                              AsyncCallback callback,
		                              object state)
		{
			return _socket.BeginSend(buffers, socketFlags, callback, state);
		}

		/// <inheritdoc />
		public IAsyncResult BeginSend(IList<ArraySegment<byte>> buffers,
		                              SocketFlags socketFlags,
		                              out SocketError errorCode,
		                              AsyncCallback callback,
		                              object state)
		{
			return _socket.BeginSend(buffers, socketFlags, out errorCode, callback, state);
		}

		/// <inheritdoc />
		public IAsyncResult BeginSend(byte[] buffer,
		                              int offset,
		                              int size,
		                              SocketFlags socketFlags,
		                              AsyncCallback callback,
		                              object state)
		{
			return _socket.BeginSend(buffer, offset, size, socketFlags, callback, state);
		}

		/// <inheritdoc />
		public IAsyncResult BeginSend(byte[] buffer,
		                              int offset,
		                              int size,
		                              SocketFlags socketFlags,
		                              out SocketError errorCode,
		                              AsyncCallback callback,
		                              object state)
		{
			return _socket.BeginSend(buffer, offset, size, socketFlags, out errorCode, callback, state);
		}

		/// <inheritdoc />
		public IAsyncResult BeginSendFile(string fileName, AsyncCallback callback, object state)
		{
			return _socket.BeginSendFile(fileName, callback, state);
		}

		/// <inheritdoc />
		public IAsyncResult BeginSendFile(string fileName,
		                                  byte[] preBuffer,
		                                  byte[] postBuffer,
		                                  TransmitFileOptions flags,
		                                  AsyncCallback callback,
		                                  object state)
		{
			return _socket.BeginSendFile(fileName, preBuffer, postBuffer, flags, callback, state);
		}

		/// <inheritdoc />
		public IAsyncResult BeginSendTo(byte[] buffer,
		                                int offset,
		                                int size,
		                                SocketFlags socketFlags,
		                                EndPoint remoteEP,
		                                AsyncCallback callback,
		                                object state)
		{
			return _socket.BeginSendTo(buffer, offset, size, socketFlags, remoteEP, callback, state);
		}

		/// <inheritdoc />
		public void Bind(EndPoint localEP)
		{
			_socket.Bind(localEP);
		}

		/// <inheritdoc />
		public void Close()
		{
			_socket.Close();
		}

		/// <inheritdoc />
		public void Close(int timeout)
		{
			_socket.Close(timeout);
		}

		/// <inheritdoc />
		public void Connect(EndPoint remoteEP)
		{
			_socket.Connect(remoteEP);
		}

		/// <inheritdoc />
		public void Connect(IPAddress address, int port)
		{
			_socket.Connect(address, port);
		}

		/// <inheritdoc />
		public void Connect(string host, int port)
		{
			_socket.Connect(host, port);
		}

		/// <inheritdoc />
		public void Connect(IPAddress[] addresses, int port)
		{
			_socket.Connect(addresses, port);
		}

		/// <inheritdoc />
		public bool ConnectAsync(SocketAsyncEventArgs e)
		{
			return _socket.ConnectAsync(e);
		}

		/// <inheritdoc />
		public void Disconnect(bool reuseSocket)
		{
			_socket.Disconnect(reuseSocket);
		}

		/// <inheritdoc />
		public bool DisconnectAsync(SocketAsyncEventArgs e)
		{
			return _socket.DisconnectAsync(e);
		}

		/// <inheritdoc />
		public void Dispose()
		{
			_socket.Dispose();
		}

		/// <inheritdoc />
		public SocketInformation DuplicateAndClose(int targetProcessId)
		{
			return _socket.DuplicateAndClose(targetProcessId);
		}

		/// <inheritdoc />
		public ISocket EndAccept(IAsyncResult asyncResult)
		{
			return new Socket2(_socket.EndAccept(asyncResult));
		}

		/// <inheritdoc />
		public ISocket EndAccept(out byte[] buffer, IAsyncResult asyncResult)
		{
			return new Socket2(_socket.EndAccept(out buffer, asyncResult));
		}

		/// <inheritdoc />
		public ISocket EndAccept(out byte[] buffer, out int bytesTransferred, IAsyncResult asyncResult)
		{
			return new Socket2(_socket.EndAccept(out buffer, out bytesTransferred, asyncResult));
		}

		/// <inheritdoc />
		public void EndConnect(IAsyncResult asyncResult)
		{
			_socket.EndConnect(asyncResult);
		}

		/// <inheritdoc />
		public void EndDisconnect(IAsyncResult asyncResult)
		{
			_socket.EndDisconnect(asyncResult);
		}

		/// <inheritdoc />
		public int EndReceive(IAsyncResult asyncResult)
		{
			return _socket.EndReceive(asyncResult);
		}

		/// <inheritdoc />
		public int EndReceive(IAsyncResult asyncResult, out SocketError errorCode)
		{
			return _socket.EndReceive(asyncResult, out errorCode);
		}

		/// <inheritdoc />
		public int EndReceiveFrom(IAsyncResult asyncResult, ref EndPoint endPoint)
		{
			return _socket.EndReceiveFrom(asyncResult, ref endPoint);
		}

		/// <inheritdoc />
		public int EndReceiveMessageFrom(IAsyncResult asyncResult,
		                                 ref SocketFlags socketFlags,
		                                 ref EndPoint endPoint,
		                                 out IPPacketInformation ipPacketInformation)
		{
			return _socket.EndReceiveMessageFrom(asyncResult,
			                                     ref socketFlags,
			                                     ref endPoint,
			                                     out ipPacketInformation);
		}

		/// <inheritdoc />
		public int EndSend(IAsyncResult asyncResult)
		{
			return _socket.EndSend(asyncResult);
		}

		/// <inheritdoc />
		public int EndSend(IAsyncResult asyncResult, out SocketError errorCode)
		{
			return _socket.EndSend(asyncResult, out errorCode);
		}

		/// <inheritdoc />
		public void EndSendFile(IAsyncResult asyncResult)
		{
			_socket.EndSendFile(asyncResult);
		}

		/// <inheritdoc />
		public int EndSendTo(IAsyncResult asyncResult)
		{
			return _socket.EndSendTo(asyncResult);
		}

		/// <inheritdoc />
		public object GetSocketOption(SocketOptionLevel optionLevel, SocketOptionName optionName)
		{
			return _socket.GetSocketOption(optionLevel, optionName);
		}

		/// <inheritdoc />
		public byte[] GetSocketOption(SocketOptionLevel optionLevel, SocketOptionName optionName, int optionLength)
		{
			return _socket.GetSocketOption(optionLevel, optionName, optionLength);
		}

		/// <inheritdoc />
		public void GetSocketOption(SocketOptionLevel optionLevel, SocketOptionName optionName, byte[] optionValue)
		{
			_socket.GetSocketOption(optionLevel, optionName, optionValue);
		}

		/// <inheritdoc />
		public int IOControl(int ioControlCode, byte[] optionInValue, byte[] optionOutValue)
		{
			return _socket.IOControl(ioControlCode, optionInValue, optionOutValue);
		}

		/// <inheritdoc />
		public int IOControl(IOControlCode ioControlCode, byte[] optionInValue, byte[] optionOutValue)
		{
			return _socket.IOControl(ioControlCode, optionInValue, optionOutValue);
		}

		/// <inheritdoc />
		public void Listen(int backlog)
		{
			_socket.Listen(backlog);
		}

		/// <inheritdoc />
		public bool Poll(int microSeconds, SelectMode mode)
		{
			return _socket.Poll(microSeconds, mode);
		}

		/// <inheritdoc />
		public int Receive(byte[] buffer)
		{
			return _socket.Receive(buffer);
		}

		/// <inheritdoc />
		public int Receive(IList<ArraySegment<byte>> buffers)
		{
			return _socket.Receive(buffers);
		}

		/// <inheritdoc />
		public int Receive(IList<ArraySegment<byte>> buffers, SocketFlags socketFlags)
		{
			return _socket.Receive(buffers, socketFlags);
		}

		/// <inheritdoc />
		public int Receive(byte[] buffer, SocketFlags socketFlags)
		{
			return _socket.Receive(buffer, socketFlags);
		}

		/// <inheritdoc />
		public int Receive(IList<ArraySegment<byte>> buffers, SocketFlags socketFlags, out SocketError errorCode)
		{
			return _socket.Receive(buffers, socketFlags, out errorCode);
		}

		/// <inheritdoc />
		public int Receive(byte[] buffer, int size, SocketFlags socketFlags)
		{
			return _socket.Receive(buffer, size, socketFlags);
		}

		/// <inheritdoc />
		public int Receive(byte[] buffer, int offset, int size, SocketFlags socketFlags)
		{
			return _socket.Receive(buffer, offset, size, socketFlags);
		}

		/// <inheritdoc />
		public int Receive(byte[] buffer, int offset, int size, SocketFlags socketFlags, out SocketError errorCode)
		{
			return _socket.Receive(buffer, offset, size, socketFlags, out errorCode);
		}

		/// <inheritdoc />
		public bool ReceiveAsync(SocketAsyncEventArgs e)
		{
			return _socket.ReceiveAsync(e);
		}

		/// <inheritdoc />
		public int ReceiveFrom(byte[] buffer, ref EndPoint remoteEP)
		{
			return _socket.ReceiveFrom(buffer, ref remoteEP);
		}

		/// <inheritdoc />
		public int ReceiveFrom(byte[] buffer, SocketFlags socketFlags, ref EndPoint remoteEP)
		{
			return _socket.ReceiveFrom(buffer, socketFlags, ref remoteEP);
		}

		/// <inheritdoc />
		public int ReceiveFrom(byte[] buffer, int size, SocketFlags socketFlags, ref EndPoint remoteEP)
		{
			return _socket.ReceiveFrom(buffer, size, socketFlags, ref remoteEP);
		}

		/// <inheritdoc />
		public int ReceiveFrom(byte[] buffer, int offset, int size, SocketFlags socketFlags, ref EndPoint remoteEP)
		{
			return _socket.ReceiveFrom(buffer, offset, size, socketFlags, ref remoteEP);
		}

		/// <inheritdoc />
		public bool ReceiveFromAsync(SocketAsyncEventArgs e)
		{
			return _socket.ReceiveFromAsync(e);
		}

		/// <inheritdoc />
		public int ReceiveMessageFrom(byte[] buffer,
		                              int offset,
		                              int size,
		                              ref SocketFlags socketFlags,
		                              ref EndPoint remoteEP,
		                              out IPPacketInformation ipPacketInformation)
		{
			return _socket.ReceiveMessageFrom(buffer, offset, size, ref socketFlags, ref remoteEP, out ipPacketInformation);
		}

		/// <inheritdoc />
		public bool ReceiveMessageFromAsync(SocketAsyncEventArgs e)
		{
			return _socket.ReceiveMessageFromAsync(e);
		}

		/// <inheritdoc />
		public int Send(byte[] buffer)
		{
			return _socket.Send(buffer);
		}

		/// <inheritdoc />
		public int Send(IList<ArraySegment<byte>> buffers)
		{
			return _socket.Send(buffers);
		}

		/// <inheritdoc />
		public int Send(byte[] buffer, SocketFlags socketFlags)
		{
			return _socket.Send(buffer, socketFlags);
		}

		/// <inheritdoc />
		public int Send(IList<ArraySegment<byte>> buffers, SocketFlags socketFlags)
		{
			return _socket.Send(buffers, socketFlags);
		}

		/// <inheritdoc />
		public int Send(byte[] buffer, int size, SocketFlags socketFlags)
		{
			return _socket.Send(buffer, size, socketFlags);
		}

		/// <inheritdoc />
		public int Send(IList<ArraySegment<byte>> buffers, SocketFlags socketFlags, out SocketError errorCode)
		{
			return _socket.Send(buffers, socketFlags, out errorCode);
		}

		/// <inheritdoc />
		public int Send(byte[] buffer, int offset, int size, SocketFlags socketFlags)
		{
			return _socket.Send(buffer, offset, size, socketFlags);
		}

		/// <inheritdoc />
		public int Send(byte[] buffer, int offset, int size, SocketFlags socketFlags, out SocketError errorCode)
		{
			return _socket.Send(buffer, offset, size, socketFlags, out errorCode);
		}

		/// <inheritdoc />
		public bool SendAsync(SocketAsyncEventArgs e)
		{
			return _socket.SendAsync(e);
		}

		/// <inheritdoc />
		public void SendFile(string fileName)
		{
			_socket.SendFile(fileName);
		}

		/// <inheritdoc />
		public void SendFile(string fileName, byte[] preBuffer, byte[] postBuffer, TransmitFileOptions flags)
		{
			_socket.SendFile(fileName, preBuffer, postBuffer, flags);
		}

		/// <inheritdoc />
		public bool SendPacketsAsync(SocketAsyncEventArgs e)
		{
			return _socket.SendPacketsAsync(e);
		}

		/// <inheritdoc />
		public int SendTo(byte[] buffer, EndPoint remoteEP)
		{
			return _socket.SendTo(buffer, remoteEP);
		}

		/// <inheritdoc />
		public int SendTo(byte[] buffer, SocketFlags socketFlags, EndPoint remoteEP)
		{
			return _socket.SendTo(buffer, socketFlags, remoteEP);
		}

		/// <inheritdoc />
		public int SendTo(byte[] buffer, int size, SocketFlags socketFlags, EndPoint remoteEP)
		{
			return _socket.SendTo(buffer, size, socketFlags, remoteEP);
		}

		/// <inheritdoc />
		public int SendTo(byte[] buffer, int offset, int size, SocketFlags socketFlags, EndPoint remoteEP)
		{
			return _socket.SendTo(buffer, offset, size, socketFlags, remoteEP);
		}

		/// <inheritdoc />
		public bool SendToAsync(SocketAsyncEventArgs e)
		{
			return _socket.SendToAsync(e);
		}

		/// <inheritdoc />
		public void SetIPProtectionLevel(IPProtectionLevel level)
		{
			_socket.SetIPProtectionLevel(level);
		}

		/// <inheritdoc />
		public void SetSocketOption(SocketOptionLevel optionLevel, SocketOptionName optionName, int optionValue)
		{
			_socket.SetSocketOption(optionLevel, optionName, optionValue);
		}

		/// <inheritdoc />
		public void SetSocketOption(SocketOptionLevel optionLevel, SocketOptionName optionName, byte[] optionValue)
		{
			_socket.SetSocketOption(optionLevel, optionName, optionValue);
		}

		/// <inheritdoc />
		public void SetSocketOption(SocketOptionLevel optionLevel, SocketOptionName optionName, object optionValue)
		{
			_socket.SetSocketOption(optionLevel, optionName, optionValue);
		}

		/// <inheritdoc />
		public void SetSocketOption(SocketOptionLevel optionLevel, SocketOptionName optionName, bool optionValue)
		{
			_socket.SetSocketOption(optionLevel, optionName, optionValue);
		}

		/// <inheritdoc />
		public void Shutdown(SocketShutdown how)
		{
			_socket.Shutdown(how);
		}
	}
}