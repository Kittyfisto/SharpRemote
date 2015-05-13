using System.IO;
using System.Reflection;

namespace SharpRemote
{
	public interface ISerializer
	{
		/// <summary>
		/// Registers the given type to be a singleton.
		/// The given method is used to retrieve the instance again upon deserialization
		/// and must have a signature of static T().
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="getSingleton"></param>
		void RegisterSingleton<T>(MethodInfo getSingleton);

		void RegisterType<T>();
		void WriteObject(BinaryWriter writer, object value);
		object ReadObject(BinaryReader reader);
	}
}