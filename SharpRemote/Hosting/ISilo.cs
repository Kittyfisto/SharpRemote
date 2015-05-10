using System;

namespace SharpRemote.Hosting
{
	/// <summary>
	/// Responsible for creating & providing grains.
	/// It's sole purpose is to hide away the location of an object.
	/// An object can be located in any of the following (relative to the caller):
	/// - The same AppDomain
	/// - A different AppDomain
	/// - A different Process
	/// - A different Machine
	/// </summary>
	public interface ISilo
		: IDisposable
	{
		/// <summary>
		/// Creates a new instance of the given type and returns an interface to it.
		/// </summary>
		/// <typeparam name="TInterface"></typeparam>
		/// <param name="assemblyQualifiedTypeName">The fully qualified typename of the type to instantiate</param>
		/// <param name="parameters"></param>
		/// <returns></returns>
		TInterface CreateGrain<TInterface>(string assemblyQualifiedTypeName, params object[] parameters)
			where TInterface : class;

		/// <summary>
		/// Creates a new instance of the given type and returns an interface to it.
		/// </summary>
		/// <typeparam name="TInterface"></typeparam>
		/// <param name="implementation">The type to instantiate</param>
		/// <param name="parameters"></param>
		/// <returns></returns>
		TInterface CreateGrain<TInterface>(Type implementation, params object[] parameters)
			where TInterface : class;
	}
}