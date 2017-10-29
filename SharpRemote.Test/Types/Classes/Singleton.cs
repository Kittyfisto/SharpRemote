namespace SharpRemote.Test.Types.Classes
{
	public sealed class Singleton
	{
		private static readonly Singleton _instance;

		static Singleton()
		{
			_instance = new Singleton();
		}

		private Singleton()
		{ }

		[SingletonFactoryMethod]
		public static Singleton GetInstance()
		{
			return _instance;
		}

		public string Value
		{
			get { return "There can only be one"; }
		}
	}
}