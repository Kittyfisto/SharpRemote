using System;

namespace SharpRemote.Hosting
{
	/// <summary>
	///     Hosts all objects in the calling process without any remoting overhead.
	///     The object returned is the object being created - not some proxy.
	/// </summary>
	public sealed class InProcessSilo
		: ISilo
	{
		public TInterface CreateGrain<TInterface>(string assemblyQualifiedTypeName) where TInterface : class
		{
			return CreateGrain<TInterface>(Type.GetType(assemblyQualifiedTypeName));
		}

		public TInterface CreateGrain<TInterface>(Type implementation) where TInterface : class
		{
			object subject = Activator.CreateInstance(implementation);
			return (TInterface) subject;
		}

		public void Dispose()
		{
		}
	}
}