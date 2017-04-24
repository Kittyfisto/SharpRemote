using System;
using System.Globalization;
using SharpRemote.WebApi.Requests;

namespace SharpRemote.WebApi.Routes
{
	internal sealed class Int32Parser
		: ArgumentParser
	{
		public override object Extract(string str, int start, out int consumed)
		{
			var tmp = str.Substring(start);

			int value;
			if (Int32.TryParse(tmp, NumberStyles.Integer, CultureInfo.CurrentCulture, out value))
			{
				var digits = (int)Math.Ceiling(Math.Log10(value));
				consumed = digits;
				return value;
			}

			consumed = 0;
			return null;
		}
	}
}