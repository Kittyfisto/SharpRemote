using System;

namespace SharpRemote.WebApi.Routes.Parsers
{
	internal sealed class BoolParser
		: ArgumentParser
	{
		public override bool RequiresTerminator => false;

		public override Type Type => typeof(bool);

		public override bool TryExtract(string str, int startIndex, out object value, out int consumed)
		{
			if (str.IndexOf("true", startIndex, StringComparison.InvariantCultureIgnoreCase) == startIndex)
			{
				value = true;
				consumed = 4;
				return true;
			}
			if (str.IndexOf("false", startIndex, StringComparison.InvariantCultureIgnoreCase) == startIndex)
			{
				value = false;
				consumed = 5;
				return true;
			}

			value = null;
			consumed = 0;
			return false;
		}
	}
}