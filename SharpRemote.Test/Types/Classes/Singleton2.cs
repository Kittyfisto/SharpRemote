using System.Runtime.Serialization;

namespace SharpRemote.Test.Types.Classes
{
	[DataContract]
	public sealed class Singleton2
		: ISingleton
	{
		private static readonly Singleton2 _instance;

		static Singleton2()
		{
			_instance = new Singleton2();
		}

		private Singleton2()
		{}

		[SingletonFactoryMethod]
		public static Singleton2 Instance => _instance;
	}
}