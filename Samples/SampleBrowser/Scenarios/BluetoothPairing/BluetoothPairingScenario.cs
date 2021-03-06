﻿using System.Threading.Tasks;
using System.Windows;
using SharpRemote;

namespace SampleBrowser.Scenarios.BluetoothPairing
{
	public sealed class BluetoothPairingScenario
		: AbstractScenario
	{
		public BluetoothPairingScenario()
			: base(
			"Bluetooth Pairing",
			"Automatically pair this computer with a remote one",
				  isEnabled: false)
		{
			
		}

		public override FrameworkElement CreateView()
		{
			return new BluetoothView();
		}

		protected override bool RunTest()
		{
			using (var endPoint = new BluetoothRemotingEndPoint())
			{
				return true;
			}
		}

		protected override Task StartAsync()
		{
			return Task.Factory.StartNew(() =>
				{

				});
		}

		protected override Task StopAsync()
		{
			return Task.Factory.StartNew(() =>
			{

			});
		}
	}
}