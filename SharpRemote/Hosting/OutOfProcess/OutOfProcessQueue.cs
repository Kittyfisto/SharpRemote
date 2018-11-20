using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using log4net;

namespace SharpRemote.Hosting.OutOfProcess
{
	/// <summary>
	///     Responsible for starting, restarting and stopping another application.
	/// </summary>
	/// <remarks>
	///     This classes' right to exist comes from the fact that synchronizing the Start() and HandleFailure()
	///     events is quite complicated and brittle (a lot of callbacks => chance for deadlocks would increase) and
	///     therefore serialization using a worker thread and a queue is way simpler.
	///     The main problem is having to deal with late failures while we're in the process of starting the host
	///     application. Ideally we only want to handle those (successive) failures once the current process is finished,
	///     hence the approach using a queue.
	/// </remarks>
	internal sealed class OutOfProcessQueue
		: IDisposable
	{
		public enum OperationResult
		{
			Processed,
			Ignored
		}

		public enum OperationType
		{
			Start,
			Stop,
			HandleFailure
		}

		private static readonly ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

		private readonly ConcurrentQueue<Operation> _actions;
		private readonly ISocketEndPoint _endPoint;
		private readonly IFailureHandler _failureHandler;
		private readonly FailureSettings _failureSettings;
		private readonly ProcessWatchdog _process;
		private readonly object _syncRoot;
		private readonly Thread _thread;
		private ConnectionId _currentConnection;
		private int _currentPid;
		private volatile bool _isDisposed;
		private bool _started;

		public OutOfProcessQueue(
			ProcessWatchdog process,
			ISocketEndPoint endPoint,
			IFailureHandler failureHandler,
			FailureSettings failureSettings
			)
		{
			if (process == null)
				throw new ArgumentNullException(nameof(process));
			if (endPoint == null)
				throw new ArgumentNullException(nameof(endPoint));
			if (failureHandler == null)
				throw new ArgumentNullException(nameof(failureHandler));
			if (failureSettings == null)
				throw new ArgumentNullException(nameof(failureSettings));

			_syncRoot = new object();

			_process = process;
			_process.OnFaultDetected += ProcessOnOnFaultDetected;

			_endPoint = endPoint;
			_endPoint.OnFailure += EndPointOnOnFailure;

			_failureHandler = failureHandler;
			_failureSettings = failureSettings;

			_actions = new ConcurrentQueue<Operation>();
			_thread = new Thread(Do);
			_thread.Start();
		}

		public void Dispose()
		{
			_process.OnFaultDetected -= ProcessOnOnFaultDetected;
			_isDisposed = true;
		}

		/// <summary>
		///     This event is invoked whenever the host process was successfully started, and a connection
		///     to it was established.
		/// </summary>
		public event Action OnHostStarted;

		public Task Start()
		{
			Operation op = Operation.Start();
			_actions.Enqueue(op);
			return op.Task;
		}

		public Task Stop()
		{
			Operation op = Operation.Stop();
			_actions.Enqueue(op);
			return op.Task;
		}

		/// <summary>
		///     Is called when the endpoint reports a failure.
		/// </summary>
		private void EndPointOnOnFailure(EndPointDisconnectReason endPointReason, ConnectionId connectionId)
		{
			lock (_syncRoot)
			{
				// If we're disposing this silo (or have disposed it alrady), then the heartbeat monitor
				// reported a failure that we caused intentionally (by killing the host process) and thus
				// this "failure" musn't be reported.
				if (_isDisposed)
					return;
			}

			// The socket will have logged all this information already thus we can skip it here
			Log.DebugFormat("SocketEndPoint detected a failure of the connection to the host process: {0}", endPointReason);

			Failure failure;
			switch (endPointReason)
			{
				case EndPointDisconnectReason.ReadFailure:
				case EndPointDisconnectReason.RpcInvalidResponse:
				case EndPointDisconnectReason.WriteFailure:
				case EndPointDisconnectReason.ConnectionAborted:
				case EndPointDisconnectReason.ConnectionReset:
				case EndPointDisconnectReason.ConnectionTimedOut:
					failure = Failure.ConnectionFailure;
					break;

				case EndPointDisconnectReason.RequestedByEndPoint:
				case EndPointDisconnectReason.RequestedByRemotEndPoint:
					failure = Failure.ConnectionClosed;
					break;

				case EndPointDisconnectReason.HeartbeatFailure:
					failure = Failure.HeartbeatFailure;
					break;

				case EndPointDisconnectReason.UnhandledException:
					failure = Failure.UnhandledException;
					break;

				default:
					Log.WarnFormat("Unknown EndPointDisconnectReason: {0}", endPointReason);
					failure = Failure.Unknown;
					break;
			}

			Operation op = Operation.HandleFailure(failure, connectionId);
			_actions.Enqueue(op);
		}

		private void ProcessOnOnFaultDetected(int pid, ProcessFailureReason processFailureReason)
		{
			lock (_syncRoot)
			{
				// If we're disposing this silo (or have disposed it alrady), then the heartbeat monitor
				// reported a failure that we caused intentionally (by killing the host process) and thus
				// this "failure" musn't be reported.
				if (_isDisposed)
					return;
			}

			Operation op = Operation.HandleFailure(Failure.HostProcessExited, pid);
			_actions.Enqueue(op);
		}

		#region Operation execution

		public enum LoopResult
		{
			Continue,
			Stop,
		}

		public int CurrentPid
		{
			get { return _currentPid; }
		}

		public ConnectionId CurrentConnection => _currentConnection;

		private void Do()
		{
			while (!_isDisposed)
			{
				try
				{
					Operation operation;
					if (_actions.TryDequeue(out operation))
					{
						Do(operation);
					}
					else
					{
						Thread.Sleep(100);
					}
				}
				catch (Exception e)
				{
					Log.ErrorFormat("Caught unexpected exception: {0}", e);
				}
			}
		}

		private void Do(Operation operation)
		{
			Log.DebugFormat("Beginning operator {0}...", operation);

			Action<Operation> proc;
			switch (operation.Type)
			{
				case OperationType.Start:
					proc = DoStart;
					break;

				case OperationType.Stop:
					proc = DoStop;
					break;

				case OperationType.HandleFailure:
					proc = op => DoHandleFailure(op);
					break;

				default:
					proc = DoNothing;
					break;
			}

			operation.Execute(proc);
		}

		private void DoStart(Operation op)
		{
			_started = true;
			StartInternal();
		}

		private void DoStop(Operation op)
		{
			_started = false;

			_endPoint.Disconnect();
			_process.TryKill();
		}

		private void DoNothing(Operation unused)
		{
			
		}

		private void StartInternal()
		{
			if (_process.IsProcessRunning)
				throw new InvalidOperationException();

			//
			// We try to start the goddamn host process in a loop until:
			// - The start & connection succeeds
			// - The IFailureHandler calls quit
			// - The number of restarts is 20
			//
			// If starting failed then we throw the last exception 
			//
			List<Exception> exceptions = null;
			const int maxRestarts = 20;
			int pid = -1;
			ConnectionId connectionId = ConnectionId.None;
			for (int currentRestart = 0; currentRestart < maxRestarts; ++currentRestart)
			{
				LoopResult instruction = StartOnce(currentRestart, out pid, out connectionId, ref exceptions);
				if (instruction == LoopResult.Stop)
					break;
			}

			if (exceptions != null)
			{
				//
				// POINT OF FAILURE
				//

				_currentPid = -1;
				_currentConnection = ConnectionId.None;
				throw new AggregateException("Unable to start & establish a connection with the host process",
				                             exceptions);
			}


			//
			// POINT OF NO FAILURE BELOW
			//

			lock (_syncRoot)
			{
				_currentPid = pid;
				_currentConnection = connectionId;
			}

			try
			{
				OnHostStarted?.Invoke();
			}
			catch (Exception e)
			{
				Log.WarnFormat("The OnHostStarted event threw an exception, please don't do that: {0}", e);
			}
		}

		/// <summary>
		///     Tries to start the host application exactly once.
		/// </summary>
		/// <param name="currentRestart">The index of this restart, e.g. 0 for the first try, 1 for the 2nd, etc...</param>
		/// <param name="pid">The process id of the resulting process, if successfully started</param>
		/// <param name="connectionId"></param>
		/// <param name="exceptions">The list that will be filled with the exception(s) relevant to the Start, should any be thrown</param>
		/// <returns>True when the application was started and a connection established, false otherwise</returns>
		private LoopResult StartOnce(int currentRestart,
		                             out int pid,
		                             out ConnectionId connectionId,
		                             ref List<Exception> exceptions)
		{
			connectionId = ConnectionId.None;
			Exception exception;
			try
			{
				_process.Start(out pid);
				int? port = _process.RemotePort;
				if (_endPoint.TryConnect(new IPEndPoint(IPAddress.Loopback, port.Value),
				                         _failureSettings.EndPointConnectTimeout,
				                         out exception,
				                         out connectionId))
				{
					exceptions = null;
					return LoopResult.Stop;
				}

				Log.WarnFormat("Unable to establish a connection with the host process (PID: {1}): {0}",
				               exception,
				               _process.HostedProcessId);
			}
			catch (Exception e)
			{
				Log.WarnFormat("Caught unexpected exception after having started the host process (PID: {1}): {0}",
				               e,
				               _process.HostedProcessId);

				exception = e;
			}

			if (ProcessStartFailure(currentRestart, out pid, ref exceptions, exception))
				return LoopResult.Stop;

			return LoopResult.Continue;
		}

		private bool ProcessStartFailure(int currentRestart,
		                                 out int pid,
		                                 ref List<Exception> exceptions,
		                                 Exception e)
		{
			pid = -1;
			_process.TryKill();

			if (exceptions == null)
				exceptions = new List<Exception>();
			exceptions.Add(e);

			Decision? decision;
			TimeSpan waitTime = TimeSpan.Zero;
			try
			{
				decision = _failureHandler.OnStartFailure(currentRestart + 1,
				                                          e,
				                                          out waitTime);
			}
			catch (Exception ex)
			{
				Log.WarnFormat("IFailureHandler.OnStartFailure threw an exception - ignoring it: {0}", ex);
				decision = Decision.Stop;
			}

			if (decision != Decision.RestartHost)
				return true;

			Log.DebugFormat("Restarting the host application '{0}' failed, waiting '{1}s' and then trying again",
			                _process.HostExecutableName,
			                waitTime);

			if (waitTime >= TimeSpan.Zero)
			{
				Thread.Sleep(waitTime);
			}
			return false;
		}

		/// <summary>
		///     Handles the given failure.
		/// </summary>
		/// <param name="op"></param>
		/// <returns></returns>
		internal OperationResult DoHandleFailure(Operation op)
		{
			if (_isDisposed)
			{
				Log.DebugFormat("Ignoring failure '{0}' because this queue has been disposed of already", op);
				return OperationResult.Ignored;
			}

			// If the failure happened because the host process exited, then we have to make
			// sure that the failure actually references the CURRENT host process and not any
			// of the previous
			if (op.Pid != null && op.Pid != _currentPid)
			{
				Log.DebugFormat(
					"Ignoring failure '{0}' because it doesn't reference the current PID #{1}: It is most likely from a previous failure that was already handled",
					op,
					_currentPid);
				return OperationResult.Ignored;
			}

			// If the failure happened because the connection was closed / interrupted, then
			// we have to make sure that the failure actually references the CURRENT connection
			if (op.ConnectionId != ConnectionId.None && op.ConnectionId != _currentConnection)
			{
				Log.DebugFormat(
					"Ignoring failure '{0}' because it doesn't reference the current connection {1}: It is most likely from a previous failure that was already handled",
					op,
					_currentConnection);
				return OperationResult.Ignored;
			}

			// The failure happened because the user called Stop() which in turn killed the host process
			// (and therefore it's no surprise the connection dropped). This is NOT failure from the user's
			// perspective and thus is swalled as well...
			if (!_started)
			{
				Log.DebugFormat("Ignoring failure '{0}' because it occured after Stop() has been called and is very likely a result of it",
				                op);
				return OperationResult.Ignored;
			}

			Failure? failure = op.Failure;
			if (failure == null)
				return OperationResult.Ignored;

			var decision = Decision.Stop;
			try
			{
				Decision? handlerResolution = _failureHandler.OnFailure(failure.Value);
				if (handlerResolution != null)
					decision = handlerResolution.Value;
			}
			catch (Exception e)
			{
				Log.WarnFormat("OnFaultDetected threw an exception - ignoring it: {0}", e);
			}

			if (op.Failure != Failure.HostProcessExited)
			{
				_process.TryKill();
			}

			// We don't want to call disconnect in case this method is executing because 
			// of an endpoint failure - because we're called from the endpoint's Disconnect method.
			// Calling disconnect again would overwrite the disconnect failure...
			if (!op.DueToEndPoint)
			{
				_endPoint.Disconnect();
			}

			Resolution resolution = ResolveFailure(failure.Value, decision);

			try
			{
				_failureHandler.OnResolutionFinished(failure.Value, decision, resolution);
			}
			catch (Exception e)
			{
				Log.WarnFormat("IFailureHandler.OnResolutionFinished threw an exception - ignoring it: {0}", e);
			}

			return 0;
		}

		/// <summary>
		///     Actually tries to resolve the failure (if possible).
		/// </summary>
		/// <param name="failure"></param>
		/// <param name="decision"></param>
		/// <returns></returns>
		private Resolution ResolveFailure(Failure failure, Decision decision)
		{
			switch (decision)
			{
				case Decision.Stop: //< No need to do anything further...
					_currentPid = 0;
					_currentConnection = ConnectionId.None;
					return Resolution.Stopped;

				case Decision.RestartHost:
					return RestartHost(failure, decision);

				default:
					Log.WarnFormat("Unknown decision '{0}' - interpreting it as '{1}'",
					               decision,
					               Decision.Stop);
					return Resolution.Stopped;
			}
		}

		private Resolution RestartHost(Failure failure, Decision decision)
		{
			try
			{
				StartInternal();
				Log.InfoFormat("Successfully recovered from failure '{0}' by restarting the host application '{1}' (PID: {2})",
				               failure,
				               _process.HostExecutableName,
				               _currentPid);

				return Resolution.Restarted;
			}
			catch (Exception e)
			{
				Log.WarnFormat(
					"Tried to resolve the failure '{0}' by restarting the host - but this failed as well - we have to give up: {1}",
					failure,
					e);

				try
				{
					_failureHandler.OnResolutionFailed(failure, decision, e);
				}
				catch (Exception ex)
				{
					Log.WarnFormat("IFailureHandler.OnResolutionFailed threw an exception - ignoring it: {0}", ex);
				}

				return Resolution.Stopped;
			}
		}

		#endregion

		public struct Operation
		{
			public readonly ConnectionId ConnectionId;
			public readonly bool DueToEndPoint;
			public readonly Failure? Failure;
			public readonly int? Pid;
			public readonly Task Task;
			public readonly OperationType Type;
			private readonly TaskCompletionSource<int> _taskSource;

			private Operation(OperationType type,
			                  Failure? failure,
			                  int? pid,
			                  ConnectionId connectionId,
			                  bool dueToEndPoint)
			{
				Type = type;
				Pid = pid;
				ConnectionId = connectionId;
				DueToEndPoint = dueToEndPoint;
				Failure = failure;
				_taskSource = new TaskCompletionSource<int>();
				Task = _taskSource.Task;
			}

			public override string ToString()
			{
				if (Type == OperationType.HandleFailure)
				{
					if (Pid != null)
						return string.Format("HandleFailure: {0}, PID #{1}",
						                     Failure,
						                     Pid);

					return string.Format("HandleFailure: {0}, {1}", Failure, ConnectionId);
				}

				return string.Format("Start host application");
			}

			public static Operation Start()
			{
				return new Operation(OperationType.Start, null, null, ConnectionId.None, false);
			}

			public static Operation Stop()
			{
				return new Operation(OperationType.Stop, null, null, ConnectionId.None, false);
			}

			public static Operation HandleFailure(Failure failure, int pid)
			{
				return new Operation(OperationType.HandleFailure,
				                     failure,
				                     pid,
				                     ConnectionId.None,
				                     false);
			}

			public static Operation HandleFailure(Failure failure, ConnectionId connectionId)
			{
				return new Operation(OperationType.HandleFailure,
				                     failure,
				                     null,
				                     connectionId,
				                     true);
			}

			public void Execute(Action<Operation> fn)
			{
				try
				{
					fn(this);
					_taskSource.SetResult(42);
				}
				catch (Exception e)
				{
					_taskSource.SetException(e);
				}
			}
		}
	}
}