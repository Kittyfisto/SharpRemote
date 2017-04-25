namespace SharpRemote.Test.WebApi
{
	public sealed class Game
	{
		public int Id { get; }
		public string Name { get; }

		public Game(int id, string name)
		{
			Id = id;
			Name = name;
		}
	}
}