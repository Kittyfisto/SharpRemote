using System;

namespace SharpRemote.Hosting
{
	/// <summary>
	/// Responsible for creating & hosting objects.
	/// </summary>
	public interface ISubjectHost
		: IDisposable
	{
		/// <summary>
		/// 
		/// </summary>
		/// <param name="type"></param>
		/// <param name="interfaceType"></param>
		/// <returns></returns>
		ulong CreateSubject1(Type type, Type interfaceType);

		/// <summary>
		/// 
		/// </summary>
		/// <param name="assemblyQualifiedTypeName"></param>
		/// <param name="interfaceType"></param>
		/// <returns></returns>
		ulong CreateSubject2(string assemblyQualifiedTypeName, Type interfaceType);

		/// <summary>
		/// 
		/// </summary>
		/// <returns></returns>
		ulong CreateSubject3(Type interfaceType);

		/// <summary>
		/// 
		/// </summary>
		/// <param name="implementation"></param>
		/// <param name="interfaceType"></param>
		void RegisterDefaultImplementation(Type implementation, Type interfaceType);
	}
}