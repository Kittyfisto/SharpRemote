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
		/// <param name="objectId"></param>
		/// <param name="type"></param>
		/// <param name="interfaceType"></param>
		/// <returns></returns>
		void CreateSubject1(ulong objectId, Type type, Type interfaceType);

		/// <summary>
		/// 
		/// </summary>
		/// <param name="objectId"></param>
		/// <param name="assemblyQualifiedTypeName"></param>
		/// <param name="interfaceType"></param>
		/// <returns></returns>
		void CreateSubject2(ulong objectId, string assemblyQualifiedTypeName, Type interfaceType);

		/// <summary>
		/// 
		/// </summary>
		/// <param name="objectId"></param>
		/// <param name="interfaceType"></param>
		/// <returns></returns>
		void CreateSubject3(ulong objectId, Type interfaceType);

		/// <summary>
		/// 
		/// </summary>
		/// <param name="implementation"></param>
		/// <param name="interfaceType"></param>
		void RegisterDefaultImplementation(Type implementation, Type interfaceType);
	}
}