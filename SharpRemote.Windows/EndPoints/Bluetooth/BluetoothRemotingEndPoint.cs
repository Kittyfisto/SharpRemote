using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using InTheHand.Net;
using InTheHand.Net.Bluetooth;
using InTheHand.Net.Sockets;

// ReSharper disable CheckNamespace
namespace SharpRemote
// ReSharper restore CheckNamespace
{
	/// <summary>
	/// NOT FINISHED.
	/// </summary>
	internal sealed class BluetoothRemotingEndPoint
		: AbstractBinaryStreamEndPoint<IDisposable>
	{
		private BluetoothEndPoint _localEndPoint;

		/// <summary>
		/// NOT FINISHED
		/// </summary>
		public BluetoothRemotingEndPoint()
			: base(null, null, EndPointType.Client, null, null, null, null, null, null)
		{
			
		}

		/// <summary>
		/// NOT FINISHED
		/// </summary>
		/// <param name="serviceGuid"></param>
		public void Bind(Guid serviceGuid)
		{
			_localEndPoint = new BluetoothEndPoint(new BluetoothAddress(1234), serviceGuid);
		}

		/// <summary>
		/// NOT FINISHED
		/// </summary>
		/// <param name="timeout"></param>
		public void Connect(TimeSpan timeout)
		{
			var devices = DiscoverDevices(timeout);

			int n = 0;
		}

		/// <summary>
		/// NOT FINISHED
		/// </summary>
		/// <param name="timeout"></param>
		/// <returns></returns>
		public static BluetoothDeviceInfo[] DiscoverDevices(TimeSpan timeout)
		{
			BluetoothDeviceInfo[] devices;
			using (var @event = new ManualResetEvent(false))
			{
				devices = null;
				var comp = new BluetoothComponent();
				EventHandler<DiscoverDevicesEventArgs> progress = (unused, args) =>
					{
						devices = args.Devices;
					};
				EventHandler<DiscoverDevicesEventArgs> completed = (unused, args) =>
					{
						devices = args.Devices;
						@event.Set();
					};
				comp.DiscoverDevicesProgress += progress;
				comp.DiscoverDevicesComplete += completed;
				try
				{
					comp.DiscoverDevicesAsync(10, true, true, true, false, null);
					if (!@event.WaitOne(timeout))
						return new BluetoothDeviceInfo[0];
				}
				finally
				{
					comp.DiscoverDevicesProgress -= progress;
					comp.DiscoverDevicesComplete -= completed;
				}
			}
			return devices;
		}


		protected override EndPoint InternalLocalEndPoint
		{
			get { throw new NotImplementedException(); }
		}

		protected override EndPoint InternalRemoteEndPoint
		{
			get { throw new NotImplementedException(); }
			set { throw new NotImplementedException(); }
		}

		protected override void DisposeAdditional()
		{
			throw new NotImplementedException();
		}

		protected override void DisconnectTransport(IDisposable socket, bool reuseSocket)
		{
			throw new NotImplementedException();
		}

		protected override void DisposeAfterDisconnect(IDisposable socket)
		{
			throw new NotImplementedException();
		}

		protected override bool SendGoodbye(IDisposable socket, long waitTime, TimeSpan timeSpan)
		{
			throw new NotImplementedException();
		}

		protected override void Send(IDisposable socket, byte[] data, int offset, int size)
		{
			throw new NotImplementedException();
		}

		protected override EndPoint GetRemoteEndPointOf(IDisposable socket)
		{
			throw new NotImplementedException();
		}

		protected override bool SynchronizedWrite(IDisposable socket, byte[] data, int length, out SocketError err)
		{
			throw new NotImplementedException();
		}

		protected override bool SynchronizedRead(IDisposable socket, byte[] buffer, TimeSpan timeout, out SocketError err)
		{
			throw new NotImplementedException();
		}

		protected override bool SynchronizedRead(IDisposable socket, byte[] buffer, out SocketError err)
		{
			throw new NotImplementedException();
		}
	}
}