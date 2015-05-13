namespace SharpRemote.Test.Types.Classes
{
	public sealed class Singleton
	{
		static Singleton()
		{
			Instance = new Singleton();
		}

		[SingletonFactoryMethod]
		public static Singleton Instance { get; private set; }

		public string Value
		{
			get { return "There can only be one"; }
		}
	}
}