namespace SharpRemote.Test.WebApi
{
	public sealed class Game
	{
		private readonly string _name;

		public string Name => _name;

		public Game(string name)
		{
			_name = name;
		}
	}
}