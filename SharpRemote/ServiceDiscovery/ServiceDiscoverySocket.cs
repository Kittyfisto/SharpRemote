using System;
using System.Collections.Generic;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Reflection;
using log4net;
using Exception = System.Exception;
using System.Diagnostics.Contracts;

namespace SharpRemote.ServiceDiscovery
{
	/// <summary>
	///     Service discovery socket implementation that is bound to a single address (and thus to a single network interface).
	/// </summary>
	internal sealed class ServiceDiscoverySocket
		: IServiceDiscoverySocket
			, IDisposable
	{
		private const int MaxLength = 508;
		private const string NoName = "";

		private static readonly ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

		private readonly NetworkInterface _iface;
		private readonly IPAddress _localAddress;
		private readonly IPEndPoint _localEndPoint;
		private readonly IPAddress _multicastAddress;
		private readonly IPEndPoint _multicastEndPoint;
		private readonly string _name;
		private readonly string _networkInterfaceId;
		private readonly bool _sendLegacyResponse;
		private readonly INetworkServiceRegisty _services;
		private readonly Socket _socket;

		private bool _isDisposed;

		public ServiceDiscoverySocket(NetworkInterface iface,
		                              IPAddress localAddress,
		                              IPAddress multicastAddress,
		                              int port,
		                              int ttl,
		                              INetworkServiceRegisty services,
		                              bool sendLegacyResponse)
		{
			_iface = iface;
			_name = iface.Name;
			_networkInterfaceId = iface.Id;
			_services = services;
			_localAddress = localAddress;
			_localEndPoint = new IPEndPoint(_localAddress, port);
			_multicastAddress = multicastAddress;
			_multicastEndPoint = new IPEndPoint(multicastAddress, port);
			_sendLegacyResponse = sendLegacyResponse;

			_socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp)
			{
				MulticastLoopback = true
			};
			_socket.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.MulticastTimeToLive, ttl);
			_socket.SetSocketOption(SocketOptionLevel.Socket,
			                        SocketOptionName.ReuseAddress, optionValue: true);
			_socket.Bind(new IPEndPoint(localAddress, port));

			var addressFamily = _localAddress.AddressFamily;
			if (addressFamily == AddressFamily.InterNetwork)
				try
				{
					_socket.SetSocketOption(SocketOptionLevel.IP,
					                        SocketOptionName.AddMembership,
					                        new MulticastOption(_multicastAddress,
					                                            _localAddress));

					Log.DebugFormat("Joined multicast group {0} for {1}@{2}",
					                _multicastAddress,
					                _localAddress,
					                iface.Name);
				}
				catch (SocketException e)
				{
					Log.DebugFormat("Unable to join multicast group {0} for {1}@{2}: {3}",
					                _multicastAddress,
					                _localAddress,
					                iface.Name,
					                e);
				}
			else
				throw new ArgumentOutOfRangeException(string.Format("Service discovery is not implemented for '{0}'",
				                                                    addressFamily));

