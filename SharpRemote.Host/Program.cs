using System;
using System.Diagnostics;
using System.Net;
using System.Reflection;
using System.Threading;
using SharpRemote.Hosting;
using log4net;

namespace SharpRemote.Host
{
	internal class Program
		: IDisposable
	{
		private static readonly ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
		private readonly Process _parentProcess;

		private readonly int? _parentProcessId;
		private readonly ManualResetEvent _waitHandle;

		private Program(string[] args)
		{
			int pid;
			if (args.Length >= 1 && int.TryParse(args[0], out pid))
			{
				_parentProcessId = pid;
				_parentProcess = Process.GetProcessById(pid);
				_parentProcess.EnableRaisingEvents = true;
				_parentProcess.Exited += ParentProcessOnExited;
			}

			_waitHandle = new ManualResetEvent(false);
		}

		public void Dispose()
		{
			_waitHandle.Dispose();
		}

		private void ParentProcessOnExited(object sender, EventArgs eventArgs)
		{
			Shutdown();
		}

		private void Shutdown()
		{
			OnSubjectHostDisposed();
		}

		private static void Main(string[] args)
		{
			using (var program = new Program(args))
			{
				program.Run();
			}
		}

		public void Run()
		{
			Console.WriteLine(ProcessSilo.Constants.BootingMessage);

			const ulong subjectHostId = ProcessSilo.Constants.SubjectHostId;
			const ulong firstServantId = subjectHostId + 1;

			try
			{
				using (var endpoint = new SocketRemotingEndPoint())
				using (var host = new SubjectHost(endpoint, firstServantId, OnSubjectHostDisposed))
				{
					var servant = endpoint.CreateServant(subjectHostId, (ISubjectHost) host);

					endpoint.Bind(IPAddress.Loopback);
					Console.WriteLine(endpoint.LocalEndPoint.Port);
					Console.WriteLine(ProcessSilo.Constants.ReadyMessage);

					_waitHandle.WaitOne();
					Console.WriteLine(ProcessSilo.Constants.ShutdownMessage);
				}
			}
			catch (Exception e)
			{
				Console.WriteLine("Exception: {0}", e.Message);
			}
		}

		private void OnSubjectHostDisposed()
		{
			_waitHandle.Set();
		}
	}
}