using System;

namespace SharpRemote.EndPoints
{
	internal interface IInternalRemotingEndPoint
		: IRemotingEndPoint
	{
		/// <summary>
		///     The total number of <see cref="IProxy" />s that have been removed from this endpoint because
		///     they're no longer used.
		/// </summary>
		long NumProxiesCollected { get; }

		/// <summary>
		///     The total number of <see cref="IServant" />s that have been removed from this endpoint because
		///     their subjects have been collected by the GC.
		/// </summary>
		long NumServantsCollected { get; }

		/// <summary>
		///     The total amount of time this endpoint spent collecting garbage.
		/// </summary>
		 TimeSpan GarbageCollectionTime { get; }
	}
}