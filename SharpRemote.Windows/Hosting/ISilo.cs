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
		/// Whether or not this silo has been disposed of.
		/// </summary>
		bool IsDisposed { get; }

		/// <summary>
		/// Registers the default
		/// </summary>
		/// <typeparam name="TInterface"></typeparam>
		/// <typeparam name="TImplementation"></typeparam>
		void RegisterDefaultImplementation<TInterface, TImplementation>()
			where TImplementation : TInterface
			where TInterface : class;

		/// <summary>
		/// Creates a new object that implements the given interface.
		/// The type of the implementation is defined via <see cref="RegisterDefaultImplementation{TInterface, TImplementation}()"/>
		/// or via <see cref="OutOfProcessSiloServer.RegisterDefaultImplementation{T, TImplementation}()"/>.
		/// </summary>
		/// <typeparam name="TInterface"></typeparam>
		/// <param name="parameters"></param>
		/// <returns></returns>
		TInterface CreateGrain<TInterface>(params object[] parameters)
			where TInterface : class;

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

		/// <summary>
		/// Creates a new instance of the given type and returns an interface to it.
		/// </summary>
		/// <typeparam name="TInterface"></typeparam>
		/// <typeparam name="TImplementation">The type to instantiate</typeparam>
		/// <param name="parameters"></param>
		/// <returns></returns>
		TInterface CreateGrain<TInterface, TImplementation>(params object[] parameters)
			where TInterface : class
			where TImplementation : TInterface;
	}
}