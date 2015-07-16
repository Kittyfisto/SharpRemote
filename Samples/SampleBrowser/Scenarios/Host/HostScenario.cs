using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows;
using SharpRemote;
using SharpRemote.Hosting;

namespace SampleBrowser.Scenarios.Host
{
	public sealed class HostScenario
		: AbstractScenario
	{
		private readonly ObservableCollection<string> _hostOutput;

		public HostScenario()
			: base("Host",
			       "Start and connect to a host application on the same computer and run a test suite over the network")
		{
			_hostOutput = new ObservableCollection<string>();
		}

		public IEnumerable<string> HostOutput
		{
			get { return _hostOutput; }
		}

		public override FrameworkElement CreateView()
		{
			return new HostView();
		}

		protected override bool RunTest()
		{
			using (var appender = new LogInterceptor(Log))
			using (var silo = new OutOfProcessSilo(hostOutputWritten: LogHost))
			{
				var instance = silo.CreateGrain<ISample>(typeof (Sample));
				Log(string.Format("Have you been called yet? - {0}", instance.HaveYouBeenCalledYet()));
				instance.Call("This message is sent via a remote procedure call through the ISample interface");
				Log(string.Format("What about now? - {0}", instance.HaveYouBeenCalledYet()));

				Log("What's the value of PI?");
				instance.WritePi();
				return true;
			}
		}

		private void LogHost(string that)
		{
			App.Dispatcher.BeginInvoke(new Action(() => _hostOutput.Add(that)));
		}

		protected override Task Start()
		{
			return Task.Factory.StartNew(() => { });
		}

		protected override Task Stop()
		{
			return Task.Factory.StartNew(() => { });
		}
	}
}