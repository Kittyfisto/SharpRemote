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
		public TInterface CreateGrain<TInterface>(string assemblyQualifiedTypeName, params object[] parameters) where TInterface : class
		{
			return CreateGrain<TInterface>(Type.GetType(assemblyQualifiedTypeName), parameters);
		}

		public TInterface CreateGrain<TInterface>(Type implementation, params object[] parameters) where TInterface : class
		{
			object subject = Activator.CreateInstance(implementation, parameters);
			return (TInterface) subject;
		}

		public void Dispose()
		{
		}
	}
}