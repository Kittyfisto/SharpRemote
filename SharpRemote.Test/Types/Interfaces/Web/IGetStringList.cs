using System.Collections.Generic;
using SharpRemote.WebApi;

namespace SharpRemote.Test.Types.Interfaces.Web
{
	public interface IGetStringList
	{
		[HttpGet]
		IEnumerable<string> Get();

		[HttpGet("{0}")]
		string Get(int index);
	}
}