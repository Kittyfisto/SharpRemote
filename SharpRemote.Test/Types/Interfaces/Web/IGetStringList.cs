using System.Collections.Generic;
using SharpRemote.WebApi;

namespace SharpRemote.Test.Types.Interfaces.Web
{
	public interface IGetStringList
	{
		[Route]
		IEnumerable<string> Get();

		[Route("{0}")]
		string Get(int index);
	}
}