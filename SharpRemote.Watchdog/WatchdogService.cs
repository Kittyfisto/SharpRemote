using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.ServiceProcess;

namespace SharpRemote.Watchdog
{
	public partial class WatchdogService
		: ServiceBase
	{
		private const string EventSourceName = "SharpRemote.Watchdog";
		private const string EventLogName = "Log";
		private readonly EventLog _eventLog;
		private ServiceStatus _currentStatus;

		public WatchdogService()
		{
			InitializeComponent();

			_eventLog = new EventLog();
			if (!EventLog.SourceExists(EventSourceName))
			{
				EventLog.CreateEventSource(EventSourceName, EventLogName);
			}
			_eventLog.Source = EventSourceName;
			_eventLog.Log = EventLogName;
		}

		protected override void OnStart(string[] args)
		{
			CurrentStatus = new ServiceStatus
				{
					dwCurrentState = ServiceState.SERVICE_START_PENDING,
					dwWaitHint = 100000
				};

			_eventLog.WriteEntry("Starting watchdog");

			CurrentStatus = new ServiceStatus
			{
				dwCurrentState = ServiceState.SERVICE_RUNNING,
				dwWaitHint = 100000
			};
		}

		protected override void OnStop()
		{
			CurrentStatus = new ServiceStatus
			{
				dwCurrentState = ServiceState.SERVICE_STOP_PENDING,
				dwWaitHint = 100000
			};

			_eventLog.WriteEntry("Stopping watchdog");
			CurrentStatus = new ServiceStatus
			{
				dwCurrentState = ServiceState.SERVICE_STOPPED,
				dwWaitHint = 100000
			};

		}

		private ServiceStatus CurrentStatus
		{
			get { return _currentStatus; }
			set
			{
				_currentStatus = value;
				SetServiceStatus(ServiceHandle, ref _currentStatus);
			}
		}

		[DllImport("advapi32.dll", SetLastError = true)]
		private static extern bool SetServiceStatus(IntPtr handle, ref ServiceStatus serviceStatus);
	}
}