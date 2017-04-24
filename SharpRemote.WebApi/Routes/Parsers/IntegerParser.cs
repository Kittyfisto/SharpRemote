namespace SharpRemote.WebApi.Routes.Parsers
{
	internal abstract class IntegerParser
		: ArgumentParser
	{
		public override bool RequiresTerminator => true;

		protected bool TryGetDigits(string pattern,
			int startIndex,
			out string digits)
		{
			int i;
			for (i = startIndex; i < pattern.Length; ++i)
			{
				if (!char.IsDigit(pattern, i))
				{
					if (i != startIndex || pattern[i] != '-')
					{
						break;
					}
				}
			}

			if (i == startIndex)
			{
				digits = null;
				return false;
			}

			digits = pattern.Substring(startIndex, i - startIndex);
			return true;
		}
	}
}