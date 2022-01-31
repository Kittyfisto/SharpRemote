using System.Collections.Generic;

namespace SharpRemote.WebApi.Test
{
	public interface IGameController
	{
		[Route]
		IEnumerable<Game> GetAll();

		[Route("{0}")]
		Game Get(int id);
	}
}