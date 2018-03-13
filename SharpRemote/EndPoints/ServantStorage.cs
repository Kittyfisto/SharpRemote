using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using log4net;
using SharpRemote.CodeGeneration;

namespace SharpRemote.EndPoints
{
	/// <summary>
	///     Responsible for storing a list of servants.
	/// </summary>
	/// <remarks>
	///     Provides atomic access.
	/// </remarks>
	internal sealed class ServantStorage
		: IDisposable
	{
		private static readonly ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
		private readonly ICodeGenerator _codeGenerator;
		private readonly IEndPointChannel _endPointChannel;
		private readonly GrainIdGenerator _idGenerator;
		private readonly IRemotingEndPoint _remotingEndPoint;
		private readonly Dictionary<ulong, IServant> _servantsById;
		private readonly WeakKeyDictionary<object, IServant> _servantsBySubject;

		private readonly object _syncRoot;
		private int _numServantsCollected;

		public ServantStorage(IRemotingEndPoint remotingEndPoint,
		                      IEndPointChannel endPointChannel,
		                      GrainIdGenerator idGenerator,
		                      ICodeGenerator codeGenerator)
		{
			if (remotingEndPoint == null)
				throw new ArgumentNullException(nameof(remotingEndPoint));
			if (endPointChannel == null)
				throw new ArgumentNullException(nameof(endPointChannel));
			if (idGenerator == null)
				throw new ArgumentNullException(nameof(idGenerator));
			if (codeGenerator == null)
				throw new ArgumentNullException(nameof(codeGenerator));

			_remotingEndPoint = remotingEndPoint;
			_endPointChannel = endPointChannel;
			_idGenerator = idGenerator;
			_codeGenerator = codeGenerator;
			_syncRoot = new object();
			_servantsById = new Dictionary<ulong, IServant>();
			_servantsBySubject = new WeakKeyDictionary<object, IServant>();
		}

		public int NumServantsCollected => _numServantsCollected;

		/// <summary>
		///     Returns all the servnats of this endpoint.
		///     Used for testing.
		/// </summary>
		public IEnumerable<IServant> Servants
		{
			get
			{
				lock (_syncRoot)
				{
					return _servantsById.Values.ToList();
				}
			}
		}

		public void Dispose()
		{
			lock (_syncRoot)
			{
				_servantsBySubject?.Dispose();
				_servantsById.Clear();
			}
		}

		public IServant CreateServant<T>(ulong objectId, T subject) where T : class
		{
			if (Log.IsDebugEnabled)
				Log.DebugFormat("{0}: Creating new servant (#{3}) '{1}' implementing '{2}'",
				                _remotingEndPoint.Name,
				                subject.GetType().FullName,
				                typeof(T).FullName,
				                objectId
				               );

			var servant = _codeGenerator.CreateServant(_remotingEndPoint, _endPointChannel, objectId, subject);
			lock (_syncRoot)
			{
				_servantsById.Add(objectId, servant);
				_servantsBySubject.Add(subject, servant);
			}

			return servant;
		}

		public T RetrieveSubject<T>(ulong objectId) where T : class
		{
			Type interfaceType = null;
			lock (_syncRoot)
			{
				IServant servant;
				if (_servantsById.TryGetValue(objectId, out servant))
				{
					var target = servant.Subject as T;
					if (target != null) return target;

					interfaceType = servant.InterfaceType;
				}
			}

			if (interfaceType != null)
				Log.WarnFormat("{0}: Unable to retrieve subject with id '{1}' as {2}: It was registered as {3}",
				               _remotingEndPoint.Name,
				               objectId,
				               typeof(T),
				               interfaceType);
			else
				Log.WarnFormat("{0}: Unable to retrieve subject with id '{1}', it might have been garbage collected already",
				               _remotingEndPoint.Name,
				               objectId);

			return null;
		}

		public IServant GetExistingOrCreateNewServant<T>(T subject) where T : class
		{
			lock (_syncRoot)
			{
				IServant servant;
				if (!_servantsBySubject.TryGetValue(subject, out servant))
				{
					var nextId = _idGenerator.GetGrainId();
					servant = CreateServant(nextId, subject);
				}

				return servant;
			}
		}

		public int RemoveUnusedServants()
		{
			lock (_syncRoot)
			{
				var collectedServants = _servantsBySubject.Collect(returnCollectedValues: true);
				if (collectedServants != null)
				{
					foreach (var servant in collectedServants)
					{
						if (Log.IsDebugEnabled)
							Log.DebugFormat(
							                "{0}: Removing servant '#{1}' from list of available servants because it's subject is no longer reachable (it has been garbage collected)",
							                _remotingEndPoint.Name,
							                servant.ObjectId);

						_servantsById.Remove(servant.ObjectId);
					}

					_numServantsCollected += collectedServants.Count;
					return collectedServants.Count;
				}

				return 0;
			}
		}

		public bool TryGetServant(ulong servantId, out IServant servant, out int numServants)
		{
			lock (_syncRoot)
			{
				numServants = _servantsById.Count;
				return _servantsById.TryGetValue(servantId, out servant);
			}
		}
	}
}