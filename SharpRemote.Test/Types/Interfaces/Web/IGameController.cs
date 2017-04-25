using System.Collections.Generic;
using SharpRemote.Test.WebApi;
using SharpRemote.WebApi;

namespace SharpRemote.Test.Types.Interfaces.Web
{
	public interface IGameController
	{
		[Route]
		IEnumerable<Game> GetAll();

		[Route("{0}")]
		Game Get(int id);
	}
}