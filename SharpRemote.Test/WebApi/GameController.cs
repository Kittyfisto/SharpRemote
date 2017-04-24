using System.Collections.Generic;
using SharpRemote.WebApi;

namespace SharpRemote.Test.WebApi
{
	public sealed class GameController
	{
		private readonly List<Game> _games;

		public GameController()
		{
			_games = new List<Game>
			{
				new Game("Ocarina of Time"),
				new Game("")
			};
		}

		[HttpGet]
		public IEnumerable<Game> GetAll()
		{
			return _games;
		}
	}
}