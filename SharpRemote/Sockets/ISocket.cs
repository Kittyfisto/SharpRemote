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
	internal interface ISocket
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

		//
		// Summary:
		//     Gets or sets a value that indicates whether the System.Net.Sockets.Socket is
		//     in blocking mode.
		//
		// Returns:
		//     true if the System.Net.Sockets.Socket will block; otherwise, false. The default
		//     is true.
		//

		/// <exception cref="System.Net.Sockets.SocketException">
		///     An error occurred when attempting to access the socket. See the Remarks section
		///     for more information.
		/// </exception>
		/// <exception cref="System.ObjectDisposedException">
		///     The System.Net.Sockets.Socket has been closed.
		/// </exception>
		bool Blocking { get; set; }

		//
		// Summary:
		//     Gets a value that indicates whether a System.Net.Sockets.Socket is connected
		//     to a remote host as of the last Overload:System.Net.Sockets.Socket.Send or Overload:System.Net.Sockets.Socket.Receive
		//     operation.
		//
		// Returns:
		//     true if the System.Net.Sockets.Socket was connected to a remote resource as of
		//     the most recent operation; otherwise, false.
		bool Connected { get; }

		//
		// Summary:
		//     Gets or sets a System.Boolean value that specifies whether the System.Net.Sockets.Socket
		//     allows Internet Protocol (IP) datagrams to be fragmented.
		//
		// Returns:
		//     true if the System.Net.Sockets.Socket allows datagram fragmentation; otherwise,
		//     false. The default is true.
		//
		// Exceptions:
		//   T:System.NotSupportedException:
		//     This property can be set only for sockets in the System.Net.Sockets.AddressFamily.InterNetwork
		//     or System.Net.Sockets.AddressFamily.InterNetworkV6 families.

		/// <exception cref="System.Net.Sockets.SocketException">
		///     An error occurred when attempting to access the socket. See the Remarks section
		///     for more information.
		/// </exception>
		/// <exception cref="System.ObjectDisposedException">
		///     The System.Net.Sockets.Socket has been closed.
		/// </exception>
		bool DontFragment { get; set; }
		//
		// Summary:
		//     Gets or sets a System.Boolean value that specifies whether the System.Net.Sockets.Socket
		//     is a dual-mode socket used for both IPv4 and IPv6.
		//
		// Returns:
		//     Returns System.Boolean.true if the System.Net.Sockets.Socket is a dual-mode socket;
		//     otherwise, false. The default is false.
		bool DualMode { get; set; }
		//
		// Summary:
		//     Gets or sets a System.Boolean value that specifies whether the System.Net.Sockets.Socket
		//     can send or receive broadcast packets.
		//
		// Returns:
		//     true if the System.Net.Sockets.Socket allows broadcast packets; otherwise, false.
		//     The default is false.
		//
		// Exceptions:
		//   T:System.Net.Sockets.SocketException:
		//     This option is valid for a datagram socket only.
		//
		//   T:System.ObjectDisposedException:
		//     The System.Net.Sockets.Socket has been closed.
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
		///     The System.Net.Sockets.Socket has been closed.
		/// </exception>
		/// <exception cref="System.InvalidOperationException">
		/// System.Net.Sockets.Socket.Bind(System.Net.EndPoint) has been called for this
		///     System.Net.Sockets.Socket.
		/// </exception>
		bool ExclusiveAddressUse { get; set; }
		//
		// Summary:
		//     Gets the operating system handle for the System.Net.Sockets.Socket.
		//
		// Returns:
		//     An System.IntPtr that represents the operating system handle for the System.Net.Sockets.Socket.
		IntPtr Handle { get; }
		//
		// Summary:
		//     Gets a value that indicates whether the System.Net.Sockets.Socket is bound to
		//     a specific local port.
		//
		// Returns:
		//     true if the System.Net.Sockets.Socket is bound to a local port; otherwise, false.
		bool IsBound { get; }
		//
		// Summary:
		//     Gets or sets a value that specifies whether the System.Net.Sockets.Socket will
		//     delay closing a socket in an attempt to send all pending data.
		//
		// Returns:
		//     A System.Net.Sockets.LingerOption that specifies how to linger while closing
		//     a socket.
		//
		// Exceptions:
		//   T:System.Net.Sockets.SocketException:
		//     An error occurred when attempting to access the socket.
		//
		//   T:System.ObjectDisposedException:
		//     The System.Net.Sockets.Socket has been closed.
		LingerOption LingerState { get; set; }
		//
		// Summary:
		//     Gets the local endpoint.
		//
		// Returns:
		//     The System.Net.EndPoint that the System.Net.Sockets.Socket is using for communications.
		//
		// Exceptions:
		//   T:System.Net.Sockets.SocketException:
		//     An error occurred when attempting to access the socket. See the Remarks section
		//     for more information.
		//
		//   T:System.ObjectDisposedException:
		//     The System.Net.Sockets.Socket has been closed.
		EndPoint LocalEndPoint { get; }
		//
		// Summary:
		//     Gets or sets a value that specifies whether outgoing multicast packets are delivered
		//     to the sending application.
		//
		// Returns:
		//     true if the System.Net.Sockets.Socket receives outgoing multicast packets; otherwise,
		//     false.
		//
		// Exceptions:
		//   T:System.Net.Sockets.SocketException:
		//     An error occurred when attempting to access the socket.
		//
		//   T:System.ObjectDisposedException:
		//     The System.Net.Sockets.Socket has been closed.
		bool MulticastLoopback { get; set; }
		//
		// Summary:
		//     Gets or sets a System.Boolean value that specifies whether the stream System.Net.Sockets.Socket
		//     is using the Nagle algorithm.
		//
		// Returns:
		//     false if the System.Net.Sockets.Socket uses the Nagle algorithm; otherwise, true.
		//     The default is false.
		//
		// Exceptions:
		//   T:System.Net.Sockets.SocketException:
		//     An error occurred when attempting to access the System.Net.Sockets.Socket. See
		//     the Remarks section for more information.
		//
		//   T:System.ObjectDisposedException:
		//     The System.Net.Sockets.Socket has been closed.
		bool NoDelay { get; set; }
		//
		// Summary:
		//     Gets the protocol type of the System.Net.Sockets.Socket.
		//
		// Returns:
		//     One of the System.Net.Sockets.ProtocolType values.
		ProtocolType ProtocolType { get; }
		//
		// Summary:
		//     Gets or sets a value that specifies the size of the receive buffer of the System.Net.Sockets.Socket.
		//
		// Returns:
		//     An System.Int32 that contains the size, in bytes, of the receive buffer. The
		//     default is 8192.
		//
		// Exceptions:
		//   T:System.Net.Sockets.SocketException:
		//     An error occurred when attempting to access the socket.
		//
		//   T:System.ObjectDisposedException:
		//     The System.Net.Sockets.Socket has been closed.
		//
		//   T:System.ArgumentOutOfRangeException:
		//     The value specified for a set operation is less than 0.
		int ReceiveBufferSize { get; set; }
		//
		// Summary:
		//     Gets or sets a value that specifies the amount of time after which a synchronous
		//     Overload:System.Net.Sockets.Socket.Receive call will time out.
		//
		// Returns:
		//     The time-out value, in milliseconds. The default value is 0, which indicates
		//     an infinite time-out period. Specifying -1 also indicates an infinite time-out
		//     period.
		//
		// Exceptions:
		//   T:System.Net.Sockets.SocketException:
		//     An error occurred when attempting to access the socket.
		//
		//   T:System.ObjectDisposedException:
		//     The System.Net.Sockets.Socket has been closed.
		//
		//   T:System.ArgumentOutOfRangeException:
		//     The value specified for a set operation is less than -1.
		int ReceiveTimeout { get; set; }
		//
		// Summary:
		//     Gets the remote endpoint.
		//
		// Returns:
		//     The System.Net.EndPoint with which the System.Net.Sockets.Socket is communicating.
		//
		// Exceptions:
		//   T:System.Net.Sockets.SocketException:
		//     An error occurred when attempting to access the socket. See the Remarks section
		//     for more information.
		//
		//   T:System.ObjectDisposedException:
		//     The System.Net.Sockets.Socket has been closed.
		EndPoint RemoteEndPoint { get; }
		//
		// Summary:
		//     Gets or sets a value that specifies the size of the send buffer of the System.Net.Sockets.Socket.
		//
		// Returns:
		//     An System.Int32 that contains the size, in bytes, of the send buffer. The default
		//     is 8192.
		//
		// Exceptions:
		//   T:System.Net.Sockets.SocketException:
		//     An error occurred when attempting to access the socket.
		//
		//   T:System.ObjectDisposedException:
		//     The System.Net.Sockets.Socket has been closed.
		//
		//   T:System.ArgumentOutOfRangeException:
		//     The value specified for a set operation is less than 0.
		int SendBufferSize { get; set; }
		//
		// Summary:
		//     Gets or sets a value that specifies the amount of time after which a synchronous
		//     Overload:System.Net.Sockets.Socket.Send call will time out.
		//
		// Returns:
		//     The time-out value, in milliseconds. If you set the property with a value between
		//     1 and 499, the value will be changed to 500. The default value is 0, which indicates
		//     an infinite time-out period. Specifying -1 also indicates an infinite time-out
		//     period.
		//
		// Exceptions:
		//   T:System.Net.Sockets.SocketException:
		//     An error occurred when attempting to access the socket.
		//
		//   T:System.ObjectDisposedException:
		//     The System.Net.Sockets.Socket has been closed.
		//
		//   T:System.ArgumentOutOfRangeException:
		//     The value specified for a set operation is less than -1.
		int SendTimeout { get; set; }
		//
		// Summary:
		//     Gets the type of the System.Net.Sockets.Socket.
		//
		// Returns:
		//     One of the System.Net.Sockets.SocketType values.
		SocketType SocketType { get; }
		//
		// Summary:
		//     Gets or sets a value that specifies the Time To Live (TTL) value of Internet
		//     Protocol (IP) packets sent by the System.Net.Sockets.Socket.
		//
		// Returns:
		//     The TTL value.
		//
		// Exceptions:
		//   T:System.ArgumentOutOfRangeException:
		//     The TTL value can't be set to a negative number.
		//
		//   T:System.NotSupportedException:
		//     This property can be set only for sockets in the System.Net.Sockets.AddressFamily.InterNetwork
		//     or System.Net.Sockets.AddressFamily.InterNetworkV6 families.
		//
		//   T:System.Net.Sockets.SocketException:
		//     An error occurred when attempting to access the socket. This error is also returned
		//     when an attempt was made to set TTL to a value higher than 255.
		//
		//   T:System.ObjectDisposedException:
		//     The System.Net.Sockets.Socket has been closed.
		short Ttl { get; set; }

		//
		// Summary:
		//     Specifies whether the socket should only use Overlapped I/O mode.
		//
		// Returns:
		//     true if the System.Net.Sockets.Socket uses only overlapped I/O; otherwise, false.
		//     The default is false.
		//
		// Exceptions:
		//   T:System.InvalidOperationException:
		//     The socket has been bound to a completion port.
		bool UseOnlyOverlappedIO { get; set; }

		//
		// Summary:
		//     Creates a new System.Net.Sockets.Socket for a newly created connection.
		//
		// Returns:
		//     A System.Net.Sockets.Socket for a newly created connection.
		//
		// Exceptions:
		//   T:System.Net.Sockets.SocketException:
		//     An error occurred when attempting to access the socket. See the Remarks section
		//     for more information.
		//
		//   T:System.ObjectDisposedException:
		//     The System.Net.Sockets.Socket has been closed.
		//
		//   T:System.InvalidOperationException:
		//     The accepting socket is not listening for connections. You must call System.Net.Sockets.Socket.Bind(System.Net.EndPoint)
		//     and System.Net.Sockets.Socket.Listen(System.Int32) before calling System.Net.Sockets.Socket.Accept.
		ISocket Accept();

		//
		// Summary:
		//     Begins an asynchronous operation to accept an incoming connection attempt.
		//
		// Parameters:
		//   e:
		//     The System.Net.Sockets.SocketAsyncEventArgs object to use for this asynchronous
		//     socket operation.
		//
		// Returns:
		//     Returns true if the I/O operation is pending. The System.Net.Sockets.SocketAsyncEventArgs.Completed
		//     event on the e parameter will be raised upon completion of the operation.Returns
		//     false if the I/O operation completed synchronously. The System.Net.Sockets.SocketAsyncEventArgs.Completed
		//     event on the e parameter will not be raised and the e object passed as a parameter
		//     may be examined immediately after the method call returns to retrieve the result
		//     of the operation.
		//
		// Exceptions:
		//   T:System.ArgumentException:
		//     An argument is not valid. This exception occurs if the buffer provided is not
		//     large enough. The buffer must be at least 2 * (sizeof(SOCKADDR_STORAGE + 16)
		//     bytes. This exception also occurs if multiple buffers are specified, the System.Net.Sockets.SocketAsyncEventArgs.BufferList
		//     property is not null.
		//
		//   T:System.ArgumentOutOfRangeException:
		//     An argument is out of range. The exception occurs if the System.Net.Sockets.SocketAsyncEventArgs.Count
		//     is less than 0.
		//
		//   T:System.InvalidOperationException:
		//     An invalid operation was requested. This exception occurs if the accepting System.Net.Sockets.Socket
		//     is not listening for connections or the accepted socket is bound. You must call
		//     the System.Net.Sockets.Socket.Bind(System.Net.EndPoint) and System.Net.Sockets.Socket.Listen(System.Int32)
		//     method before calling the System.Net.Sockets.Socket.AcceptAsync(System.Net.Sockets.SocketAsyncEventArgs)
		//     method.This exception also occurs if the socket is already connected or a socket
		//     operation was already in progress using the specified e parameter.
		//
		//   T:System.Net.Sockets.SocketException:
		//     An error occurred when attempting to access the socket. See the Remarks section
		//     for more information.
		//
		//   T:System.NotSupportedException:
		//     Windows XP or later is required for this method.
		//
		//   T:System.ObjectDisposedException:
		//     The System.Net.Sockets.Socket has been closed.
		bool AcceptAsync(SocketAsyncEventArgs e);

		//
		// Summary:
		//     Begins an asynchronous operation to accept an incoming connection attempt.
		//
		// Parameters:
		//   callback:
		//     The System.AsyncCallback delegate.
		//
		//   state:
		//     An object that contains state information for this request.
		//
		// Returns:
		//     An System.IAsyncResult that references the asynchronous System.Net.Sockets.Socket
		//     creation.
		//
		// Exceptions:
		//   T:System.ObjectDisposedException:
		//     The System.Net.Sockets.Socket object has been closed.
		//
		//   T:System.NotSupportedException:
		//     Windows NT is required for this method.
		//
		//   T:System.InvalidOperationException:
		//     The accepting socket is not listening for connections. You must call System.Net.Sockets.Socket.Bind(System.Net.EndPoint)
		//     and System.Net.Sockets.Socket.Listen(System.Int32) before calling System.Net.Sockets.Socket.BeginAccept(System.AsyncCallback,System.Object).-or-
		//     The accepted socket is bound.
		//
		//   T:System.ArgumentOutOfRangeException:
		//     receiveSize is less than 0.
		//
		//   T:System.Net.Sockets.SocketException:
		//     An error occurred when attempting to access the socket. See the Remarks section
		//     for more information.
		IAsyncResult BeginAccept(AsyncCallback callback, object state);

		//
		// Summary:
		//     Begins an asynchronous operation to accept an incoming connection attempt and
		//     receives the first block of data sent by the client application.
		//
		// Parameters:
		//   receiveSize:
		//     The number of bytes to accept from the sender.
		//
		//   callback:
		//     The System.AsyncCallback delegate.
		//
		//   state:
		//     An object that contains state information for this request.
		//
		// Returns:
		//     An System.IAsyncResult that references the asynchronous System.Net.Sockets.Socket
		//     creation.
		//
		// Exceptions:
		//   T:System.ObjectDisposedException:
		//     The System.Net.Sockets.Socket object has been closed.
		//
		//   T:System.NotSupportedException:
		//     Windows NT is required for this method.
		//
		//   T:System.InvalidOperationException:
		//     The accepting socket is not listening for connections. You must call System.Net.Sockets.Socket.Bind(System.Net.EndPoint)
		//     and System.Net.Sockets.Socket.Listen(System.Int32) before calling System.Net.Sockets.Socket.BeginAccept(System.AsyncCallback,System.Object).-or-
		//     The accepted socket is bound.
		//
		//   T:System.ArgumentOutOfRangeException:
		//     receiveSize is less than 0.
		//
		//   T:System.Net.Sockets.SocketException:
		//     An error occurred when attempting to access the socket. See the Remarks section
		//     for more information.
		IAsyncResult BeginAccept(int receiveSize, AsyncCallback callback, object state);

		//
		// Summary:
		//     Begins an asynchronous operation to accept an incoming connection attempt from
		//     a specified socket and receives the first block of data sent by the client application.
		//
		// Parameters:
		//   acceptSocket:
		//     The accepted System.Net.Sockets.Socket object. This value may be null.
		//
		//   receiveSize:
		//     The maximum number of bytes to receive.
		//
		//   callback:
		//     The System.AsyncCallback delegate.
		//
		//   state:
		//     An object that contains state information for this request.
		//
		// Returns:
		//     An System.IAsyncResult object that references the asynchronous System.Net.Sockets.Socket
		//     object creation.
		//
		// Exceptions:
		//   T:System.ObjectDisposedException:
		//     The System.Net.Sockets.Socket object has been closed.
		//
		//   T:System.NotSupportedException:
		//     Windows NT is required for this method.
		//
		//   T:System.InvalidOperationException:
		//     The accepting socket is not listening for connections. You must call System.Net.Sockets.Socket.Bind(System.Net.EndPoint)
		//     and System.Net.Sockets.Socket.Listen(System.Int32) before calling System.Net.Sockets.Socket.BeginAccept(System.AsyncCallback,System.Object).-or-
		//     The accepted socket is bound.
		//
		//   T:System.ArgumentOutOfRangeException:
		//     receiveSize is less than 0.
		//
		//   T:System.Net.Sockets.SocketException:
		//     An error occurred when attempting to access the socket. See the Remarks section
		//     for more information.
		IAsyncResult BeginAccept(Socket acceptSocket, int receiveSize, AsyncCallback callback, object state);
		//
		// Summary:
		//     Begins an asynchronous request for a remote host connection.
		//
		// Parameters:
		//   remoteEP:
		//     An System.Net.EndPoint that represents the remote host.
		//
		//   callback:
		//     The System.AsyncCallback delegate.
		//
		//   state:
		//     An object that contains state information for this request.
		//
		// Returns:
		//     An System.IAsyncResult that references the asynchronous connection.
		//
		// Exceptions:
		//   T:System.ArgumentNullException:
		//     remoteEP is null.
		//
		//   T:System.Net.Sockets.SocketException:
		//     An error occurred when attempting to access the socket. See the Remarks section
		//     for more information.
		//
		//   T:System.ObjectDisposedException:
		//     The System.Net.Sockets.Socket has been closed.
		//
		//   T:System.Security.SecurityException:
		//     A caller higher in the call stack does not have permission for the requested
		//     operation.
		//
		//   T:System.InvalidOperationException:
		//     The System.Net.Sockets.Socket is System.Net.Sockets.Socket.Listen(System.Int32)ing.
		IAsyncResult BeginConnect(EndPoint remoteEP, AsyncCallback callback, object state);
		//
		// Summary:
		//     Begins an asynchronous request for a remote host connection. The host is specified
		//     by an System.Net.IPAddress array and a port number.
		//
		// Parameters:
		//   addresses:
		//     At least one System.Net.IPAddress, designating the remote host.
		//
		//   port:
		//     The port number of the remote host.
		//
		//   requestCallback:
		//     An System.AsyncCallback delegate that references the method to invoke when the
		//     connect operation is complete.
		//
		//   state:
		//     A user-defined object that contains information about the connect operation.
		//     This object is passed to the requestCallback delegate when the operation is complete.
		//
		// Returns:
		//     An System.IAsyncResult that references the asynchronous connections.
		//
		// Exceptions:
		//   T:System.ArgumentNullException:
		//     addresses is null.
		//
		//   T:System.Net.Sockets.SocketException:
		//     An error occurred when attempting to access the socket. See the Remarks section
		//     for more information.
		//
		//   T:System.ObjectDisposedException:
		//     The System.Net.Sockets.Socket has been closed.
		//
		//   T:System.NotSupportedException:
		//     This method is valid for sockets that use System.Net.Sockets.AddressFamily.InterNetwork
		//     or System.Net.Sockets.AddressFamily.InterNetworkV6.
		//
		//   T:System.ArgumentOutOfRangeException:
		//     The port number is not valid.
		//
		//   T:System.ArgumentException:
		//     The length of address is zero.
		//
		//   T:System.InvalidOperationException:
		//     The System.Net.Sockets.Socket is System.Net.Sockets.Socket.Listen(System.Int32)ing.
		IAsyncResult BeginConnect(IPAddress[] addresses, int port, AsyncCallback requestCallback, object state);
		//
		// Summary:
		//     Begins an asynchronous request for a remote host connection. The host is specified
		//     by an System.Net.IPAddress and a port number.
		//
		// Parameters:
		//   address:
		//     The System.Net.IPAddress of the remote host.
		//
		//   port:
		//     The port number of the remote host.
		//
		//   requestCallback:
		//     An System.AsyncCallback delegate that references the method to invoke when the
		//     connect operation is complete.
		//
		//   state:
		//     A user-defined object that contains information about the connect operation.
		//     This object is passed to the requestCallback delegate when the operation is complete.
		//
		// Returns:
		//     An System.IAsyncResult that references the asynchronous connection.
		//
		// Exceptions:
		//   T:System.ArgumentNullException:
		//     address is null.
		//
		//   T:System.Net.Sockets.SocketException:
		//     An error occurred when attempting to access the socket. See the Remarks section
		//     for more information.
		//
		//   T:System.ObjectDisposedException:
		//     The System.Net.Sockets.Socket has been closed.
		//
		//   T:System.NotSupportedException:
		//     The System.Net.Sockets.Socket is not in the socket family.
		//
		//   T:System.ArgumentOutOfRangeException:
		//     The port number is not valid.
		//
		//   T:System.ArgumentException:
		//     The length of address is zero.
		//
		//   T:System.InvalidOperationException:
		//     The System.Net.Sockets.Socket is System.Net.Sockets.Socket.Listen(System.Int32)ing.
		IAsyncResult BeginConnect(IPAddress address, int port, AsyncCallback requestCallback, object state);
		//
		// Summary:
		//     Begins an asynchronous request for a remote host connection. The host is specified
		//     by a host name and a port number.
		//
		// Parameters:
		//   host:
		//     The name of the remote host.
		//
		//   port:
		//     The port number of the remote host.
		//
		//   requestCallback:
		//     An System.AsyncCallback delegate that references the method to invoke when the
		//     connect operation is complete.
		//
		//   state:
		//     A user-defined object that contains information about the connect operation.
		//     This object is passed to the requestCallback delegate when the operation is complete.
		//
		// Returns:
		//     An System.IAsyncResult that references the asynchronous connection.
		//
		// Exceptions:
		//   T:System.ArgumentNullException:
		//     host is null.
		//
		//   T:System.ObjectDisposedException:
		//     The System.Net.Sockets.Socket has been closed.
		//
		//   T:System.NotSupportedException:
		//     This method is valid for sockets in the System.Net.Sockets.AddressFamily.InterNetwork
		//     or System.Net.Sockets.AddressFamily.InterNetworkV6 families.
		//
		//   T:System.ArgumentOutOfRangeException:
		//     The port number is not valid.
		//
		//   T:System.InvalidOperationException:
		//     The System.Net.Sockets.Socket is System.Net.Sockets.Socket.Listen(System.Int32)ing.
		IAsyncResult BeginConnect(string host, int port, AsyncCallback requestCallback, object state);
		//
		// Summary:
		//     Begins an asynchronous request to disconnect from a remote endpoint.
		//
		// Parameters:
		//   reuseSocket:
		//     true if this socket can be reused after the connection is closed; otherwise,
		//     false.
		//
		//   callback:
		//     The System.AsyncCallback delegate.
		//
		//   state:
		//     An object that contains state information for this request.
		//
		// Returns:
		//     An System.IAsyncResult object that references the asynchronous operation.
		//
		// Exceptions:
		//   T:System.NotSupportedException:
		//     The operating system is Windows 2000 or earlier, and this method requires Windows
		//     XP.
		//
		//   T:System.ObjectDisposedException:
		//     The System.Net.Sockets.Socket object has been closed.
		//
		//   T:System.Net.Sockets.SocketException:
		//     An error occurred when attempting to access the socket. See the Remarks section
		//     for more information.
		IAsyncResult BeginDisconnect(bool reuseSocket, AsyncCallback callback, object state);
		//
		// Summary:
		//     Begins to asynchronously receive data from a connected System.Net.Sockets.Socket.
		//
		// Parameters:
		//   buffers:
		//     An array of type System.Byte that is the storage location for the received data.
		//
		//   socketFlags:
		//     A bitwise combination of the System.Net.Sockets.SocketFlags values.
		//
		//   callback:
		//     An System.AsyncCallback delegate that references the method to invoke when the
		//     operation is complete.
		//
		//   state:
		//     A user-defined object that contains information about the receive operation.
		//     This object is passed to the System.Net.Sockets.Socket.EndReceive(System.IAsyncResult)
		//     delegate when the operation is complete.
		//
		// Returns:
		//     An System.IAsyncResult that references the asynchronous read.
		//
		// Exceptions:
		//   T:System.ArgumentNullException:
		//     buffer is null.
		//
		//   T:System.Net.Sockets.SocketException:
		//     An error occurred when attempting to access the socket. See the Remarks section
		//     for more information.
		//
		//   T:System.ObjectDisposedException:
		//     System.Net.Sockets.Socket has been closed.
		IAsyncResult BeginReceive(IList<ArraySegment<byte>> buffers, SocketFlags socketFlags, AsyncCallback callback, object state);
		//
		// Summary:
		//     Begins to asynchronously receive data from a connected System.Net.Sockets.Socket.
		//
		// Parameters:
		//   buffers:
		//     An array of type System.Byte that is the storage location for the received data.
		//
		//   socketFlags:
		//     A bitwise combination of the System.Net.Sockets.SocketFlags values.
		//
		//   errorCode:
		//     A System.Net.Sockets.SocketError object that stores the socket error.
		//
		//   callback:
		//     An System.AsyncCallback delegate that references the method to invoke when the
		//     operation is complete.
		//
		//   state:
		//     A user-defined object that contains information about the receive operation.
		//     This object is passed to the System.Net.Sockets.Socket.EndReceive(System.IAsyncResult)
		//     delegate when the operation is complete.
		//
		// Returns:
		//     An System.IAsyncResult that references the asynchronous read.
		//
		// Exceptions:
		//   T:System.ArgumentNullException:
		//     buffer is null.
		//
		//   T:System.Net.Sockets.SocketException:
		//     An error occurred when attempting to access the socket. See the Remarks section
		//     for more information.
		//
		//   T:System.ObjectDisposedException:
		//     System.Net.Sockets.Socket has been closed.
		IAsyncResult BeginReceive(IList<ArraySegment<byte>> buffers, SocketFlags socketFlags, out SocketError errorCode, AsyncCallback callback, object state);
		//
		// Summary:
		//     Begins to asynchronously receive data from a connected System.Net.Sockets.Socket.
		//
		// Parameters:
		//   buffer:
		//     An array of type System.Byte that is the storage location for the received data.
		//
		//   offset:
		//     The zero-based position in the buffer parameter at which to store the received
		//     data.
		//
		//   size:
		//     The number of bytes to receive.
		//
		//   socketFlags:
		//     A bitwise combination of the System.Net.Sockets.SocketFlags values.
		//
		//   callback:
		//     An System.AsyncCallback delegate that references the method to invoke when the
		//     operation is complete.
		//
		//   state:
		//     A user-defined object that contains information about the receive operation.
		//     This object is passed to the System.Net.Sockets.Socket.EndReceive(System.IAsyncResult)
		//     delegate when the operation is complete.
		//
		// Returns:
		//     An System.IAsyncResult that references the asynchronous read.
		//
		// Exceptions:
		//   T:System.ArgumentNullException:
		//     buffer is null.
		//
		//   T:System.Net.Sockets.SocketException:
		//     An error occurred when attempting to access the socket. See the Remarks section
		//     for more information.
		//
		//   T:System.ObjectDisposedException:
		//     System.Net.Sockets.Socket has been closed.
		//
		//   T:System.ArgumentOutOfRangeException:
		//     offset is less than 0.-or- offset is greater than the length of buffer.-or- size
		//     is less than 0.-or- size is greater than the length of buffer minus the value
		//     of the offset parameter.
		IAsyncResult BeginReceive(byte[] buffer, int offset, int size, SocketFlags socketFlags, AsyncCallback callback, object state);
		//
		// Summary:
		//     Begins to asynchronously receive data from a connected System.Net.Sockets.Socket.
		//
		// Parameters:
		//   buffer:
		//     An array of type System.Byte that is the storage location for the received data.
		//
		//   offset:
		//     The location in buffer to store the received data.
		//
		//   size:
		//     The number of bytes to receive.
		//
		//   socketFlags:
		//     A bitwise combination of the System.Net.Sockets.SocketFlags values.
		//
		//   errorCode:
		//     A System.Net.Sockets.SocketError object that stores the socket error.
		//
		//   callback:
		//     An System.AsyncCallback delegate that references the method to invoke when the
		//     operation is complete.
		//
		//   state:
		//     A user-defined object that contains information about the receive operation.
		//     This object is passed to the System.Net.Sockets.Socket.EndReceive(System.IAsyncResult)
		//     delegate when the operation is complete.
		//
		// Returns:
		//     An System.IAsyncResult that references the asynchronous read.
		//
		// Exceptions:
		//   T:System.ArgumentNullException:
		//     buffer is null.
		//
		//   T:System.Net.Sockets.SocketException:
		//     An error occurred when attempting to access the socket. See the Remarks section
		//     for more information.
		//
		//   T:System.ObjectDisposedException:
		//     System.Net.Sockets.Socket has been closed.
		//
		//   T:System.ArgumentOutOfRangeException:
		//     offset is less than 0.-or- offset is greater than the length of buffer.-or- size
		//     is less than 0.-or- size is greater than the length of buffer minus the value
		//     of the offset parameter.
		IAsyncResult BeginReceive(byte[] buffer, int offset, int size, SocketFlags socketFlags, out SocketError errorCode, AsyncCallback callback, object state);
		//
		// Summary:
		//     Begins to asynchronously receive data from a specified network device.
		//
		// Parameters:
		//   buffer:
		//     An array of type System.Byte that is the storage location for the received data.
		//
		//   offset:
		//     The zero-based position in the buffer parameter at which to store the data.
		//
		//   size:
		//     The number of bytes to receive.
		//
		//   socketFlags:
		//     A bitwise combination of the System.Net.Sockets.SocketFlags values.
		//
		//   remoteEP:
		//     An System.Net.EndPoint that represents the source of the data.
		//
		//   callback:
		//     The System.AsyncCallback delegate.
		//
		//   state:
		//     An object that contains state information for this request.
		//
		// Returns:
		//     An System.IAsyncResult that references the asynchronous read.
		//
		// Exceptions:
		//   T:System.ArgumentNullException:
		//     buffer is null.-or- remoteEP is null.
		//
		//   T:System.Net.Sockets.SocketException:
		//     An error occurred when attempting to access the socket. See the Remarks section
		//     for more information.
		//
		//   T:System.ArgumentOutOfRangeException:
		//     offset is less than 0.-or- offset is greater than the length of buffer.-or- size
		//     is less than 0.-or- size is greater than the length of buffer minus the value
		//     of the offset parameter.
		//
		//   T:System.ObjectDisposedException:
		//     The System.Net.Sockets.Socket has been closed.
		//
		//   T:System.Security.SecurityException:
		//     A caller higher in the call stack does not have permission for the requested
		//     operation.
		IAsyncResult BeginReceiveFrom(byte[] buffer, int offset, int size, SocketFlags socketFlags, ref EndPoint remoteEP, AsyncCallback callback, object state);
		//
		// Summary:
		//     Begins to asynchronously receive the specified number of bytes of data into the
		//     specified location of the data buffer, using the specified System.Net.Sockets.SocketFlags,
		//     and stores the endpoint and packet information..
		//
		// Parameters:
		//   buffer:
		//     An array of type System.Byte that is the storage location for the received data.
		//
		//   offset:
		//     The zero-based position in the buffer parameter at which to store the data.
		//
		//   size:
		//     The number of bytes to receive.
		//
		//   socketFlags:
		//     A bitwise combination of the System.Net.Sockets.SocketFlags values.
		//
		//   remoteEP:
		//     An System.Net.EndPoint that represents the source of the data.
		//
		//   callback:
		//     The System.AsyncCallback delegate.
		//
		//   state:
		//     An object that contains state information for this request.
		//
		// Returns:
		//     An System.IAsyncResult that references the asynchronous read.
		//
		// Exceptions:
		//   T:System.ArgumentNullException:
		//     buffer is null.-or- remoteEP is null.
		//
		//   T:System.Net.Sockets.SocketException:
		//     An error occurred when attempting to access the socket. See the Remarks section
		//     for more information.
		//
		//   T:System.ArgumentOutOfRangeException:
		//     offset is less than 0.-or- offset is greater than the length of buffer.-or- size
		//     is less than 0.-or- size is greater than the length of buffer minus the value
		//     of the offset parameter.
		//
		//   T:System.ObjectDisposedException:
		//     The System.Net.Sockets.Socket has been closed.
		//
		//   T:System.NotSupportedException:
		//     The operating system is Windows 2000 or earlier, and this method requires Windows
		//     XP.
		IAsyncResult BeginReceiveMessageFrom(byte[] buffer, int offset, int size, SocketFlags socketFlags, ref EndPoint remoteEP, AsyncCallback callback, object state);
		//
		// Summary:
		//     Sends data asynchronously to a connected System.Net.Sockets.Socket.
		//
		// Parameters:
		//   buffers:
		//     An array of type System.Byte that contains the data to send.
		//
		//   socketFlags:
		//     A bitwise combination of the System.Net.Sockets.SocketFlags values.
		//
		//   callback:
		//     The System.AsyncCallback delegate.
		//
		//   state:
		//     An object that contains state information for this request.
		//
		// Returns:
		//     An System.IAsyncResult that references the asynchronous send.
		//
		// Exceptions:
		//   T:System.ArgumentNullException:
		//     buffers is null.
		//
		//   T:System.ArgumentException:
		//     buffers is empty.
		//
		//   T:System.Net.Sockets.SocketException:
		//     An error occurred when attempting to access the socket. See remarks section below.
		//
		//   T:System.ObjectDisposedException:
		//     The System.Net.Sockets.Socket has been closed.
		IAsyncResult BeginSend(IList<ArraySegment<byte>> buffers, SocketFlags socketFlags, AsyncCallback callback, object state);
		//
		// Summary:
		//     Sends data asynchronously to a connected System.Net.Sockets.Socket.
		//
		// Parameters:
		//   buffers:
		//     An array of type System.Byte that contains the data to send.
		//
		//   socketFlags:
		//     A bitwise combination of the System.Net.Sockets.SocketFlags values.
		//
		//   errorCode:
		//     A System.Net.Sockets.SocketError object that stores the socket error.
		//
		//   callback:
		//     The System.AsyncCallback delegate.
		//
		//   state:
		//     An object that contains state information for this request.
		//
		// Returns:
		//     An System.IAsyncResult that references the asynchronous send.
		//
		// Exceptions:
		//   T:System.ArgumentNullException:
		//     buffers is null.
		//
		//   T:System.ArgumentException:
		//     buffers is empty.
		//
		//   T:System.Net.Sockets.SocketException:
		//     An error occurred when attempting to access the socket. See remarks section below.
		//
		//   T:System.ObjectDisposedException:
		//     The System.Net.Sockets.Socket has been closed.
		IAsyncResult BeginSend(IList<ArraySegment<byte>> buffers, SocketFlags socketFlags, out SocketError errorCode, AsyncCallback callback, object state);
		//
		// Summary:
		//     Sends data asynchronously to a connected System.Net.Sockets.Socket.
		//
		// Parameters:
		//   buffer:
		//     An array of type System.Byte that contains the data to send.
		//
		//   offset:
		//     The zero-based position in the buffer parameter at which to begin sending data.
		//
		//   size:
		//     The number of bytes to send.
		//
		//   socketFlags:
		//     A bitwise combination of the System.Net.Sockets.SocketFlags values.
		//
		//   callback:
		//     The System.AsyncCallback delegate.
		//
		//   state:
		//     An object that contains state information for this request.
		//
		// Returns:
		//     An System.IAsyncResult that references the asynchronous send.
		//
		// Exceptions:
		//   T:System.ArgumentNullException:
		//     buffer is null.
		//
		//   T:System.Net.Sockets.SocketException:
		//     An error occurred when attempting to access the socket. See remarks section below.
		//
		//   T:System.ArgumentOutOfRangeException:
		//     offset is less than 0.-or- offset is less than the length of buffer.-or- size
		//     is less than 0.-or- size is greater than the length of buffer minus the value
		//     of the offset parameter.
		//
		//   T:System.ObjectDisposedException:
		//     The System.Net.Sockets.Socket has been closed.
		IAsyncResult BeginSend(byte[] buffer, int offset, int size, SocketFlags socketFlags, AsyncCallback callback, object state);
		//
		// Summary:
		//     Sends data asynchronously to a connected System.Net.Sockets.Socket.
		//
		// Parameters:
		//   buffer:
		//     An array of type System.Byte that contains the data to send.
		//
		//   offset:
		//     The zero-based position in the buffer parameter at which to begin sending data.
		//
		//   size:
		//     The number of bytes to send.
		//
		//   socketFlags:
		//     A bitwise combination of the System.Net.Sockets.SocketFlags values.
		//
		//   errorCode:
		//     A System.Net.Sockets.SocketError object that stores the socket error.
		//
		//   callback:
		//     The System.AsyncCallback delegate.
		//
		//   state:
		//     An object that contains state information for this request.
		//
		// Returns:
		//     An System.IAsyncResult that references the asynchronous send.
		//
		// Exceptions:
		//   T:System.ArgumentNullException:
		//     buffer is null.
		//
		//   T:System.Net.Sockets.SocketException:
		//     An error occurred when attempting to access the socket. See remarks section below.
		//
		//   T:System.ArgumentOutOfRangeException:
		//     offset is less than 0.-or- offset is less than the length of buffer.-or- size
		//     is less than 0.-or- size is greater than the length of buffer minus the value
		//     of the offset parameter.
		//
		//   T:System.ObjectDisposedException:
		//     The System.Net.Sockets.Socket has been closed.
		IAsyncResult BeginSend(byte[] buffer, int offset, int size, SocketFlags socketFlags, out SocketError errorCode, AsyncCallback callback, object state);
		//
		// Summary:
		//     Sends the file fileName to a connected System.Net.Sockets.Socket object using
		//     the System.Net.Sockets.TransmitFileOptions.UseDefaultWorkerThread flag.
		//
		// Parameters:
		//   fileName:
		//     A string that contains the path and name of the file to send. This parameter
		//     can be null.
		//
		//   callback:
		//     The System.AsyncCallback delegate.
		//
		//   state:
		//     An object that contains state information for this request.
		//
		// Returns:
		//     An System.IAsyncResult object that represents the asynchronous send.
		//
		// Exceptions:
		//   T:System.ObjectDisposedException:
		//     The System.Net.Sockets.Socket object has been closed.
		//
		//   T:System.NotSupportedException:
		//     The socket is not connected to a remote host.
		//
		//   T:System.IO.FileNotFoundException:
		//     The file fileName was not found.
		//
		//   T:System.Net.Sockets.SocketException:
		//     An error occurred when attempting to access the socket. See remarks section below.
		IAsyncResult BeginSendFile(string fileName, AsyncCallback callback, object state);
		//
		// Summary:
		//     Sends a file and buffers of data asynchronously to a connected System.Net.Sockets.Socket
		//     object.
		//
		// Parameters:
		//   fileName:
		//     A string that contains the path and name of the file to be sent. This parameter
		//     can be null.
		//
		//   preBuffer:
		//     A System.Byte array that contains data to be sent before the file is sent. This
		//     parameter can be null.
		//
		//   postBuffer:
		//     A System.Byte array that contains data to be sent after the file is sent. This
		//     parameter can be null.
		//
		//   flags:
		//     A bitwise combination of System.Net.Sockets.TransmitFileOptions values.
		//
		//   callback:
		//     An System.AsyncCallback delegate to be invoked when this operation completes.
		//     This parameter can be null.
		//
		//   state:
		//     A user-defined object that contains state information for this request. This
		//     parameter can be null.
		//
		// Returns:
		//     An System.IAsyncResult object that represents the asynchronous operation.
		//
		// Exceptions:
		//   T:System.ObjectDisposedException:
		//     The System.Net.Sockets.Socket object has been closed.
		//
		//   T:System.Net.Sockets.SocketException:
		//     An error occurred when attempting to access the socket. See remarks section below.
		//
		//   T:System.NotSupportedException:
		//     The operating system is not Windows NT or later.- or - The socket is not connected
		//     to a remote host.
		//
		//   T:System.IO.FileNotFoundException:
		//     The file fileName was not found.
		IAsyncResult BeginSendFile(string fileName, byte[] preBuffer, byte[] postBuffer, TransmitFileOptions flags, AsyncCallback callback, object state);
		//
		// Summary:
		//     Sends data asynchronously to a specific remote host.
		//
		// Parameters:
		//   buffer:
		//     An array of type System.Byte that contains the data to send.
		//
		//   offset:
		//     The zero-based position in buffer at which to begin sending data.
		//
		//   size:
		//     The number of bytes to send.
		//
		//   socketFlags:
		//     A bitwise combination of the System.Net.Sockets.SocketFlags values.
		//
		//   remoteEP:
		//     An System.Net.EndPoint that represents the remote device.
		//
		//   callback:
		//     The System.AsyncCallback delegate.
		//
		//   state:
		//     An object that contains state information for this request.
		//
		// Returns:
		//     An System.IAsyncResult that references the asynchronous send.
		//
		// Exceptions:
		//   T:System.ArgumentNullException:
		//     buffer is null.-or- remoteEP is null.
		//
		//   T:System.Net.Sockets.SocketException:
		//     An error occurred when attempting to access the socket. See the Remarks section
		//     for more information.
		//
		//   T:System.ArgumentOutOfRangeException:
		//     offset is less than 0.-or- offset is greater than the length of buffer.-or- size
		//     is less than 0.-or- size is greater than the length of buffer minus the value
		//     of the offset parameter.
		//
		//   T:System.ObjectDisposedException:
		//     The System.Net.Sockets.Socket has been closed.
		//
		//   T:System.Security.SecurityException:
		//     A caller higher in the call stack does not have permission for the requested
		//     operation.
		IAsyncResult BeginSendTo(byte[] buffer, int offset, int size, SocketFlags socketFlags, EndPoint remoteEP, AsyncCallback callback, object state);
		//
		// Summary:
		//     Associates a System.Net.Sockets.Socket with a local endpoint.
		//
		// Parameters:
		//   localEP:
		//     The local System.Net.EndPoint to associate with the System.Net.Sockets.Socket.
		//
		// Exceptions:
		//   T:System.ArgumentNullException:
		//     localEP is null.
		//
		//   T:System.Net.Sockets.SocketException:
		//     An error occurred when attempting to access the socket. See the Remarks section
		//     for more information.
		//
		//   T:System.ObjectDisposedException:
		//     The System.Net.Sockets.Socket has been closed.
		//
		//   T:System.Security.SecurityException:
		//     A caller higher in the call stack does not have permission for the requested
		//     operation.
		void Bind(EndPoint localEP);
		//
		// Summary:
		//     Closes the System.Net.Sockets.Socket connection and releases all associated resources.
		void Close();
		//
		// Summary:
		//     Closes the System.Net.Sockets.Socket connection and releases all associated resources
		//     with a specified timeout to allow queued data to be sent.
		//
		// Parameters:
		//   timeout:
		//     Wait up to timeout seconds to send any remaining data, then close the socket.
		void Close(int timeout);
		//
		// Summary:
		//     Establishes a connection to a remote host.
		//
		// Parameters:
		//   remoteEP:
		//     An System.Net.EndPoint that represents the remote device.
		//
		// Exceptions:
		//   T:System.ArgumentNullException:
		//     remoteEP is null.
		//
		//   T:System.Net.Sockets.SocketException:
		//     An error occurred when attempting to access the socket. See the Remarks section
		//     for more information.
		//
		//   T:System.ObjectDisposedException:
		//     The System.Net.Sockets.Socket has been closed.
		//
		//   T:System.Security.SecurityException:
		//     A caller higher in the call stack does not have permission for the requested
		//     operation.
		//
		//   T:System.InvalidOperationException:
		//     The System.Net.Sockets.Socket is System.Net.Sockets.Socket.Listen(System.Int32)ing.
		void Connect(EndPoint remoteEP);
		//
		// Summary:
		//     Establishes a connection to a remote host. The host is specified by an IP address
		//     and a port number.
		//
		// Parameters:
		//   address:
		//     The IP address of the remote host.
		//
		//   port:
		//     The port number of the remote host.
		//
		// Exceptions:
		//   T:System.ArgumentNullException:
		//     address is null.
		//
		//   T:System.ArgumentOutOfRangeException:
		//     The port number is not valid.
		//
		//   T:System.Net.Sockets.SocketException:
		//     An error occurred when attempting to access the socket. See the Remarks section
		//     for more information.
		//
		//   T:System.ObjectDisposedException:
		//     The System.Net.Sockets.Socket has been closed.
		//
		//   T:System.NotSupportedException:
		//     This method is valid for sockets in the System.Net.Sockets.AddressFamily.InterNetwork
		//     or System.Net.Sockets.AddressFamily.InterNetworkV6 families.
		//
		//   T:System.ArgumentException:
		//     The length of address is zero.
		//
		//   T:System.InvalidOperationException:
		//     The System.Net.Sockets.Socket is System.Net.Sockets.Socket.Listen(System.Int32)ing.
		void Connect(IPAddress address, int port);
		//
		// Summary:
		//     Establishes a connection to a remote host. The host is specified by a host name
		//     and a port number.
		//
		// Parameters:
		//   host:
		//     The name of the remote host.
		//
		//   port:
		//     The port number of the remote host.
		//
		// Exceptions:
		//   T:System.ArgumentNullException:
		//     host is null.
		//
		//   T:System.ArgumentOutOfRangeException:
		//     The port number is not valid.
		//
		//   T:System.Net.Sockets.SocketException:
		//     An error occurred when attempting to access the socket. See the Remarks section
		//     for more information.
		//
		//   T:System.ObjectDisposedException:
		//     The System.Net.Sockets.Socket has been closed.
		//
		//   T:System.NotSupportedException:
		//     This method is valid for sockets in the System.Net.Sockets.AddressFamily.InterNetwork
		//     or System.Net.Sockets.AddressFamily.InterNetworkV6 families.
		//
		//   T:System.InvalidOperationException:
		//     The System.Net.Sockets.Socket is System.Net.Sockets.Socket.Listen(System.Int32)ing.
		void Connect(string host, int port);
		//
		// Summary:
		//     Establishes a connection to a remote host. The host is specified by an array
		//     of IP addresses and a port number.
		//
		// Parameters:
		//   addresses:
		//     The IP addresses of the remote host.
		//
		//   port:
		//     The port number of the remote host.
		//
		// Exceptions:
		//   T:System.ArgumentNullException:
		//     addresses is null.
		//
		//   T:System.ArgumentOutOfRangeException:
		//     The port number is not valid.
		//
		//   T:System.Net.Sockets.SocketException:
		//     An error occurred when attempting to access the socket. See the Remarks section
		//     for more information.
		//
		//   T:System.ObjectDisposedException:
		//     The System.Net.Sockets.Socket has been closed.
		//
		//   T:System.NotSupportedException:
		//     This method is valid for sockets in the System.Net.Sockets.AddressFamily.InterNetwork
		//     or System.Net.Sockets.AddressFamily.InterNetworkV6 families.
		//
		//   T:System.ArgumentException:
		//     The length of address is zero.
		//
		//   T:System.InvalidOperationException:
		//     The System.Net.Sockets.Socket is System.Net.Sockets.Socket.Listen(System.Int32)ing.
		void Connect(IPAddress[] addresses, int port);
		//
		// Summary:
		//     Begins an asynchronous request for a connection to a remote host.
		//
		// Parameters:
		//   e:
		//     The System.Net.Sockets.SocketAsyncEventArgs object to use for this asynchronous
		//     socket operation.
		//
		// Returns:
		//     Returns true if the I/O operation is pending. The System.Net.Sockets.SocketAsyncEventArgs.Completed
		//     event on the e parameter will be raised upon completion of the operation. Returns
		//     false if the I/O operation completed synchronously. In this case, The System.Net.Sockets.SocketAsyncEventArgs.Completed
		//     event on the e parameter will not be raised and the e object passed as a parameter
		//     may be examined immediately after the method call returns to retrieve the result
		//     of the operation.
		//
		// Exceptions:
		//   T:System.ArgumentException:
		//     An argument is not valid. This exception occurs if multiple buffers are specified,
		//     the System.Net.Sockets.SocketAsyncEventArgs.BufferList property is not null.
		//
		//   T:System.ArgumentNullException:
		//     The e parameter cannot be null and the System.Net.Sockets.SocketAsyncEventArgs.RemoteEndPoint
		//     cannot be null.
		//
		//   T:System.InvalidOperationException:
		//     The System.Net.Sockets.Socket is listening or a socket operation was already
		//     in progress using the System.Net.Sockets.SocketAsyncEventArgs object specified
		//     in the e parameter.
		//
		//   T:System.Net.Sockets.SocketException:
		//     An error occurred when attempting to access the socket. See the Remarks section
		//     for more information.
		//
		//   T:System.NotSupportedException:
		//     Windows XP or later is required for this method. This exception also occurs if
		//     the local endpoint and the System.Net.Sockets.SocketAsyncEventArgs.RemoteEndPoint
		//     are not the same address family.
		//
		//   T:System.ObjectDisposedException:
		//     The System.Net.Sockets.Socket has been closed.
		//
		//   T:System.Security.SecurityException:
		//     A caller higher in the call stack does not have permission for the requested
		//     operation.
		bool ConnectAsync(SocketAsyncEventArgs e);
		//
		// Summary:
		//     Closes the socket connection and allows reuse of the socket.
		//
		// Parameters:
		//   reuseSocket:
		//     true if this socket can be reused after the current connection is closed; otherwise,
		//     false.
		//
		// Exceptions:
		//   T:System.PlatformNotSupportedException:
		//     This method requires Windows 2000 or earlier, or the exception will be thrown.
		//
		//   T:System.ObjectDisposedException:
		//     The System.Net.Sockets.Socket object has been closed.
		//
		//   T:System.Net.Sockets.SocketException:
		//     An error occurred when attempting to access the socket. See the Remarks section
		//     for more information.
		void Disconnect(bool reuseSocket);
		//
		// Summary:
		//     Begins an asynchronous request to disconnect from a remote endpoint.
		//
		// Parameters:
		//   e:
		//     The System.Net.Sockets.SocketAsyncEventArgs object to use for this asynchronous
		//     socket operation.
		//
		// Returns:
		//     Returns true if the I/O operation is pending. The System.Net.Sockets.SocketAsyncEventArgs.Completed
		//     event on the e parameter will be raised upon completion of the operation. Returns
		//     false if the I/O operation completed synchronously. In this case, The System.Net.Sockets.SocketAsyncEventArgs.Completed
		//     event on the e parameter will not be raised and the e object passed as a parameter
		//     may be examined immediately after the method call returns to retrieve the result
		//     of the operation.
		//
		// Exceptions:
		//   T:System.ArgumentNullException:
		//     The e parameter cannot be null.
		//
		//   T:System.InvalidOperationException:
		//     A socket operation was already in progress using the System.Net.Sockets.SocketAsyncEventArgs
		//     object specified in the e parameter.
		//
		//   T:System.NotSupportedException:
		//     Windows XP or later is required for this method.
		//
		//   T:System.ObjectDisposedException:
		//     The System.Net.Sockets.Socket has been closed.
		//
		//   T:System.Net.Sockets.SocketException:
		//     An error occurred when attempting to access the socket.
		bool DisconnectAsync(SocketAsyncEventArgs e);

		//
		// Summary:
		//     Duplicates the socket reference for the target process, and closes the socket
		//     for this process.
		//
		// Parameters:
		//   targetProcessId:
		//     The ID of the target process where a duplicate of the socket reference is created.
		//
		// Returns:
		//     The socket reference to be passed to the target process.
		//
		// Exceptions:
		//   T:System.Net.Sockets.SocketException:
		//     targetProcessID is not a valid process id.-or- Duplication of the socket reference
		//     failed.
		SocketInformation DuplicateAndClose(int targetProcessId);
		//
		// Summary:
		//     Asynchronously accepts an incoming connection attempt and creates a new System.Net.Sockets.Socket
		//     to handle remote host communication.
		//
		// Parameters:
		//   asyncResult:
		//     An System.IAsyncResult that stores state information for this asynchronous operation
		//     as well as any user defined data.
		//
		// Returns:
		//     A System.Net.Sockets.Socket to handle communication with the remote host.
		//
		// Exceptions:
		//   T:System.ArgumentNullException:
		//     asyncResult is null.
		//
		//   T:System.ArgumentException:
		//     asyncResult was not created by a call to System.Net.Sockets.Socket.BeginAccept(System.AsyncCallback,System.Object).
		//
		//   T:System.Net.Sockets.SocketException:
		//     An error occurred when attempting to access the socket. See the Remarks section
		//     for more information.
		//
		//   T:System.ObjectDisposedException:
		//     The System.Net.Sockets.Socket has been closed.
		//
		//   T:System.InvalidOperationException:
		//     System.Net.Sockets.Socket.EndAccept(System.IAsyncResult) method was previously
		//     called.
		//
		//   T:System.NotSupportedException:
		//     Windows NT is required for this method.
		ISocket EndAccept(IAsyncResult asyncResult);
		//
		// Summary:
		//     Asynchronously accepts an incoming connection attempt and creates a new System.Net.Sockets.Socket
		//     object to handle remote host communication. This method returns a buffer that
		//     contains the initial data transferred.
		//
		// Parameters:
		//   buffer:
		//     An array of type System.Byte that contains the bytes transferred.
		//
		//   asyncResult:
		//     An System.IAsyncResult object that stores state information for this asynchronous
		//     operation as well as any user defined data.
		//
		// Returns:
		//     A System.Net.Sockets.Socket object to handle communication with the remote host.
		//
		// Exceptions:
		//   T:System.NotSupportedException:
		//     Windows NT is required for this method.
		//
		//   T:System.ObjectDisposedException:
		//     The System.Net.Sockets.Socket object has been closed.
		//
		//   T:System.ArgumentNullException:
		//     asyncResult is empty.
		//
		//   T:System.ArgumentException:
		//     asyncResult was not created by a call to System.Net.Sockets.Socket.BeginAccept(System.AsyncCallback,System.Object).
		//
		//   T:System.InvalidOperationException:
		//     System.Net.Sockets.Socket.EndAccept(System.IAsyncResult) method was previously
		//     called.
		//
		//   T:System.Net.Sockets.SocketException:
		//     An error occurred when attempting to access the System.Net.Sockets.Socket See
		//     the Remarks section for more information.
		ISocket EndAccept(out byte[] buffer, IAsyncResult asyncResult);
		//
		// Summary:
		//     Asynchronously accepts an incoming connection attempt and creates a new System.Net.Sockets.Socket
		//     object to handle remote host communication. This method returns a buffer that
		//     contains the initial data and the number of bytes transferred.
		//
		// Parameters:
		//   buffer:
		//     An array of type System.Byte that contains the bytes transferred.
		//
		//   bytesTransferred:
		//     The number of bytes transferred.
		//
		//   asyncResult:
		//     An System.IAsyncResult object that stores state information for this asynchronous
		//     operation as well as any user defined data.
		//
		// Returns:
		//     A System.Net.Sockets.Socket object to handle communication with the remote host.
		//
		// Exceptions:
		//   T:System.NotSupportedException:
		//     Windows NT is required for this method.
		//
		//   T:System.ObjectDisposedException:
		//     The System.Net.Sockets.Socket object has been closed.
		//
		//   T:System.ArgumentNullException:
		//     asyncResult is empty.
		//
		//   T:System.ArgumentException:
		//     asyncResult was not created by a call to System.Net.Sockets.Socket.BeginAccept(System.AsyncCallback,System.Object).
		//
		//   T:System.InvalidOperationException:
		//     System.Net.Sockets.Socket.EndAccept(System.IAsyncResult) method was previously
		//     called.
		//
		//   T:System.Net.Sockets.SocketException:
		//     An error occurred when attempting to access the System.Net.Sockets.Socket. See
		//     the Remarks section for more information.
		ISocket EndAccept(out byte[] buffer, out int bytesTransferred, IAsyncResult asyncResult);
		//
		// Summary:
		//     Ends a pending asynchronous connection request.
		//
		// Parameters:
		//   asyncResult:
		//     An System.IAsyncResult that stores state information and any user defined data
		//     for this asynchronous operation.
		//
		// Exceptions:
		//   T:System.ArgumentNullException:
		//     asyncResult is null.
		//
		//   T:System.ArgumentException:
		//     asyncResult was not returned by a call to the System.Net.Sockets.Socket.BeginConnect(System.Net.EndPoint,System.AsyncCallback,System.Object)
		//     method.
		//
		//   T:System.InvalidOperationException:
		//     System.Net.Sockets.Socket.EndConnect(System.IAsyncResult) was previously called
		//     for the asynchronous connection.
		//
		//   T:System.Net.Sockets.SocketException:
		//     An error occurred when attempting to access the socket. See the Remarks section
		//     for more information.
		//
		//   T:System.ObjectDisposedException:
		//     The System.Net.Sockets.Socket has been closed.
		void EndConnect(IAsyncResult asyncResult);
		//
		// Summary:
		//     Ends a pending asynchronous disconnect request.
		//
		// Parameters:
		//   asyncResult:
		//     An System.IAsyncResult object that stores state information and any user-defined
		//     data for this asynchronous operation.
		//
		// Exceptions:
		//   T:System.NotSupportedException:
		//     The operating system is Windows 2000 or earlier, and this method requires Windows
		//     XP.
		//
		//   T:System.ObjectDisposedException:
		//     The System.Net.Sockets.Socket object has been closed.
		//
		//   T:System.ArgumentNullException:
		//     asyncResult is null.
		//
		//   T:System.ArgumentException:
		//     asyncResult was not returned by a call to the System.Net.Sockets.Socket.BeginDisconnect(System.Boolean,System.AsyncCallback,System.Object)
		//     method.
		//
		//   T:System.InvalidOperationException:
		//     System.Net.Sockets.Socket.EndDisconnect(System.IAsyncResult) was previously called
		//     for the asynchronous connection.
		//
		//   T:System.Net.Sockets.SocketException:
		//     An error occurred when attempting to access the socket. See the Remarks section
		//     for more information.
		//
		//   T:System.Net.WebException:
		//     The disconnect request has timed out.
		void EndDisconnect(IAsyncResult asyncResult);
		//
		// Summary:
		//     Ends a pending asynchronous read.
		//
		// Parameters:
		//   asyncResult:
		//     An System.IAsyncResult that stores state information and any user defined data
		//     for this asynchronous operation.
		//
		// Returns:
		//     The number of bytes received.
		//
		// Exceptions:
		//   T:System.ArgumentNullException:
		//     asyncResult is null.
		//
		//   T:System.ArgumentException:
		//     asyncResult was not returned by a call to the System.Net.Sockets.Socket.BeginReceive(System.Byte[],System.Int32,System.Int32,System.Net.Sockets.SocketFlags,System.AsyncCallback,System.Object)
		//     method.
		//
		//   T:System.InvalidOperationException:
		//     System.Net.Sockets.Socket.EndReceive(System.IAsyncResult) was previously called
		//     for the asynchronous read.
		//
		//   T:System.Net.Sockets.SocketException:
		//     An error occurred when attempting to access the socket. See the Remarks section
		//     for more information.
		//
		//   T:System.ObjectDisposedException:
		//     The System.Net.Sockets.Socket has been closed.
		int EndReceive(IAsyncResult asyncResult);
		//
		// Summary:
		//     Ends a pending asynchronous read.
		//
		// Parameters:
		//   asyncResult:
		//     An System.IAsyncResult that stores state information and any user defined data
		//     for this asynchronous operation.
		//
		//   errorCode:
		//     A System.Net.Sockets.SocketError object that stores the socket error.
		//
		// Returns:
		//     The number of bytes received.
		//
		// Exceptions:
		//   T:System.ArgumentNullException:
		//     asyncResult is null.
		//
		//   T:System.ArgumentException:
		//     asyncResult was not returned by a call to the System.Net.Sockets.Socket.BeginReceive(System.Byte[],System.Int32,System.Int32,System.Net.Sockets.SocketFlags,System.AsyncCallback,System.Object)
		//     method.
		//
		//   T:System.InvalidOperationException:
		//     System.Net.Sockets.Socket.EndReceive(System.IAsyncResult) was previously called
		//     for the asynchronous read.
		//
		//   T:System.Net.Sockets.SocketException:
		//     An error occurred when attempting to access the socket. See the Remarks section
		//     for more information.
		//
		//   T:System.ObjectDisposedException:
		//     The System.Net.Sockets.Socket has been closed.
		int EndReceive(IAsyncResult asyncResult, out SocketError errorCode);
		//
		// Summary:
		//     Ends a pending asynchronous read from a specific endpoint.
		//
		// Parameters:
		//   asyncResult:
		//     An System.IAsyncResult that stores state information and any user defined data
		//     for this asynchronous operation.
		//
		//   endPoint:
		//     The source System.Net.EndPoint.
		//
		// Returns:
		//     If successful, the number of bytes received. If unsuccessful, returns 0.
		//
		// Exceptions:
		//   T:System.ArgumentNullException:
		//     asyncResult is null.
		//
		//   T:System.ArgumentException:
		//     asyncResult was not returned by a call to the System.Net.Sockets.Socket.BeginReceiveFrom(System.Byte[],System.Int32,System.Int32,System.Net.Sockets.SocketFlags,System.Net.EndPoint@,System.AsyncCallback,System.Object)
		//     method.
		//
		//   T:System.InvalidOperationException:
		//     System.Net.Sockets.Socket.EndReceiveFrom(System.IAsyncResult,System.Net.EndPoint@)
		//     was previously called for the asynchronous read.
		//
		//   T:System.Net.Sockets.SocketException:
		//     An error occurred when attempting to access the socket. See the Remarks section
		//     for more information.
		//
		//   T:System.ObjectDisposedException:
		//     The System.Net.Sockets.Socket has been closed.
		int EndReceiveFrom(IAsyncResult asyncResult, ref EndPoint endPoint);
		//
		// Summary:
		//     Ends a pending asynchronous read from a specific endpoint. This method also reveals
		//     more information about the packet than System.Net.Sockets.Socket.EndReceiveFrom(System.IAsyncResult,System.Net.EndPoint@).
		//
		// Parameters:
		//   asyncResult:
		//     An System.IAsyncResult that stores state information and any user defined data
		//     for this asynchronous operation.
		//
		//   socketFlags:
		//     A bitwise combination of the System.Net.Sockets.SocketFlags values for the received
		//     packet.
		//
		//   endPoint:
		//     The source System.Net.EndPoint.
		//
		//   ipPacketInformation:
		//     The System.Net.IPAddress and interface of the received packet.
		//
		// Returns:
		//     If successful, the number of bytes received. If unsuccessful, returns 0.
		//
		// Exceptions:
		//   T:System.ArgumentNullException:
		//     asyncResult is null-or- endPoint is null.
		//
		//   T:System.ArgumentException:
		//     asyncResult was not returned by a call to the System.Net.Sockets.Socket.BeginReceiveMessageFrom(System.Byte[],System.Int32,System.Int32,System.Net.Sockets.SocketFlags,System.Net.EndPoint@,System.AsyncCallback,System.Object)
		//     method.
		//
		//   T:System.InvalidOperationException:
		//     System.Net.Sockets.Socket.EndReceiveMessageFrom(System.IAsyncResult,System.Net.Sockets.SocketFlags@,System.Net.EndPoint@,System.Net.Sockets.IPPacketInformation@)
		//     was previously called for the asynchronous read.
		//
		//   T:System.Net.Sockets.SocketException:
		//     An error occurred when attempting to access the socket. See the Remarks section
		//     for more information.
		//
		//   T:System.ObjectDisposedException:
		//     The System.Net.Sockets.Socket has been closed.
		int EndReceiveMessageFrom(IAsyncResult asyncResult, ref SocketFlags socketFlags, ref EndPoint endPoint, out IPPacketInformation ipPacketInformation);
		//
		// Summary:
		//     Ends a pending asynchronous send.
		//
		// Parameters:
		//   asyncResult:
		//     An System.IAsyncResult that stores state information for this asynchronous operation.
		//
		// Returns:
		//     If successful, the number of bytes sent to the System.Net.Sockets.Socket; otherwise,
		//     an invalid System.Net.Sockets.Socket error.
		//
		// Exceptions:
		//   T:System.ArgumentNullException:
		//     asyncResult is null.
		//
		//   T:System.ArgumentException:
		//     asyncResult was not returned by a call to the System.Net.Sockets.Socket.BeginSend(System.Byte[],System.Int32,System.Int32,System.Net.Sockets.SocketFlags,System.AsyncCallback,System.Object)
		//     method.
		//
		//   T:System.InvalidOperationException:
		//     System.Net.Sockets.Socket.EndSend(System.IAsyncResult) was previously called
		//     for the asynchronous send.
		//
		//   T:System.Net.Sockets.SocketException:
		//     An error occurred when attempting to access the socket. See the Remarks section
		//     for more information.
		//
		//   T:System.ObjectDisposedException:
		//     The System.Net.Sockets.Socket has been closed.
		int EndSend(IAsyncResult asyncResult);
		//
		// Summary:
		//     Ends a pending asynchronous send.
		//
		// Parameters:
		//   asyncResult:
		//     An System.IAsyncResult that stores state information for this asynchronous operation.
		//
		//   errorCode:
		//     A System.Net.Sockets.SocketError object that stores the socket error.
		//
		// Returns:
		//     If successful, the number of bytes sent to the System.Net.Sockets.Socket; otherwise,
		//     an invalid System.Net.Sockets.Socket error.
		//
		// Exceptions:
		//   T:System.ArgumentNullException:
		//     asyncResult is null.
		//
		//   T:System.ArgumentException:
		//     asyncResult was not returned by a call to the System.Net.Sockets.Socket.BeginSend(System.Byte[],System.Int32,System.Int32,System.Net.Sockets.SocketFlags,System.AsyncCallback,System.Object)
		//     method.
		//
		//   T:System.InvalidOperationException:
		//     System.Net.Sockets.Socket.EndSend(System.IAsyncResult) was previously called
		//     for the asynchronous send.
		//
		//   T:System.Net.Sockets.SocketException:
		//     An error occurred when attempting to access the socket. See the Remarks section
		//     for more information.
		//
		//   T:System.ObjectDisposedException:
		//     The System.Net.Sockets.Socket has been closed.
		int EndSend(IAsyncResult asyncResult, out SocketError errorCode);
		//
		// Summary:
		//     Ends a pending asynchronous send of a file.
		//
		// Parameters:
		//   asyncResult:
		//     An System.IAsyncResult object that stores state information for this asynchronous
		//     operation.
		//
		// Exceptions:
		//   T:System.NotSupportedException:
		//     Windows NT is required for this method.
		//
		//   T:System.ObjectDisposedException:
		//     The System.Net.Sockets.Socket object has been closed.
		//
		//   T:System.ArgumentNullException:
		//     asyncResult is empty.
		//
		//   T:System.ArgumentException:
		//     asyncResult was not returned by a call to the System.Net.Sockets.Socket.BeginSendFile(System.String,System.AsyncCallback,System.Object)
		//     method.
		//
		//   T:System.InvalidOperationException:
		//     System.Net.Sockets.Socket.EndSendFile(System.IAsyncResult) was previously called
		//     for the asynchronous System.Net.Sockets.Socket.BeginSendFile(System.String,System.AsyncCallback,System.Object).
		//
		//   T:System.Net.Sockets.SocketException:
		//     An error occurred when attempting to access the socket. See remarks section below.
		void EndSendFile(IAsyncResult asyncResult);
		//
		// Summary:
		//     Ends a pending asynchronous send to a specific location.
		//
		// Parameters:
		//   asyncResult:
		//     An System.IAsyncResult that stores state information and any user defined data
		//     for this asynchronous operation.
		//
		// Returns:
		//     If successful, the number of bytes sent; otherwise, an invalid System.Net.Sockets.Socket
		//     error.
		//
		// Exceptions:
		//   T:System.ArgumentNullException:
		//     asyncResult is null.
		//
		//   T:System.ArgumentException:
		//     asyncResult was not returned by a call to the System.Net.Sockets.Socket.BeginSendTo(System.Byte[],System.Int32,System.Int32,System.Net.Sockets.SocketFlags,System.Net.EndPoint,System.AsyncCallback,System.Object)
		//     method.
		//
		//   T:System.InvalidOperationException:
		//     System.Net.Sockets.Socket.EndSendTo(System.IAsyncResult) was previously called
		//     for the asynchronous send.
		//
		//   T:System.Net.Sockets.SocketException:
		//     An error occurred when attempting to access the socket. See the Remarks section
		//     for more information.
		//
		//   T:System.ObjectDisposedException:
		//     The System.Net.Sockets.Socket has been closed.
		int EndSendTo(IAsyncResult asyncResult);
		//
		// Summary:
		//     Returns the value of a specified System.Net.Sockets.Socket option, represented
		//     as an object.
		//
		// Parameters:
		//   optionLevel:
		//     One of the System.Net.Sockets.SocketOptionLevel values.
		//
		//   optionName:
		//     One of the System.Net.Sockets.SocketOptionName values.
		//
		// Returns:
		//     An object that represents the value of the option. When the optionName parameter
		//     is set to System.Net.Sockets.SocketOptionName.Linger the return value is an instance
		//     of the System.Net.Sockets.LingerOption class. When optionName is set to System.Net.Sockets.SocketOptionName.AddMembership
		//     or System.Net.Sockets.SocketOptionName.DropMembership, the return value is an
		//     instance of the System.Net.Sockets.MulticastOption class. When optionName is
		//     any other value, the return value is an integer.
		//
		// Exceptions:
		//   T:System.Net.Sockets.SocketException:
		//     An error occurred when attempting to access the socket. See the Remarks section
		//     for more information.-or-optionName was set to the unsupported value System.Net.Sockets.SocketOptionName.MaxConnections.
		//
		//   T:System.ObjectDisposedException:
		//     The System.Net.Sockets.Socket has been closed.
		object GetSocketOption(SocketOptionLevel optionLevel, SocketOptionName optionName);
		//
		// Summary:
		//     Returns the value of the specified System.Net.Sockets.Socket option in an array.
		//
		// Parameters:
		//   optionLevel:
		//     One of the System.Net.Sockets.SocketOptionLevel values.
		//
		//   optionName:
		//     One of the System.Net.Sockets.SocketOptionName values.
		//
		//   optionLength:
		//     The length, in bytes, of the expected return value.
		//
		// Returns:
		//     An array of type System.Byte that contains the value of the socket option.
		//
		// Exceptions:
		//   T:System.Net.Sockets.SocketException:
		//     An error occurred when attempting to access the socket. See the Remarks section
		//     for more information. - or -In .NET Compact Framework applications, the Windows
		//     CE default buffer space is set to 32768 bytes. You can change the per socket
		//     buffer space by calling Overload:System.Net.Sockets.Socket.SetSocketOption.
		//
		//   T:System.ObjectDisposedException:
		//     The System.Net.Sockets.Socket has been closed.
		byte[] GetSocketOption(SocketOptionLevel optionLevel, SocketOptionName optionName, int optionLength);
		//
		// Summary:
		//     Returns the specified System.Net.Sockets.Socket option setting, represented as
		//     a byte array.
		//
		// Parameters:
		//   optionLevel:
		//     One of the System.Net.Sockets.SocketOptionLevel values.
		//
		//   optionName:
		//     One of the System.Net.Sockets.SocketOptionName values.
		//
		//   optionValue:
		//     An array of type System.Byte that is to receive the option setting.
		//
		// Exceptions:
		//   T:System.Net.Sockets.SocketException:
		//     An error occurred when attempting to access the socket. See the Remarks section
		//     for more information. - or -In .NET Compact Framework applications, the Windows
		//     CE default buffer space is set to 32768 bytes. You can change the per socket
		//     buffer space by calling Overload:System.Net.Sockets.Socket.SetSocketOption.
		//
		//   T:System.ObjectDisposedException:
		//     The System.Net.Sockets.Socket has been closed.
		void GetSocketOption(SocketOptionLevel optionLevel, SocketOptionName optionName, byte[] optionValue);
		//
		// Summary:
		//     Sets low-level operating modes for the System.Net.Sockets.Socket using numerical
		//     control codes.
		//
		// Parameters:
		//   ioControlCode:
		//     An System.Int32 value that specifies the control code of the operation to perform.
		//
		//   optionInValue:
		//     A System.Byte array that contains the input data required by the operation.
		//
		//   optionOutValue:
		//     A System.Byte array that contains the output data returned by the operation.
		//
		// Returns:
		//     The number of bytes in the optionOutValue parameter.
		//
		// Exceptions:
		//   T:System.Net.Sockets.SocketException:
		//     An error occurred when attempting to access the socket. See the Remarks section
		//     for more information.
		//
		//   T:System.ObjectDisposedException:
		//     The System.Net.Sockets.Socket has been closed.
		//
		//   T:System.InvalidOperationException:
		//     An attempt was made to change the blocking mode without using the System.Net.Sockets.Socket.Blocking
		//     property.
		//
		//   T:System.Security.SecurityException:
		//     A caller in the call stack does not have the required permissions.
		int IOControl(int ioControlCode, byte[] optionInValue, byte[] optionOutValue);
		//
		// Summary:
		//     Sets low-level operating modes for the System.Net.Sockets.Socket using the System.Net.Sockets.IOControlCode
		//     enumeration to specify control codes.
		//
		// Parameters:
		//   ioControlCode:
		//     A System.Net.Sockets.IOControlCode value that specifies the control code of the
		//     operation to perform.
		//
		//   optionInValue:
		//     An array of type System.Byte that contains the input data required by the operation.
		//
		//   optionOutValue:
		//     An array of type System.Byte that contains the output data returned by the operation.
		//
		// Returns:
		//     The number of bytes in the optionOutValue parameter.
		//
		// Exceptions:
		//   T:System.Net.Sockets.SocketException:
		//     An error occurred when attempting to access the socket. See the Remarks section
		//     for more information.
		//
		//   T:System.ObjectDisposedException:
		//     The System.Net.Sockets.Socket has been closed.
		//
		//   T:System.InvalidOperationException:
		//     An attempt was made to change the blocking mode without using the System.Net.Sockets.Socket.Blocking
		//     property.
		int IOControl(IOControlCode ioControlCode, byte[] optionInValue, byte[] optionOutValue);
		//
		// Summary:
		//     Places a System.Net.Sockets.Socket in a listening state.
		//
		// Parameters:
		//   backlog:
		//     The maximum length of the pending connections queue.
		//
		// Exceptions:
		//   T:System.Net.Sockets.SocketException:
		//     An error occurred when attempting to access the socket. See the Remarks section
		//     for more information.
		//
		//   T:System.ObjectDisposedException:
		//     The System.Net.Sockets.Socket has been closed.
		void Listen(int backlog);
		//
		// Summary:
		//     Determines the status of the System.Net.Sockets.Socket.
		//
		// Parameters:
		//   microSeconds:
		//     The time to wait for a response, in microseconds.
		//
		//   mode:
		//     One of the System.Net.Sockets.SelectMode values.
		//
		// Returns:
		//     The status of the System.Net.Sockets.Socket based on the polling mode value passed
		//     in the mode parameter.Mode Return Value System.Net.Sockets.SelectMode.SelectReadtrue
		//     if System.Net.Sockets.Socket.Listen(System.Int32) has been called and a connection
		//     is pending; -or- true if data is available for reading; -or- true if the connection
		//     has been closed, reset, or terminated; otherwise, returns false. System.Net.Sockets.SelectMode.SelectWritetrue,
		//     if processing a System.Net.Sockets.Socket.Connect(System.Net.EndPoint), and the
		//     connection has succeeded; -or- true if data can be sent; otherwise, returns false.
		//     System.Net.Sockets.SelectMode.SelectErrortrue if processing a System.Net.Sockets.Socket.Connect(System.Net.EndPoint)
		//     that does not block, and the connection has failed; -or- true if System.Net.Sockets.SocketOptionName.OutOfBandInline
		//     is not set and out-of-band data is available; otherwise, returns false.
		//
		// Exceptions:
		//   T:System.NotSupportedException:
		//     The mode parameter is not one of the System.Net.Sockets.SelectMode values.
		//
		//   T:System.Net.Sockets.SocketException:
		//     An error occurred when attempting to access the socket. See remarks below.
		//
		//   T:System.ObjectDisposedException:
		//     The System.Net.Sockets.Socket has been closed.
		bool Poll(int microSeconds, SelectMode mode);
		//
		// Summary:
		//     Receives data from a bound System.Net.Sockets.Socket into a receive buffer.
		//
		// Parameters:
		//   buffer:
		//     An array of type System.Byte that is the storage location for the received data.
		//
		// Returns:
		//     The number of bytes received.
		//
		// Exceptions:
		//   T:System.ArgumentNullException:
		//     buffer is null.
		//
		//   T:System.Net.Sockets.SocketException:
		//     An error occurred when attempting to access the socket. See the Remarks section
		//     for more information.
		//
		//   T:System.ObjectDisposedException:
		//     The System.Net.Sockets.Socket has been closed.
		//
		//   T:System.Security.SecurityException:
		//     A caller in the call stack does not have the required permissions.
		int Receive(byte[] buffer);
		//
		// Summary:
		//     Receives data from a bound System.Net.Sockets.Socket into the list of receive
		//     buffers.
		//
		// Parameters:
		//   buffers:
		//     A list of System.ArraySegment`1s of type System.Byte that contains the received
		//     data.
		//
		// Returns:
		//     The number of bytes received.
		//
		// Exceptions:
		//   T:System.ArgumentNullException:
		//     The buffer parameter is null.
		//
		//   T:System.Net.Sockets.SocketException:
		//     An error occurred while attempting to access the socket. See the Remarks section
		//     for more information.
		//
		//   T:System.ObjectDisposedException:
		//     The System.Net.Sockets.Socket has been closed.
		int Receive(IList<ArraySegment<byte>> buffers);
		//
		// Summary:
		//     Receives data from a bound System.Net.Sockets.Socket into the list of receive
		//     buffers, using the specified System.Net.Sockets.SocketFlags.
		//
		// Parameters:
		//   buffers:
		//     A list of System.ArraySegment`1s of type System.Byte that contains the received
		//     data.
		//
		//   socketFlags:
		//     A bitwise combination of the System.Net.Sockets.SocketFlags values.
		//
		// Returns:
		//     The number of bytes received.
		//
		// Exceptions:
		//   T:System.ArgumentNullException:
		//     buffers is null.-or-buffers.Count is zero.
		//
		//   T:System.Net.Sockets.SocketException:
		//     An error occurred while attempting to access the socket. See the Remarks section
		//     for more information.
		//
		//   T:System.ObjectDisposedException:
		//     The System.Net.Sockets.Socket has been closed.
		int Receive(IList<ArraySegment<byte>> buffers, SocketFlags socketFlags);
		//
		// Summary:
		//     Receives data from a bound System.Net.Sockets.Socket into a receive buffer, using
		//     the specified System.Net.Sockets.SocketFlags.
		//
		// Parameters:
		//   buffer:
		//     An array of type System.Byte that is the storage location for the received data.
		//
		//   socketFlags:
		//     A bitwise combination of the System.Net.Sockets.SocketFlags values.
		//
		// Returns:
		//     The number of bytes received.
		//
		// Exceptions:
		//   T:System.ArgumentNullException:
		//     buffer is null.
		//
		//   T:System.Net.Sockets.SocketException:
		//     An error occurred when attempting to access the socket. See the Remarks section
		//     for more information.
		//
		//   T:System.ObjectDisposedException:
		//     The System.Net.Sockets.Socket has been closed.
		//
		//   T:System.Security.SecurityException:
		//     A caller in the call stack does not have the required permissions.
		int Receive(byte[] buffer, SocketFlags socketFlags);
		//
		// Summary:
		//     Receives data from a bound System.Net.Sockets.Socket into the list of receive
		//     buffers, using the specified System.Net.Sockets.SocketFlags.
		//
		// Parameters:
		//   buffers:
		//     A list of System.ArraySegment`1s of type System.Byte that contains the received
		//     data.
		//
		//   socketFlags:
		//     A bitwise combination of the System.Net.Sockets.SocketFlags values.
		//
		//   errorCode:
		//     A System.Net.Sockets.SocketError object that stores the socket error.
		//
		// Returns:
		//     The number of bytes received.
		//
		// Exceptions:
		//   T:System.ArgumentNullException:
		//     buffers is null.-or-buffers.Count is zero.
		//
		//   T:System.Net.Sockets.SocketException:
		//     An error occurred while attempting to access the socket. See the Remarks section
		//     for more information.
		//
		//   T:System.ObjectDisposedException:
		//     The System.Net.Sockets.Socket has been closed.
		int Receive(IList<ArraySegment<byte>> buffers, SocketFlags socketFlags, out SocketError errorCode);
		//
		// Summary:
		//     Receives the specified number of bytes of data from a bound System.Net.Sockets.Socket
		//     into a receive buffer, using the specified System.Net.Sockets.SocketFlags.
		//
		// Parameters:
		//   buffer:
		//     An array of type System.Byte that is the storage location for the received data.
		//
		//   size:
		//     The number of bytes to receive.
		//
		//   socketFlags:
		//     A bitwise combination of the System.Net.Sockets.SocketFlags values.
		//
		// Returns:
		//     The number of bytes received.
		//
		// Exceptions:
		//   T:System.ArgumentNullException:
		//     buffer is null.
		//
		//   T:System.ArgumentOutOfRangeException:
		//     size exceeds the size of buffer.
		//
		//   T:System.Net.Sockets.SocketException:
		//     An error occurred when attempting to access the socket. See the Remarks section
		//     for more information.
		//
		//   T:System.ObjectDisposedException:
		//     The System.Net.Sockets.Socket has been closed.
		//
		//   T:System.Security.SecurityException:
		//     A caller in the call stack does not have the required permissions.
		int Receive(byte[] buffer, int size, SocketFlags socketFlags);
		//
		// Summary:
		//     Receives the specified number of bytes from a bound System.Net.Sockets.Socket
		//     into the specified offset position of the receive buffer, using the specified
		//     System.Net.Sockets.SocketFlags.
		//
		// Parameters:
		//   buffer:
		//     An array of type System.Byte that is the storage location for received data.
		//
		//   offset:
		//     The location in buffer to store the received data.
		//
		//   size:
		//     The number of bytes to receive.
		//
		//   socketFlags:
		//     A bitwise combination of the System.Net.Sockets.SocketFlags values.
		//
		// Returns:
		//     The number of bytes received.
		//
		// Exceptions:
		//   T:System.ArgumentNullException:
		//     buffer is null.
		//
		//   T:System.ArgumentOutOfRangeException:
		//     offset is less than 0.-or- offset is greater than the length of buffer.-or- size
		//     is less than 0.-or- size is greater than the length of buffer minus the value
		//     of the offset parameter.
		//
		//   T:System.Net.Sockets.SocketException:
		//     socketFlags is not a valid combination of values.-or- The System.Net.Sockets.Socket.LocalEndPoint
		//     property was not set.-or- An operating system error occurs while accessing the
		//     System.Net.Sockets.Socket.
		//
		//   T:System.ObjectDisposedException:
		//     The System.Net.Sockets.Socket has been closed.
		//
		//   T:System.Security.SecurityException:
		//     A caller in the call stack does not have the required permissions.
		int Receive(byte[] buffer, int offset, int size, SocketFlags socketFlags);
		//
		// Summary:
		//     Receives data from a bound System.Net.Sockets.Socket into a receive buffer, using
		//     the specified System.Net.Sockets.SocketFlags.
		//
		// Parameters:
		//   buffer:
		//     An array of type System.Byte that is the storage location for the received data.
		//
		//   offset:
		//     The position in the buffer parameter to store the received data.
		//
		//   size:
		//     The number of bytes to receive.
		//
		//   socketFlags:
		//     A bitwise combination of the System.Net.Sockets.SocketFlags values.
		//
		//   errorCode:
		//     A System.Net.Sockets.SocketError object that stores the socket error.
		//
		// Returns:
		//     The number of bytes received.
		//
		// Exceptions:
		//   T:System.ArgumentNullException:
		//     buffer is null.
		//
		//   T:System.ArgumentOutOfRangeException:
		//     offset is less than 0.-or- offset is greater than the length of buffer.-or- size
		//     is less than 0.-or- size is greater than the length of buffer minus the value
		//     of the offset parameter.
		//
		//   T:System.Net.Sockets.SocketException:
		//     socketFlags is not a valid combination of values.-or- The System.Net.Sockets.Socket.LocalEndPoint
		//     property is not set.-or- An operating system error occurs while accessing the
		//     System.Net.Sockets.Socket.
		//
		//   T:System.ObjectDisposedException:
		//     The System.Net.Sockets.Socket has been closed.
		//
		//   T:System.Security.SecurityException:
		//     A caller in the call stack does not have the required permissions.
		int Receive(byte[] buffer, int offset, int size, SocketFlags socketFlags, out SocketError errorCode);
		//
		// Summary:
		//     Begins an asynchronous request to receive data from a connected System.Net.Sockets.Socket
		//     object.
		//
		// Parameters:
		//   e:
		//     The System.Net.Sockets.SocketAsyncEventArgs object to use for this asynchronous
		//     socket operation.
		//
		// Returns:
		//     Returns true if the I/O operation is pending. The System.Net.Sockets.SocketAsyncEventArgs.Completed
		//     event on the e parameter will be raised upon completion of the operation. Returns
		//     false if the I/O operation completed synchronously. In this case, The System.Net.Sockets.SocketAsyncEventArgs.Completed
		//     event on the e parameter will not be raised and the e object passed as a parameter
		//     may be examined immediately after the method call returns to retrieve the result
		//     of the operation.
		//
		// Exceptions:
		//   T:System.ArgumentException:
		//     An argument was invalid. The System.Net.Sockets.SocketAsyncEventArgs.Buffer or
		//     System.Net.Sockets.SocketAsyncEventArgs.BufferList properties on the e parameter
		//     must reference valid buffers. One or the other of these properties may be set,
		//     but not both at the same time.
		//
		//   T:System.InvalidOperationException:
		//     A socket operation was already in progress using the System.Net.Sockets.SocketAsyncEventArgs
		//     object specified in the e parameter.
		//
		//   T:System.NotSupportedException:
		//     Windows XP or later is required for this method.
		//
		//   T:System.ObjectDisposedException:
		//     The System.Net.Sockets.Socket has been closed.
		//
		//   T:System.Net.Sockets.SocketException:
		//     An error occurred when attempting to access the socket. See the Remarks section
		//     for more information.
		bool ReceiveAsync(SocketAsyncEventArgs e);
		//
		// Summary:
		//     Receives a datagram into the data buffer and stores the endpoint.
		//
		// Parameters:
		//   buffer:
		//     An array of type System.Byte that is the storage location for received data.
		//
		//   remoteEP:
		//     An System.Net.EndPoint, passed by reference, that represents the remote server.
		//
		// Returns:
		//     The number of bytes received.
		//
		// Exceptions:
		//   T:System.ArgumentNullException:
		//     buffer is null.-or- remoteEP is null.
		//
		//   T:System.Net.Sockets.SocketException:
		//     An error occurred when attempting to access the socket. See the Remarks section
		//     for more information.
		//
		//   T:System.ObjectDisposedException:
		//     The System.Net.Sockets.Socket has been closed.
		//
		//   T:System.Security.SecurityException:
		//     A caller in the call stack does not have the required permissions.
		int ReceiveFrom(byte[] buffer, ref EndPoint remoteEP);
		//
		// Summary:
		//     Receives a datagram into the data buffer, using the specified System.Net.Sockets.SocketFlags,
		//     and stores the endpoint.
		//
		// Parameters:
		//   buffer:
		//     An array of type System.Byte that is the storage location for the received data.
		//
		//   socketFlags:
		//     A bitwise combination of the System.Net.Sockets.SocketFlags values.
		//
		//   remoteEP:
		//     An System.Net.EndPoint, passed by reference, that represents the remote server.
		//
		// Returns:
		//     The number of bytes received.
		//
		// Exceptions:
		//   T:System.ArgumentNullException:
		//     buffer is null.-or- remoteEP is null.
		//
		//   T:System.Net.Sockets.SocketException:
		//     An error occurred when attempting to access the socket. See the Remarks section
		//     for more information.
		//
		//   T:System.ObjectDisposedException:
		//     The System.Net.Sockets.Socket has been closed.
		//
		//   T:System.Security.SecurityException:
		//     A caller in the call stack does not have the required permissions.
		int ReceiveFrom(byte[] buffer, SocketFlags socketFlags, ref EndPoint remoteEP);
		//
		// Summary:
		//     Receives the specified number of bytes into the data buffer, using the specified
		//     System.Net.Sockets.SocketFlags, and stores the endpoint.
		//
		// Parameters:
		//   buffer:
		//     An array of type System.Byte that is the storage location for received data.
		//
		//   size:
		//     The number of bytes to receive.
		//
		//   socketFlags:
		//     A bitwise combination of the System.Net.Sockets.SocketFlags values.
		//
		//   remoteEP:
		//     An System.Net.EndPoint, passed by reference, that represents the remote server.
		//
		// Returns:
		//     The number of bytes received.
		//
		// Exceptions:
		//   T:System.ArgumentNullException:
		//     buffer is null.-or- remoteEP is null.
		//
		//   T:System.ArgumentOutOfRangeException:
		//     size is less than 0.-or- size is greater than the length of buffer.
		//
		//   T:System.Net.Sockets.SocketException:
		//     socketFlags is not a valid combination of values.-or- The System.Net.Sockets.Socket.LocalEndPoint
		//     property was not set.-or- An operating system error occurs while accessing the
		//     System.Net.Sockets.Socket.
		//
		//   T:System.ObjectDisposedException:
		//     The System.Net.Sockets.Socket has been closed.
		//
		//   T:System.Security.SecurityException:
		//     A caller in the call stack does not have the required permissions.
		int ReceiveFrom(byte[] buffer, int size, SocketFlags socketFlags, ref EndPoint remoteEP);
		//
		// Summary:
		//     Receives the specified number of bytes of data into the specified location of
		//     the data buffer, using the specified System.Net.Sockets.SocketFlags, and stores
		//     the endpoint.
		//
		// Parameters:
		//   buffer:
		//     An array of type System.Byte that is the storage location for received data.
		//
		//   offset:
		//     The position in the buffer parameter to store the received data.
		//
		//   size:
		//     The number of bytes to receive.
		//
		//   socketFlags:
		//     A bitwise combination of the System.Net.Sockets.SocketFlags values.
		//
		//   remoteEP:
		//     An System.Net.EndPoint, passed by reference, that represents the remote server.
		//
		// Returns:
		//     The number of bytes received.
		//
		// Exceptions:
		//   T:System.ArgumentNullException:
		//     buffer is null.-or- remoteEP is null.
		//
		//   T:System.ArgumentOutOfRangeException:
		//     offset is less than 0.-or- offset is greater than the length of buffer.-or- size
		//     is less than 0.-or- size is greater than the length of the buffer minus the value
		//     of the offset parameter.
		//
		//   T:System.Net.Sockets.SocketException:
		//     socketFlags is not a valid combination of values.-or- The System.Net.Sockets.Socket.LocalEndPoint
		//     property was not set.-or- An error occurred when attempting to access the socket.
		//     See the Remarks section for more information.
		//
		//   T:System.ObjectDisposedException:
		//     The System.Net.Sockets.Socket has been closed.
		int ReceiveFrom(byte[] buffer, int offset, int size, SocketFlags socketFlags, ref EndPoint remoteEP);
		//
		// Summary:
		//     Begins to asynchronously receive data from a specified network device.
		//
		// Parameters:
		//   e:
		//     The System.Net.Sockets.SocketAsyncEventArgs object to use for this asynchronous
		//     socket operation.
		//
		// Returns:
		//     Returns true if the I/O operation is pending. The System.Net.Sockets.SocketAsyncEventArgs.Completed
		//     event on the e parameter will be raised upon completion of the operation. Returns
		//     false if the I/O operation completed synchronously. In this case, The System.Net.Sockets.SocketAsyncEventArgs.Completed
		//     event on the e parameter will not be raised and the e object passed as a parameter
		//     may be examined immediately after the method call returns to retrieve the result
		//     of the operation.
		//
		// Exceptions:
		//   T:System.ArgumentNullException:
		//     The System.Net.Sockets.SocketAsyncEventArgs.RemoteEndPoint cannot be null.
		//
		//   T:System.InvalidOperationException:
		//     A socket operation was already in progress using the System.Net.Sockets.SocketAsyncEventArgs
		//     object specified in the e parameter.
		//
		//   T:System.NotSupportedException:
		//     Windows XP or later is required for this method.
		//
		//   T:System.ObjectDisposedException:
		//     The System.Net.Sockets.Socket has been closed.
		//
		//   T:System.Net.Sockets.SocketException:
		//     An error occurred when attempting to access the socket.
		bool ReceiveFromAsync(SocketAsyncEventArgs e);
		//
		// Summary:
		//     Receives the specified number of bytes of data into the specified location of
		//     the data buffer, using the specified System.Net.Sockets.SocketFlags, and stores
		//     the endpoint and packet information.
		//
		// Parameters:
		//   buffer:
		//     An array of type System.Byte that is the storage location for received data.
		//
		//   offset:
		//     The position in the buffer parameter to store the received data.
		//
		//   size:
		//     The number of bytes to receive.
		//
		//   socketFlags:
		//     A bitwise combination of the System.Net.Sockets.SocketFlags values.
		//
		//   remoteEP:
		//     An System.Net.EndPoint, passed by reference, that represents the remote server.
		//
		//   ipPacketInformation:
		//     An System.Net.Sockets.IPPacketInformation holding address and interface information.
		//
		// Returns:
		//     The number of bytes received.
		//
		// Exceptions:
		//   T:System.ArgumentNullException:
		//     buffer is null.- or- remoteEP is null.
		//
		//   T:System.ArgumentOutOfRangeException:
		//     offset is less than 0.-or- offset is greater than the length of buffer.-or- size
		//     is less than 0.-or- size is greater than the length of the buffer minus the value
		//     of the offset parameter.
		//
		//   T:System.Net.Sockets.SocketException:
		//     socketFlags is not a valid combination of values.-or- The System.Net.Sockets.Socket.LocalEndPoint
		//     property was not set.-or- The .NET Framework is running on an AMD 64-bit processor.-or-
		//     An error occurred when attempting to access the socket. See the Remarks section
		//     for more information.
		//
		//   T:System.ObjectDisposedException:
		//     The System.Net.Sockets.Socket has been closed.
		//
		//   T:System.NotSupportedException:
		//     The operating system is Windows 2000 or earlier, and this method requires Windows
		//     XP.
		int ReceiveMessageFrom(byte[] buffer, int offset, int size, ref SocketFlags socketFlags, ref EndPoint remoteEP, out IPPacketInformation ipPacketInformation);
		//
		// Summary:
		//     Begins to asynchronously receive the specified number of bytes of data into the
		//     specified location in the data buffer, using the specified System.Net.Sockets.SocketAsyncEventArgs.SocketFlags,
		//     and stores the endpoint and packet information.
		//
		// Parameters:
		//   e:
		//     The System.Net.Sockets.SocketAsyncEventArgs object to use for this asynchronous
		//     socket operation.
		//
		// Returns:
		//     Returns true if the I/O operation is pending. The System.Net.Sockets.SocketAsyncEventArgs.Completed
		//     event on the e parameter will be raised upon completion of the operation. Returns
		//     false if the I/O operation completed synchronously. In this case, The System.Net.Sockets.SocketAsyncEventArgs.Completed
		//     event on the e parameter will not be raised and the e object passed as a parameter
		//     may be examined immediately after the method call returns to retrieve the result
		//     of the operation.
		//
		// Exceptions:
		//   T:System.ArgumentNullException:
		//     The System.Net.Sockets.SocketAsyncEventArgs.RemoteEndPoint cannot be null.
		//
		//   T:System.NotSupportedException:
		//     Windows XP or later is required for this method.
		//
		//   T:System.ObjectDisposedException:
		//     The System.Net.Sockets.Socket has been closed.
		//
		//   T:System.Net.Sockets.SocketException:
		//     An error occurred when attempting to access the socket.
		bool ReceiveMessageFromAsync(SocketAsyncEventArgs e);
		//
		// Summary:
		//     Sends data to a connected System.Net.Sockets.Socket.
		//
		// Parameters:
		//   buffer:
		//     An array of type System.Byte that contains the data to be sent.
		//
		// Returns:
		//     The number of bytes sent to the System.Net.Sockets.Socket.
		//
		// Exceptions:
		//   T:System.ArgumentNullException:
		//     buffer is null.
		//
		//   T:System.Net.Sockets.SocketException:
		//     An error occurred when attempting to access the socket. See the Remarks section
		//     for more information.
		//
		//   T:System.ObjectDisposedException:
		//     The System.Net.Sockets.Socket has been closed.
		int Send(byte[] buffer);
		//
		// Summary:
		//     Sends the set of buffers in the list to a connected System.Net.Sockets.Socket.
		//
		// Parameters:
		//   buffers:
		//     A list of System.ArraySegment`1s of type System.Byte that contains the data to
		//     be sent.
		//
		// Returns:
		//     The number of bytes sent to the System.Net.Sockets.Socket.
		//
		// Exceptions:
		//   T:System.ArgumentNullException:
		//     buffers is null.
		//
		//   T:System.ArgumentException:
		//     buffers is empty.
		//
		//   T:System.Net.Sockets.SocketException:
		//     An error occurred when attempting to access the socket. See remarks section below.
		//
		//   T:System.ObjectDisposedException:
		//     The System.Net.Sockets.Socket has been closed.
		int Send(IList<ArraySegment<byte>> buffers);

		//
		// Summary:
		//     Sends data to a connected System.Net.Sockets.Socket using the specified System.Net.Sockets.SocketFlags.
		//
		// Parameters:
		//   buffer:
		//     An array of type System.Byte that contains the data to be sent.
		//
		//   socketFlags:
		//     A bitwise combination of the System.Net.Sockets.SocketFlags values.
		//
		// Returns:
		//     The number of bytes sent to the System.Net.Sockets.Socket.
		//
		// Exceptions:
		//   T:System.ArgumentNullException:
		//     buffer is null.
		//
		//   T:System.Net.Sockets.SocketException:
		//     An error occurred when attempting to access the socket. See the Remarks section
		//     for more information.
		//
		//   T:System.ObjectDisposedException:
		//     The System.Net.Sockets.Socket has been closed.
		int Send(byte[] buffer, SocketFlags socketFlags);

		//
		// Summary:
		//     Sends the set of buffers in the list to a connected System.Net.Sockets.Socket,
		//     using the specified System.Net.Sockets.SocketFlags.
		//
		// Parameters:
		//   buffers:
		//     A list of System.ArraySegment`1s of type System.Byte that contains the data to
		//     be sent.
		//
		//   socketFlags:
		//     A bitwise combination of the System.Net.Sockets.SocketFlags values.
		//
		// Returns:
		//     The number of bytes sent to the System.Net.Sockets.Socket.
		//
		// Exceptions:
		//   T:System.ArgumentNullException:
		//     buffers is null.
		//
		//   T:System.ArgumentException:
		//     buffers is empty.
		//
		//   T:System.Net.Sockets.SocketException:
		//     An error occurred when attempting to access the socket. See the Remarks section
		//     for more information.
		//
		//   T:System.ObjectDisposedException:
		//     The System.Net.Sockets.Socket has been closed.
		int Send(IList<ArraySegment<byte>> buffers, SocketFlags socketFlags);

		//
		// Summary:
		//     Sends the specified number of bytes of data to a connected System.Net.Sockets.Socket,
		//     using the specified System.Net.Sockets.SocketFlags.
		//
		// Parameters:
		//   buffer:
		//     An array of type System.Byte that contains the data to be sent.
		//
		//   size:
		//     The number of bytes to send.
		//
		//   socketFlags:
		//     A bitwise combination of the System.Net.Sockets.SocketFlags values.
		//
		// Returns:
		//     The number of bytes sent to the System.Net.Sockets.Socket.
		//
		// Exceptions:
		//   T:System.ArgumentNullException:
		//     buffer is null.
		//
		//   T:System.ArgumentOutOfRangeException:
		//     size is less than 0 or exceeds the size of the buffer.
		//
		//   T:System.Net.Sockets.SocketException:
		//     socketFlags is not a valid combination of values.-or- An operating system error
		//     occurs while accessing the socket. See the Remarks section for more information.
		//
		//   T:System.ObjectDisposedException:
		//     The System.Net.Sockets.Socket has been closed.
		int Send(byte[] buffer, int size, SocketFlags socketFlags);

		//
		// Summary:
		//     Sends the set of buffers in the list to a connected System.Net.Sockets.Socket,
		//     using the specified System.Net.Sockets.SocketFlags.
		//
		// Parameters:
		//   buffers:
		//     A list of System.ArraySegment`1s of type System.Byte that contains the data to
		//     be sent.
		//
		//   socketFlags:
		//     A bitwise combination of the System.Net.Sockets.SocketFlags values.
		//
		//   errorCode:
		//     A System.Net.Sockets.SocketError object that stores the socket error.
		//
		// Returns:
		//     The number of bytes sent to the System.Net.Sockets.Socket.
		//
		// Exceptions:
		//   T:System.ArgumentNullException:
		//     buffers is null.
		//
		//   T:System.ArgumentException:
		//     buffers is empty.
		//
		//   T:System.Net.Sockets.SocketException:
		//     An error occurred when attempting to access the socket. See the Remarks section
		//     for more information.
		//
		//   T:System.ObjectDisposedException:
		//     The System.Net.Sockets.Socket has been closed.
		int Send(IList<ArraySegment<byte>> buffers, SocketFlags socketFlags, out SocketError errorCode);

		//
		// Summary:
		//     Sends the specified number of bytes of data to a connected System.Net.Sockets.Socket,
		//     starting at the specified offset, and using the specified System.Net.Sockets.SocketFlags.
		//
		// Parameters:
		//   buffer:
		//     An array of type System.Byte that contains the data to be sent.
		//
		//   offset:
		//     The position in the data buffer at which to begin sending data.
		//
		//   size:
		//     The number of bytes to send.
		//
		//   socketFlags:
		//     A bitwise combination of the System.Net.Sockets.SocketFlags values.
		//
		// Returns:
		//     The number of bytes sent to the System.Net.Sockets.Socket.
		//
		// Exceptions:
		//   T:System.ArgumentNullException:
		//     buffer is null.
		//
		//   T:System.ArgumentOutOfRangeException:
		//     offset is less than 0.-or- offset is greater than the length of buffer.-or- size
		//     is less than 0.-or- size is greater than the length of buffer minus the value
		//     of the offset parameter.
		//
		//   T:System.Net.Sockets.SocketException:
		//     socketFlags is not a valid combination of values.-or- An operating system error
		//     occurs while accessing the System.Net.Sockets.Socket. See the Remarks section
		//     for more information.
		//
		//   T:System.ObjectDisposedException:
		//     The System.Net.Sockets.Socket has been closed.
		int Send(byte[] buffer, int offset, int size, SocketFlags socketFlags);

		//
		// Summary:
		//     Sends the specified number of bytes of data to a connected System.Net.Sockets.Socket,
		//     starting at the specified offset, and using the specified System.Net.Sockets.SocketFlags
		//
		// Parameters:
		//   buffer:
		//     An array of type System.Byte that contains the data to be sent.
		//
		//   offset:
		//     The position in the data buffer at which to begin sending data.
		//
		//   size:
		//     The number of bytes to send.
		//
		//   socketFlags:
		//     A bitwise combination of the System.Net.Sockets.SocketFlags values.
		//
		//   errorCode:
		//     A System.Net.Sockets.SocketError object that stores the socket error.
		//
		// Returns:
		//     The number of bytes sent to the System.Net.Sockets.Socket.
		//
		// Exceptions:
		//   T:System.ArgumentNullException:
		//     buffer is null.
		//
		//   T:System.ArgumentOutOfRangeException:
		//     offset is less than 0.-or- offset is greater than the length of buffer.-or- size
		//     is less than 0.-or- size is greater than the length of buffer minus the value
		//     of the offset parameter.
		//
		//   T:System.Net.Sockets.SocketException:
		//     socketFlags is not a valid combination of values.-or- An operating system error
		//     occurs while accessing the System.Net.Sockets.Socket. See the Remarks section
		//     for more information.
		//
		//   T:System.ObjectDisposedException:
		//     The System.Net.Sockets.Socket has been closed.
		int Send(byte[] buffer, int offset, int size, SocketFlags socketFlags, out SocketError errorCode);

		//
		// Summary:
		//     Sends data asynchronously to a connected System.Net.Sockets.Socket object.
		//
		// Parameters:
		//   e:
		//     The System.Net.Sockets.SocketAsyncEventArgs object to use for this asynchronous
		//     socket operation.
		//
		// Returns:
		//     Returns true if the I/O operation is pending. The System.Net.Sockets.SocketAsyncEventArgs.Completed
		//     event on the e parameter will be raised upon completion of the operation. Returns
		//     false if the I/O operation completed synchronously. In this case, The System.Net.Sockets.SocketAsyncEventArgs.Completed
		//     event on the e parameter will not be raised and the e object passed as a parameter
		//     may be examined immediately after the method call returns to retrieve the result
		//     of the operation.
		//
		// Exceptions:
		//   T:System.ArgumentException:
		//     The System.Net.Sockets.SocketAsyncEventArgs.Buffer or System.Net.Sockets.SocketAsyncEventArgs.BufferList
		//     properties on the e parameter must reference valid buffers. One or the other
		//     of these properties may be set, but not both at the same time.
		//
		//   T:System.InvalidOperationException:
		//     A socket operation was already in progress using the System.Net.Sockets.SocketAsyncEventArgs
		//     object specified in the e parameter.
		//
		//   T:System.NotSupportedException:
		//     Windows XP or later is required for this method.
		//
		//   T:System.ObjectDisposedException:
		//     The System.Net.Sockets.Socket has been closed.
		//
		//   T:System.Net.Sockets.SocketException:
		//     The System.Net.Sockets.Socket is not yet connected or was not obtained via an
		//     System.Net.Sockets.Socket.Accept, System.Net.Sockets.Socket.AcceptAsync(System.Net.Sockets.SocketAsyncEventArgs),or
		//     Overload:System.Net.Sockets.Socket.BeginAccept, method.
		bool SendAsync(SocketAsyncEventArgs e);

		//
		// Summary:
		//     Sends the file fileName to a connected System.Net.Sockets.Socket object with
		//     the System.Net.Sockets.TransmitFileOptions.UseDefaultWorkerThread transmit flag.
		//
		// Parameters:
		//   fileName:
		//     A System.String that contains the path and name of the file to be sent. This
		//     parameter can be null.
		//
		// Exceptions:
		//   T:System.NotSupportedException:
		//     The socket is not connected to a remote host.
		//
		//   T:System.ObjectDisposedException:
		//     The System.Net.Sockets.Socket object has been closed.
		//
		//   T:System.InvalidOperationException:
		//     The System.Net.Sockets.Socket object is not in blocking mode and cannot accept
		//     this synchronous call.
		//
		//   T:System.IO.FileNotFoundException:
		//     The file fileName was not found.
		//
		//   T:System.Net.Sockets.SocketException:
		//     An error occurred when attempting to access the socket. See the Remarks section
		//     for more information.
		void SendFile(string fileName);

		//
		// Summary:
		//     Sends the file fileName and buffers of data to a connected System.Net.Sockets.Socket
		//     object using the specified System.Net.Sockets.TransmitFileOptions value.
		//
		// Parameters:
		//   fileName:
		//     A System.String that contains the path and name of the file to be sent. This
		//     parameter can be null.
		//
		//   preBuffer:
		//     A System.Byte array that contains data to be sent before the file is sent. This
		//     parameter can be null.
		//
		//   postBuffer:
		//     A System.Byte array that contains data to be sent after the file is sent. This
		//     parameter can be null.
		//
		//   flags:
		//     One or more of System.Net.Sockets.TransmitFileOptions values.
		//
		// Exceptions:
		//   T:System.NotSupportedException:
		//     The operating system is not Windows NT or later.- or - The socket is not connected
		//     to a remote host.
		//
		//   T:System.ObjectDisposedException:
		//     The System.Net.Sockets.Socket object has been closed.
		//
		//   T:System.InvalidOperationException:
		//     The System.Net.Sockets.Socket object is not in blocking mode and cannot accept
		//     this synchronous call.
		//
		//   T:System.IO.FileNotFoundException:
		//     The file fileName was not found.
		//
		//   T:System.Net.Sockets.SocketException:
		//     An error occurred when attempting to access the socket. See the Remarks section
		//     for more information.
		void SendFile(string fileName, byte[] preBuffer, byte[] postBuffer, TransmitFileOptions flags);

		//
		// Summary:
		//     Sends a collection of files or in memory data buffers asynchronously to a connected
		//     System.Net.Sockets.Socket object.
		//
		// Parameters:
		//   e:
		//     The System.Net.Sockets.SocketAsyncEventArgs object to use for this asynchronous
		//     socket operation.
		//
		// Returns:
		//     Returns true if the I/O operation is pending. The System.Net.Sockets.SocketAsyncEventArgs.Completed
		//     event on the e parameter will be raised upon completion of the operation. Returns
		//     false if the I/O operation completed synchronously. In this case, The System.Net.Sockets.SocketAsyncEventArgs.Completed
		//     event on the e parameter will not be raised and the e object passed as a parameter
		//     may be examined immediately after the method call returns to retrieve the result
		//     of the operation.
		//
		// Exceptions:
		//   T:System.IO.FileNotFoundException:
		//     The file specified in the System.Net.Sockets.SendPacketsElement.FilePath property
		//     was not found.
		//
		//   T:System.InvalidOperationException:
		//     A socket operation was already in progress using the System.Net.Sockets.SocketAsyncEventArgs
		//     object specified in the e parameter.
		//
		//   T:System.NotSupportedException:
		//     Windows XP or later is required for this method. This exception also occurs if
		//     the System.Net.Sockets.Socket is not connected to a remote host.
		//
		//   T:System.ObjectDisposedException:
		//     The System.Net.Sockets.Socket has been closed.
		//
		//   T:System.Net.Sockets.SocketException:
		//     A connectionless System.Net.Sockets.Socket is being used and the file being sent
		//     exceeds the maximum packet size of the underlying transport.
		bool SendPacketsAsync(SocketAsyncEventArgs e);

		//
		// Summary:
		//     Sends data to the specified endpoint.
		//
		// Parameters:
		//   buffer:
		//     An array of type System.Byte that contains the data to be sent.
		//
		//   remoteEP:
		//     The System.Net.EndPoint that represents the destination for the data.
		//
		// Returns:
		//     The number of bytes sent.
		//
		// Exceptions:
		//   T:System.ArgumentNullException:
		//     buffer is null.-or- remoteEP is null.
		//
		//   T:System.Net.Sockets.SocketException:
		//     An error occurred when attempting to access the socket. See the Remarks section
		//     for more information.
		//
		//   T:System.ObjectDisposedException:
		//     The System.Net.Sockets.Socket has been closed.
		int SendTo(byte[] buffer, EndPoint remoteEP);

		//
		// Summary:
		//     Sends data to a specific endpoint using the specified System.Net.Sockets.SocketFlags.
		//
		// Parameters:
		//   buffer:
		//     An array of type System.Byte that contains the data to be sent.
		//
		//   socketFlags:
		//     A bitwise combination of the System.Net.Sockets.SocketFlags values.
		//
		//   remoteEP:
		//     The System.Net.EndPoint that represents the destination location for the data.
		//
		// Returns:
		//     The number of bytes sent.
		//
		// Exceptions:
		//   T:System.ArgumentNullException:
		//     buffer is null.-or- remoteEP is null.
		//
		//   T:System.Net.Sockets.SocketException:
		//     An error occurred when attempting to access the socket. See the Remarks section
		//     for more information.
		//
		//   T:System.ObjectDisposedException:
		//     The System.Net.Sockets.Socket has been closed.
		int SendTo(byte[] buffer, SocketFlags socketFlags, EndPoint remoteEP);

		//
		// Summary:
		//     Sends the specified number of bytes of data to the specified endpoint using the
		//     specified System.Net.Sockets.SocketFlags.
		//
		// Parameters:
		//   buffer:
		//     An array of type System.Byte that contains the data to be sent.
		//
		//   size:
		//     The number of bytes to send.
		//
		//   socketFlags:
		//     A bitwise combination of the System.Net.Sockets.SocketFlags values.
		//
		//   remoteEP:
		//     The System.Net.EndPoint that represents the destination location for the data.
		//
		// Returns:
		//     The number of bytes sent.
		//
		// Exceptions:
		//   T:System.ArgumentNullException:
		//     buffer is null.-or- remoteEP is null.
		//
		//   T:System.ArgumentOutOfRangeException:
		//     The specified size exceeds the size of buffer.
		//
		//   T:System.Net.Sockets.SocketException:
		//     An error occurred when attempting to access the socket. See the Remarks section
		//     for more information.
		//
		//   T:System.ObjectDisposedException:
		//     The System.Net.Sockets.Socket has been closed.
		int SendTo(byte[] buffer, int size, SocketFlags socketFlags, EndPoint remoteEP);

		//
		// Summary:
		//     Sends the specified number of bytes of data to the specified endpoint, starting
		//     at the specified location in the buffer, and using the specified System.Net.Sockets.SocketFlags.
		//
		// Parameters:
		//   buffer:
		//     An array of type System.Byte that contains the data to be sent.
		//
		//   offset:
		//     The position in the data buffer at which to begin sending data.
		//
		//   size:
		//     The number of bytes to send.
		//
		//   socketFlags:
		//     A bitwise combination of the System.Net.Sockets.SocketFlags values.
		//
		//   remoteEP:
		//     The System.Net.EndPoint that represents the destination location for the data.
		//
		// Returns:
		//     The number of bytes sent.
		//
		// Exceptions:
		//   T:System.ArgumentNullException:
		//     buffer is null.-or- remoteEP is null.
		//
		//   T:System.ArgumentOutOfRangeException:
		//     offset is less than 0.-or- offset is greater than the length of buffer.-or- size
		//     is less than 0.-or- size is greater than the length of buffer minus the value
		//     of the offset parameter.
		//
		//   T:System.Net.Sockets.SocketException:
		//     socketFlags is not a valid combination of values.-or- An operating system error
		//     occurs while accessing the System.Net.Sockets.Socket. See the Remarks section
		//     for more information.
		//
		//   T:System.ObjectDisposedException:
		//     The System.Net.Sockets.Socket has been closed.
		//
		//   T:System.Security.SecurityException:
		//     A caller in the call stack does not have the required permissions.
		int SendTo(byte[] buffer, int offset, int size, SocketFlags socketFlags, EndPoint remoteEP);

		//
		// Summary:
		//     Sends data asynchronously to a specific remote host.
		//
		// Parameters:
		//   e:
		//     The System.Net.Sockets.SocketAsyncEventArgs object to use for this asynchronous
		//     socket operation.
		//
		// Returns:
		//     Returns true if the I/O operation is pending. The System.Net.Sockets.SocketAsyncEventArgs.Completed
		//     event on the e parameter will be raised upon completion of the operation. Returns
		//     false if the I/O operation completed synchronously. In this case, The System.Net.Sockets.SocketAsyncEventArgs.Completed
		//     event on the e parameter will not be raised and the e object passed as a parameter
		//     may be examined immediately after the method call returns to retrieve the result
		//     of the operation.
		//
		// Exceptions:
		//   T:System.ArgumentNullException:
		//     The System.Net.Sockets.SocketAsyncEventArgs.RemoteEndPoint cannot be null.
		//
		//   T:System.InvalidOperationException:
		//     A socket operation was already in progress using the System.Net.Sockets.SocketAsyncEventArgs
		//     object specified in the e parameter.
		//
		//   T:System.NotSupportedException:
		//     Windows XP or later is required for this method.
		//
		//   T:System.ObjectDisposedException:
		//     The System.Net.Sockets.Socket has been closed.
		//
		//   T:System.Net.Sockets.SocketException:
		//     The protocol specified is connection-oriented, but the System.Net.Sockets.Socket
		//     is not yet connected.
		bool SendToAsync(SocketAsyncEventArgs e);

		//
		// Summary:
		//     Set the IP protection level on a socket.
		//
		// Parameters:
		//   level:
		//     The IP protection level to set on this socket.
		//
		// Exceptions:
		//   T:System.ArgumentException:
		//     The level parameter cannot be System.Net.Sockets.IPProtectionLevel.Unspecified.
		//     The IP protection level cannot be set to unspecified.
		//
		//   T:System.NotSupportedException:
		//     The System.Net.Sockets.AddressFamily of the socket must be either System.Net.Sockets.AddressFamily.InterNetworkV6
		//     or System.Net.Sockets.AddressFamily.InterNetwork.
		void SetIPProtectionLevel(IPProtectionLevel level);

		//
		// Summary:
		//     Sets the specified System.Net.Sockets.Socket option to the specified integer
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
		//     A value of the option.
		//

		/// <exception cref="System.Net.Sockets.SocketException">
		///     An error occurred when attempting to access the socket. See the Remarks section
		///     for more information.
		/// </exception>
		/// <exception cref="System.ObjectDisposedException">
		///     The System.Net.Sockets.Socket has been closed.
		/// </exception>
		void SetSocketOption(SocketOptionLevel optionLevel, SocketOptionName optionName, int optionValue);
		//
		// Summary:
		//     Sets the specified System.Net.Sockets.Socket option to the specified value, represented
		//     as a byte array.
		//
		// Parameters:
		//   optionLevel:
		//     One of the System.Net.Sockets.SocketOptionLevel values.
		//
		//   optionName:
		//     One of the System.Net.Sockets.SocketOptionName values.
		//
		//   optionValue:
		//     An array of type System.Byte that represents the value of the option.
		//

		/// <exception cref="System.Net.Sockets.SocketException">
		///     An error occurred when attempting to access the socket. See the Remarks section
		///     for more information.
		/// </exception>
		/// <exception cref="System.ObjectDisposedException">
		///     The System.Net.Sockets.Socket has been closed.
		/// </exception>
		void SetSocketOption(SocketOptionLevel optionLevel, SocketOptionName optionName, byte[] optionValue);
		//
		// Summary:
		//     Sets the specified System.Net.Sockets.Socket option to the specified value, represented
		//     as an object.
		//
		// Parameters:
		//   optionLevel:
		//     One of the System.Net.Sockets.SocketOptionLevel values.
		//
		//   optionName:
		//     One of the System.Net.Sockets.SocketOptionName values.
		//
		//   optionValue:
		//     A System.Net.Sockets.LingerOption or System.Net.Sockets.MulticastOption that
		//     contains the value of the option.
		//
		// Exceptions:
		//   T:System.ArgumentNullException:
		//     optionValue is null.
		//
		//   T:System.Net.Sockets.SocketException:
		//     An error occurred when attempting to access the socket. See the Remarks section
		//     for more information.
		void SetSocketOption(SocketOptionLevel optionLevel, SocketOptionName optionName, object optionValue);

		//
		// Summary:
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
		// Exceptions:
		/// <exception cref="System.Net.Sockets.SocketException">
		///     An error occurred when attempting to access the socket. See the Remarks section
		///     for more information.
		/// </exception>
		/// <exception cref="System.ObjectDisposedException">
		///     The System.Net.Sockets.Socket has been closed.
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
		///     The System.Net.Sockets.Socket has been closed.
		/// </exception>
		void Shutdown(SocketShutdown how);
	}
}