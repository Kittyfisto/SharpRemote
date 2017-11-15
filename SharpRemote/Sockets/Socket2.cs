using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;

namespace SharpRemote.Sockets
{
	internal sealed class Socket2
		: ISocket
	{
		private readonly Socket _socket;

		public Socket2(Socket socket)
		{
			if (socket == null)
				throw new ArgumentNullException(nameof(socket));

			_socket = socket;
		}
		
		public Socket2(AddressFamily addressFamily, SocketType socketType, ProtocolType protocolType)
		{
			_socket = new Socket(addressFamily, socketType, protocolType);
		}

		public Socket2(SocketInformation socketInformation)
		{
			_socket = new Socket(socketInformation);
		}

		public Socket2(SocketType socketType, ProtocolType protocolType)
		{
			_socket = new Socket(socketType, protocolType);
		}

		public AddressFamily AddressFamily => _socket.AddressFamily;

		public int Available => _socket.Available;

		public bool Blocking
		{
			get { return _socket.Blocking; }
			set { _socket.Blocking = value; }
		}

		public bool Connected => _socket.Connected;

		public bool DontFragment
		{
			get { return _socket.DontFragment; }
			set { _socket.DontFragment = value; }
		}

		public bool DualMode
		{
			get { return _socket.DualMode; }
			set { _socket.DualMode = value; }
		}

		public bool EnableBroadcast
		{
			get { return _socket.EnableBroadcast; }
			set { _socket.EnableBroadcast = value; }
		}

		public bool ExclusiveAddressUse
		{
			get { return _socket.ExclusiveAddressUse; }
			set { _socket.ExclusiveAddressUse = value; }
		}

		public IntPtr Handle => _socket.Handle;

		public bool IsBound => _socket.IsBound;

		public LingerOption LingerState
		{
			get { return _socket.LingerState; }
			set { _socket.LingerState = value; }
		}

		public EndPoint LocalEndPoint => _socket.LocalEndPoint;

		public bool MulticastLoopback
		{
			get { return _socket.MulticastLoopback; }
			set { _socket.MulticastLoopback = value; }
		}

		public bool NoDelay
		{
			get { return _socket.NoDelay; }
			set { _socket.NoDelay = value; }
		}

		public ProtocolType ProtocolType => _socket.ProtocolType;

		public int ReceiveBufferSize
		{
			get { return _socket.ReceiveBufferSize; }
			set { _socket.ReceiveBufferSize = value; }
		}

		public int ReceiveTimeout
		{
			get { return _socket.ReceiveTimeout; }
			set { _socket.ReceiveTimeout = value; }
		}

		public EndPoint RemoteEndPoint => _socket.RemoteEndPoint;

		public int SendBufferSize
		{
			get { return _socket.SendBufferSize; }
			set { _socket.SendBufferSize = value; }
		}

		public int SendTimeout
		{
			get { return _socket.SendTimeout; }
			set { _socket.SendTimeout = value; }
		}

		public SocketType SocketType => _socket.SocketType;

		public short Ttl
		{
			get { return _socket.Ttl; }
			set { _socket.Ttl = value; }
		}

		public bool UseOnlyOverlappedIO
		{
			get { return _socket.UseOnlyOverlappedIO; }
			set { _socket.UseOnlyOverlappedIO = value; }
		}

		public ISocket Accept()
		{
			return new Socket2(_socket.Accept());
		}

		public bool AcceptAsync(SocketAsyncEventArgs e)
		{
			return _socket.AcceptAsync(e);
		}

		public IAsyncResult BeginAccept(AsyncCallback callback, object state)
		{
			return _socket.BeginAccept(callback, state);
		}

		public IAsyncResult BeginAccept(int receiveSize, AsyncCallback callback, object state)
		{
			return _socket.BeginAccept(receiveSize, callback, state);
		}

		public IAsyncResult BeginAccept(Socket acceptSocket, int receiveSize, AsyncCallback callback, object state)
		{
			return _socket.BeginAccept(acceptSocket, receiveSize, callback, state);
		}

		public IAsyncResult BeginConnect(EndPoint remoteEP, AsyncCallback callback, object state)
		{
			return _socket.BeginConnect(remoteEP, callback, state);
		}

		public IAsyncResult BeginConnect(IPAddress[] addresses, int port, AsyncCallback requestCallback, object state)
		{
			return _socket.BeginConnect(addresses, port, requestCallback, state);
		}

		public IAsyncResult BeginConnect(IPAddress address, int port, AsyncCallback requestCallback, object state)
		{
			return _socket.BeginConnect(address, port, requestCallback, state);
		}

		public IAsyncResult BeginConnect(string host, int port, AsyncCallback requestCallback, object state)
		{
			return _socket.BeginConnect(host, port, requestCallback, state);
		}

		public IAsyncResult BeginDisconnect(bool reuseSocket, AsyncCallback callback, object state)
		{
			return _socket.BeginDisconnect(reuseSocket, callback, state);
		}

		public IAsyncResult BeginReceive(IList<ArraySegment<byte>> buffers, SocketFlags socketFlags, AsyncCallback callback, object state)
		{
			return _socket.BeginReceive(buffers, socketFlags, callback, state);
		}

		public IAsyncResult BeginReceive(IList<ArraySegment<byte>> buffers,
		                                 SocketFlags socketFlags,
		                                 out SocketError errorCode,
		                                 AsyncCallback callback,
		                                 object state)
		{
			return _socket.BeginReceive(buffers, socketFlags, out errorCode, callback, state);
		}

		public IAsyncResult BeginReceive(byte[] buffer,
		                                 int offset,
		                                 int size,
		                                 SocketFlags socketFlags,
		                                 AsyncCallback callback,
		                                 object state)
		{
			return _socket.BeginReceive(buffer, offset, size, socketFlags, callback, state);
		}

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

		public IAsyncResult BeginSend(IList<ArraySegment<byte>> buffers, SocketFlags socketFlags, AsyncCallback callback, object state)
		{
			return _socket.BeginSend(buffers, socketFlags, callback, state);
		}

		public IAsyncResult BeginSend(IList<ArraySegment<byte>> buffers,
		                              SocketFlags socketFlags,
		                              out SocketError errorCode,
		                              AsyncCallback callback,
		                              object state)
		{
			return _socket.BeginSend(buffers, socketFlags, out errorCode, callback, state);
		}

		public IAsyncResult BeginSend(byte[] buffer,
		                              int offset,
		                              int size,
		                              SocketFlags socketFlags,
		                              AsyncCallback callback,
		                              object state)
		{
			return _socket.BeginSend(buffer, offset, size, socketFlags, callback, state);
		}

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

		public IAsyncResult BeginSendFile(string fileName, AsyncCallback callback, object state)
		{
			return _socket.BeginSendFile(fileName, callback, state);
		}

		public IAsyncResult BeginSendFile(string fileName,
		                                  byte[] preBuffer,
		                                  byte[] postBuffer,
		                                  TransmitFileOptions flags,
		                                  AsyncCallback callback,
		                                  object state)
		{
			return _socket.BeginSendFile(fileName, preBuffer, postBuffer, flags, callback, state);
		}

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

		public void Bind(EndPoint localEP)
		{
			_socket.Bind(localEP);
		}

		public void Close()
		{
			_socket.Close();
		}

		public void Close(int timeout)
		{
			_socket.Close(timeout);
		}

		public void Connect(EndPoint remoteEP)
		{
			_socket.Connect(remoteEP);
		}

		public void Connect(IPAddress address, int port)
		{
			_socket.Connect(address, port);
		}

		public void Connect(string host, int port)
		{
			_socket.Connect(host, port);
		}

		public void Connect(IPAddress[] addresses, int port)
		{
			_socket.Connect(addresses, port);
		}

		public bool ConnectAsync(SocketAsyncEventArgs e)
		{
			return _socket.ConnectAsync(e);
		}

		public void Disconnect(bool reuseSocket)
		{
			_socket.Disconnect(reuseSocket);
		}

		public bool DisconnectAsync(SocketAsyncEventArgs e)
		{
			return _socket.DisconnectAsync(e);
		}

		public void Dispose()
		{
			_socket.Dispose();
		}

		public SocketInformation DuplicateAndClose(int targetProcessId)
		{
			return _socket.DuplicateAndClose(targetProcessId);
		}

		public ISocket EndAccept(IAsyncResult asyncResult)
		{
			return new Socket2(_socket.EndAccept(asyncResult));
		}

		public ISocket EndAccept(out byte[] buffer, IAsyncResult asyncResult)
		{
			return new Socket2(_socket.EndAccept(out buffer, asyncResult));
		}

		public ISocket EndAccept(out byte[] buffer, out int bytesTransferred, IAsyncResult asyncResult)
		{
			return new Socket2(_socket.EndAccept(out buffer, out bytesTransferred, asyncResult));
		}

		public void EndConnect(IAsyncResult asyncResult)
		{
			_socket.EndConnect(asyncResult);
		}

		public void EndDisconnect(IAsyncResult asyncResult)
		{
			_socket.EndDisconnect(asyncResult);
		}

		public int EndReceive(IAsyncResult asyncResult)
		{
			return _socket.EndReceive(asyncResult);
		}

		public int EndReceive(IAsyncResult asyncResult, out SocketError errorCode)
		{
			return _socket.EndReceive(asyncResult, out errorCode);
		}

		public int EndReceiveFrom(IAsyncResult asyncResult, ref EndPoint endPoint)
		{
			return _socket.EndReceiveFrom(asyncResult, ref endPoint);
		}

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

		public int EndSend(IAsyncResult asyncResult)
		{
			return _socket.EndSend(asyncResult);
		}

		public int EndSend(IAsyncResult asyncResult, out SocketError errorCode)
		{
			return _socket.EndSend(asyncResult, out errorCode);
		}

		public void EndSendFile(IAsyncResult asyncResult)
		{
			_socket.EndSendFile(asyncResult);
		}

		public int EndSendTo(IAsyncResult asyncResult)
		{
			return _socket.EndSendTo(asyncResult);
		}

		public object GetSocketOption(SocketOptionLevel optionLevel, SocketOptionName optionName)
		{
			return _socket.GetSocketOption(optionLevel, optionName);
		}

		public byte[] GetSocketOption(SocketOptionLevel optionLevel, SocketOptionName optionName, int optionLength)
		{
			return _socket.GetSocketOption(optionLevel, optionName, optionLength);
		}

		public void GetSocketOption(SocketOptionLevel optionLevel, SocketOptionName optionName, byte[] optionValue)
		{
			_socket.GetSocketOption(optionLevel, optionName, optionValue);
		}

		public int IOControl(int ioControlCode, byte[] optionInValue, byte[] optionOutValue)
		{
			return _socket.IOControl(ioControlCode, optionInValue, optionOutValue);
		}

		public int IOControl(IOControlCode ioControlCode, byte[] optionInValue, byte[] optionOutValue)
		{
			return _socket.IOControl(ioControlCode, optionInValue, optionOutValue);
		}

		public void Listen(int backlog)
		{
			_socket.Listen(backlog);
		}

		public bool Poll(int microSeconds, SelectMode mode)
		{
			return _socket.Poll(microSeconds, mode);
		}

		public int Receive(byte[] buffer)
		{
			return _socket.Receive(buffer);
		}

		public int Receive(IList<ArraySegment<byte>> buffers)
		{
			return _socket.Receive(buffers);
		}

		public int Receive(IList<ArraySegment<byte>> buffers, SocketFlags socketFlags)
		{
			return _socket.Receive(buffers, socketFlags);
		}

		public int Receive(byte[] buffer, SocketFlags socketFlags)
		{
			return _socket.Receive(buffer, socketFlags);
		}

		public int Receive(IList<ArraySegment<byte>> buffers, SocketFlags socketFlags, out SocketError errorCode)
		{
			return _socket.Receive(buffers, socketFlags, out errorCode);
		}

		public int Receive(byte[] buffer, int size, SocketFlags socketFlags)
		{
			return _socket.Receive(buffer, size, socketFlags);
		}

		public int Receive(byte[] buffer, int offset, int size, SocketFlags socketFlags)
		{
			return _socket.Receive(buffer, offset, size, socketFlags);
		}

		public int Receive(byte[] buffer, int offset, int size, SocketFlags socketFlags, out SocketError errorCode)
		{
			return _socket.Receive(buffer, offset, size, socketFlags, out errorCode);
		}

		public bool ReceiveAsync(SocketAsyncEventArgs e)
		{
			return _socket.ReceiveAsync(e);
		}

		public int ReceiveFrom(byte[] buffer, ref EndPoint remoteEP)
		{
			return _socket.ReceiveFrom(buffer, ref remoteEP);
		}

		public int ReceiveFrom(byte[] buffer, SocketFlags socketFlags, ref EndPoint remoteEP)
		{
			return _socket.ReceiveFrom(buffer, socketFlags, ref remoteEP);
		}

		public int ReceiveFrom(byte[] buffer, int size, SocketFlags socketFlags, ref EndPoint remoteEP)
		{
			return _socket.ReceiveFrom(buffer, size, socketFlags, ref remoteEP);
		}

		public int ReceiveFrom(byte[] buffer, int offset, int size, SocketFlags socketFlags, ref EndPoint remoteEP)
		{
			return _socket.ReceiveFrom(buffer, offset, size, socketFlags, ref remoteEP);
		}

		public bool ReceiveFromAsync(SocketAsyncEventArgs e)
		{
			return _socket.ReceiveFromAsync(e);
		}

		public int ReceiveMessageFrom(byte[] buffer,
		                              int offset,
		                              int size,
		                              ref SocketFlags socketFlags,
		                              ref EndPoint remoteEP,
		                              out IPPacketInformation ipPacketInformation)
		{
			return _socket.ReceiveMessageFrom(buffer, offset, size, ref socketFlags, ref remoteEP, out ipPacketInformation);
		}

		public bool ReceiveMessageFromAsync(SocketAsyncEventArgs e)
		{
			return _socket.ReceiveMessageFromAsync(e);
		}

		public int Send(byte[] buffer)
		{
			return _socket.Send(buffer);
		}

		public int Send(IList<ArraySegment<byte>> buffers)
		{
			return _socket.Send(buffers);
		}

		public int Send(byte[] buffer, SocketFlags socketFlags)
		{
			return _socket.Send(buffer, socketFlags);
		}

		public int Send(IList<ArraySegment<byte>> buffers, SocketFlags socketFlags)
		{
			return _socket.Send(buffers, socketFlags);
		}

		public int Send(byte[] buffer, int size, SocketFlags socketFlags)
		{
			return _socket.Send(buffer, size, socketFlags);
		}

		public int Send(IList<ArraySegment<byte>> buffers, SocketFlags socketFlags, out SocketError errorCode)
		{
			return _socket.Send(buffers, socketFlags, out errorCode);
		}

		public int Send(byte[] buffer, int offset, int size, SocketFlags socketFlags)
		{
			return _socket.Send(buffer, offset, size, socketFlags);
		}

		public int Send(byte[] buffer, int offset, int size, SocketFlags socketFlags, out SocketError errorCode)
		{
			return _socket.Send(buffer, offset, size, socketFlags, out errorCode);
		}

		public bool SendAsync(SocketAsyncEventArgs e)
		{
			return _socket.SendAsync(e);
		}

		public void SendFile(string fileName)
		{
			_socket.SendFile(fileName);
		}

		public void SendFile(string fileName, byte[] preBuffer, byte[] postBuffer, TransmitFileOptions flags)
		{
			_socket.SendFile(fileName, preBuffer, postBuffer, flags);
		}

		public bool SendPacketsAsync(SocketAsyncEventArgs e)
		{
			return _socket.SendPacketsAsync(e);
		}

		public int SendTo(byte[] buffer, EndPoint remoteEP)
		{
			return _socket.SendTo(buffer, remoteEP);
		}

		public int SendTo(byte[] buffer, SocketFlags socketFlags, EndPoint remoteEP)
		{
			return _socket.SendTo(buffer, socketFlags, remoteEP);
		}

		public int SendTo(byte[] buffer, int size, SocketFlags socketFlags, EndPoint remoteEP)
		{
			return _socket.SendTo(buffer, size, socketFlags, remoteEP);
		}

		public int SendTo(byte[] buffer, int offset, int size, SocketFlags socketFlags, EndPoint remoteEP)
		{
			return _socket.SendTo(buffer, offset, size, socketFlags, remoteEP);
		}

		public bool SendToAsync(SocketAsyncEventArgs e)
		{
			return _socket.SendToAsync(e);
		}

		public void SetIPProtectionLevel(IPProtectionLevel level)
		{
			_socket.SetIPProtectionLevel(level);
		}

		public void SetSocketOption(SocketOptionLevel optionLevel, SocketOptionName optionName, int optionValue)
		{
			_socket.SetSocketOption(optionLevel, optionName, optionValue);
		}

		public void SetSocketOption(SocketOptionLevel optionLevel, SocketOptionName optionName, byte[] optionValue)
		{
			_socket.SetSocketOption(optionLevel, optionName, optionValue);
		}

		public void SetSocketOption(SocketOptionLevel optionLevel, SocketOptionName optionName, object optionValue)
		{
			_socket.SetSocketOption(optionLevel, optionName, optionValue);
		}

		public void SetSocketOption(SocketOptionLevel optionLevel, SocketOptionName optionName, bool optionValue)
		{
			_socket.SetSocketOption(optionLevel, optionName, optionValue);
		}

		public void Shutdown(SocketShutdown how)
		{
			_socket.Shutdown(how);
		}
	}
}