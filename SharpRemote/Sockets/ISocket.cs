using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;

namespace SharpRemote.Sockets
{
	/// <summary>
	///     Offers an interface identical to <see cref="Socket" /> and can be used as a drop in replacement.
	///     Want to unit test code using a Socket but cannot be Microsoft didn't include any interface?
	///     Fear no more...
	/// </summary>
	/// <remarks>
	///     This should be part of the System.Extensions project and be publicly available.
	/// </remarks>
	public interface ISocket
		: IDisposable
	{
		/// <summary>
		/// Gets the address family of the System.Net.Sockets.Socket.
		/// </summary>
		/// <returns>One of the System.Net.Sockets.AddressFamily values.</returns>
		AddressFamily AddressFamily { get; }

		/// <summary>
		/// Gets the amount of data that has been received from the network and is available
		///     to be read.
		/// </summary>
		/// <returns>The number of bytes of data received from the network and available to be read.</returns>
		/// <exception cref="System.Net.Sockets.SocketException">
		/// An error occurred when attempting to access the socket. See the Remarks section
		///     for more information.
		/// </exception>
		/// <exception cref="System.ObjectDisposedException">
		/// The System.Net.Sockets.Socket has been closed.
		/// </exception>
		int Available { get; }

		///
		///<Summary>
		///     Gets or sets a value that indicates whether the System.Net.Sockets.Socket is
		///     in blocking mode.
		///</Summary>
		/// <Returns>
		///     true if the System.Net.Sockets.Socket will block; otherwise, false. The default
		///     is true.
		///</Returns>
		/// <exception cref="System.Net.Sockets.SocketException">
		///     An error occurred when attempting to access the socket. See the Remarks section
		///     for more information.
		/// </exception>
		/// <exception cref="System.ObjectDisposedException">
		/// The System.Net.Sockets.Socket has been closed.
		/// </exception>
		bool Blocking { get; set; }

		///<Summary>
		///     Gets a value that indicates whether a System.Net.Sockets.Socket is connected
		///     to a remote host as of the last Overload:System.Net.Sockets.Socket.Send or Overload:System.Net.Sockets.Socket.Receive
		///     operation.
		///</Summary>
		/// <Returns>
		///     true if the System.Net.Sockets.Socket was connected to a remote resource as of
		///     the most recent operation; otherwise, false.
		/// </Returns>
		bool Connected { get; }

		///<Summary>
		///     Gets or sets a System.Boolean value that specifies whether the System.Net.Sockets.Socket
		///     allows Internet Protocol (IP) datagrams to be fragmented.
		///</Summary><Returns>
		///     true if the System.Net.Sockets.Socket allows datagram fragmentation; otherwise,
		///     false. The default is true.
		/// </Returns>
		/// <exception cref="System.NotSupportedException">
		/// This property can be set only for sockets in the System.Net.Sockets.AddressFamily.InterNetwork
		///     or System.Net.Sockets.AddressFamily.InterNetworkV6 families.
		/// </exception>
		/// <exception cref="System.Net.Sockets.SocketException">
		///     An error occurred when attempting to access the socket. See the Remarks section
		///     for more information.
		/// </exception>
		/// <exception cref="System.ObjectDisposedException">
		/// The System.Net.Sockets.Socket has been closed.
		/// </exception>
		bool DontFragment { get; set; }

		///<Summary>
		///     Gets or sets a System.Boolean value that specifies whether the System.Net.Sockets.Socket
		///     is a dual-mode socket used for both IPv4 and IPv6.
		///</Summary>
		/// <Returns>
		///     Returns System.Boolean.true if the System.Net.Sockets.Socket is a dual-mode socket;
		///     otherwise, false. The default is false.
		/// </Returns>
		bool DualMode { get; set; }

		///<Summary>
		///     Gets or sets a System.Boolean value that specifies whether the System.Net.Sockets.Socket
		///     can send or receive broadcast packets.
		///</Summary>
		/// <Returns>
		///     true if the System.Net.Sockets.Socket allows broadcast packets; otherwise, false.
		///     The default is false.
		/// </Returns>
		///<exception cref="System.Net.Sockets.SocketException">
		/// This option is valid for a datagram socket only.
		/// </exception>
		/// <exception cref="System.ObjectDisposedException">
		/// The System.Net.Sockets.Socket has been closed.
		/// </exception>
		bool EnableBroadcast { get; set; }

		/// <summary>
		/// Gets or sets a System.Boolean value that specifies whether the System.Net.Sockets.Socket
		///     allows only one process to bind to a port.
		/// </summary>
		/// <remarks>
		/// true if the System.Net.Sockets.Socket allows only one socket to bind to a specific
		///     port; otherwise, false. The default is true for Windows Server 2003 and Windows
		///     XP Service Pack 2, and false for all other versions.
		/// </remarks>
		/// <exception cref="System.Net.Sockets.SocketException">
		///     An error occurred when attempting to access the socket. See the Remarks section
		///     for more information.
		/// </exception>
		/// <exception cref="System.ObjectDisposedException">
		/// The System.Net.Sockets.Socket has been closed.
		/// </exception>
		/// <exception cref="System.InvalidOperationException">
		/// System.Net.Sockets.Socket.Bind(System.Net.EndPoint) has been called for this
		///     System.Net.Sockets.Socket.
		/// </exception>
		bool ExclusiveAddressUse { get; set; }

		///<Summary>
		///     Gets the operating system handle for the System.Net.Sockets.Socket.
		///</Summary>
		/// <Returns>
		///     An System.IntPtr that represents the operating system handle for the System.Net.Sockets.Socket.
		/// </Returns>
		IntPtr Handle { get; }

		/// <Summary>
		///     Gets a value that indicates whether the System.Net.Sockets.Socket is bound to
		///     a specific local port.
		/// </Summary>
		/// <Returns>
		///     true if the System.Net.Sockets.Socket is bound to a local port; otherwise, false.
		/// </Returns>
		bool IsBound { get; }

		/// <Summary>
		///     Gets or sets a value that specifies whether the System.Net.Sockets.Socket will
		///     delay closing a socket in an attempt to send all pending data.
		/// </Summary>
		/// <Returns>
		///     A System.Net.Sockets.LingerOption that specifies how to linger while closing
		///     a socket.
		/// </Returns>
		/// <exception cref="System.Net.Sockets.SocketException">
		///     An error occurred when attempting to access the socket.
		/// </exception>
		/// <exception cref="System.ObjectDisposedException">The System.Net.Sockets.Socket has been closed.</exception>
		LingerOption LingerState { get; set; }

		/// <Summary>
		///     Gets the local endpoint.
		/// </Summary>
		/// <Returns>
		///     The System.Net.EndPoint that the System.Net.Sockets.Socket is using for communications.
		/// </Returns>
		/// <exception cref="System.Net.Sockets.SocketException">
		///     An error occurred when attempting to access the socket. See the Remarks section
		///     for more information.
		/// </exception>
		/// <exception cref="System.ObjectDisposedException">The System.Net.Sockets.Socket has been closed.</exception>
		EndPoint LocalEndPoint { get; }

		/// <Summary>
		///     Gets or sets a value that specifies whether outgoing multicast packets are delivered
		///     to the sending application.
		/// </Summary>
		/// <Returns>
		///     true if the System.Net.Sockets.Socket receives outgoing multicast packets; otherwise,
		///     false.
		/// </Returns>
		/// <exception cref="System.Net.Sockets.SocketException">
		///     An error occurred when attempting to access the socket.
		/// </exception>
		/// <exception cref="System.ObjectDisposedException">The System.Net.Sockets.Socket has been closed.</exception>
		bool MulticastLoopback { get; set; }

		/// <Summary>
		///     Gets or sets a System.Boolean value that specifies whether the stream System.Net.Sockets.Socket
		///     is using the Nagle algorithm.
		/// </Summary>
		/// <Returns>
		///     false if the System.Net.Sockets.Socket uses the Nagle algorithm; otherwise, true.
		///     The default is false.
		/// </Returns>
		/// <exception cref="System.Net.Sockets.SocketException"></exception>
		/// An error occurred when attempting to access the System.Net.Sockets.Socket. See
		/// the Remarks section for more information.
		/// <exception cref="System.ObjectDisposedException">The System.Net.Sockets.Socket has been closed.</exception>
		bool NoDelay { get; set; }

		/// <Summary>
		///     Gets the protocol type of the System.Net.Sockets.Socket.
		/// </Summary>
		/// <Returns>
		///     One of the System.Net.Sockets.ProtocolType values.
		/// </Returns>
		ProtocolType ProtocolType { get; }

		/// <Summary>
		///     Gets or sets a value that specifies the size of the receive buffer of the System.Net.Sockets.Socket.
		/// </Summary>
		/// <Returns>
		///     An System.Int32 that contains the size, in bytes, of the receive buffer. The
		///     default is 8192.
		/// </Returns>
		/// <exception cref="System.Net.Sockets.SocketException">
		///     An error occurred when attempting to access the socket.
		/// </exception>
		/// <exception cref="System.ObjectDisposedException">The System.Net.Sockets.Socket has been closed.</exception>
		/// <exception cref="System.ArgumentOutOfRangeException">
		///     The value specified for a set operation is less than 0.
		/// </exception>
		int ReceiveBufferSize { get; set; }

		/// <Summary>
		///     Gets or sets a value that specifies the amount of time after which a synchronous
		///     Overload:System.Net.Sockets.Socket.Receive call will time out.
		/// </Summary>
		/// <Returns>
		///     The time-out value, in milliseconds. The default value is 0, which indicates
		///     an infinite time-out period. Specifying -1 also indicates an infinite time-out
		///     period.
		/// </Returns>
		/// <exception cref="System.Net.Sockets.SocketException">
		///     An error occurred when attempting to access the socket.
		/// </exception>
		/// <exception cref="System.ObjectDisposedException">The System.Net.Sockets.Socket has been closed.</exception>
		/// <exception cref="System.ArgumentOutOfRangeException">
		///     The value specified for a set operation is less than -1.
		/// </exception>
		int ReceiveTimeout { get; set; }

		/// <Summary>
		///     Gets the remote endpoint.
		/// </Summary>
		/// <Returns>
		///     The System.Net.EndPoint with which the System.Net.Sockets.Socket is communicating.
		/// </Returns>
		/// <exception cref="System.Net.Sockets.SocketException">
		///     An error occurred when attempting to access the socket. See the Remarks section
		///     for more information.
		/// </exception>
		/// <exception cref="System.ObjectDisposedException">The System.Net.Sockets.Socket has been closed.</exception>
		EndPoint RemoteEndPoint { get; }

		/// <Summary>
		///     Gets or sets a value that specifies the size of the send buffer of the System.Net.Sockets.Socket.
		/// </Summary>
		/// <Returns>
		///     An System.Int32 that contains the size, in bytes, of the send buffer. The default
		///     is 8192.
		/// </Returns>
		/// <exception cref="System.Net.Sockets.SocketException">An error occurred when attempting to access the socket.</exception>
		/// <exception cref="System.ObjectDisposedException">The System.Net.Sockets.Socket has been closed.</exception>
		/// <exception cref="System.ArgumentOutOfRangeException">
		///     The value specified for a set operation is less than 0.
		/// </exception>
		int SendBufferSize { get; set; }

		/// <Summary>
		///     Gets or sets a value that specifies the amount of time after which a synchronous
		///     Overload:System.Net.Sockets.Socket.Send call will time out.
		/// </Summary>
		/// <Returns>
		///     The time-out value, in milliseconds. If you set the property with a value between
		///     1 and 499, the value will be changed to 500. The default value is 0, which indicates
		///     an infinite time-out period. Specifying -1 also indicates an infinite time-out
		///     period.
		/// </Returns>
		/// <exception cref="System.Net.Sockets.SocketException">An error occurred when attempting to access the socket.</exception>
		/// <exception cref="System.ObjectDisposedException">The System.Net.Sockets.Socket has been closed.</exception>
		/// <exception cref="System.ArgumentOutOfRangeException">
		///     The value specified for a set operation is less than -1.
		/// </exception>
		int SendTimeout { get; set; }

		///<Summary>
		///     Gets the type of the System.Net.Sockets.Socket.
		///</Summary>
		/// <Returns>
		///     One of the System.Net.Sockets.SocketType values.
		/// </Returns>
		SocketType SocketType { get; }

		/// <Summary>
		///     Gets or sets a value that specifies the Time To Live (TTL) value of Internet
		///     Protocol (IP) packets sent by the System.Net.Sockets.Socket.
		/// </Summary>
		/// <Returns>
		///     The TTL value.
		/// </Returns>
		/// <exception cref="System.ArgumentOutOfRangeException">
		///     The TTL value can't be set to a negative number.
		/// </exception>
		/// <exception cref="System.NotSupportedException">
		///     This property can be set only for sockets in the System.Net.Sockets.AddressFamily.InterNetwork
		///     or System.Net.Sockets.AddressFamily.InterNetworkV6 families.
		/// </exception>
		/// <exception cref="System.Net.Sockets.SocketException"></exception>
		/// An error occurred when attempting to access the socket. This error is also returned
		/// when an attempt was made to set TTL to a value higher than 255.
		/// <exception cref="System.ObjectDisposedException">The System.Net.Sockets.Socket has been closed.</exception>
		short Ttl { get; set; }
		
		///<Summary>
		///     Creates a new System.Net.Sockets.Socket for a newly created connection.
		///</Summary>
		/// <Returns>
		///     A System.Net.Sockets.Socket for a newly created connection.
		/// </Returns>
		///   <exception cref="System.Net.Sockets.SocketException">
		/// An error occurred when attempting to access the socket. See the Remarks section
		///     for more information.
		/// </exception>
		///   <exception cref="System.ObjectDisposedException">The System.Net.Sockets.Socket has been closed.</exception>
		///<exception cref="System.InvalidOperationException">
		/// The accepting socket is not listening for connections. You must call System.Net.Sockets.Socket.Bind(System.Net.EndPoint)
		///     and System.Net.Sockets.Socket.Listen(System.Int32) before calling System.Net.Sockets.Socket.Accept.
		/// </exception>
		ISocket Accept();

		///<Summary>
		///     Begins an asynchronous operation to accept an incoming connection attempt.
		///</Summary>
		/// <param name="e">
		/// The System.Net.Sockets.SocketAsyncEventArgs object to use for this asynchronous
		///     socket operation.</param>
		/// <Returns>
		///     Returns true if the I/O operation is pending. The System.Net.Sockets.SocketAsyncEventArgs.Completed
		///     event on the e parameter will be raised upon completion of the operation.Returns
		///     false if the I/O operation completed synchronously. The System.Net.Sockets.SocketAsyncEventArgs.Completed
		///     event on the e parameter will not be raised and the e object passed as a parameter
		///     may be examined immediately after the method call returns to retrieve the result
		///     of the operation.
		/// </Returns>
		/// <exception cref="System.ArgumentException">
		/// An argument is not valid. This exception occurs if the buffer provided is not
		///     large enough. The buffer must be at least 2 * (sizeof(SOCKADDR_STORAGE + 16)
		///     bytes. This exception also occurs if multiple buffers are specified, the System.Net.Sockets.SocketAsyncEventArgs.BufferList
		///     property is not null.
		/// </exception>
		/// <exception cref="System.ArgumentOutOfRangeException">
		/// An argument is out of range. The exception occurs if the System.Net.Sockets.SocketAsyncEventArgs.Count
		///     is less than 0.
		/// </exception>
		/// <exception cref="System.InvalidOperationException">
		/// An invalid operation was requested. This exception occurs if the accepting System.Net.Sockets.Socket
		///     is not listening for connections or the accepted socket is bound. You must call
		///     the System.Net.Sockets.Socket.Bind(System.Net.EndPoint) and System.Net.Sockets.Socket.Listen(System.Int32)
		///     method before calling the System.Net.Sockets.Socket.AcceptAsync(System.Net.Sockets.SocketAsyncEventArgs)
		///     method.This exception also occurs if the socket is already connected or a socket
		///     operation was already in progress using the specified e parameter.
		/// </exception>
		///   <exception cref="System.Net.Sockets.SocketException">
		/// An error occurred when attempting to access the socket. See the Remarks section
		///     for more information.
		/// </exception>
		/// <exception cref="System.NotSupportedException">
		/// Windows XP or later is required for this method.
		/// </exception>
		///   <exception cref="System.ObjectDisposedException">The System.Net.Sockets.Socket has been closed.</exception>
		bool AcceptAsync(SocketAsyncEventArgs e);

		///<Summary>
		///     Begins an asynchronous operation to accept an incoming connection attempt.
		///</Summary>
		/// <param name="callback">The System.AsyncCallback delegate.</param>
		/// <param name="state">An object that contains state information for this request.</param>
		/// <Returns>
		///     An System.IAsyncResult that references the asynchronous System.Net.Sockets.Socket
		///     creation.
		/// </Returns>
		///   <exception cref="System.ObjectDisposedException">The System.Net.Sockets.Socket has been closed.</exception>
		/// <exception cref="System.NotSupportedException">
		///     Windows NT is required for this method.
		/// </exception>
		/// <exception cref="System.InvalidOperationException">
		///     The accepting socket is not listening for connections. You must call System.Net.Sockets.Socket.Bind(System.Net.EndPoint)
		///     and System.Net.Sockets.Socket.Listen(System.Int32) before calling System.Net.Sockets.Socket.BeginAccept(System.AsyncCallback,System.Object).-or-
		///     The accepted socket is bound.
		/// </exception>
		/// <exception cref="System.ArgumentOutOfRangeException">
		/// receiveSize is less than 0.
		/// </exception>
		///   <exception cref="System.Net.Sockets.SocketException">
		///     An error occurred when attempting to access the socket. See the Remarks section
		///     for more information.
		/// </exception>
		IAsyncResult BeginAccept(AsyncCallback callback, object state);

		///<Summary>
		///     Begins an asynchronous operation to accept an incoming connection attempt and
		///     receives the first block of data sent by the client application.
		///</Summary>
		/// <param name="receiveSize">The number of bytes to accept from the sender.</param>
		/// <param name="callback">The System.AsyncCallback delegate.</param>
		/// <param name="state">An object that contains state information for this request.</param>
		/// <Returns>
		///     An System.IAsyncResult that references the asynchronous System.Net.Sockets.Socket
		///     creation.
		/// </Returns>
		///   <exception cref="System.ObjectDisposedException">The System.Net.Sockets.Socket has been closed.</exception>
		///   <exception cref="System.NotSupportedException">
		///     Windows NT is required for this method.
		/// </exception>
		///   <exception cref="System.InvalidOperationException">
		///     The accepting socket is not listening for connections. You must call System.Net.Sockets.Socket.Bind(System.Net.EndPoint)
		///     and System.Net.Sockets.Socket.Listen(System.Int32) before calling System.Net.Sockets.Socket.BeginAccept(System.AsyncCallback,System.Object).-or-
		///     The accepted socket is bound.
		/// </exception>
		///   <exception cref="System.ArgumentOutOfRangeException">
		///     receiveSize is less than 0.
		/// </exception>
		///   <exception cref="System.Net.Sockets.SocketException">
		///     An error occurred when attempting to access the socket. See the Remarks section
		///     for more information.
		/// </exception>
		IAsyncResult BeginAccept(int receiveSize, AsyncCallback callback, object state);

		/// <Summary>
		///     Begins an asynchronous operation to accept an incoming connection attempt from
		///     a specified socket and receives the first block of data sent by the client application.
		/// </Summary>
		/// <param name="acceptSocket">The accepted System.Net.Sockets.Socket object. This value may be null.</param>
		/// <param name="receiveSize">The maximum number of bytes to receive.</param>
		/// <param name="callback">The System.AsyncCallback delegate.</param>
		/// <param name="state">An object that contains state information for this request.</param>
		/// <Returns>
		///     An System.IAsyncResult object that references the asynchronous System.Net.Sockets.Socket
		///     object creation.
		/// </Returns>
		/// <exception cref="System.ObjectDisposedException">The System.Net.Sockets.Socket has been closed.</exception>
		/// The System.Net.Sockets.Socket object has been closed.
		/// <exception cref="System.NotSupportedException">
		///     Windows NT is required for this method.
		/// </exception>
		/// <exception cref="System.InvalidOperationException">
		///     The accepting socket is not listening for connections. You must call
		///     System.Net.Sockets.Socket.Bind(System.Net.EndPoint)
		///     and System.Net.Sockets.Socket.Listen(System.Int32) before calling
		///     System.Net.Sockets.Socket.BeginAccept(System.AsyncCallback,System.Object).-or-
		///     The accepted socket is bound.
		/// </exception>
		/// <exception cref="System.ArgumentOutOfRangeException">
		///     receiveSize is less than 0.
		/// </exception>
		/// <exception cref="System.Net.Sockets.SocketException">
		///     An error occurred when attempting to access the socket. See the Remarks section
		///     for more information.
		/// </exception>
		IAsyncResult BeginAccept(Socket acceptSocket, int receiveSize, AsyncCallback callback, object state);

		///<Summary>
		///     Begins an asynchronous request for a remote host connection.
		///</Summary>
		/// <param name="remoteEP">An System.Net.EndPoint that represents the remote host.</param>
		/// <param name="callback">The System.AsyncCallback delegate.</param>
		/// <param name="state">An object that contains state information for this request.</param>
		/// <Returns>
		///     An System.IAsyncResult that references the asynchronous connection.
		/// </Returns>
		///   <exception cref="System.ArgumentNullException">
		///     remoteEP is null.
		/// </exception>
		///   <exception cref="System.Net.Sockets.SocketException">
		///     An error occurred when attempting to access the socket. See the Remarks section
		///     for more information.</exception>
		///   <exception cref="System.ObjectDisposedException">The System.Net.Sockets.Socket has been closed.</exception>
		///   <exception cref="System.Security.SecurityException">
		///     A caller higher in the call stack does not have permission for the requested
		///     operation.
		/// </exception>
		///   <exception cref="System.InvalidOperationException">
		///     The System.Net.Sockets.Socket is System.Net.Sockets.Socket.Listen(System.Int32)ing.
		/// </exception>
		IAsyncResult BeginConnect(EndPoint remoteEP, AsyncCallback callback, object state);

		///<Summary>
		///     Begins an asynchronous request for a remote host connection. The host is specified
		///     by an System.Net.IPAddress array and a port number.
		///</Summary>
		/// <param name="addresses">At least one System.Net.IPAddress, designating the remote host.</param>
		/// <param name="port">The port number of the remote host.</param>
		/// <param name="requestCallback">
		///     An System.AsyncCallback delegate that references the method to invoke when the
		///     connect operation is complete.
		/// </param>
		/// <param name="state">
		///     A user-defined object that contains information about the connect operation.
		///     This object is passed to the requestCallback delegate when the operation is complete.
		/// </param>
		/// <Returns>
		///     An System.IAsyncResult that references the asynchronous connections.
		/// </Returns>
		///   <exception cref="System.ArgumentNullException">
		///     addresses is null.
		/// </exception>
		///   <exception cref="System.Net.Sockets.SocketException">
		///     An error occurred when attempting to access the socket. See the Remarks section
		///     for more information.</exception>
		///   <exception cref="System.ObjectDisposedException">The System.Net.Sockets.Socket has been closed.</exception>
		///   <exception cref="System.NotSupportedException">
		///     This method is valid for sockets that use System.Net.Sockets.AddressFamily.InterNetwork
		///     or System.Net.Sockets.AddressFamily.InterNetworkV6.
		/// </exception>
		///   <exception cref="System.ArgumentOutOfRangeException">
		///     The port number is not valid.</exception>
		///   <exception cref="System.ArgumentException">
		///     The length of address is zero.</exception>
		///   <exception cref="System.InvalidOperationException">
		///     The System.Net.Sockets.Socket is System.Net.Sockets.Socket.Listen(System.Int32)ing.</exception>
		IAsyncResult BeginConnect(IPAddress[] addresses, int port, AsyncCallback requestCallback, object state);

		///<Summary>
		///     Begins an asynchronous request for a remote host connection. The host is specified
		///     by an System.Net.IPAddress and a port number.
		///</Summary>
		/// <param name="address">The System.Net.IPAddress of the remote host.</param>
		/// <param name="port">The port number of the remote host.</param>
		/// <param name="requestCallback">
		///     An System.AsyncCallback delegate that references the method to invoke when the
		///     connect operation is complete.
		/// </param>
		/// <param name="state">
		///     A user-defined object that contains information about the connect operation.
		///     This object is passed to the requestCallback delegate when the operation is complete.
		/// </param>
		/// <Returns>
		///     An System.IAsyncResult that references the asynchronous connection.
		/// </Returns>
		///   <exception cref="System.ArgumentNullException">
		///     address is null.</exception>
		///   <exception cref="System.Net.Sockets.SocketException">
		///     An error occurred when attempting to access the socket. See the Remarks section
		///     for more information.</exception>
		///   <exception cref="System.ObjectDisposedException">The System.Net.Sockets.Socket has been closed.</exception>
		///   <exception cref="System.NotSupportedException">
		///     The System.Net.Sockets.Socket is not in the socket family.</exception>
		///   <exception cref="System.ArgumentOutOfRangeException">
		///     The port number is not valid.
		/// </exception>
		///   <exception cref="System.ArgumentException">
		///     The length of address is zero.</exception>
		///   <exception cref="System.InvalidOperationException">
		///     The System.Net.Sockets.Socket is System.Net.Sockets.Socket.Listen(System.Int32)ing.</exception>
		IAsyncResult BeginConnect(IPAddress address, int port, AsyncCallback requestCallback, object state);

		///<Summary>
		///     Begins an asynchronous request for a remote host connection. The host is specified
		///     by a host name and a port number.
		///</Summary>
		/// <param name="host">The name of the remote host.</param>
		/// <param name="port">The port number of the remote host.</param>
		/// <param name="requestCallback">
		///     An System.AsyncCallback delegate that references the method to invoke when the
		///     connect operation is complete.</param>
		/// <param name="state">
		///     A user-defined object that contains information about the connect operation.
		///     This object is passed to the requestCallback delegate when the operation is complete.
		/// </param>
		/// <Returns>
		///     An System.IAsyncResult that references the asynchronous connection.
		/// </Returns>
		///   <exception cref="System.ArgumentNullException">
		///     host is null.</exception>
		///   <exception cref="System.ObjectDisposedException">The System.Net.Sockets.Socket has been closed.</exception>
		///   <exception cref="System.NotSupportedException">
		///     This method is valid for sockets in the System.Net.Sockets.AddressFamily.InterNetwork
		///     or System.Net.Sockets.AddressFamily.InterNetworkV6 families.
		/// </exception>
		///   <exception cref="System.ArgumentOutOfRangeException">
		///     The port number is not valid.
		/// </exception>
		///   <exception cref="System.InvalidOperationException">
		///     The System.Net.Sockets.Socket is System.Net.Sockets.Socket.Listen(System.Int32)ing.
		/// </exception>
		IAsyncResult BeginConnect(string host, int port, AsyncCallback requestCallback, object state);

		///<Summary>
		///     Begins an asynchronous request to disconnect from a remote endpoint.
		///</Summary>
		/// <param name="reuseSocket">
		///     true if this socket can be reused after the connection is closed; otherwise,
		///     false.</param>
		/// <param name="callback">The System.AsyncCallback delegate.</param>
		/// <param name="state">An object that contains state information for this request.</param>
		/// <Returns>
		///     An System.IAsyncResult object that references the asynchronous operation.
		/// </Returns>
		///   <exception cref="System.NotSupportedException">
		///     The operating system is Windows 2000 or earlier, and this method requires Windows
		///     XP.</exception>
		///   <exception cref="System.ObjectDisposedException">The System.Net.Sockets.Socket has been closed.</exception>
		///   <exception cref="System.Net.Sockets.SocketException">
		///     An error occurred when attempting to access the socket. See the Remarks section
		///     for more information.</exception>
		IAsyncResult BeginDisconnect(bool reuseSocket, AsyncCallback callback, object state);

		///<Summary>
		///     Begins to asynchronously receive data from a connected System.Net.Sockets.Socket.
		///</Summary>
		/// <param name="buffers">An array of type System.Byte that is the storage location for the received data.</param>
		/// <param name="socketFlags">A bitwise combination of the System.Net.Sockets.SocketFlags values.</param>
		/// <param name="callback">
		///     An System.AsyncCallback delegate that references the method to invoke when the
		///     operation is complete.</param>
		/// <param name="state">
		///     A user-defined object that contains information about the receive operation.
		///     This object is passed to the System.Net.Sockets.Socket.EndReceive(System.IAsyncResult)
		///     delegate when the operation is complete.
		/// </param>
		/// <Returns>
		///     An System.IAsyncResult that references the asynchronous read.
		/// </Returns>
		///   <exception cref="System.ArgumentNullException">
		///     buffer is null.
		/// </exception>
		///   <exception cref="System.Net.Sockets.SocketException">
		///     An error occurred when attempting to access the socket. See the Remarks section
		///     for more information.</exception>
		///   <exception cref="System.ObjectDisposedException">The System.Net.Sockets.Socket has been closed.</exception>
		IAsyncResult BeginReceive(IList<ArraySegment<byte>> buffers, SocketFlags socketFlags, AsyncCallback callback, object state);

		///<Summary>
		///     Begins to asynchronously receive data from a connected System.Net.Sockets.Socket.
		///</Summary>
		/// <param name="buffers">
		///     An array of type System.Byte that is the storage location for the received data.
		/// </param>
		/// <param name="socketFlags">
		///     A bitwise combination of the System.Net.Sockets.SocketFlags values.
		/// </param>
		/// <param name="errorCode">
		///     A System.Net.Sockets.SocketError object that stores the socket error.
		/// </param>
		/// <param name="callback">
		///     An System.AsyncCallback delegate that references the method to invoke when the
		///     operation is complete.
		/// </param>
		/// <param name="state">
		///     A user-defined object that contains information about the receive operation.
		///     This object is passed to the System.Net.Sockets.Socket.EndReceive(System.IAsyncResult)
		///     delegate when the operation is complete.
		/// </param>
		/// <Returns>
		///     An System.IAsyncResult that references the asynchronous read.
		/// </Returns>
		///   <exception cref="System.ArgumentNullException">
		///     buffer is null.</exception>
		///   <exception cref="System.Net.Sockets.SocketException">
		///     An error occurred when attempting to access the socket. See the Remarks section
		///     for more information.</exception>
		///   <exception cref="System.ObjectDisposedException">The System.Net.Sockets.Socket has been closed.</exception>
		IAsyncResult BeginReceive(IList<ArraySegment<byte>> buffers, SocketFlags socketFlags, out SocketError errorCode, AsyncCallback callback, object state);

		///<Summary>
		///     Begins to asynchronously receive data from a connected System.Net.Sockets.Socket.
		///
		/// Parameters:
		///   buffer:
		///     An array of type System.Byte that is the storage location for the received data.
		///
		///   offset:
		///     The zero-based position in the buffer parameter at which to store the received
		///     data.
		///
		///   size:
		///     The number of bytes to receive.
		///
		///   socketFlags:
		///     A bitwise combination of the System.Net.Sockets.SocketFlags values.
		///
		///   callback:
		///     An System.AsyncCallback delegate that references the method to invoke when the
		///     operation is complete.
		///
		///   state:
		///     A user-defined object that contains information about the receive operation.
		///     This object is passed to the System.Net.Sockets.Socket.EndReceive(System.IAsyncResult)
		///     delegate when the operation is complete.
		///
		///</Summary><Returns>
		///     An System.IAsyncResult that references the asynchronous read.
		///
		/// </Returns>
		///   <exception cref="System.ArgumentNullException"></exception>
		///     buffer is null.
		///
		///   <exception cref="System.Net.Sockets.SocketException"></exception>
		///     An error occurred when attempting to access the socket. See the Remarks section
		///     for more information.
		///
		///   <exception cref="System.ObjectDisposedException">The System.Net.Sockets.Socket has been closed.</exception>
		///     System.Net.Sockets.Socket has been closed.
		///
		///   <exception cref="System.ArgumentOutOfRangeException"></exception>
		///     offset is less than 0.-or- offset is greater than the length of buffer.-or- size
		///     is less than 0.-or- size is greater than the length of buffer minus the value
		///     of the offset parameter.
		IAsyncResult BeginReceive(byte[] buffer, int offset, int size, SocketFlags socketFlags, AsyncCallback callback, object state);
		///
		///<Summary>
		///     Begins to asynchronously receive data from a connected System.Net.Sockets.Socket.
		///
		/// Parameters:
		///   buffer:
		///     An array of type System.Byte that is the storage location for the received data.
		///
		///   offset:
		///     The location in buffer to store the received data.
		///
		///   size:
		///     The number of bytes to receive.
		///
		///   socketFlags:
		///     A bitwise combination of the System.Net.Sockets.SocketFlags values.
		///
		///   errorCode:
		///     A System.Net.Sockets.SocketError object that stores the socket error.
		///
		///   callback:
		///     An System.AsyncCallback delegate that references the method to invoke when the
		///     operation is complete.
		///
		///   state:
		///     A user-defined object that contains information about the receive operation.
		///     This object is passed to the System.Net.Sockets.Socket.EndReceive(System.IAsyncResult)
		///     delegate when the operation is complete.
		///
		///</Summary><Returns>
		///     An System.IAsyncResult that references the asynchronous read.
		///
		/// </Returns>
		///   <exception cref="System.ArgumentNullException"></exception>
		///     buffer is null.
		///
		///   <exception cref="System.Net.Sockets.SocketException"></exception>
		///     An error occurred when attempting to access the socket. See the Remarks section
		///     for more information.
		///
		///   <exception cref="System.ObjectDisposedException">The System.Net.Sockets.Socket has been closed.</exception>
		///     System.Net.Sockets.Socket has been closed.
		///
		///   <exception cref="System.ArgumentOutOfRangeException"></exception>
		///     offset is less than 0.-or- offset is greater than the length of buffer.-or- size
		///     is less than 0.-or- size is greater than the length of buffer minus the value
		///     of the offset parameter.
		IAsyncResult BeginReceive(byte[] buffer, int offset, int size, SocketFlags socketFlags, out SocketError errorCode, AsyncCallback callback, object state);
		///
		///<Summary>
		///     Begins to asynchronously receive data from a specified network device.
		///
		/// Parameters:
		///   buffer:
		///     An array of type System.Byte that is the storage location for the received data.
		///
		///   offset:
		///     The zero-based position in the buffer parameter at which to store the data.
		///
		///   size:
		///     The number of bytes to receive.
		///
		///   socketFlags:
		///     A bitwise combination of the System.Net.Sockets.SocketFlags values.
		///
		///   remoteEP:
		///     An System.Net.EndPoint that represents the source of the data.
		///
		///   callback:
		///     The System.AsyncCallback delegate.
		///
		///   state:
		///     An object that contains state information for this request.
		///
		///</Summary><Returns>
		///     An System.IAsyncResult that references the asynchronous read.
		///
		/// </Returns>
		///   <exception cref="System.ArgumentNullException"></exception>
		///     buffer is null.-or- remoteEP is null.
		///
		///   <exception cref="System.Net.Sockets.SocketException"></exception>
		///     An error occurred when attempting to access the socket. See the Remarks section
		///     for more information.
		///
		///   <exception cref="System.ArgumentOutOfRangeException"></exception>
		///     offset is less than 0.-or- offset is greater than the length of buffer.-or- size
		///     is less than 0.-or- size is greater than the length of buffer minus the value
		///     of the offset parameter.
		///
		///   <exception cref="System.ObjectDisposedException">The System.Net.Sockets.Socket has been closed.</exception>
		
		///
		///   <exception cref="System.Security.SecurityException"></exception>
		///     A caller higher in the call stack does not have permission for the requested
		///     operation.
		IAsyncResult BeginReceiveFrom(byte[] buffer, int offset, int size, SocketFlags socketFlags, ref EndPoint remoteEP, AsyncCallback callback, object state);
		///
		///<Summary>
		///     Begins to asynchronously receive the specified number of bytes of data into the
		///     specified location of the data buffer, using the specified System.Net.Sockets.SocketFlags,
		///     and stores the endpoint and packet information..
		///
		/// Parameters:
		///   buffer:
		///     An array of type System.Byte that is the storage location for the received data.
		///
		///   offset:
		///     The zero-based position in the buffer parameter at which to store the data.
		///
		///   size:
		///     The number of bytes to receive.
		///
		///   socketFlags:
		///     A bitwise combination of the System.Net.Sockets.SocketFlags values.
		///
		///   remoteEP:
		///     An System.Net.EndPoint that represents the source of the data.
		///
		///   callback:
		///     The System.AsyncCallback delegate.
		///
		///   state:
		///     An object that contains state information for this request.
		///
		///</Summary><Returns>
		///     An System.IAsyncResult that references the asynchronous read.
		///
		/// </Returns>
		///   <exception cref="System.ArgumentNullException"></exception>
		///     buffer is null.-or- remoteEP is null.
		///
		///   <exception cref="System.Net.Sockets.SocketException"></exception>
		///     An error occurred when attempting to access the socket. See the Remarks section
		///     for more information.
		///
		///   <exception cref="System.ArgumentOutOfRangeException"></exception>
		///     offset is less than 0.-or- offset is greater than the length of buffer.-or- size
		///     is less than 0.-or- size is greater than the length of buffer minus the value
		///     of the offset parameter.
		///
		///   <exception cref="System.ObjectDisposedException">The System.Net.Sockets.Socket has been closed.</exception>
		
		///
		///   <exception cref="System.NotSupportedException"></exception>
		///     The operating system is Windows 2000 or earlier, and this method requires Windows
		///     XP.
		IAsyncResult BeginReceiveMessageFrom(byte[] buffer, int offset, int size, SocketFlags socketFlags, ref EndPoint remoteEP, AsyncCallback callback, object state);
		///
		///<Summary>
		///     Sends data asynchronously to a connected System.Net.Sockets.Socket.
		///
		/// Parameters:
		///   buffers:
		///     An array of type System.Byte that contains the data to send.
		///
		///   socketFlags:
		///     A bitwise combination of the System.Net.Sockets.SocketFlags values.
		///
		///   callback:
		///     The System.AsyncCallback delegate.
		///
		///   state:
		///     An object that contains state information for this request.
		///
		///</Summary><Returns>
		///     An System.IAsyncResult that references the asynchronous send.
		///
		/// </Returns>
		///   <exception cref="System.ArgumentNullException"></exception>
		///     buffers is null.
		///
		///   <exception cref="System.ArgumentException"></exception>
		///     buffers is empty.
		///
		///   <exception cref="System.Net.Sockets.SocketException"></exception>
		///     An error occurred when attempting to access the socket. See remarks section below.
		///
		///   <exception cref="System.ObjectDisposedException">The System.Net.Sockets.Socket has been closed.</exception>
		
		IAsyncResult BeginSend(IList<ArraySegment<byte>> buffers, SocketFlags socketFlags, AsyncCallback callback, object state);
		///
		///<Summary>
		///     Sends data asynchronously to a connected System.Net.Sockets.Socket.
		///
		/// Parameters:
		///   buffers:
		///     An array of type System.Byte that contains the data to send.
		///
		///   socketFlags:
		///     A bitwise combination of the System.Net.Sockets.SocketFlags values.
		///
		///   errorCode:
		///     A System.Net.Sockets.SocketError object that stores the socket error.
		///
		///   callback:
		///     The System.AsyncCallback delegate.
		///
		///   state:
		///     An object that contains state information for this request.
		///
		///</Summary><Returns>
		///     An System.IAsyncResult that references the asynchronous send.
		///
		/// </Returns>
		///   <exception cref="System.ArgumentNullException"></exception>
		///     buffers is null.
		///
		///   <exception cref="System.ArgumentException"></exception>
		///     buffers is empty.
		///
		///   <exception cref="System.Net.Sockets.SocketException"></exception>
		///     An error occurred when attempting to access the socket. See remarks section below.
		///
		///   <exception cref="System.ObjectDisposedException">The System.Net.Sockets.Socket has been closed.</exception>
		
		IAsyncResult BeginSend(IList<ArraySegment<byte>> buffers, SocketFlags socketFlags, out SocketError errorCode, AsyncCallback callback, object state);
		///
		///<Summary>
		///     Sends data asynchronously to a connected System.Net.Sockets.Socket.
		///
		/// Parameters:
		///   buffer:
		///     An array of type System.Byte that contains the data to send.
		///
		///   offset:
		///     The zero-based position in the buffer parameter at which to begin sending data.
		///
		///   size:
		///     The number of bytes to send.
		///
		///   socketFlags:
		///     A bitwise combination of the System.Net.Sockets.SocketFlags values.
		///
		///   callback:
		///     The System.AsyncCallback delegate.
		///
		///   state:
		///     An object that contains state information for this request.
		///
		///</Summary><Returns>
		///     An System.IAsyncResult that references the asynchronous send.
		///
		/// </Returns>
		///   <exception cref="System.ArgumentNullException"></exception>
		///     buffer is null.
		///
		///   <exception cref="System.Net.Sockets.SocketException"></exception>
		///     An error occurred when attempting to access the socket. See remarks section below.
		///
		///   <exception cref="System.ArgumentOutOfRangeException"></exception>
		///     offset is less than 0.-or- offset is less than the length of buffer.-or- size
		///     is less than 0.-or- size is greater than the length of buffer minus the value
		///     of the offset parameter.
		///
		///   <exception cref="System.ObjectDisposedException">The System.Net.Sockets.Socket has been closed.</exception>
		
		IAsyncResult BeginSend(byte[] buffer, int offset, int size, SocketFlags socketFlags, AsyncCallback callback, object state);
		///
		///<Summary>
		///     Sends data asynchronously to a connected System.Net.Sockets.Socket.
		///
		/// Parameters:
		///   buffer:
		///     An array of type System.Byte that contains the data to send.
		///
		///   offset:
		///     The zero-based position in the buffer parameter at which to begin sending data.
		///
		///   size:
		///     The number of bytes to send.
		///
		///   socketFlags:
		///     A bitwise combination of the System.Net.Sockets.SocketFlags values.
		///
		///   errorCode:
		///     A System.Net.Sockets.SocketError object that stores the socket error.
		///
		///   callback:
		///     The System.AsyncCallback delegate.
		///
		///   state:
		///     An object that contains state information for this request.
		///
		///</Summary><Returns>
		///     An System.IAsyncResult that references the asynchronous send.
		///
		/// </Returns>
		///   <exception cref="System.ArgumentNullException"></exception>
		///     buffer is null.
		///
		///   <exception cref="System.Net.Sockets.SocketException"></exception>
		///     An error occurred when attempting to access the socket. See remarks section below.
		///
		///   <exception cref="System.ArgumentOutOfRangeException"></exception>
		///     offset is less than 0.-or- offset is less than the length of buffer.-or- size
		///     is less than 0.-or- size is greater than the length of buffer minus the value
		///     of the offset parameter.
		///
		///   <exception cref="System.ObjectDisposedException">The System.Net.Sockets.Socket has been closed.</exception>
		
		IAsyncResult BeginSend(byte[] buffer, int offset, int size, SocketFlags socketFlags, out SocketError errorCode, AsyncCallback callback, object state);
		///
		///<Summary>
		///     Sends the file fileName to a connected System.Net.Sockets.Socket object using
		///     the System.Net.Sockets.TransmitFileOptions.UseDefaultWorkerThread flag.
		///
		/// Parameters:
		///   fileName:
		///     A string that contains the path and name of the file to send. This parameter
		///     can be null.
		///
		///   callback:
		///     The System.AsyncCallback delegate.
		///
		///   state:
		///     An object that contains state information for this request.
		///
		///</Summary><Returns>
		///     An System.IAsyncResult object that represents the asynchronous send.
		///
		/// </Returns>
		///   <exception cref="System.ObjectDisposedException">The System.Net.Sockets.Socket has been closed.</exception>
		///     The System.Net.Sockets.Socket object has been closed.
		///
		///   <exception cref="System.NotSupportedException"></exception>
		///     The socket is not connected to a remote host.
		///
		///   <exception cref="System.IO.FileNotFoundException"></exception>
		///     The file fileName was not found.
		///
		///   <exception cref="System.Net.Sockets.SocketException"></exception>
		///     An error occurred when attempting to access the socket. See remarks section below.
		IAsyncResult BeginSendFile(string fileName, AsyncCallback callback, object state);
		///
		///<Summary>
		///     Sends a file and buffers of data asynchronously to a connected System.Net.Sockets.Socket
		///     object.
		///
		/// Parameters:
		///   fileName:
		///     A string that contains the path and name of the file to be sent. This parameter
		///     can be null.
		///
		///   preBuffer:
		///     A System.Byte array that contains data to be sent before the file is sent. This
		///     parameter can be null.
		///
		///   postBuffer:
		///     A System.Byte array that contains data to be sent after the file is sent. This
		///     parameter can be null.
		///
		///   flags:
		///     A bitwise combination of System.Net.Sockets.TransmitFileOptions values.
		///
		///   callback:
		///     An System.AsyncCallback delegate to be invoked when this operation completes.
		///     This parameter can be null.
		///
		///   state:
		///     A user-defined object that contains state information for this request. This
		///     parameter can be null.
		///
		///</Summary><Returns>
		///     An System.IAsyncResult object that represents the asynchronous operation.
		///
		/// </Returns>
		///   <exception cref="System.ObjectDisposedException">The System.Net.Sockets.Socket has been closed.</exception>
		///     The System.Net.Sockets.Socket object has been closed.
		///
		///   <exception cref="System.Net.Sockets.SocketException"></exception>
		///     An error occurred when attempting to access the socket. See remarks section below.
		///
		///   <exception cref="System.NotSupportedException"></exception>
		///     The operating system is not Windows NT or later.- or - The socket is not connected
		///     to a remote host.
		///
		///   <exception cref="System.IO.FileNotFoundException"></exception>
		///     The file fileName was not found.
		IAsyncResult BeginSendFile(string fileName, byte[] preBuffer, byte[] postBuffer, TransmitFileOptions flags, AsyncCallback callback, object state);
		///
		///<Summary>
		///     Sends data asynchronously to a specific remote host.
		///
		/// Parameters:
		///   buffer:
		///     An array of type System.Byte that contains the data to send.
		///
		///   offset:
		///     The zero-based position in buffer at which to begin sending data.
		///
		///   size:
		///     The number of bytes to send.
		///
		///   socketFlags:
		///     A bitwise combination of the System.Net.Sockets.SocketFlags values.
		///
		///   remoteEP:
		///     An System.Net.EndPoint that represents the remote device.
		///
		///   callback:
		///     The System.AsyncCallback delegate.
		///
		///   state:
		///     An object that contains state information for this request.
		///
		///</Summary><Returns>
		///     An System.IAsyncResult that references the asynchronous send.
		///
		/// </Returns>
		///   <exception cref="System.ArgumentNullException"></exception>
		///     buffer is null.-or- remoteEP is null.
		///
		///   <exception cref="System.Net.Sockets.SocketException"></exception>
		///     An error occurred when attempting to access the socket. See the Remarks section
		///     for more information.
		///
		///   <exception cref="System.ArgumentOutOfRangeException"></exception>
		///     offset is less than 0.-or- offset is greater than the length of buffer.-or- size
		///     is less than 0.-or- size is greater than the length of buffer minus the value
		///     of the offset parameter.
		///
		///   <exception cref="System.ObjectDisposedException">The System.Net.Sockets.Socket has been closed.</exception>
		
		///
		///   <exception cref="System.Security.SecurityException"></exception>
		///     A caller higher in the call stack does not have permission for the requested
		///     operation.
		IAsyncResult BeginSendTo(byte[] buffer, int offset, int size, SocketFlags socketFlags, EndPoint remoteEP, AsyncCallback callback, object state);

		///<Summary>
		///     Associates a System.Net.Sockets.Socket with a local endpoint.
		///</Summary>
		/// <param name="localEP">
		///     The local System.Net.EndPoint to associate with the System.Net.Sockets.Socket.
		/// </param>
		///   <exception cref="System.ArgumentNullException">
		///     localEP is null.
		/// </exception>
		///   <exception cref="System.Net.Sockets.SocketException">
		///     An error occurred when attempting to access the socket. See the Remarks section
		///     for more information.
		/// </exception>
		///   <exception cref="System.ObjectDisposedException">The System.Net.Sockets.Socket has been closed.</exception>
		///   <exception cref="System.Security.SecurityException">
		///     A caller higher in the call stack does not have permission for the requested
		///     operation.
		/// </exception>
		void Bind(EndPoint localEP);

		///<Summary>
		///     Closes the System.Net.Sockets.Socket connection and releases all associated resources.
		/// </Summary>
		void Close();

		///<Summary>
		///     Closes the System.Net.Sockets.Socket connection and releases all associated resources
		///     with a specified timeout to allow queued data to be sent.
		/// </Summary>
		///<param name="timeout">Wait up to timeout seconds to send any remaining data, then close the socket.</param>
		void Close(int timeout);

		///<Summary>
		///     Establishes a connection to a remote host.
		///</Summary>
		/// <param name="remoteEP">An System.Net.EndPoint that represents the remote device.</param>
		///   <exception cref="System.ArgumentNullException">
		///     remoteEP is null.
		/// </exception>
		///   <exception cref="System.Net.Sockets.SocketException">
		///     An error occurred when attempting to access the socket. See the Remarks section
		///     for more information.</exception>
		///   <exception cref="System.ObjectDisposedException">The System.Net.Sockets.Socket has been closed.</exception>
		///   <exception cref="System.Security.SecurityException">
		///     A caller higher in the call stack does not have permission for the requested
		///     operation.</exception>
		///   <exception cref="System.InvalidOperationException">
		///     The System.Net.Sockets.Socket is System.Net.Sockets.Socket.Listen(System.Int32)ing.</exception>
		void Connect(EndPoint remoteEP);

		///<Summary>
		///     Establishes a connection to a remote host. The host is specified by an IP address
		///     and a port number.
		/// </Summary>
		///<param name="address">
		///     The IP address of the remote host.
		/// </param>
		/// <param name="port">
		///     The port number of the remote host.
		/// </param>
		///   <exception cref="System.ArgumentNullException">
		///     address is null.
		/// </exception>
		///   <exception cref="System.ArgumentOutOfRangeException">
		///     The port number is not valid.</exception>
		///   <exception cref="System.Net.Sockets.SocketException">
		///     An error occurred when attempting to access the socket. See the Remarks section
		///     for more information.</exception>
		///   <exception cref="System.ObjectDisposedException">The System.Net.Sockets.Socket has been closed.</exception>
		///   <exception cref="System.NotSupportedException">
		///     This method is valid for sockets in the System.Net.Sockets.AddressFamily.InterNetwork
		///     or System.Net.Sockets.AddressFamily.InterNetworkV6 families.</exception>
		///   <exception cref="System.ArgumentException">
		///     The length of address is zero.</exception>
		///   <exception cref="System.InvalidOperationException">
		///     The System.Net.Sockets.Socket is System.Net.Sockets.Socket.Listen(System.Int32)ing.
		/// </exception>
		void Connect(IPAddress address, int port);

		///<Summary>
		///     Establishes a connection to a remote host. The host is specified by a host name
		///     and a port number.
		///</Summary>
		/// <param name="host">The name of the remote host.</param>
		/// <param name="port">The port number of the remote host.</param>
		///   <exception cref="System.ArgumentNullException">
		///     host is null.</exception>
		///   <exception cref="System.ArgumentOutOfRangeException">
		///     The port number is not valid.</exception>
		///   <exception cref="System.Net.Sockets.SocketException">
		///     An error occurred when attempting to access the socket. See the Remarks section
		///     for more information.</exception>
		///   <exception cref="System.ObjectDisposedException">The System.Net.Sockets.Socket has been closed.</exception>
		///   <exception cref="System.NotSupportedException">
		///     This method is valid for sockets in the System.Net.Sockets.AddressFamily.InterNetwork
		///     or System.Net.Sockets.AddressFamily.InterNetworkV6 families.</exception>
		///   <exception cref="System.InvalidOperationException">
		///     The System.Net.Sockets.Socket is System.Net.Sockets.Socket.Listen(System.Int32)ing.</exception>
		void Connect(string host, int port);

		///<Summary>
		///     Establishes a connection to a remote host. The host is specified by an array
		///     of IP addresses and a port number.
		///</Summary>
		/// <param name="addresses">
		///     The IP addresses of the remote host.
		/// </param>
		/// <param name="port">
		///     The port number of the remote host.
		/// </param>
		///   <exception cref="System.ArgumentNullException">
		///     addresses is null.</exception>
		///   <exception cref="System.ArgumentOutOfRangeException">
		///     The port number is not valid.
		/// </exception>
		///   <exception cref="System.Net.Sockets.SocketException">
		///     An error occurred when attempting to access the socket. See the Remarks section
		///     for more information.</exception>
		///   <exception cref="System.ObjectDisposedException">The System.Net.Sockets.Socket has been closed.</exception>
		///   <exception cref="System.NotSupportedException">
		///     This method is valid for sockets in the System.Net.Sockets.AddressFamily.InterNetwork
		///     or System.Net.Sockets.AddressFamily.InterNetworkV6 families.
		/// </exception>
		///   <exception cref="System.ArgumentException">
		///     The length of address is zero.</exception>
		///   <exception cref="System.InvalidOperationException">
		///     The System.Net.Sockets.Socket is System.Net.Sockets.Socket.Listen(System.Int32)ing.</exception>
		void Connect(IPAddress[] addresses, int port);
		///
		///<Summary>
		///     Begins an asynchronous request for a connection to a remote host.
		///
		/// Parameters:
		///   e:
		///     The System.Net.Sockets.SocketAsyncEventArgs object to use for this asynchronous
		///     socket operation.
		///
		///</Summary><Returns>
		///     Returns true if the I/O operation is pending. The System.Net.Sockets.SocketAsyncEventArgs.Completed
		///     event on the e parameter will be raised upon completion of the operation. Returns
		///     false if the I/O operation completed synchronously. In this case, The System.Net.Sockets.SocketAsyncEventArgs.Completed
		///     event on the e parameter will not be raised and the e object passed as a parameter
		///     may be examined immediately after the method call returns to retrieve the result
		///     of the operation.
		///
		/// </Returns>
		///   <exception cref="System.ArgumentException"></exception>
		///     An argument is not valid. This exception occurs if multiple buffers are specified,
		///     the System.Net.Sockets.SocketAsyncEventArgs.BufferList property is not null.
		///
		///   <exception cref="System.ArgumentNullException"></exception>
		///     The e parameter cannot be null and the System.Net.Sockets.SocketAsyncEventArgs.RemoteEndPoint
		///     cannot be null.
		///
		///   <exception cref="System.InvalidOperationException"></exception>
		///     The System.Net.Sockets.Socket is listening or a socket operation was already
		///     in progress using the System.Net.Sockets.SocketAsyncEventArgs object specified
		///     in the e parameter.
		///
		///   <exception cref="System.Net.Sockets.SocketException"></exception>
		///     An error occurred when attempting to access the socket. See the Remarks section
		///     for more information.
		///
		///   <exception cref="System.NotSupportedException"></exception>
		///     Windows XP or later is required for this method. This exception also occurs if
		///     the local endpoint and the System.Net.Sockets.SocketAsyncEventArgs.RemoteEndPoint
		///     are not the same address family.
		///
		///   <exception cref="System.ObjectDisposedException">The System.Net.Sockets.Socket has been closed.</exception>
		
		///
		///   <exception cref="System.Security.SecurityException"></exception>
		///     A caller higher in the call stack does not have permission for the requested
		///     operation.
		bool ConnectAsync(SocketAsyncEventArgs e);

		///<Summary>
		///     Closes the socket connection and allows reuse of the socket.
		///</Summary>
		/// <param name="reuseSocket">
		///     true if this socket can be reused after the current connection is closed; otherwise,
		///     false.
		/// </param>
		///   <exception cref="System.PlatformNotSupportedException">
		///     This method requires Windows 2000 or earlier, or the exception will be thrown.
		/// </exception>
		///   <exception cref="System.ObjectDisposedException">The System.Net.Sockets.Socket has been closed.</exception>
		///   <exception cref="System.Net.Sockets.SocketException">
		///     An error occurred when attempting to access the socket. See the Remarks section
		///     for more information.
		/// </exception>
		void Disconnect(bool reuseSocket);

		///<Summary>
		///     Begins an asynchronous request to disconnect from a remote endpoint.
		///</Summary>
		/// <param name="e">
		///     The System.Net.Sockets.SocketAsyncEventArgs object to use for this asynchronous
		///     socket operation.
		/// </param>
		/// <Returns>
		///     Returns true if the I/O operation is pending. The System.Net.Sockets.SocketAsyncEventArgs.Completed
		///     event on the e parameter will be raised upon completion of the operation. Returns
		///     false if the I/O operation completed synchronously. In this case, The System.Net.Sockets.SocketAsyncEventArgs.Completed
		///     event on the e parameter will not be raised and the e object passed as a parameter
		///     may be examined immediately after the method call returns to retrieve the result
		///     of the operation.
		/// </Returns>
		///   <exception cref="System.ArgumentNullException">
		///     The e parameter cannot be null.
		/// </exception>
		///   <exception cref="System.InvalidOperationException">
		///     A socket operation was already in progress using the System.Net.Sockets.SocketAsyncEventArgs
		///     object specified in the e parameter.</exception>
		///   <exception cref="System.NotSupportedException">
		///     Windows XP or later is required for this method.
		/// </exception>
		///   <exception cref="System.ObjectDisposedException">The System.Net.Sockets.Socket has been closed.</exception>
		///   <exception cref="System.Net.Sockets.SocketException">
		///     An error occurred when attempting to access the socket.
		/// </exception>
		bool DisconnectAsync(SocketAsyncEventArgs e);

		///
		///<Summary>
		///     Duplicates the socket reference for the target process, and closes the socket
		///     for this process.
		///
		/// Parameters:
		///   targetProcessId:
		///     The ID of the target process where a duplicate of the socket reference is created.
		///
		///</Summary><Returns>
		///     The socket reference to be passed to the target process.
		///
		/// </Returns>
		///   <exception cref="System.Net.Sockets.SocketException"></exception>
		///     targetProcessID is not a valid process id.-or- Duplication of the socket reference
		///     failed.
		SocketInformation DuplicateAndClose(int targetProcessId);
		///
		///<Summary>
		///     Asynchronously accepts an incoming connection attempt and creates a new System.Net.Sockets.Socket
		///     to handle remote host communication.
		///
		/// Parameters:
		///   asyncResult:
		///     An System.IAsyncResult that stores state information for this asynchronous operation
		///     as well as any user defined data.
		///
		///</Summary><Returns>
		///     A System.Net.Sockets.Socket to handle communication with the remote host.
		///
		/// </Returns>
		///   <exception cref="System.ArgumentNullException"></exception>
		///     asyncResult is null.
		///
		///   <exception cref="System.ArgumentException"></exception>
		///     asyncResult was not created by a call to System.Net.Sockets.Socket.BeginAccept(System.AsyncCallback,System.Object).
		///
		///   <exception cref="System.Net.Sockets.SocketException"></exception>
		///     An error occurred when attempting to access the socket. See the Remarks section
		///     for more information.
		///
		///   <exception cref="System.ObjectDisposedException">The System.Net.Sockets.Socket has been closed.</exception>
		
		///
		///   <exception cref="System.InvalidOperationException"></exception>
		///     System.Net.Sockets.Socket.EndAccept(System.IAsyncResult) method was previously
		///     called.
		///
		///   <exception cref="System.NotSupportedException"></exception>
		///     Windows NT is required for this method.
		ISocket EndAccept(IAsyncResult asyncResult);
		///
		///<Summary>
		///     Asynchronously accepts an incoming connection attempt and creates a new System.Net.Sockets.Socket
		///     object to handle remote host communication. This method returns a buffer that
		///     contains the initial data transferred.
		///
		/// Parameters:
		///   buffer:
		///     An array of type System.Byte that contains the bytes transferred.
		///
		///   asyncResult:
		///     An System.IAsyncResult object that stores state information for this asynchronous
		///     operation as well as any user defined data.
		///
		///</Summary><Returns>
		///     A System.Net.Sockets.Socket object to handle communication with the remote host.
		///
		/// </Returns>
		///   <exception cref="System.NotSupportedException"></exception>
		///     Windows NT is required for this method.
		///
		///   <exception cref="System.ObjectDisposedException">The System.Net.Sockets.Socket has been closed.</exception>
		///     The System.Net.Sockets.Socket object has been closed.
		///
		///   <exception cref="System.ArgumentNullException"></exception>
		///     asyncResult is empty.
		///
		///   <exception cref="System.ArgumentException"></exception>
		///     asyncResult was not created by a call to System.Net.Sockets.Socket.BeginAccept(System.AsyncCallback,System.Object).
		///
		///   <exception cref="System.InvalidOperationException"></exception>
		///     System.Net.Sockets.Socket.EndAccept(System.IAsyncResult) method was previously
		///     called.
		///
		///   <exception cref="System.Net.Sockets.SocketException"></exception>
		///     An error occurred when attempting to access the System.Net.Sockets.Socket See
		///     the Remarks section for more information.
		ISocket EndAccept(out byte[] buffer, IAsyncResult asyncResult);
		///
		///<Summary>
		///     Asynchronously accepts an incoming connection attempt and creates a new System.Net.Sockets.Socket
		///     object to handle remote host communication. This method returns a buffer that
		///     contains the initial data and the number of bytes transferred.
		///
		/// Parameters:
		///   buffer:
		///     An array of type System.Byte that contains the bytes transferred.
		///
		///   bytesTransferred:
		///     The number of bytes transferred.
		///
		///   asyncResult:
		///     An System.IAsyncResult object that stores state information for this asynchronous
		///     operation as well as any user defined data.
		///
		///</Summary><Returns>
		///     A System.Net.Sockets.Socket object to handle communication with the remote host.
		///
		/// </Returns>
		///   <exception cref="System.NotSupportedException"></exception>
		///     Windows NT is required for this method.
		///
		///   <exception cref="System.ObjectDisposedException">The System.Net.Sockets.Socket has been closed.</exception>
		///     The System.Net.Sockets.Socket object has been closed.
		///
		///   <exception cref="System.ArgumentNullException"></exception>
		///     asyncResult is empty.
		///
		///   <exception cref="System.ArgumentException"></exception>
		///     asyncResult was not created by a call to System.Net.Sockets.Socket.BeginAccept(System.AsyncCallback,System.Object).
		///
		///   <exception cref="System.InvalidOperationException"></exception>
		///     System.Net.Sockets.Socket.EndAccept(System.IAsyncResult) method was previously
		///     called.
		///
		///   <exception cref="System.Net.Sockets.SocketException"></exception>
		///     An error occurred when attempting to access the System.Net.Sockets.Socket. See
		///     the Remarks section for more information.
		ISocket EndAccept(out byte[] buffer, out int bytesTransferred, IAsyncResult asyncResult);

		///<Summary>
		///     Ends a pending asynchronous connection request.
		///</Summary>
		/// <param name="asyncResult">
		///     An System.IAsyncResult that stores state information and any user defined data
		///     for this asynchronous operation.
		/// </param>
		///   <exception cref="System.ArgumentNullException">
		///     asyncResult is null.
		/// </exception>
		///   <exception cref="System.ArgumentException">
		///     asyncResult was not returned by a call to the System.Net.Sockets.Socket.BeginConnect(System.Net.EndPoint,System.AsyncCallback,System.Object)
		///     method.
		/// </exception>
		///   <exception cref="System.InvalidOperationException">
		///     System.Net.Sockets.Socket.EndConnect(System.IAsyncResult) was previously called
		///     for the asynchronous connection.
		/// </exception>
		///   <exception cref="System.Net.Sockets.SocketException">
		///     An error occurred when attempting to access the socket. See the Remarks section
		///     for more information.
		/// </exception>
		///   <exception cref="System.ObjectDisposedException">The System.Net.Sockets.Socket has been closed.</exception>
		void EndConnect(IAsyncResult asyncResult);

		///<Summary>
		///     Ends a pending asynchronous disconnect request.
		///</Summary>
		/// <param name="asyncResult">
		///     An System.IAsyncResult object that stores state information and any user-defined
		///     data for this asynchronous operation.
		/// </param>
		///   <exception cref="System.NotSupportedException">
		///     The operating system is Windows 2000 or earlier, and this method requires Windows
		///     XP.
		/// </exception>
		///   <exception cref="System.ObjectDisposedException">The System.Net.Sockets.Socket has been closed.</exception>
		///   <exception cref="System.ArgumentNullException">
		///     asyncResult is null.
		/// </exception>
		///   <exception cref="System.ArgumentException">
		///     asyncResult was not returned by a call to the System.Net.Sockets.Socket.BeginDisconnect(System.Boolean,System.AsyncCallback,System.Object)
		///     method.
		/// </exception>
		///   <exception cref="System.InvalidOperationException">
		///     System.Net.Sockets.Socket.EndDisconnect(System.IAsyncResult) was previously called
		///     for the asynchronous connection.
		/// </exception>
		///   <exception cref="System.Net.Sockets.SocketException">
		///     An error occurred when attempting to access the socket. See the Remarks section
		///     for more information.
		/// </exception>
		///   <exception cref="System.Net.WebException">
		///     The disconnect request has timed out.
		/// </exception>
		void EndDisconnect(IAsyncResult asyncResult);
		///
		///<Summary>
		///     Ends a pending asynchronous read.
		///
		/// Parameters:
		///   asyncResult:
		///     An System.IAsyncResult that stores state information and any user defined data
		///     for this asynchronous operation.
		///
		///</Summary><Returns>
		///     The number of bytes received.
		///
		/// </Returns>
		///   <exception cref="System.ArgumentNullException"></exception>
		///     asyncResult is null.
		///
		///   <exception cref="System.ArgumentException"></exception>
		///     asyncResult was not returned by a call to the System.Net.Sockets.Socket.BeginReceive(System.Byte[],System.Int32,System.Int32,System.Net.Sockets.SocketFlags,System.AsyncCallback,System.Object)
		///     method.
		///
		///   <exception cref="System.InvalidOperationException"></exception>
		///     System.Net.Sockets.Socket.EndReceive(System.IAsyncResult) was previously called
		///     for the asynchronous read.
		///
		///   <exception cref="System.Net.Sockets.SocketException"></exception>
		///     An error occurred when attempting to access the socket. See the Remarks section
		///     for more information.
		///
		///   <exception cref="System.ObjectDisposedException">The System.Net.Sockets.Socket has been closed.</exception>
		
		int EndReceive(IAsyncResult asyncResult);
		///
		///<Summary>
		///     Ends a pending asynchronous read.
		///
		/// Parameters:
		///   asyncResult:
		///     An System.IAsyncResult that stores state information and any user defined data
		///     for this asynchronous operation.
		///
		///   errorCode:
		///     A System.Net.Sockets.SocketError object that stores the socket error.
		///
		///</Summary><Returns>
		///     The number of bytes received.
		///
		/// </Returns>
		///   <exception cref="System.ArgumentNullException"></exception>
		///     asyncResult is null.
		///
		///   <exception cref="System.ArgumentException"></exception>
		///     asyncResult was not returned by a call to the System.Net.Sockets.Socket.BeginReceive(System.Byte[],System.Int32,System.Int32,System.Net.Sockets.SocketFlags,System.AsyncCallback,System.Object)
		///     method.
		///
		///   <exception cref="System.InvalidOperationException"></exception>
		///     System.Net.Sockets.Socket.EndReceive(System.IAsyncResult) was previously called
		///     for the asynchronous read.
		///
		///   <exception cref="System.Net.Sockets.SocketException"></exception>
		///     An error occurred when attempting to access the socket. See the Remarks section
		///     for more information.
		///
		///   <exception cref="System.ObjectDisposedException">The System.Net.Sockets.Socket has been closed.</exception>
		
		int EndReceive(IAsyncResult asyncResult, out SocketError errorCode);
		///
		///<Summary>
		///     Ends a pending asynchronous read from a specific endpoint.
		///
		/// Parameters:
		///   asyncResult:
		///     An System.IAsyncResult that stores state information and any user defined data
		///     for this asynchronous operation.
		///
		///   endPoint:
		///     The source System.Net.EndPoint.
		///
		///</Summary><Returns>
		///     If successful, the number of bytes received. If unsuccessful, returns 0.
		///
		/// </Returns>
		///   <exception cref="System.ArgumentNullException"></exception>
		///     asyncResult is null.
		///
		///   <exception cref="System.ArgumentException"></exception>
		///     asyncResult was not returned by a call to the System.Net.Sockets.Socket.BeginReceiveFrom(System.Byte[],System.Int32,System.Int32,System.Net.Sockets.SocketFlags,System.Net.EndPoint@,System.AsyncCallback,System.Object)
		///     method.
		///
		///   <exception cref="System.InvalidOperationException"></exception>
		///     System.Net.Sockets.Socket.EndReceiveFrom(System.IAsyncResult,System.Net.EndPoint@)
		///     was previously called for the asynchronous read.
		///
		///   <exception cref="System.Net.Sockets.SocketException"></exception>
		///     An error occurred when attempting to access the socket. See the Remarks section
		///     for more information.
		///
		///   <exception cref="System.ObjectDisposedException">The System.Net.Sockets.Socket has been closed.</exception>
		
		int EndReceiveFrom(IAsyncResult asyncResult, ref EndPoint endPoint);
		///
		///<Summary>
		///     Ends a pending asynchronous read from a specific endpoint. This method also reveals
		///     more information about the packet than System.Net.Sockets.Socket.EndReceiveFrom(System.IAsyncResult,System.Net.EndPoint@).
		///
		/// Parameters:
		///   asyncResult:
		///     An System.IAsyncResult that stores state information and any user defined data
		///     for this asynchronous operation.
		///
		///   socketFlags:
		///     A bitwise combination of the System.Net.Sockets.SocketFlags values for the received
		///     packet.
		///
		///   endPoint:
		///     The source System.Net.EndPoint.
		///
		///   ipPacketInformation:
		///     The System.Net.IPAddress and interface of the received packet.
		///
		///</Summary><Returns>
		///     If successful, the number of bytes received. If unsuccessful, returns 0.
		///
		/// </Returns>
		///   <exception cref="System.ArgumentNullException"></exception>
		///     asyncResult is null-or- endPoint is null.
		///
		///   <exception cref="System.ArgumentException"></exception>
		///     asyncResult was not returned by a call to the System.Net.Sockets.Socket.BeginReceiveMessageFrom(System.Byte[],System.Int32,System.Int32,System.Net.Sockets.SocketFlags,System.Net.EndPoint@,System.AsyncCallback,System.Object)
		///     method.
		///
		///   <exception cref="System.InvalidOperationException"></exception>
		///     System.Net.Sockets.Socket.EndReceiveMessageFrom(System.IAsyncResult,System.Net.Sockets.SocketFlags@,System.Net.EndPoint@,System.Net.Sockets.IPPacketInformation@)
		///     was previously called for the asynchronous read.
		///
		///   <exception cref="System.Net.Sockets.SocketException"></exception>
		///     An error occurred when attempting to access the socket. See the Remarks section
		///     for more information.
		///
		///   <exception cref="System.ObjectDisposedException">The System.Net.Sockets.Socket has been closed.</exception>
		
		int EndReceiveMessageFrom(IAsyncResult asyncResult, ref SocketFlags socketFlags, ref EndPoint endPoint, out IPPacketInformation ipPacketInformation);
		///
		///<Summary>
		///     Ends a pending asynchronous send.
		///
		/// Parameters:
		///   asyncResult:
		///     An System.IAsyncResult that stores state information for this asynchronous operation.
		///
		///</Summary><Returns>
		///     If successful, the number of bytes sent to the System.Net.Sockets.Socket; otherwise,
		///     an invalid System.Net.Sockets.Socket error.
		///
		/// </Returns>
		///   <exception cref="System.ArgumentNullException"></exception>
		///     asyncResult is null.
		///
		///   <exception cref="System.ArgumentException"></exception>
		///     asyncResult was not returned by a call to the System.Net.Sockets.Socket.BeginSend(System.Byte[],System.Int32,System.Int32,System.Net.Sockets.SocketFlags,System.AsyncCallback,System.Object)
		///     method.
		///
		///   <exception cref="System.InvalidOperationException"></exception>
		///     System.Net.Sockets.Socket.EndSend(System.IAsyncResult) was previously called
		///     for the asynchronous send.
		///
		///   <exception cref="System.Net.Sockets.SocketException"></exception>
		///     An error occurred when attempting to access the socket. See the Remarks section
		///     for more information.
		///
		///   <exception cref="System.ObjectDisposedException">The System.Net.Sockets.Socket has been closed.</exception>
		
		int EndSend(IAsyncResult asyncResult);
		///
		///<Summary>
		///     Ends a pending asynchronous send.
		///
		/// Parameters:
		///   asyncResult:
		///     An System.IAsyncResult that stores state information for this asynchronous operation.
		///
		///   errorCode:
		///     A System.Net.Sockets.SocketError object that stores the socket error.
		///
		///</Summary><Returns>
		///     If successful, the number of bytes sent to the System.Net.Sockets.Socket; otherwise,
		///     an invalid System.Net.Sockets.Socket error.
		///
		/// </Returns>
		///   <exception cref="System.ArgumentNullException"></exception>
		///     asyncResult is null.
		///
		///   <exception cref="System.ArgumentException"></exception>
		///     asyncResult was not returned by a call to the System.Net.Sockets.Socket.BeginSend(System.Byte[],System.Int32,System.Int32,System.Net.Sockets.SocketFlags,System.AsyncCallback,System.Object)
		///     method.
		///
		///   <exception cref="System.InvalidOperationException"></exception>
		///     System.Net.Sockets.Socket.EndSend(System.IAsyncResult) was previously called
		///     for the asynchronous send.
		///
		///   <exception cref="System.Net.Sockets.SocketException"></exception>
		///     An error occurred when attempting to access the socket. See the Remarks section
		///     for more information.
		///
		///   <exception cref="System.ObjectDisposedException">The System.Net.Sockets.Socket has been closed.</exception>
		int EndSend(IAsyncResult asyncResult, out SocketError errorCode);

		///<Summary>
		///     Ends a pending asynchronous send of a file.
		///</Summary>
		/// Parameters:
		///   asyncResult:
		///     An System.IAsyncResult object that stores state information for this asynchronous
		///     operation.
		///   <exception cref="System.NotSupportedException"></exception>
		///     Windows NT is required for this method.
		///
		///   <exception cref="System.ObjectDisposedException">The System.Net.Sockets.Socket has been closed.</exception>
		///     The System.Net.Sockets.Socket object has been closed.
		///
		///   <exception cref="System.ArgumentNullException"></exception>
		///     asyncResult is empty.
		///
		///   <exception cref="System.ArgumentException"></exception>
		///     asyncResult was not returned by a call to the System.Net.Sockets.Socket.BeginSendFile(System.String,System.AsyncCallback,System.Object)
		///     method.
		///
		///   <exception cref="System.InvalidOperationException"></exception>
		///     System.Net.Sockets.Socket.EndSendFile(System.IAsyncResult) was previously called
		///     for the asynchronous System.Net.Sockets.Socket.BeginSendFile(System.String,System.AsyncCallback,System.Object).
		///
		///   <exception cref="System.Net.Sockets.SocketException"></exception>
		///     An error occurred when attempting to access the socket. See remarks section below.
		void EndSendFile(IAsyncResult asyncResult);

		///<Summary>
		///     Ends a pending asynchronous send to a specific location.
		///
		/// Parameters:
		///   asyncResult:
		///     An System.IAsyncResult that stores state information and any user defined data
		///     for this asynchronous operation.
		///
		///</Summary><Returns>
		///     If successful, the number of bytes sent; otherwise, an invalid System.Net.Sockets.Socket
		///     error.
		///
		/// </Returns>
		///   <exception cref="System.ArgumentNullException"></exception>
		///     asyncResult is null.
		///
		///   <exception cref="System.ArgumentException"></exception>
		///     asyncResult was not returned by a call to the System.Net.Sockets.Socket.BeginSendTo(System.Byte[],System.Int32,System.Int32,System.Net.Sockets.SocketFlags,System.Net.EndPoint,System.AsyncCallback,System.Object)
		///     method.
		///
		///   <exception cref="System.InvalidOperationException"></exception>
		///     System.Net.Sockets.Socket.EndSendTo(System.IAsyncResult) was previously called
		///     for the asynchronous send.
		///
		///   <exception cref="System.Net.Sockets.SocketException"></exception>
		///     An error occurred when attempting to access the socket. See the Remarks section
		///     for more information.
		///
		///   <exception cref="System.ObjectDisposedException">The System.Net.Sockets.Socket has been closed.</exception>
		
		int EndSendTo(IAsyncResult asyncResult);
		///
		///<Summary>
		///     Returns the value of a specified System.Net.Sockets.Socket option, represented
		///     as an object.
		///
		/// Parameters:
		///   optionLevel:
		///     One of the System.Net.Sockets.SocketOptionLevel values.
		///
		///   optionName:
		///     One of the System.Net.Sockets.SocketOptionName values.
		///
		///</Summary><Returns>
		///     An object that represents the value of the option. When the optionName parameter
		///     is set to System.Net.Sockets.SocketOptionName.Linger the return value is an instance
		///     of the System.Net.Sockets.LingerOption class. When optionName is set to System.Net.Sockets.SocketOptionName.AddMembership
		///     or System.Net.Sockets.SocketOptionName.DropMembership, the return value is an
		///     instance of the System.Net.Sockets.MulticastOption class. When optionName is
		///     any other value, the return value is an integer.
		///
		/// </Returns>
		///   <exception cref="System.Net.Sockets.SocketException"></exception>
		///     An error occurred when attempting to access the socket. See the Remarks section
		///     for more information.-or-optionName was set to the unsupported value System.Net.Sockets.SocketOptionName.MaxConnections.
		///
		///   <exception cref="System.ObjectDisposedException">The System.Net.Sockets.Socket has been closed.</exception>
		
		object GetSocketOption(SocketOptionLevel optionLevel, SocketOptionName optionName);
		///
		///<Summary>
		///     Returns the value of the specified System.Net.Sockets.Socket option in an array.
		///
		/// Parameters:
		///   optionLevel:
		///     One of the System.Net.Sockets.SocketOptionLevel values.
		///
		///   optionName:
		///     One of the System.Net.Sockets.SocketOptionName values.
		///
		///   optionLength:
		///     The length, in bytes, of the expected return value.
		///
		///</Summary><Returns>
		///     An array of type System.Byte that contains the value of the socket option.
		///
		/// </Returns>
		///   <exception cref="System.Net.Sockets.SocketException"></exception>
		///     An error occurred when attempting to access the socket. See the Remarks section
		///     for more information. - or -In .NET Compact Framework applications, the Windows
		///     CE default buffer space is set to 32768 bytes. You can change the per socket
		///     buffer space by calling Overload:System.Net.Sockets.Socket.SetSocketOption.
		///
		///   <exception cref="System.ObjectDisposedException">The System.Net.Sockets.Socket has been closed.</exception>
		
		byte[] GetSocketOption(SocketOptionLevel optionLevel, SocketOptionName optionName, int optionLength);
		///
		///<Summary>
		///     Returns the specified System.Net.Sockets.Socket option setting, represented as
		///     a byte array.
		///</Summary>
		/// Parameters:
		///   optionLevel:
		///     One of the System.Net.Sockets.SocketOptionLevel values.
		///
		///   optionName:
		///     One of the System.Net.Sockets.SocketOptionName values.
		///
		///   optionValue:
		///     An array of type System.Byte that is to receive the option setting.
		///   <exception cref="System.Net.Sockets.SocketException"></exception>
		///     An error occurred when attempting to access the socket. See the Remarks section
		///     for more information. - or -In .NET Compact Framework applications, the Windows
		///     CE default buffer space is set to 32768 bytes. You can change the per socket
		///     buffer space by calling Overload:System.Net.Sockets.Socket.SetSocketOption.
		///   <exception cref="System.ObjectDisposedException">The System.Net.Sockets.Socket has been closed.</exception>
		void GetSocketOption(SocketOptionLevel optionLevel, SocketOptionName optionName, byte[] optionValue);

		///<Summary>
		///     Sets low-level operating modes for the System.Net.Sockets.Socket using numerical
		///     control codes.
		///
		/// Parameters:
		///   ioControlCode:
		///     An System.Int32 value that specifies the control code of the operation to perform.
		///
		///   optionInValue:
		///     A System.Byte array that contains the input data required by the operation.
		///
		///   optionOutValue:
		///     A System.Byte array that contains the output data returned by the operation.
		///
		///</Summary><Returns>
		///     The number of bytes in the optionOutValue parameter.
		///
		/// </Returns>
		///   <exception cref="System.Net.Sockets.SocketException"></exception>
		///     An error occurred when attempting to access the socket. See the Remarks section
		///     for more information.
		///
		///   <exception cref="System.ObjectDisposedException">The System.Net.Sockets.Socket has been closed.</exception>
		
		///
		///   <exception cref="System.InvalidOperationException"></exception>
		///     An attempt was made to change the blocking mode without using the System.Net.Sockets.Socket.Blocking
		///     property.
		///
		///   <exception cref="System.Security.SecurityException"></exception>
		///     A caller in the call stack does not have the required permissions.
		int IOControl(int ioControlCode, byte[] optionInValue, byte[] optionOutValue);
		///
		///<Summary>
		///     Sets low-level operating modes for the System.Net.Sockets.Socket using the System.Net.Sockets.IOControlCode
		///     enumeration to specify control codes.
		///
		/// Parameters:
		///   ioControlCode:
		///     A System.Net.Sockets.IOControlCode value that specifies the control code of the
		///     operation to perform.
		///
		///   optionInValue:
		///     An array of type System.Byte that contains the input data required by the operation.
		///
		///   optionOutValue:
		///     An array of type System.Byte that contains the output data returned by the operation.
		///
		///</Summary><Returns>
		///     The number of bytes in the optionOutValue parameter.
		///
		/// </Returns>
		///   <exception cref="System.Net.Sockets.SocketException"></exception>
		///     An error occurred when attempting to access the socket. See the Remarks section
		///     for more information.
		///
		///   <exception cref="System.ObjectDisposedException">The System.Net.Sockets.Socket has been closed.</exception>
		
		///
		///   <exception cref="System.InvalidOperationException"></exception>
		///     An attempt was made to change the blocking mode without using the System.Net.Sockets.Socket.Blocking
		///     property.
		int IOControl(IOControlCode ioControlCode, byte[] optionInValue, byte[] optionOutValue);
		///
		///<Summary>
		///     Places a System.Net.Sockets.Socket in a listening state.
		///</Summary>
		/// Parameters:
		///   backlog:
		///     The maximum length of the pending connections queue.
		///   <exception cref="System.Net.Sockets.SocketException"></exception>
		///     An error occurred when attempting to access the socket. See the Remarks section
		///     for more information.
		///   <exception cref="System.ObjectDisposedException">The System.Net.Sockets.Socket has been closed.</exception>
		void Listen(int backlog);

		///<Summary>
		///     Determines the status of the System.Net.Sockets.Socket.
		///
		/// Parameters:
		///   microSeconds:
		///     The time to wait for a response, in microseconds.
		///
		///   mode:
		///     One of the System.Net.Sockets.SelectMode values.
		///
		///</Summary><Returns>
		///     The status of the System.Net.Sockets.Socket based on the polling mode value passed
		///     in the mode parameter.Mode Return Value System.Net.Sockets.SelectMode.SelectReadtrue
		///     if System.Net.Sockets.Socket.Listen(System.Int32) has been called and a connection
		///     is pending; -or- true if data is available for reading; -or- true if the connection
		///     has been closed, reset, or terminated; otherwise, returns false. System.Net.Sockets.SelectMode.SelectWritetrue,
		///     if processing a System.Net.Sockets.Socket.Connect(System.Net.EndPoint), and the
		///     connection has succeeded; -or- true if data can be sent; otherwise, returns false.
		///     System.Net.Sockets.SelectMode.SelectErrortrue if processing a System.Net.Sockets.Socket.Connect(System.Net.EndPoint)
		///     that does not block, and the connection has failed; -or- true if System.Net.Sockets.SocketOptionName.OutOfBandInline
		///     is not set and out-of-band data is available; otherwise, returns false.
		///
		/// </Returns>
		///   <exception cref="System.NotSupportedException"></exception>
		///     The mode parameter is not one of the System.Net.Sockets.SelectMode values.
		///
		///   <exception cref="System.Net.Sockets.SocketException"></exception>
		///     An error occurred when attempting to access the socket. See remarks below.
		///
		///   <exception cref="System.ObjectDisposedException">The System.Net.Sockets.Socket has been closed.</exception>
		
		bool Poll(int microSeconds, SelectMode mode);
		///
		///<Summary>
		///     Receives data from a bound System.Net.Sockets.Socket into a receive buffer.
		///
		/// Parameters:
		///   buffer:
		///     An array of type System.Byte that is the storage location for the received data.
		///
		///</Summary><Returns>
		///     The number of bytes received.
		///
		/// </Returns>
		///   <exception cref="System.ArgumentNullException"></exception>
		///     buffer is null.
		///
		///   <exception cref="System.Net.Sockets.SocketException"></exception>
		///     An error occurred when attempting to access the socket. See the Remarks section
		///     for more information.
		///
		///   <exception cref="System.ObjectDisposedException">The System.Net.Sockets.Socket has been closed.</exception>
		
		///
		///   <exception cref="System.Security.SecurityException"></exception>
		///     A caller in the call stack does not have the required permissions.
		int Receive(byte[] buffer);
		///
		///<Summary>
		///     Receives data from a bound System.Net.Sockets.Socket into the list of receive
		///     buffers.
		///
		/// Parameters:
		///   buffers:
		///     A list of System.ArraySegment`1s of type System.Byte that contains the received
		///     data.
		///
		///</Summary><Returns>
		///     The number of bytes received.
		///
		/// </Returns>
		///   <exception cref="System.ArgumentNullException"></exception>
		///     The buffer parameter is null.
		///
		///   <exception cref="System.Net.Sockets.SocketException"></exception>
		///     An error occurred while attempting to access the socket. See the Remarks section
		///     for more information.
		///
		///   <exception cref="System.ObjectDisposedException">The System.Net.Sockets.Socket has been closed.</exception>
		
		int Receive(IList<ArraySegment<byte>> buffers);
		///
		///<Summary>
		///     Receives data from a bound System.Net.Sockets.Socket into the list of receive
		///     buffers, using the specified System.Net.Sockets.SocketFlags.
		///
		/// Parameters:
		///   buffers:
		///     A list of System.ArraySegment`1s of type System.Byte that contains the received
		///     data.
		///
		///   socketFlags:
		///     A bitwise combination of the System.Net.Sockets.SocketFlags values.
		///
		///</Summary><Returns>
		///     The number of bytes received.
		///
		/// </Returns>
		///   <exception cref="System.ArgumentNullException"></exception>
		///     buffers is null.-or-buffers.Count is zero.
		///
		///   <exception cref="System.Net.Sockets.SocketException"></exception>
		///     An error occurred while attempting to access the socket. See the Remarks section
		///     for more information.
		///
		///   <exception cref="System.ObjectDisposedException">The System.Net.Sockets.Socket has been closed.</exception>
		
		int Receive(IList<ArraySegment<byte>> buffers, SocketFlags socketFlags);
		///
		///<Summary>
		///     Receives data from a bound System.Net.Sockets.Socket into a receive buffer, using
		///     the specified System.Net.Sockets.SocketFlags.
		///
		/// Parameters:
		///   buffer:
		///     An array of type System.Byte that is the storage location for the received data.
		///
		///   socketFlags:
		///     A bitwise combination of the System.Net.Sockets.SocketFlags values.
		///
		///</Summary><Returns>
		///     The number of bytes received.
		///
		/// </Returns>
		///   <exception cref="System.ArgumentNullException"></exception>
		///     buffer is null.
		///
		///   <exception cref="System.Net.Sockets.SocketException"></exception>
		///     An error occurred when attempting to access the socket. See the Remarks section
		///     for more information.
		///
		///   <exception cref="System.ObjectDisposedException">The System.Net.Sockets.Socket has been closed.</exception>
		
		///
		///   <exception cref="System.Security.SecurityException"></exception>
		///     A caller in the call stack does not have the required permissions.
		int Receive(byte[] buffer, SocketFlags socketFlags);
		///
		///<Summary>
		///     Receives data from a bound System.Net.Sockets.Socket into the list of receive
		///     buffers, using the specified System.Net.Sockets.SocketFlags.
		///
		/// Parameters:
		///   buffers:
		///     A list of System.ArraySegment`1s of type System.Byte that contains the received
		///     data.
		///
		///   socketFlags:
		///     A bitwise combination of the System.Net.Sockets.SocketFlags values.
		///
		///   errorCode:
		///     A System.Net.Sockets.SocketError object that stores the socket error.
		///
		///</Summary><Returns>
		///     The number of bytes received.
		///
		/// </Returns>
		///   <exception cref="System.ArgumentNullException"></exception>
		///     buffers is null.-or-buffers.Count is zero.
		///
		///   <exception cref="System.Net.Sockets.SocketException"></exception>
		///     An error occurred while attempting to access the socket. See the Remarks section
		///     for more information.
		///
		///   <exception cref="System.ObjectDisposedException">The System.Net.Sockets.Socket has been closed.</exception>
		
		int Receive(IList<ArraySegment<byte>> buffers, SocketFlags socketFlags, out SocketError errorCode);
		///
		///<Summary>
		///     Receives the specified number of bytes of data from a bound System.Net.Sockets.Socket
		///     into a receive buffer, using the specified System.Net.Sockets.SocketFlags.
		///
		/// Parameters:
		///   buffer:
		///     An array of type System.Byte that is the storage location for the received data.
		///
		///   size:
		///     The number of bytes to receive.
		///
		///   socketFlags:
		///     A bitwise combination of the System.Net.Sockets.SocketFlags values.
		///
		///</Summary><Returns>
		///     The number of bytes received.
		///
		/// </Returns>
		///   <exception cref="System.ArgumentNullException"></exception>
		///     buffer is null.
		///
		///   <exception cref="System.ArgumentOutOfRangeException"></exception>
		///     size exceeds the size of buffer.
		///
		///   <exception cref="System.Net.Sockets.SocketException"></exception>
		///     An error occurred when attempting to access the socket. See the Remarks section
		///     for more information.
		///
		///   <exception cref="System.ObjectDisposedException">The System.Net.Sockets.Socket has been closed.</exception>
		
		///
		///   <exception cref="System.Security.SecurityException"></exception>
		///     A caller in the call stack does not have the required permissions.
		int Receive(byte[] buffer, int size, SocketFlags socketFlags);
		///
		///<Summary>
		///     Receives the specified number of bytes from a bound System.Net.Sockets.Socket
		///     into the specified offset position of the receive buffer, using the specified
		///     System.Net.Sockets.SocketFlags.
		///
		/// Parameters:
		///   buffer:
		///     An array of type System.Byte that is the storage location for received data.
		///
		///   offset:
		///     The location in buffer to store the received data.
		///
		///   size:
		///     The number of bytes to receive.
		///
		///   socketFlags:
		///     A bitwise combination of the System.Net.Sockets.SocketFlags values.
		///
		///</Summary><Returns>
		///     The number of bytes received.
		///
		/// </Returns>
		///   <exception cref="System.ArgumentNullException"></exception>
		///     buffer is null.
		///
		///   <exception cref="System.ArgumentOutOfRangeException"></exception>
		///     offset is less than 0.-or- offset is greater than the length of buffer.-or- size
		///     is less than 0.-or- size is greater than the length of buffer minus the value
		///     of the offset parameter.
		///
		///   <exception cref="System.Net.Sockets.SocketException"></exception>
		///     socketFlags is not a valid combination of values.-or- The System.Net.Sockets.Socket.LocalEndPoint
		///     property was not set.-or- An operating system error occurs while accessing the
		///     System.Net.Sockets.Socket.
		///
		///   <exception cref="System.ObjectDisposedException">The System.Net.Sockets.Socket has been closed.</exception>
		
		///
		///   <exception cref="System.Security.SecurityException"></exception>
		///     A caller in the call stack does not have the required permissions.
		int Receive(byte[] buffer, int offset, int size, SocketFlags socketFlags);
		///
		///<Summary>
		///     Receives data from a bound System.Net.Sockets.Socket into a receive buffer, using
		///     the specified System.Net.Sockets.SocketFlags.
		///
		/// Parameters:
		///   buffer:
		///     An array of type System.Byte that is the storage location for the received data.
		///
		///   offset:
		///     The position in the buffer parameter to store the received data.
		///
		///   size:
		///     The number of bytes to receive.
		///
		///   socketFlags:
		///     A bitwise combination of the System.Net.Sockets.SocketFlags values.
		///
		///   errorCode:
		///     A System.Net.Sockets.SocketError object that stores the socket error.
		///
		///</Summary><Returns>
		///     The number of bytes received.
		///
		/// </Returns>
		///   <exception cref="System.ArgumentNullException"></exception>
		///     buffer is null.
		///
		///   <exception cref="System.ArgumentOutOfRangeException"></exception>
		///     offset is less than 0.-or- offset is greater than the length of buffer.-or- size
		///     is less than 0.-or- size is greater than the length of buffer minus the value
		///     of the offset parameter.
		///
		///   <exception cref="System.Net.Sockets.SocketException"></exception>
		///     socketFlags is not a valid combination of values.-or- The System.Net.Sockets.Socket.LocalEndPoint
		///     property is not set.-or- An operating system error occurs while accessing the
		///     System.Net.Sockets.Socket.
		///
		///   <exception cref="System.ObjectDisposedException">The System.Net.Sockets.Socket has been closed.</exception>
		
		///
		///   <exception cref="System.Security.SecurityException"></exception>
		///     A caller in the call stack does not have the required permissions.
		int Receive(byte[] buffer, int offset, int size, SocketFlags socketFlags, out SocketError errorCode);
		///
		///<Summary>
		///     Begins an asynchronous request to receive data from a connected System.Net.Sockets.Socket
		///     object.
		///
		/// Parameters:
		///   e:
		///     The System.Net.Sockets.SocketAsyncEventArgs object to use for this asynchronous
		///     socket operation.
		///
		///</Summary><Returns>
		///     Returns true if the I/O operation is pending. The System.Net.Sockets.SocketAsyncEventArgs.Completed
		///     event on the e parameter will be raised upon completion of the operation. Returns
		///     false if the I/O operation completed synchronously. In this case, The System.Net.Sockets.SocketAsyncEventArgs.Completed
		///     event on the e parameter will not be raised and the e object passed as a parameter
		///     may be examined immediately after the method call returns to retrieve the result
		///     of the operation.
		///
		/// </Returns>
		///   <exception cref="System.ArgumentException"></exception>
		///     An argument was invalid. The System.Net.Sockets.SocketAsyncEventArgs.Buffer or
		///     System.Net.Sockets.SocketAsyncEventArgs.BufferList properties on the e parameter
		///     must reference valid buffers. One or the other of these properties may be set,
		///     but not both at the same time.
		///
		///   <exception cref="System.InvalidOperationException"></exception>
		///     A socket operation was already in progress using the System.Net.Sockets.SocketAsyncEventArgs
		///     object specified in the e parameter.
		///
		///   <exception cref="System.NotSupportedException"></exception>
		///     Windows XP or later is required for this method.
		///
		///   <exception cref="System.ObjectDisposedException">The System.Net.Sockets.Socket has been closed.</exception>
		
		///
		///   <exception cref="System.Net.Sockets.SocketException"></exception>
		///     An error occurred when attempting to access the socket. See the Remarks section
		///     for more information.
		bool ReceiveAsync(SocketAsyncEventArgs e);
		///
		///<Summary>
		///     Receives a datagram into the data buffer and stores the endpoint.
		///
		/// Parameters:
		///   buffer:
		///     An array of type System.Byte that is the storage location for received data.
		///
		///   remoteEP:
		///     An System.Net.EndPoint, passed by reference, that represents the remote server.
		///
		///</Summary><Returns>
		///     The number of bytes received.
		///
		/// </Returns>
		///   <exception cref="System.ArgumentNullException"></exception>
		///     buffer is null.-or- remoteEP is null.
		///
		///   <exception cref="System.Net.Sockets.SocketException"></exception>
		///     An error occurred when attempting to access the socket. See the Remarks section
		///     for more information.
		///
		///   <exception cref="System.ObjectDisposedException">The System.Net.Sockets.Socket has been closed.</exception>
		
		///
		///   <exception cref="System.Security.SecurityException"></exception>
		///     A caller in the call stack does not have the required permissions.
		int ReceiveFrom(byte[] buffer, ref EndPoint remoteEP);
		///
		///<Summary>
		///     Receives a datagram into the data buffer, using the specified System.Net.Sockets.SocketFlags,
		///     and stores the endpoint.
		///
		/// Parameters:
		///   buffer:
		///     An array of type System.Byte that is the storage location for the received data.
		///
		///   socketFlags:
		///     A bitwise combination of the System.Net.Sockets.SocketFlags values.
		///
		///   remoteEP:
		///     An System.Net.EndPoint, passed by reference, that represents the remote server.
		///
		///</Summary><Returns>
		///     The number of bytes received.
		///
		/// </Returns>
		///   <exception cref="System.ArgumentNullException"></exception>
		///     buffer is null.-or- remoteEP is null.
		///
		///   <exception cref="System.Net.Sockets.SocketException"></exception>
		///     An error occurred when attempting to access the socket. See the Remarks section
		///     for more information.
		///
		///   <exception cref="System.ObjectDisposedException">The System.Net.Sockets.Socket has been closed.</exception>
		
		///
		///   <exception cref="System.Security.SecurityException"></exception>
		///     A caller in the call stack does not have the required permissions.
		int ReceiveFrom(byte[] buffer, SocketFlags socketFlags, ref EndPoint remoteEP);
		///
		///<Summary>
		///     Receives the specified number of bytes into the data buffer, using the specified
		///     System.Net.Sockets.SocketFlags, and stores the endpoint.
		///
		/// Parameters:
		///   buffer:
		///     An array of type System.Byte that is the storage location for received data.
		///
		///   size:
		///     The number of bytes to receive.
		///
		///   socketFlags:
		///     A bitwise combination of the System.Net.Sockets.SocketFlags values.
		///
		///   remoteEP:
		///     An System.Net.EndPoint, passed by reference, that represents the remote server.
		///
		///</Summary><Returns>
		///     The number of bytes received.
		///
		/// </Returns>
		///   <exception cref="System.ArgumentNullException"></exception>
		///     buffer is null.-or- remoteEP is null.
		///
		///   <exception cref="System.ArgumentOutOfRangeException"></exception>
		///     size is less than 0.-or- size is greater than the length of buffer.
		///
		///   <exception cref="System.Net.Sockets.SocketException"></exception>
		///     socketFlags is not a valid combination of values.-or- The System.Net.Sockets.Socket.LocalEndPoint
		///     property was not set.-or- An operating system error occurs while accessing the
		///     System.Net.Sockets.Socket.
		///
		///   <exception cref="System.ObjectDisposedException">The System.Net.Sockets.Socket has been closed.</exception>
		
		///
		///   <exception cref="System.Security.SecurityException"></exception>
		///     A caller in the call stack does not have the required permissions.
		int ReceiveFrom(byte[] buffer, int size, SocketFlags socketFlags, ref EndPoint remoteEP);
		///
		///<Summary>
		///     Receives the specified number of bytes of data into the specified location of
		///     the data buffer, using the specified System.Net.Sockets.SocketFlags, and stores
		///     the endpoint.
		///
		/// Parameters:
		///   buffer:
		///     An array of type System.Byte that is the storage location for received data.
		///
		///   offset:
		///     The position in the buffer parameter to store the received data.
		///
		///   size:
		///     The number of bytes to receive.
		///
		///   socketFlags:
		///     A bitwise combination of the System.Net.Sockets.SocketFlags values.
		///
		///   remoteEP:
		///     An System.Net.EndPoint, passed by reference, that represents the remote server.
		///
		///</Summary><Returns>
		///     The number of bytes received.
		///
		/// </Returns>
		///   <exception cref="System.ArgumentNullException"></exception>
		///     buffer is null.-or- remoteEP is null.
		///
		///   <exception cref="System.ArgumentOutOfRangeException"></exception>
		///     offset is less than 0.-or- offset is greater than the length of buffer.-or- size
		///     is less than 0.-or- size is greater than the length of the buffer minus the value
		///     of the offset parameter.
		///
		///   <exception cref="System.Net.Sockets.SocketException"></exception>
		///     socketFlags is not a valid combination of values.-or- The System.Net.Sockets.Socket.LocalEndPoint
		///     property was not set.-or- An error occurred when attempting to access the socket.
		///     See the Remarks section for more information.
		///
		///   <exception cref="System.ObjectDisposedException">The System.Net.Sockets.Socket has been closed.</exception>
		
		int ReceiveFrom(byte[] buffer, int offset, int size, SocketFlags socketFlags, ref EndPoint remoteEP);
		///
		///<Summary>
		///     Begins to asynchronously receive data from a specified network device.
		///
		/// Parameters:
		///   e:
		///     The System.Net.Sockets.SocketAsyncEventArgs object to use for this asynchronous
		///     socket operation.
		///
		///</Summary><Returns>
		///     Returns true if the I/O operation is pending. The System.Net.Sockets.SocketAsyncEventArgs.Completed
		///     event on the e parameter will be raised upon completion of the operation. Returns
		///     false if the I/O operation completed synchronously. In this case, The System.Net.Sockets.SocketAsyncEventArgs.Completed
		///     event on the e parameter will not be raised and the e object passed as a parameter
		///     may be examined immediately after the method call returns to retrieve the result
		///     of the operation.
		///
		/// </Returns>
		///   <exception cref="System.ArgumentNullException"></exception>
		///     The System.Net.Sockets.SocketAsyncEventArgs.RemoteEndPoint cannot be null.
		///
		///   <exception cref="System.InvalidOperationException"></exception>
		///     A socket operation was already in progress using the System.Net.Sockets.SocketAsyncEventArgs
		///     object specified in the e parameter.
		///
		///   <exception cref="System.NotSupportedException"></exception>
		///     Windows XP or later is required for this method.
		///
		///   <exception cref="System.ObjectDisposedException">The System.Net.Sockets.Socket has been closed.</exception>
		
		///
		///   <exception cref="System.Net.Sockets.SocketException"></exception>
		///     An error occurred when attempting to access the socket.
		bool ReceiveFromAsync(SocketAsyncEventArgs e);
		///
		///<Summary>
		///     Receives the specified number of bytes of data into the specified location of
		///     the data buffer, using the specified System.Net.Sockets.SocketFlags, and stores
		///     the endpoint and packet information.
		///
		/// Parameters:
		///   buffer:
		///     An array of type System.Byte that is the storage location for received data.
		///
		///   offset:
		///     The position in the buffer parameter to store the received data.
		///
		///   size:
		///     The number of bytes to receive.
		///
		///   socketFlags:
		///     A bitwise combination of the System.Net.Sockets.SocketFlags values.
		///
		///   remoteEP:
		///     An System.Net.EndPoint, passed by reference, that represents the remote server.
		///
		///   ipPacketInformation:
		///     An System.Net.Sockets.IPPacketInformation holding address and interface information.
		///
		///</Summary><Returns>
		///     The number of bytes received.
		///
		/// </Returns>
		///   <exception cref="System.ArgumentNullException"></exception>
		///     buffer is null.- or- remoteEP is null.
		///
		///   <exception cref="System.ArgumentOutOfRangeException"></exception>
		///     offset is less than 0.-or- offset is greater than the length of buffer.-or- size
		///     is less than 0.-or- size is greater than the length of the buffer minus the value
		///     of the offset parameter.
		///
		///   <exception cref="System.Net.Sockets.SocketException"></exception>
		///     socketFlags is not a valid combination of values.-or- The System.Net.Sockets.Socket.LocalEndPoint
		///     property was not set.-or- The .NET Framework is running on an AMD 64-bit processor.-or-
		///     An error occurred when attempting to access the socket. See the Remarks section
		///     for more information.
		///
		///   <exception cref="System.ObjectDisposedException">The System.Net.Sockets.Socket has been closed.</exception>
		
		///
		///   <exception cref="System.NotSupportedException"></exception>
		///     The operating system is Windows 2000 or earlier, and this method requires Windows
		///     XP.
		int ReceiveMessageFrom(byte[] buffer, int offset, int size, ref SocketFlags socketFlags, ref EndPoint remoteEP, out IPPacketInformation ipPacketInformation);
		///
		///<Summary>
		///     Begins to asynchronously receive the specified number of bytes of data into the
		///     specified location in the data buffer, using the specified System.Net.Sockets.SocketAsyncEventArgs.SocketFlags,
		///     and stores the endpoint and packet information.
		///
		/// Parameters:
		///   e:
		///     The System.Net.Sockets.SocketAsyncEventArgs object to use for this asynchronous
		///     socket operation.
		///
		///</Summary><Returns>
		///     Returns true if the I/O operation is pending. The System.Net.Sockets.SocketAsyncEventArgs.Completed
		///     event on the e parameter will be raised upon completion of the operation. Returns
		///     false if the I/O operation completed synchronously. In this case, The System.Net.Sockets.SocketAsyncEventArgs.Completed
		///     event on the e parameter will not be raised and the e object passed as a parameter
		///     may be examined immediately after the method call returns to retrieve the result
		///     of the operation.
		///
		/// </Returns>
		///   <exception cref="System.ArgumentNullException"></exception>
		///     The System.Net.Sockets.SocketAsyncEventArgs.RemoteEndPoint cannot be null.
		///
		///   <exception cref="System.NotSupportedException"></exception>
		///     Windows XP or later is required for this method.
		///
		///   <exception cref="System.ObjectDisposedException">The System.Net.Sockets.Socket has been closed.</exception>
		
		///
		///   <exception cref="System.Net.Sockets.SocketException"></exception>
		///     An error occurred when attempting to access the socket.
		bool ReceiveMessageFromAsync(SocketAsyncEventArgs e);
		///
		///<Summary>
		///     Sends data to a connected System.Net.Sockets.Socket.
		///
		/// Parameters:
		///   buffer:
		///     An array of type System.Byte that contains the data to be sent.
		///
		///</Summary><Returns>
		///     The number of bytes sent to the System.Net.Sockets.Socket.
		///
		/// </Returns>
		///   <exception cref="System.ArgumentNullException"></exception>
		///     buffer is null.
		///
		///   <exception cref="System.Net.Sockets.SocketException"></exception>
		///     An error occurred when attempting to access the socket. See the Remarks section
		///     for more information.
		///
		///   <exception cref="System.ObjectDisposedException">The System.Net.Sockets.Socket has been closed.</exception>
		
		int Send(byte[] buffer);
		///
		///<Summary>
		///     Sends the set of buffers in the list to a connected System.Net.Sockets.Socket.
		///
		/// Parameters:
		///   buffers:
		///     A list of System.ArraySegment`1s of type System.Byte that contains the data to
		///     be sent.
		///
		///</Summary><Returns>
		///     The number of bytes sent to the System.Net.Sockets.Socket.
		///
		/// </Returns>
		///   <exception cref="System.ArgumentNullException"></exception>
		///     buffers is null.
		///
		///   <exception cref="System.ArgumentException"></exception>
		///     buffers is empty.
		///
		///   <exception cref="System.Net.Sockets.SocketException"></exception>
		///     An error occurred when attempting to access the socket. See remarks section below.
		///
		///   <exception cref="System.ObjectDisposedException">The System.Net.Sockets.Socket has been closed.</exception>
		
		int Send(IList<ArraySegment<byte>> buffers);

		///
		///<Summary>
		///     Sends data to a connected System.Net.Sockets.Socket using the specified System.Net.Sockets.SocketFlags.
		///
		/// Parameters:
		///   buffer:
		///     An array of type System.Byte that contains the data to be sent.
		///
		///   socketFlags:
		///     A bitwise combination of the System.Net.Sockets.SocketFlags values.
		///
		///</Summary><Returns>
		///     The number of bytes sent to the System.Net.Sockets.Socket.
		///
		/// </Returns>
		///   <exception cref="System.ArgumentNullException"></exception>
		///     buffer is null.
		///
		///   <exception cref="System.Net.Sockets.SocketException"></exception>
		///     An error occurred when attempting to access the socket. See the Remarks section
		///     for more information.
		///
		///   <exception cref="System.ObjectDisposedException">The System.Net.Sockets.Socket has been closed.</exception>
		
		int Send(byte[] buffer, SocketFlags socketFlags);

		///
		///<Summary>
		///     Sends the set of buffers in the list to a connected System.Net.Sockets.Socket,
		///     using the specified System.Net.Sockets.SocketFlags.
		///
		/// Parameters:
		///   buffers:
		///     A list of System.ArraySegment`1s of type System.Byte that contains the data to
		///     be sent.
		///
		///   socketFlags:
		///     A bitwise combination of the System.Net.Sockets.SocketFlags values.
		///
		///</Summary><Returns>
		///     The number of bytes sent to the System.Net.Sockets.Socket.
		///
		/// </Returns>
		///   <exception cref="System.ArgumentNullException"></exception>
		///     buffers is null.
		///
		///   <exception cref="System.ArgumentException"></exception>
		///     buffers is empty.
		///
		///   <exception cref="System.Net.Sockets.SocketException"></exception>
		///     An error occurred when attempting to access the socket. See the Remarks section
		///     for more information.
		///
		///   <exception cref="System.ObjectDisposedException">The System.Net.Sockets.Socket has been closed.</exception>
		
		int Send(IList<ArraySegment<byte>> buffers, SocketFlags socketFlags);

		///
		///<Summary>
		///     Sends the specified number of bytes of data to a connected System.Net.Sockets.Socket,
		///     using the specified System.Net.Sockets.SocketFlags.
		///
		/// Parameters:
		///   buffer:
		///     An array of type System.Byte that contains the data to be sent.
		///
		///   size:
		///     The number of bytes to send.
		///
		///   socketFlags:
		///     A bitwise combination of the System.Net.Sockets.SocketFlags values.
		///
		///</Summary><Returns>
		///     The number of bytes sent to the System.Net.Sockets.Socket.
		///
		/// </Returns>
		///   <exception cref="System.ArgumentNullException"></exception>
		///     buffer is null.
		///
		///   <exception cref="System.ArgumentOutOfRangeException"></exception>
		///     size is less than 0 or exceeds the size of the buffer.
		///
		///   <exception cref="System.Net.Sockets.SocketException"></exception>
		///     socketFlags is not a valid combination of values.-or- An operating system error
		///     occurs while accessing the socket. See the Remarks section for more information.
		///
		///   <exception cref="System.ObjectDisposedException">The System.Net.Sockets.Socket has been closed.</exception>
		
		int Send(byte[] buffer, int size, SocketFlags socketFlags);

		///
		///<Summary>
		///     Sends the set of buffers in the list to a connected System.Net.Sockets.Socket,
		///     using the specified System.Net.Sockets.SocketFlags.
		///
		/// Parameters:
		///   buffers:
		///     A list of System.ArraySegment`1s of type System.Byte that contains the data to
		///     be sent.
		///
		///   socketFlags:
		///     A bitwise combination of the System.Net.Sockets.SocketFlags values.
		///
		///   errorCode:
		///     A System.Net.Sockets.SocketError object that stores the socket error.
		///
		///</Summary><Returns>
		///     The number of bytes sent to the System.Net.Sockets.Socket.
		///
		/// </Returns>
		///   <exception cref="System.ArgumentNullException"></exception>
		///     buffers is null.
		///
		///   <exception cref="System.ArgumentException"></exception>
		///     buffers is empty.
		///
		///   <exception cref="System.Net.Sockets.SocketException"></exception>
		///     An error occurred when attempting to access the socket. See the Remarks section
		///     for more information.
		///
		///   <exception cref="System.ObjectDisposedException">The System.Net.Sockets.Socket has been closed.</exception>
		
		int Send(IList<ArraySegment<byte>> buffers, SocketFlags socketFlags, out SocketError errorCode);

		///
		///<Summary>
		///     Sends the specified number of bytes of data to a connected System.Net.Sockets.Socket,
		///     starting at the specified offset, and using the specified System.Net.Sockets.SocketFlags.
		///
		/// Parameters:
		///   buffer:
		///     An array of type System.Byte that contains the data to be sent.
		///
		///   offset:
		///     The position in the data buffer at which to begin sending data.
		///
		///   size:
		///     The number of bytes to send.
		///
		///   socketFlags:
		///     A bitwise combination of the System.Net.Sockets.SocketFlags values.
		///
		///</Summary><Returns>
		///     The number of bytes sent to the System.Net.Sockets.Socket.
		///
		/// </Returns>
		///   <exception cref="System.ArgumentNullException"></exception>
		///     buffer is null.
		///
		///   <exception cref="System.ArgumentOutOfRangeException"></exception>
		///     offset is less than 0.-or- offset is greater than the length of buffer.-or- size
		///     is less than 0.-or- size is greater than the length of buffer minus the value
		///     of the offset parameter.
		///
		///   <exception cref="System.Net.Sockets.SocketException"></exception>
		///     socketFlags is not a valid combination of values.-or- An operating system error
		///     occurs while accessing the System.Net.Sockets.Socket. See the Remarks section
		///     for more information.
		///
		///   <exception cref="System.ObjectDisposedException">The System.Net.Sockets.Socket has been closed.</exception>
		
		int Send(byte[] buffer, int offset, int size, SocketFlags socketFlags);

		///
		///<Summary>
		///     Sends the specified number of bytes of data to a connected System.Net.Sockets.Socket,
		///     starting at the specified offset, and using the specified System.Net.Sockets.SocketFlags
		///
		/// Parameters:
		///   buffer:
		///     An array of type System.Byte that contains the data to be sent.
		///
		///   offset:
		///     The position in the data buffer at which to begin sending data.
		///
		///   size:
		///     The number of bytes to send.
		///
		///   socketFlags:
		///     A bitwise combination of the System.Net.Sockets.SocketFlags values.
		///
		///   errorCode:
		///     A System.Net.Sockets.SocketError object that stores the socket error.
		///
		///</Summary><Returns>
		///     The number of bytes sent to the System.Net.Sockets.Socket.
		///
		/// </Returns>
		///   <exception cref="System.ArgumentNullException"></exception>
		///     buffer is null.
		///
		///   <exception cref="System.ArgumentOutOfRangeException"></exception>
		///     offset is less than 0.-or- offset is greater than the length of buffer.-or- size
		///     is less than 0.-or- size is greater than the length of buffer minus the value
		///     of the offset parameter.
		///
		///   <exception cref="System.Net.Sockets.SocketException"></exception>
		///     socketFlags is not a valid combination of values.-or- An operating system error
		///     occurs while accessing the System.Net.Sockets.Socket. See the Remarks section
		///     for more information.
		///
		///   <exception cref="System.ObjectDisposedException">The System.Net.Sockets.Socket has been closed.</exception>
		
		int Send(byte[] buffer, int offset, int size, SocketFlags socketFlags, out SocketError errorCode);

		///
		///<Summary>
		///     Sends data asynchronously to a connected System.Net.Sockets.Socket object.
		///
		/// Parameters:
		///   e:
		///     The System.Net.Sockets.SocketAsyncEventArgs object to use for this asynchronous
		///     socket operation.
		///
		///</Summary><Returns>
		///     Returns true if the I/O operation is pending. The System.Net.Sockets.SocketAsyncEventArgs.Completed
		///     event on the e parameter will be raised upon completion of the operation. Returns
		///     false if the I/O operation completed synchronously. In this case, The System.Net.Sockets.SocketAsyncEventArgs.Completed
		///     event on the e parameter will not be raised and the e object passed as a parameter
		///     may be examined immediately after the method call returns to retrieve the result
		///     of the operation.
		///
		/// </Returns>
		///   <exception cref="System.ArgumentException"></exception>
		///     The System.Net.Sockets.SocketAsyncEventArgs.Buffer or System.Net.Sockets.SocketAsyncEventArgs.BufferList
		///     properties on the e parameter must reference valid buffers. One or the other
		///     of these properties may be set, but not both at the same time.
		///
		///   <exception cref="System.InvalidOperationException"></exception>
		///     A socket operation was already in progress using the System.Net.Sockets.SocketAsyncEventArgs
		///     object specified in the e parameter.
		///
		///   <exception cref="System.NotSupportedException"></exception>
		///     Windows XP or later is required for this method.
		///
		///   <exception cref="System.ObjectDisposedException">The System.Net.Sockets.Socket has been closed.</exception>
		
		///
		///   <exception cref="System.Net.Sockets.SocketException"></exception>
		///     The System.Net.Sockets.Socket is not yet connected or was not obtained via an
		///     System.Net.Sockets.Socket.Accept, System.Net.Sockets.Socket.AcceptAsync(System.Net.Sockets.SocketAsyncEventArgs),or
		///     Overload:System.Net.Sockets.Socket.BeginAccept, method.
		bool SendAsync(SocketAsyncEventArgs e);

		///
		///<Summary>
		///     Sends the file fileName to a connected System.Net.Sockets.Socket object with
		///     the System.Net.Sockets.TransmitFileOptions.UseDefaultWorkerThread transmit flag.
		///</Summary>
		/// <param name="fileName">
		///     A System.String that contains the path and name of the file to be sent. This
		///     parameter can be null.
		/// </param>
		///   <exception cref="System.NotSupportedException">
		///     The socket is not connected to a remote host.
		/// </exception>
		///   <exception cref="System.ObjectDisposedException">The System.Net.Sockets.Socket has been closed.</exception>
		///   <exception cref="System.InvalidOperationException">
		///     The System.Net.Sockets.Socket object is not in blocking mode and cannot accept
		///     this synchronous call.
		/// </exception>
		///   <exception cref="System.IO.FileNotFoundException">
		///     The file fileName was not found.
		/// </exception>
		///   <exception cref="System.Net.Sockets.SocketException">
		///     An error occurred when attempting to access the socket. See the Remarks section
		///     for more information.
		/// </exception>
		void SendFile(string fileName);

		///<Summary>
		///     Sends the file fileName and buffers of data to a connected System.Net.Sockets.Socket
		///     object using the specified System.Net.Sockets.TransmitFileOptions value.
		/// </Summary>
		///
		/// Parameters:
		///   fileName:
		///     A System.String that contains the path and name of the file to be sent. This
		///     parameter can be null.
		///
		///   preBuffer:
		///     A System.Byte array that contains data to be sent before the file is sent. This
		///     parameter can be null.
		///
		///   postBuffer:
		///     A System.Byte array that contains data to be sent after the file is sent. This
		///     parameter can be null.
		///
		///   flags:
		///     One or more of System.Net.Sockets.TransmitFileOptions values.
		///
		///   <exception cref="System.NotSupportedException">
		///     The operating system is not Windows NT or later.- or - The socket is not connected
		///     to a remote host.
		/// </exception>
		///   <exception cref="System.ObjectDisposedException">The System.Net.Sockets.Socket has been closed.</exception>
		///   <exception cref="System.InvalidOperationException">
		///     The System.Net.Sockets.Socket object is not in blocking mode and cannot accept
		///     this synchronous call.
		/// </exception>
		///   <exception cref="System.IO.FileNotFoundException">
		///     The file fileName was not found.
		/// </exception>
		///   <exception cref="System.Net.Sockets.SocketException">
		///     An error occurred when attempting to access the socket. See the Remarks section
		///     for more information.
		/// </exception>
		void SendFile(string fileName, byte[] preBuffer, byte[] postBuffer, TransmitFileOptions flags);

		///
		///<Summary>
		///     Sends a collection of files or in memory data buffers asynchronously to a connected
		///     System.Net.Sockets.Socket object.
		///</Summary>
		/// <param name="e">
		///     The System.Net.Sockets.SocketAsyncEventArgs object to use for this asynchronous
		///     socket operation.
		/// </param>
		/// <Returns>
		///     Returns true if the I/O operation is pending. The System.Net.Sockets.SocketAsyncEventArgs.Completed
		///     event on the e parameter will be raised upon completion of the operation. Returns
		///     false if the I/O operation completed synchronously. In this case, The System.Net.Sockets.SocketAsyncEventArgs.Completed
		///     event on the e parameter will not be raised and the e object passed as a parameter
		///     may be examined immediately after the method call returns to retrieve the result
		///     of the operation.
		/// </Returns>
		///   <exception cref="System.IO.FileNotFoundException">
		///     The file specified in the System.Net.Sockets.SendPacketsElement.FilePath property
		///     was not found.
		/// </exception>
		///   <exception cref="System.InvalidOperationException">
		///     A socket operation was already in progress using the System.Net.Sockets.SocketAsyncEventArgs
		///     object specified in the e parameter.
		/// </exception>
		///   <exception cref="System.NotSupportedException">
		///     Windows XP or later is required for this method. This exception also occurs if
		///     the System.Net.Sockets.Socket is not connected to a remote host.
		/// </exception>
		///   <exception cref="System.ObjectDisposedException">The System.Net.Sockets.Socket has been closed.</exception>
		///   <exception cref="System.Net.Sockets.SocketException">
		///     A connectionless System.Net.Sockets.Socket is being used and the file being sent
		///     exceeds the maximum packet size of the underlying transport.
		/// </exception>
		bool SendPacketsAsync(SocketAsyncEventArgs e);

		///
		///<Summary>
		///     Sends data to the specified endpoint.
		///
		/// Parameters:
		///   buffer:
		///     An array of type System.Byte that contains the data to be sent.
		///
		///   remoteEP:
		///     The System.Net.EndPoint that represents the destination for the data.
		///
		///</Summary><Returns>
		///     The number of bytes sent.
		///
		/// </Returns>
		///   <exception cref="System.ArgumentNullException"></exception>
		///     buffer is null.-or- remoteEP is null.
		///
		///   <exception cref="System.Net.Sockets.SocketException"></exception>
		///     An error occurred when attempting to access the socket. See the Remarks section
		///     for more information.
		///
		///   <exception cref="System.ObjectDisposedException">The System.Net.Sockets.Socket has been closed.</exception>
		
		int SendTo(byte[] buffer, EndPoint remoteEP);

		///
		///<Summary>
		///     Sends data to a specific endpoint using the specified System.Net.Sockets.SocketFlags.
		///
		/// Parameters:
		///   buffer:
		///     An array of type System.Byte that contains the data to be sent.
		///
		///   socketFlags:
		///     A bitwise combination of the System.Net.Sockets.SocketFlags values.
		///
		///   remoteEP:
		///     The System.Net.EndPoint that represents the destination location for the data.
		///
		///</Summary><Returns>
		///     The number of bytes sent.
		///
		/// </Returns>
		///   <exception cref="System.ArgumentNullException"></exception>
		///     buffer is null.-or- remoteEP is null.
		///
		///   <exception cref="System.Net.Sockets.SocketException"></exception>
		///     An error occurred when attempting to access the socket. See the Remarks section
		///     for more information.
		///
		///   <exception cref="System.ObjectDisposedException">The System.Net.Sockets.Socket has been closed.</exception>
		
		int SendTo(byte[] buffer, SocketFlags socketFlags, EndPoint remoteEP);

		///
		///<Summary>
		///     Sends the specified number of bytes of data to the specified endpoint using the
		///     specified System.Net.Sockets.SocketFlags.
		///
		/// Parameters:
		///   buffer:
		///     An array of type System.Byte that contains the data to be sent.
		///
		///   size:
		///     The number of bytes to send.
		///
		///   socketFlags:
		///     A bitwise combination of the System.Net.Sockets.SocketFlags values.
		///
		///   remoteEP:
		///     The System.Net.EndPoint that represents the destination location for the data.
		///
		///</Summary><Returns>
		///     The number of bytes sent.
		///
		/// </Returns>
		///   <exception cref="System.ArgumentNullException"></exception>
		///     buffer is null.-or- remoteEP is null.
		///
		///   <exception cref="System.ArgumentOutOfRangeException"></exception>
		///     The specified size exceeds the size of buffer.
		///
		///   <exception cref="System.Net.Sockets.SocketException"></exception>
		///     An error occurred when attempting to access the socket. See the Remarks section
		///     for more information.
		///
		///   <exception cref="System.ObjectDisposedException">The System.Net.Sockets.Socket has been closed.</exception>
		
		int SendTo(byte[] buffer, int size, SocketFlags socketFlags, EndPoint remoteEP);

		///
		///<Summary>
		///     Sends the specified number of bytes of data to the specified endpoint, starting
		///     at the specified location in the buffer, and using the specified System.Net.Sockets.SocketFlags.
		///
		/// Parameters:
		///   buffer:
		///     An array of type System.Byte that contains the data to be sent.
		///
		///   offset:
		///     The position in the data buffer at which to begin sending data.
		///
		///   size:
		///     The number of bytes to send.
		///
		///   socketFlags:
		///     A bitwise combination of the System.Net.Sockets.SocketFlags values.
		///
		///   remoteEP:
		///     The System.Net.EndPoint that represents the destination location for the data.
		///
		///</Summary><Returns>
		///     The number of bytes sent.
		///
		/// </Returns>
		///   <exception cref="System.ArgumentNullException"></exception>
		///     buffer is null.-or- remoteEP is null.
		///
		///   <exception cref="System.ArgumentOutOfRangeException"></exception>
		///     offset is less than 0.-or- offset is greater than the length of buffer.-or- size
		///     is less than 0.-or- size is greater than the length of buffer minus the value
		///     of the offset parameter.
		///
		///   <exception cref="System.Net.Sockets.SocketException"></exception>
		///     socketFlags is not a valid combination of values.-or- An operating system error
		///     occurs while accessing the System.Net.Sockets.Socket. See the Remarks section
		///     for more information.
		///
		///   <exception cref="System.ObjectDisposedException">The System.Net.Sockets.Socket has been closed.</exception>
		
		///
		///   <exception cref="System.Security.SecurityException"></exception>
		///     A caller in the call stack does not have the required permissions.
		int SendTo(byte[] buffer, int offset, int size, SocketFlags socketFlags, EndPoint remoteEP);

		///<Summary>
		///     Sends data asynchronously to a specific remote host.
		///</Summary>
		/// <param name="e">
		///     The System.Net.Sockets.SocketAsyncEventArgs object to use for this asynchronous
		///     socket operation.
		/// </param>
		/// <Returns>
		///     Returns true if the I/O operation is pending. The System.Net.Sockets.SocketAsyncEventArgs.Completed
		///     event on the e parameter will be raised upon completion of the operation. Returns
		///     false if the I/O operation completed synchronously. In this case, The System.Net.Sockets.SocketAsyncEventArgs.Completed
		///     event on the e parameter will not be raised and the e object passed as a parameter
		///     may be examined immediately after the method call returns to retrieve the result
		///     of the operation.
		/// </Returns>
		///   <exception cref="System.ArgumentNullException"></exception>
		///     The System.Net.Sockets.SocketAsyncEventArgs.RemoteEndPoint cannot be null.
		///
		///   <exception cref="System.InvalidOperationException"></exception>
		///     A socket operation was already in progress using the System.Net.Sockets.SocketAsyncEventArgs
		///     object specified in the e parameter.
		///
		///   <exception cref="System.NotSupportedException"></exception>
		///     Windows XP or later is required for this method.
		///
		///   <exception cref="System.ObjectDisposedException">The System.Net.Sockets.Socket has been closed.</exception>
		///   <exception cref="System.Net.Sockets.SocketException">
		///     The protocol specified is connection-oriented, but the System.Net.Sockets.Socket
		///     is not yet connected.
		/// </exception>
		bool SendToAsync(SocketAsyncEventArgs e);

		///<Summary>
		///     Set the IP protection level on a socket.
		/// </Summary>
		/// <param name="level">The IP protection level to set on this socket.</param>
		///   <exception cref="System.ArgumentException">
		///     The level parameter cannot be System.Net.Sockets.IPProtectionLevel.Unspecified.
		///     The IP protection level cannot be set to unspecified.
		/// </exception>
		///   <exception cref="System.NotSupportedException">
		///     The System.Net.Sockets.AddressFamily of the socket must be either System.Net.Sockets.AddressFamily.InterNetworkV6
		///     or System.Net.Sockets.AddressFamily.InterNetwork.
		/// </exception>
		void SetIPProtectionLevel(IPProtectionLevel level);

		///<Summary>
		///     Sets the specified System.Net.Sockets.Socket option to the specified integer
		///     value.
		///</Summary>
		/// <param name="optionLevel">One of the System.Net.Sockets.SocketOptionLevel values.</param>
		/// <param name="optionName">One of the System.Net.Sockets.SocketOptionName values.</param>
		/// <param name="optionValue">A value of the option.</param>
		/// <exception cref="System.Net.Sockets.SocketException">
		///     An error occurred when attempting to access the socket. See the Remarks section
		///     for more information.
		/// </exception>
		/// <exception cref="System.ObjectDisposedException">
		/// The System.Net.Sockets.Socket has been closed.
		/// </exception>
		void SetSocketOption(SocketOptionLevel optionLevel, SocketOptionName optionName, int optionValue);

		///<Summary>
		///     Sets the specified System.Net.Sockets.Socket option to the specified value, represented
		///     as a byte array.
		/// </Summary>
		///<param name="optionLevel">One of the System.Net.Sockets.SocketOptionLevel values.</param>
		/// <param name="optionName">One of the System.Net.Sockets.SocketOptionName values.</param>
		/// <param name="optionValue">An array of type System.Byte that represents the value of the option.</param>
		/// <exception cref="System.Net.Sockets.SocketException">
		///     An error occurred when attempting to access the socket. See the Remarks section
		///     for more information.
		/// </exception>
		/// <exception cref="System.ObjectDisposedException">
		/// The System.Net.Sockets.Socket has been closed.
		/// </exception>
		void SetSocketOption(SocketOptionLevel optionLevel, SocketOptionName optionName, byte[] optionValue);

		/// <Summary>
		///     Sets the specified System.Net.Sockets.Socket option to the specified value, represented
		///     as an object.
		/// </Summary>
		/// <param name="optionLevel">
		///     One of the System.Net.Sockets.SocketOptionLevel values.
		/// </param>
		/// <param name="optionName">
		///     One of the System.Net.Sockets.SocketOptionName values.
		/// </param>
		/// <param name="optionValue">
		///     A System.Net.Sockets.LingerOption or System.Net.Sockets.MulticastOption that
		///     contains the value of the option.
		/// </param>
		/// <exception cref="System.ArgumentNullException">
		///     optionValue is null.
		/// </exception>
		/// <exception cref="System.Net.Sockets.SocketException">
		///     An error occurred when attempting to access the socket. See the Remarks section
		///     for more information.
		/// </exception>
		void SetSocketOption(SocketOptionLevel optionLevel, SocketOptionName optionName, object optionValue);

		//
		//<Summary>
		//     Sets the specified System.Net.Sockets.Socket option to the specified System.Boolean
		//     value.
		//
		// Parameters:
		//   optionLevel:
		//     One of the System.Net.Sockets.SocketOptionLevel values.
		//
		//   optionName:
		//     One of the System.Net.Sockets.SocketOptionName values.
		//
		//   optionValue:
		//     The value of the option, represented as a System.Boolean.
		//
		// </Returns>
		/// <exception cref="System.Net.Sockets.SocketException">
		///     An error occurred when attempting to access the socket. See the Remarks section
		///     for more information.
		/// </exception>
		/// <exception cref="System.ObjectDisposedException">
		/// The System.Net.Sockets.Socket has been closed.
		/// </exception>
		void SetSocketOption(SocketOptionLevel optionLevel, SocketOptionName optionName, bool optionValue);

		/// <summary>
		///     Disables sends and receives on a System.Net.Sockets.Socket.
		/// </summary>
		/// <param name="how">
		///     One of the System.Net.Sockets.SocketShutdown values that specifies the operation
		///     that will no longer be allowed.
		/// </param>
		/// <exception cref="System.Net.Sockets.SocketException">
		///     An error occurred when attempting to access the socket. See the Remarks section
		///     for more information.
		/// </exception>
		/// <exception cref="System.ObjectDisposedException">
		/// The System.Net.Sockets.Socket has been closed.
		/// </exception>
		void Shutdown(SocketShutdown how);
	}
}