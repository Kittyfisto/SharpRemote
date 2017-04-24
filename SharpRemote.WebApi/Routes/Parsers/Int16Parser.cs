using System;
using System.Globalization;

namespace SharpRemote.WebApi.Routes.Parsers
{
	internal sealed class Int16Parser
		: ArgumentParser
	{
		public override bool TryExtract(string str,
			int start,
			out object value,
			out int consumed)
		{
			var tmp = str.Substring(start);

			short number;
			if (short.TryParse(tmp, NumberStyles.Integer, CultureInfo.CurrentCulture, out number))
			{
				int digits;
				switch (number)
				{
					case 0:
						digits = 1;
						break;

					case short.MinValue:
						digits = 5;
						break;

					default:
						digits = (int)Math.Floor(Math.Log10(Math.Abs(number)) + 1);
						break;
				}
				if (number < 0)
					++digits;
				consumed = digits;
				value = number;
				return true;
			}

			consumed = 0;
			value = null;
			return false;
		}
	}
}