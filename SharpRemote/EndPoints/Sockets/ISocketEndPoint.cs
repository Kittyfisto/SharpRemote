using System;
using System.Net;

// ReSharper disable CheckNamespace
namespace SharpRemote
// ReSharper restore CheckNamespace
{
	/// <summary>
	///     The interface for an endpoint which can establish a connection to another
	///     <see cref="ISocketEndPoint" /> or to a <see cref="ISocketServer" />.
	/// </summary>
	/// <example>
	///     Have another <see cref="ISocketEndPoint" /> bound(<see cref="Bind(IPAddress)" />) to
	///     a particular address then then <see cref="Connect(IPEndPoint,TimeSpan)" /> to establish a
	///     connection with it.
	/// </example>
	/// <example>
	///     Have a <see cref="ISocketServer" /> bound(<see cref="ISocketServer.Bind(IPAddress)" />) to
	///     a particular address then then <see cref="Connect(IPEndPoint,TimeSpan)" /> to establish a
	///     connection with it.
	/// </example>
	public interface ISocketEndPoint
		: IRemotingEndPoint
	{
		/// <summary>
		///     IPAddress+Port pair of the connected endPoint in case <see cref="SocketEndPoint.Connect(IPEndPoint)" /> has been
		///     called.
		///     Otherwise null.
		/// </summary>
		new IPEndPoint RemoteEndPoint { get; }

		/// <summary>
		///     IPAddress+Port pair of this endPoint in case <see cref="Bind(IPAddress)" />
		///     or
		///     has been called.
		///     Otherwise null.
		/// </summary>
		new IPEndPoint LocalEndPoint { get; }

		#region Connect

		/// <summary>
		///     Tries to connect to another endPoint with the given name.
		/// </summary>
		/// <param name="endPointName"></param>
		/// <returns>True when a connection could be established, false otherwise</returns>
		bool TryConnect(string endPointName);

		/// <summary>
		///     Tries to connects to another endPoint with the given name.
		/// </summary>
		/// <param name="endPointName"></param>
		/// <param name="timeout"></param>
		/// <returns>True when the connection succeeded, false otherwise</returns>
		/// <exception cref="ArgumentNullException">When <paramref name="endPointName" /> is null</exception>
		/// <exception cref="ArgumentException">When <paramref name="endPointName" /> is empty</exception>
		/// <exception cref="ArgumentOutOfRangeException">
		///     When <paramref name="timeout" /> is less or equal to
		///     <see cref="TimeSpan.Zero" />
		/// </exception>
		/// <exception cref="InvalidOperationException">When no network service discoverer was specified when creating this client</exception>
		bool TryConnect(string endPointName, TimeSpan timeout);

		/// <summary>
		///     Tries to connects to another endPoint with the given name.
		/// </summary>
		/// <param name="endPoint"></param>
		/// <returns>True when the connection succeeded, false otherwise</returns>
		bool TryConnect(IPEndPoint endPoint);

		/// <summary>
		///     Tries to connect this endPoint to the given one.
		/// </summary>
		/// <param name="endPoint"></param>
		/// <param name="timeout">
		///     The amount of time this method should block and await a successful connection from the remote
		///     end-point
		/// </param>
		/// <exception cref="ArgumentNullException">
		///     When <paramref name="endPoint" /> is null
		/// </exception>
		/// <exception cref="ArgumentOutOfRangeException">
		///     When <paramref name="timeout" /> is equal or less than <see cref="TimeSpan.Zero" />
		/// </exception>
		/// <exception cref="InvalidOperationException">
		///     When this endPoint is already connected to another endPoint.
		/// </exception>
		bool TryConnect(IPEndPoint endPoint, TimeSpan timeout);

		/// <summary>
		///     Tries to connect this endPoint to the given one.
		/// </summary>
		/// <param name="endPoint"></param>
		/// <param name="timeout">
		///     The amount of time this method should block and await a successful connection from the remote
		///     end-point
		/// </param>
		/// <param name="connectionId"></param>
		/// <param name="exception"></param>
		/// <exception cref="ArgumentNullException">
		///     When <paramref name="endPoint" /> is null
		/// </exception>
		/// <exception cref="ArgumentOutOfRangeException">
		///     When <paramref name="timeout" /> is equal or less than <see cref="TimeSpan.Zero" />
		/// </exception>
		/// <exception cref="InvalidOperationException">
		///     When this endPoint is already connected to another endPoint.
		/// </exception>
		bool TryConnect(IPEndPoint endPoint,
		                TimeSpan timeout,
		                out Exception exception,
		                out ConnectionId connectionId);

		/// <summary>
		///     Connects to another endPoint with the given name.
		/// </summary>
		/// <param name="endPointName"></param>
		/// <param name="timeout"></param>
		/// <exception cref="ArgumentException">
		///     In case <paramref name="endPointName" /> is null
		/// </exception>
		/// <exception cref="ArgumentOutOfRangeException">
		///     When <paramref name="timeout" /> is equal or less than <see cref="TimeSpan.Zero" />
		/// </exception>
		/// <exception cref="InvalidOperationException">
		///     When this endPoint is already connected to another endPoint.
		/// </exception>
		/// <exception cref="NoSuchIPEndPointException">
		///     When no such endPoint could be *found* - it might exist but this one is
		///     incapable of establishing a successfuly connection
		/// </exception>
		/// <exception cref="AuthenticationException">
		///     - The given endPoint is no <see cref="EndPointType.Server" />
		///     - The given endPoint failed authentication
		/// </exception>
		ConnectionId Connect(string endPointName, TimeSpan timeout);

		/// <summary>
		///     Connects this endPoint to the given one.
		/// </summary>
		/// <param name="endPoint"></param>
		/// ///
		/// <exception cref="ArgumentNullException">
		///     When <paramref name="endPoint" /> is null
		/// </exception>
		/// <exception cref="InvalidOperationException">
		///     When this endPoint is already connected to another endPoint.
		/// </exception>
		/// <exception cref="NoSuchIPEndPointException">
		///     When no such endPoint could be *found* - it might exist but this one is
		///     incapable of establishing a successfuly connection
		/// </exception>
		/// <exception cref="AuthenticationException">
		///     - The given endPoint is no <see cref="EndPointType.Server" />
		///     - The given endPoint failed authentication
		/// </exception>
		ConnectionId Connect(IPEndPoint endPoint);

		/// <summary>
		///     Connects this endPoint to the given one.
		/// </summary>
		/// <param name="endPoint"></param>
		/// <param name="timeout">
		///     The amount of time this method should block and await a successful connection from the remote
		///     end-point
		/// </param>
		/// <exception cref="ArgumentNullException">
		///     When <paramref name="endPoint" /> is null
		/// </exception>
		/// <exception cref="ArgumentOutOfRangeException">
		///     When <paramref name="timeout" /> is equal or less than <see cref="TimeSpan.Zero" />
		/// </exception>
		/// <exception cref="InvalidOperationException">
		///     When this endPoint is already connected to another endPoint.
		/// </exception>
		/// <exception cref="NoSuchIPEndPointException">
		///     When no such endPoint could be *found* - it might exist but this one is
		///     incapable of establishing a successfuly connection
		/// </exception>
		/// <exception cref="AuthenticationException">
		///     - The given endPoint is no <see cref="EndPointType.Server" />
		///     - The given endPoint failed authentication
		/// </exception>
		/// <exception cref="AuthenticationRequiredException">
		///     - The given endPoint requires authentication, but this one didn't provide any
		/// </exception>
		/// <exception cref="HandshakeException">
		///     - The handshake between this and the given endpoint failed
		/// </exception>
		ConnectionId Connect(IPEndPoint endPoint, TimeSpan timeout);

		#endregion

		#region Bind

		/// <summary>
		///     Binds this socket to the given endpoint.
		/// </summary>
		/// <param name="ep"></param>
		void Bind(IPEndPoint ep);

		/// <summary>
		///     Binds this socket to the given address.
		/// </summary>
		/// <param name="localAddress"></param>
		void Bind(IPAddress localAddress);

		#endregion
	}
}