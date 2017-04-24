using System;
using System.Globalization;

namespace SharpRemote.WebApi.Routes.Parsers
{
	internal sealed class SByteParser
		: IntegerParser
	{
		public override Type Type => typeof(sbyte);

		public override bool TryExtract(string str,
			int startIndex,
			out object value,
			out int consumed)
		{
			string digits;
			if (TryGetDigits(str, startIndex, out digits))
			{
				sbyte number;
				if (sbyte.TryParse(digits, NumberStyles.Integer, CultureInfo.InvariantCulture, out number))
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