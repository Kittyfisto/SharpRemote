using System;
using System.Reflection;
using System.Threading.Tasks;
using System.Timers;
using SharpRemote.Diagnostics;
using log4net;

// ReSharper disable CheckNamespace
namespace SharpRemote
// ReSharper restore CheckNamespace
{
	/// <summary>
	///     Default <see cref="IHeartbeat" /> implementation that returns immediately.
	/// </summary>
	internal sealed class Heartbeat
		: IHeartbeat
		, IDisposable
	{
		private static readonly ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

		private readonly Timer _timer;
		private readonly IDebugger _debugger;
		private readonly IRemotingEndPoint _endPoint;
		private readonly bool _reportDebuggerAttached;

		private bool? _isDebuggerAttached;

		public Heartbeat(IDebugger debugger,
		                 IRemotingEndPoint endPoint,
		                 bool reportDebuggerAttached)
		{
			if (debugger == null)
				throw new ArgumentNullException(nameof(debugger));
			if (endPoint == null)
				throw new ArgumentNullException(nameof(endPoint));

			_debugger = debugger;
			_endPoint = endPoint;
			_reportDebuggerAttached = reportDebuggerAttached;
			_timer = new Timer
				{
					Interval = 100
				};
			_timer.Elapsed += TimerOnElapsed;
			_timer.Start();
		}

		private void TimerOnElapsed(object sender, ElapsedEventArgs args)
		{
			try
			{
				var now = _debugger.IsDebuggerAttached;
				if (now != _isDebuggerAttached)
				{
					if (_reportDebuggerAttached)
					{
						if (now)
						{
							EmitRemoteDebuggerAttached();
						}
						else if (_isDebuggerAttached != null)
						{
							EmitRemoteDebuggerDetached();
						}
					}
				}
			}
			catch (Exception e)
			{
				Log.ErrorFormat("Caught unexpected exception: {0}", e);
			}
		}

		public event Action RemoteDebuggerAttached;

		public event Action RemoteDebuggerDetached;

		public Task Beat()
		{
			return Task.FromResult(1);
		}

		private void EmitRemoteDebuggerAttached()
		{
			Action handler = RemoteDebuggerAttached;
			if (handler != null && _endPoint.IsConnected)
			{
				try
				{
					handler();
					_isDebuggerAttached = true;
				}
				catch (RemoteProcedureCallCanceledException e)
				{
					Log.DebugFormat("Caught exception: {0}", e);
				}
			}
		}

		private void EmitRemoteDebuggerDetached()
		{
			Action handler = RemoteDebuggerDetached;
			if (handler != null && _endPoint.IsConnected)
			{
				try
				{
					handler();
					_isDebuggerAttached = false;
				}
				catch (RemoteProcedureCallCanceledException e)
				{
					Log.DebugFormat("Caught exception: {0}", e);
				}
			}
		}

		public void Dispose()
		{
			_timer.Dispose();
		}
	}
}