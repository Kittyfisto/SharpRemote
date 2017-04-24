namespace SharpRemote.WebApi.Routes.Parsers
{
	internal sealed class StringParser
		: ArgumentParser
	{
		public override bool TryExtract(string str, int startIndex, out object value, out int consumed)
		{
			// TODO: Introduce termination character extracted from route (if any)
			var tmp = str.Substring(startIndex);
			value = tmp;
			consumed = tmp.Length;
			return true;
		}
	}
}