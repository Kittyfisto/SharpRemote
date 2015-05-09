using System;
using System.Diagnostics;
using System.Net;
using System.Threading;

namespace SharpRemote.Hosting
{
	/// <summary>
	/// All requested objects are hosted in another process than the calling one, but on the same computer.
	/// </summary>
	public sealed class ProcessSilo
		: ISilo
	{
		public static class Constants
		{
			/// <summary>
			/// The id of the grain that is used to instantiate further subjects.
			/// </summary>
			public const ulong SubjectHostId = 0;

			public const string BootingMessage = "booting";
			public const string ReadyMessage = "ready";
			public const string ShutdownMessage = "goodbye";

			/// <summary>
			/// The maximum amount of time the host process has to send the "ready" message before it is assumed
			/// to be dead / crashed / broken.
			/// </summary>
			public static readonly TimeSpan ProcessReadyTimeout = TimeSpan.FromSeconds(10);

			/// <summary>
			/// 
			/// </summary>
			public static readonly TimeSpan ConnectionTimeout = TimeSpan.FromSeconds(1);
		}

		private readonly SocketEndPoint _endPoint;
		private readonly Process _process;
		private readonly ISubjectHost _subjectHost;
		private readonly ManualResetEvent _waitHandle;
		private HostState _hostState;

		private int? _remotePort;

		public enum HostState
		{
			None,

			Booting,
			Ready,
			ShuttingDown,
		}

		public ProcessSilo()
		{
			_endPoint = new SocketEndPoint(IPAddress.Loopback);
			_subjectHost = _endPoint.CreateProxy<ISubjectHost>(Constants.SubjectHostId);
			_waitHandle = new ManualResetEvent(false);

			_process = new Process
				{
					StartInfo = new ProcessStartInfo("SharpRemote.Host.exe")
						{
							Arguments = string.Format("{0}", Process.GetCurrentProcess().Id),
							UseShellExecute = false,
							RedirectStandardOutput = true,
							CreateNoWindow = true,
						}
				};
			_process.Exited += ProcessOnExited;
			_process.OutputDataReceived += ProcessOnOutputDataReceived;
			if (!_process.Start())
				throw new NotImplementedException();

			_process.BeginOutputReadLine();

			if (!_waitHandle.WaitOne(Constants.ProcessReadyTimeout))
				throw new NotImplementedException();

			var port = _remotePort;
			if (port == null)
				throw new NotImplementedException();

			_endPoint.Connect(new IPEndPoint(IPAddress.Loopback, port.Value), Constants.ConnectionTimeout);
		}

		private void ProcessOnOutputDataReceived(object sender, DataReceivedEventArgs args)
		{
			var message = args.Data;
			switch (message)
			{
				case Constants.BootingMessage:
					_hostState = HostState.Booting;
					break;

				case Constants.ReadyMessage:
					_hostState = HostState.Ready;
					_waitHandle.Set();
					break;

				case Constants.ShutdownMessage:
					_hostState = HostState.None;
					break;

				default:
					int port;
					if (int.TryParse(message, out port))
						_remotePort = port;
					break;
			}
		}

		private void ProcessOnExited(object sender, EventArgs args)
		{
			
		}

		public TInterface CreateGrain<TInterface>(Type implementation)
			where TInterface : class
		{
			var interfaceType = typeof (TInterface);
			var id = _subjectHost.CreateSubject(implementation, interfaceType);
			var proxy = _endPoint.CreateProxy<TInterface>(id);
			return proxy;
		}

		public void Dispose()
		{
			_subjectHost.TryDispose();
			_endPoint.TryDispose();

			_process.Kill();
			_process.TryDispose();
		}
	}
}