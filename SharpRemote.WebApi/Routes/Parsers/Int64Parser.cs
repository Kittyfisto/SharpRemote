using System;
using System.Globalization;

namespace SharpRemote.WebApi.Routes.Parsers
{
	internal sealed class Int64Parser
		: IntegerParser
	{
		public override Type Type => typeof(long);

		public override bool TryExtract(string str,
			int startIndex,
			out object value,
			out int consumed)
		{
			string digits;
			if (TryGetDigits(str, startIndex, out digits))
			{
				long number;
				if (long.TryParse(digits, NumberStyles.Integer, CultureInfo.InvariantCulture, out number))
				{
					value = number;
					consumed = digits.Length;
					return true;
				}
			}

			consumed = 0;
			value = null;
			return false;
		}
	}
}