			BeginReceive();
		}

		/// <summary>
		///     The name of the network interface this socket is bound to.
		/// </summary>
		public string InterfaceName => _name;

		/// <summary>
		///     The current status of the network interface this socket is bound to.
		/// </summary>
		public OperationalStatus InterfaceStatus
		{
			get
			{
				try
				{
					return _iface.OperationalStatus;
				}
				catch (Exception e)
				{
					Log.InfoFormat("Caught unexpected exception while querying status of interface '{0}': {1}",
					               _name,
					               e);
					return OperationalStatus.Unknown;
				}
			}
		}

		public void Dispose()
		{
			lock (_socket)
			{
				_socket.Dispose();
				_isDisposed = true;
			}
		}

		public void Query(string serviceName)
		{
			var query = Message.CreateQuery(serviceName ?? NoName);

			lock (_socket)
			{
				if (_isDisposed)
					return;

				_socket.SendTo(query, _multicastEndPoint);
			}
		}

		public event Action<Service> OnResponseReceived;

		private void BeginReceive()
		{
			lock (_socket)
			{
				if (_isDisposed)
					return;

				var buffer = new byte[MaxLength];
				EndPoint endPoint = _localEndPoint;
				_socket.BeginReceiveFrom(buffer, offset: 0, size: buffer.Length,
				                         socketFlags: SocketFlags.None,
				                         remoteEP: ref endPoint,
				                         callback: OnReceived, state: buffer);
			}
		}

		private void OnReceived(IAsyncResult ar)
		{
			try
			{
				EndPoint remoteEndPoint = _localEndPoint;
				int length;
				var buffer = (byte[]) ar.AsyncState;
				lock (_socket)
				{
					if (_isDisposed)
						return;

					length = _socket.EndReceiveFrom(ar, ref remoteEndPoint);
				}

				string token;
				string name;
				IPEndPoint serviceEndPoint;
				byte[] payload;
				if (Message.TryRead(buffer, out token, out name, out serviceEndPoint, out payload))
				{
					switch (token)
					{
						case Message.P2PQueryToken:
							SendResponse(name, remoteEndPoint);
							break;

						case Message.P2PResponse2Token:
						case Message.P2PResponseLegacyToken:
							EmitResponseReceived(name, serviceEndPoint, remoteEndPoint, payload);
							break;
					}
				}
				else
				{
					if (Log.IsDebugEnabled)
						Log.DebugFormat("Received invalid response ({0} bytes) from '{1}', ignoring it",
						                length,
						                remoteEndPoint);
				}
			}
			catch (SocketException e)
			{
				Log.ErrorFormat("Caught socket error: {0}", e);
			}
			catch (Exception e)
			{
				Log.ErrorFormat("Caught unexpected exception: {0}", e);
			}
			finally
			{
				BeginReceive();
			}
		}

		private void EmitResponseReceived(string name, IPEndPoint serviceEndPoint, EndPoint remoteEndPoint, byte[] payload)
		{
			if (ForwardResponse(serviceEndPoint, remoteEndPoint))
			{
				OnResponseReceived?.Invoke(new Service(name,
													   serviceEndPoint,
													   _localAddress,
													   _networkInterfaceId,
													   payload));
			}
			else
			{
				Log.DebugFormat("Ignoring local service '{0}' from '{1}', we can't possibly connect to it",
					serviceEndPoint,
					remoteEndPoint);
			}
		}

		[Pure]
		private static bool ForwardResponse(IPEndPoint serviceEndPoint, EndPoint remoteEndPoint)
		{
			// We do not want to forward services which are registered to loopback on REMOTE
			// machines (because we couldn't connect to them). Therefore we swallow those responses.
			// It is necessary to filter those on the receiving end because SharpRemote up until v0.5.91
			// used to send local services to remote requests (which is obviously bollocks).
			if (IsLoopback(serviceEndPoint) && !IsLoopback(remoteEndPoint))
				return false;

			return true;
		}

		private void SendResponse(string name, EndPoint remoteEndPoint)
		{
			var services = new List<RegisteredService>();
			lock (_socket)
			{
				services.AddRange(_services.GetServicesByName(name));
			}

			LogResponseAnswer(name, remoteEndPoint, services.Count);

			lock (_socket)
			{
				if (_isDisposed)
					return;

				foreach (var service in services)
				{
					var payload = service.Payload;
					var serviceEndPoint = service.EndPoint;
					if (IsAny(serviceEndPoint.Address))
						serviceEndPoint = new IPEndPoint(_localAddress, serviceEndPoint.Port);

					if (AnswerQuery(serviceEndPoint, remoteEndPoint))
					{
						if (Log.IsDebugEnabled)
							Log.DebugFormat("Answering query (name: {0}) from '{1}': {2}",
											name, remoteEndPoint, serviceEndPoint);

						SendResponse(service.Name, serviceEndPoint, payload);
					}
					else
					{
						if (Log.IsDebugEnabled)
							Log.DebugFormat("Not answering to remote query (name: {0}) from '{1}' with LOCAL service endpoint: {2}",
											name, remoteEndPoint, serviceEndPoint);
					}
				}
			}
		}

		private static bool AnswerQuery(IPEndPoint serviceEndPoint, EndPoint remoteEndPoint)
		{
			// We don't want to expose local service registrations to remote endpoints:
			// Therefore service's bound to loopback are only sent to requests originating from loopback...
			// See https://github.com/Kittyfisto/SharpRemote/issues/56
			if (IsLoopback(serviceEndPoint.Address) && !IsLoopback(remoteEndPoint))
				return false;

			return true;
		}

		private static void LogResponseAnswer(string name, EndPoint remoteEndPoint, int serviceCount)
		{
			if (Log.IsDebugEnabled)
			{
				if (serviceCount == 0)
					Log.DebugFormat("Received query (name: {0}) from '{1}', but no services are registered",
								name,
								remoteEndPoint);
				else
					Log.DebugFormat("Received query (name: {0}) from '{1}', answering with {2} service(s)",
								name,
								remoteEndPoint,
								serviceCount);
			}
		}

		[Pure]
		private static bool IsLoopback(EndPoint endPoint)
		{
			var ipEndPoint = endPoint as IPEndPoint;
			if (ipEndPoint == null)
				return false;

			return IsLoopback(ipEndPoint.Address);
		}

		[Pure]
		private static bool IsLoopback(IPAddress address)
		{
			return Equals(address, IPAddress.Loopback) || Equals(address, IPAddress.IPv6Loopback);
		}

		[Pure]
		private static bool IsAny(IPAddress address)
		{
			return Equals(address, IPAddress.Any) || Equals(address, IPAddress.IPv6Any);
		}

		private void SendResponse(string serviceName, IPEndPoint endPoint, byte[] payload)
		{
			var buffer = Message.CreateResponse2(serviceName, endPoint, payload);
			_socket.SendTo(buffer, _multicastEndPoint);

			if (_sendLegacyResponse
			) //< If the user requests to support super old SharpRemote versions, then we'll oblige and send responses understood by them
			{
				buffer = Message.CreateLegacyResponse(serviceName, endPoint);
				_socket.SendTo(buffer, _multicastEndPoint);
			}
		}
	}
}