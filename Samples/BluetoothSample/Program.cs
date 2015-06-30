using System;
using System.Threading.Tasks;
using InTheHand.Net;
using InTheHand.Net.Bluetooth;
using InTheHand.Net.Sockets;
using SharpRemote.EndPoints;

namespace BluetoothSample
{
	internal class Program
	{
		public static void Main(string[] args)
		{
			//DiscoverDevices();

			var radios = BluetoothRadio.AllRadios;
			if (radios.Length == 0)
			{
				Console.WriteLine("Bluetooth is turned off!");
			}
			else if (radios.Length >= 2)
			{
				var prim = radios[0];
				var sec = radios[1];

				Console.WriteLine("Local MAC: {0}", prim.LocalAddress);

				var service = new Guid();
				var ep = new BluetoothEndPoint(prim.LocalAddress, service);
				var listener = new BluetoothListener(ep);

				Console.WriteLine("Listening on {0} for incoming connections", ep);

				listener.Start();

				var other = new Task(() => Connect(sec, service, ep));
				other.Start();

				listener.AcceptSocket();
			}

			Console.ReadLine();
		}

		private static void Connect(BluetoothRadio sec, Guid service, BluetoothEndPoint remoteEndPoint)
		{
			var localEndPoint = new BluetoothEndPoint(sec.LocalAddress, service);
			Console.WriteLine("Connecting to {0}", remoteEndPoint);
			using (var client = new BluetoothClient(localEndPoint))
			{
				client.Connect(remoteEndPoint);
			}
		}

		private static void DiscoverDevices()
		{
			Console.WriteLine("Discovering devices...");
			var devices = BluetoothRemotingEndPoint.DiscoverDevices(TimeSpan.FromSeconds(60));
			Console.WriteLine("Found {0} devices", devices.Length);
			foreach (var device in devices)
			{
				Console.WriteLine("{0}, MAC {1}, {2}", device.DeviceName,
				                  device.DeviceAddress,
				                  device.ClassOfDevice);
			}
		}
	}
}