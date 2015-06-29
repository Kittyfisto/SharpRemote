using System;
using System.IO;
using System.Threading;
using InTheHand.Net;
using InTheHand.Net.Bluetooth;
using InTheHand.Net.Sockets;

namespace SharpRemote.EndPoints
{
	public sealed class BluetoothRemotingEndPoint
		: AbstractEndPoint
		  , IRemotingEndPoint
		  , IEndPointChannel
	{
		private BluetoothEndPoint _localEndPoint;

		public BluetoothRemotingEndPoint()
		{
			
		}

		public void Bind(Guid serviceGuid)
		{
			_localEndPoint = new BluetoothEndPoint(new BluetoothAddress(1234), serviceGuid);
		}

		public MemoryStream CallRemoteMethod(ulong servantId, string methodName, MemoryStream arguments)
		{
			throw new NotImplementedException();
		}

		public void Connect(TimeSpan timeout)
		{
			var devices = DiscoverDevices(timeout);

			int n = 0;
		}

		public static BluetoothDeviceInfo[] DiscoverDevices(TimeSpan timeout)
		{
			BluetoothDeviceInfo[] devices;
			using (var @event = new ManualResetEvent(false))
			{
				devices = null;
				var comp = new BluetoothComponent();
				EventHandler<DiscoverDevicesEventArgs> fn = (unused, args) =>
					{
						devices = args.Devices;
						@event.Set();
					};
				comp.DiscoverDevicesComplete += fn;
				try
				{
					comp.DiscoverDevicesAsync(10, true, true, true, false, null);
					if (!@event.WaitOne(timeout))
						throw new NoSuchEndPointException();
				}
				finally
				{
					comp.DiscoverDevicesComplete -= fn;
				}
			}
			return devices;
		}

		public void Dispose()
		{
			throw new NotImplementedException();
		}

		public string Name
		{
			get { throw new NotImplementedException(); }
		}

		public bool IsConnected
		{
			get { throw new NotImplementedException(); }
		}

		public void Disconnect()
		{
			throw new NotImplementedException();
		}

		public T CreateProxy<T>(ulong objectId) where T : class
		{
			throw new NotImplementedException();
		}

		public T GetProxy<T>(ulong objectId) where T : class
		{
			throw new NotImplementedException();
		}

		public IServant CreateServant<T>(ulong objectId, T subject) where T : class
		{
			throw new NotImplementedException();
		}

		public T GetExistingOrCreateNewProxy<T>(ulong objectId) where T : class
		{
			throw new NotImplementedException();
		}

		public IServant GetExistingOrCreateNewServant<T>(T subject) where T : class
		{
			throw new NotImplementedException();
		}
	}
}