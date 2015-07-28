using System;
using System.Collections.Generic;

namespace ConsoleApplication1
{
	internal sealed class DataListener : IDataListener
	{
		private readonly int _desiredSteps;
		private readonly List<object> _values;
		private readonly string _name;

		public DataListener(string name, int desiredSteps)
		{
			_name = name;
			_desiredSteps = desiredSteps;
			_values = new List<object>();
		}

		public bool Finished { get; set; }

		public void Process(object data)
		{
			_values.Add(data);
			if (_values.Count == _desiredSteps)
			{
				Finished = true;
			}

			const int stepSize = 10000;
			if (_values.Count%stepSize == 0)
			{
				Console.WriteLine("{0}: {1}k packets", _name, _values.Count/1000);
			}
		}
	}
}