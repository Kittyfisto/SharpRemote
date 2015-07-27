using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using InTheHand.Net;
using InTheHand.Net.Bluetooth;
using InTheHand.Net.Sockets;

namespace SharpRemote.EndPoints
{
	/// <summary>
	/// NOT FINISHED.
	/// </summary>
	public sealed class BluetoothRemotingEndPoint
		: AbstractEndPoint
		  , IRemotingEndPoint
		  , IEndPointChannel
	{
		private BluetoothEndPoint _localEndPoint;

		/// <summary>
		/// NOT FINISHED
		/// </summary>
		public BluetoothRemotingEndPoint()
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

		public Task<MemoryStream> CallRemoteMethodAsync(ulong servantId, string interfaceType, string methodName, MemoryStream arguments)
		{
			throw new NotImplementedException();
		}

		public MemoryStream CallRemoteMethod(ulong servantId, string interfaceType, string methodName, MemoryStream arguments)
		{
			throw new NotImplementedException();
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