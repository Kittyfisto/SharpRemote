using System.Collections.Generic;

namespace SharpRemote.WebApi.Test
{
	public interface IGetStringList
	{
		[Route]
		IEnumerable<string> Get();

		[Route("{0}")]
		string Get(int index);
	}
}