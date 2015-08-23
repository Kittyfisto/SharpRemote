using System;
using System.Diagnostics;
using System.Net;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using log4net;

// ReSharper disable CheckNamespace

namespace SharpRemote.Hosting
// ReSharper restore CheckNamespace
{
	/// <summary>
	///     Used in conjunction with a <see cref="OutOfProcessSilo" /> to host objects in a remote process.
	/// </summary>
	/// <example>
	///     public static void main(string[] arguments)
	///     {
	///     // Put any additional/required initialization here.
	///     using (var silo = new OutOfProcessSiloServer(arguments))
	///     {
	///     // This is the place to register any additional interfaces with this silo
	///     // silo.CreateServant(id, (IMyCustomInterface)new MyCustomImplementation());
	///     silo.Run();
	///     }
	///     }
	/// </example>
	public sealed class OutOfProcessSiloServer
		: IRemotingEndPoint
	{
		private static readonly ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
		private readonly ITypeResolver _customTypeResolver;
		private readonly SocketRemotingEndPointServer _endPoint;
		private readonly Heartbeat _heartbeatSubject;
		private readonly Latency _latencySubject;

		private readonly Process _parentProcess;
		private readonly int? _parentProcessId;
		private readonly PostMortemSettings _postMortemSettings;
		private readonly DefaultImplementationRegistry _registry;
		private readonly ManualResetEvent _waitHandle;

		/// <summary>
		///     Initializes a new silo server.
		/// </summary>
		/// <param name="args">The command line arguments given to the Main() method</param>
		/// <param name="customTypeResolver">The type resolver, if any, responsible for resolving Type objects by their assembly qualified name</param>
		/// <param name="postMortemSettings">
		///     Settings to control how and if minidumps are collected - when set to null, default values are used (
		///     <see
		///         cref="PostMortemSettings" />
		///     )
		/// </param>
		public OutOfProcessSiloServer(string[] args,
		                              ITypeResolver customTypeResolver = null,
		                              PostMortemSettings postMortemSettings = null)
		{
			if (postMortemSettings != null && !postMortemSettings.IsValid)
			{
				throw new ArgumentException("postMortemSettings");
			}

			Log.InfoFormat("Silo Server starting, args ({0}): \"{1}\", {2} custom type resolver",
			               args.Length,
			               string.Join(" ", args),
			               customTypeResolver != null ? "with" : "without"
				);

			int pid;
			if (args.Length >= 1 && int.TryParse(args[0], out pid))
			{
				_parentProcessId = pid;
				_parentProcess = Process.GetProcessById(pid);
				_parentProcess.EnableRaisingEvents = true;
				_parentProcess.Exited += ParentProcessOnExited;
			}

			if (Log.IsDebugEnabled)
			{
				Log.DebugFormat("Args.Length: {0}", args.Length);
			}

			if (args.Length >= 10)
			{
				if (postMortemSettings != null)
				{
					Log.Info("Ignoring post-mortem settings specified from the command-line");
				}
				else
				{
					var settings = new PostMortemSettings();
					bool.TryParse(args[1], out settings.CollectMinidumps);
					bool.TryParse(args[2], out settings.SuppressErrorWindows);
					bool.TryParse(args[3], out settings.HandleAccessViolations);
					bool.TryParse(args[4], out settings.HandleCrtAsserts);
					bool.TryParse(args[5], out settings.HandleCrtPureVirtualFunctionCalls);
					int tmp;
					int.TryParse(args[6], out tmp);
					settings.RuntimeVersions = (CRuntimeVersions) tmp;
					int.TryParse(args[7], out settings.NumMinidumpsRetained);
					settings.MinidumpFolder = args[8];
					settings.MinidumpName = args[9];

					if (!settings.IsValid)
					{
						Log.ErrorFormat("Received invalid post-mortem debugger settings: {0}", settings);
					}
					else
					{
						postMortemSettings = settings;
					}
				}
			}

			_registry = new DefaultImplementationRegistry();
			_waitHandle = new ManualResetEvent(false);
			_customTypeResolver = customTypeResolver;

			_postMortemSettings = postMortemSettings;
			if (_postMortemSettings != null)
			{
				Log.InfoFormat("Using post-mortem debugger: {0}", _postMortemSettings);

				if (!NativeMethods.LoadPostmortemDebugger())
				{
					int err = Marshal.GetLastWin32Error();
					Log.ErrorFormat("Unable to load the post-mortem debugger dll: {0}",
									err);
				}

				if (_postMortemSettings.CollectMinidumps)
				{
					
					if (!NativeMethods.InitDumpCollection(_postMortemSettings.NumMinidumpsRetained,
					                        _postMortemSettings.MinidumpFolder,
					                        _postMortemSettings.MinidumpName))
					{
						int err = Marshal.GetLastWin32Error();
						Log.ErrorFormat("Unable to initialize the post-mortem debugger: {0}",
						                err);
					}
					else
					{
						Log.InfoFormat("Installed post-mortem debugger; up to {0} mini dumps will automatically be saved to: {1}",
									   _postMortemSettings.NumMinidumpsRetained,
									   _postMortemSettings.MinidumpFolder
							);
					}
				}

				if (!NativeMethods.InstallPostmortemDebugger(_postMortemSettings.HandleAccessViolations,
				                                             _postMortemSettings.SuppressErrorWindows,
				                                             _postMortemSettings.HandleCrtAsserts,
				                                             _postMortemSettings.HandleCrtPureVirtualFunctionCalls,
				                                             _postMortemSettings.RuntimeVersions))
				{
					int err = Marshal.GetLastWin32Error();
					Log.ErrorFormat("Unable to install the post-mortem debugger for unhandled exceptions: {0}",
					                err);
				}
			}

			_endPoint = new SocketRemotingEndPointServer(
				customTypeResolver: customTypeResolver
				);

			_heartbeatSubject = new Heartbeat();
			_endPoint.CreateServant(OutOfProcessSilo.Constants.HeartbeatId, (IHeartbeat) _heartbeatSubject);

			_latencySubject = new Latency();
			_endPoint.CreateServant(OutOfProcessSilo.Constants.LatencyProbeId, (ILatency) _latencySubject);
		}

		/// <summary>
		/// The settings that were used to configure this server's behaviour in case of unexpected
		/// (mostly native) faults.
		/// </summary>
		public PostMortemSettings PostMortemSettings
		{
			get { return _postMortemSettings; }
		}

		/// <summary>
		///     The process id of the parent process, as specified in the command line arguments or null
		///     when no id was specified.
		/// </summary>
		public int? ParentProcessId
		{
			get { return _parentProcessId; }
		}

		public void Dispose()
		{
			_waitHandle.Dispose();
		}

		public string Name
		{
			get { return _endPoint.Name; }
		}

		public bool IsConnected
		{
			get { return _endPoint.IsConnected; }
		}

		public void Disconnect()
		{
			_endPoint.Disconnect();
		}

		public T CreateProxy<T>(ulong objectId) where T : class
		{
			return _endPoint.CreateProxy<T>(objectId);
		}

		public T GetProxy<T>(ulong objectId) where T : class
		{
			return _endPoint.GetProxy<T>(objectId);
		}

		public IServant CreateServant<T>(ulong objectId, T subject) where T : class
		{
			return _endPoint.CreateServant(objectId, subject);
		}

		public T GetExistingOrCreateNewProxy<T>(ulong objectId) where T : class
		{
			return _endPoint.GetExistingOrCreateNewProxy<T>(objectId);
		}

		public IServant GetExistingOrCreateNewServant<T>(T subject) where T : class
		{
			return _endPoint.GetExistingOrCreateNewServant(subject);
		}

		/// <summary>
		///     Registers a default implementation for the given interface so that
		///     <see cref="ISilo.CreateGrain{T}(object[])" /> can be used to create grains.
		/// </summary>
		/// <typeparam name="TInterface"></typeparam>
		/// <typeparam name="TImplementation"></typeparam>
		public void RegisterDefaultImplementation<TInterface, TImplementation>()
			where TImplementation : TInterface
			where TInterface : class
		{
			_registry.RegisterDefaultImplementation(typeof (TImplementation), typeof (TInterface));
		}

		/// <summary>
		///     Runs the server and blocks until a shutdown command is received because the
		///     <see cref="OutOfProcessSilo" /> is being disposed of or because the parent process
		///     quits unexpectedly.
		/// </summary>
		public void Run()
		{
			Console.WriteLine(ProcessWatchdog.Constants.BootingMessage);

			const ulong firstServantId = 0;

			try
			{
				using (_endPoint)
				using (var host = new SubjectHost(_endPoint,
				                                  firstServantId,
				                                  _registry,
				                                  OnSubjectHostDisposed,
				                                  _customTypeResolver))
				{
					_endPoint.CreateServant(OutOfProcessSilo.Constants.SubjectHostId, (ISubjectHost) host);

					_endPoint.Bind(IPAddress.Any);
					Console.WriteLine(_endPoint.LocalEndPoint.Port);
					Console.WriteLine(ProcessWatchdog.Constants.ReadyMessage);

					_waitHandle.WaitOne();
					Console.WriteLine(ProcessWatchdog.Constants.ShutdownMessage);
				}
			}
			catch (Exception e)
			{
				Console.WriteLine("Exception: {0}", e.Message);
			}
		}

		private void ParentProcessOnExited(object sender, EventArgs eventArgs)
		{
			Log.InfoFormat("Parent process terminated unexpectedly (exit code: {0}), shutting down...",
			               _parentProcess.ExitCode
				);

			Shutdown();
		}

		private void Shutdown()
		{
			OnSubjectHostDisposed();
		}

		private void OnSubjectHostDisposed()
		{
			_waitHandle.Set();
		}
	}
}