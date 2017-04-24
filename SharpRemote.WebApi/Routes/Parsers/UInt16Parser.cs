using System;
using System.Globalization;

namespace SharpRemote.WebApi.Routes.Parsers
{
	internal sealed class UInt16Parser
		: IntegerParser
	{
		public override Type Type => typeof(ushort);

		public override bool TryExtract(string str,
			int startIndex,
			out object value,
			out int consumed)
		{
			string digits;
			if (TryGetDigits(str, startIndex, out digits))
			{
				ushort number;
				if (ushort.TryParse(digits, NumberStyles.Integer, CultureInfo.InvariantCulture, out number))
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