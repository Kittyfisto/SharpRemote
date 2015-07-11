using System;
using System.Collections.Generic;
using System.Threading;

namespace SharpRemote.Test.Types.Interfaces
{
	public sealed class OrderedInterface
		: IOrderInterface
	{
		public readonly List<int> InstanceOrderedSequence;
		public readonly List<int> MethodOrderedSequence;
		public readonly List<int> TypeOrderedSequence;
		public readonly List<int> UnorderedSequence;
		private Thread _currentThread;

		public OrderedInterface()
		{
			UnorderedSequence = new List<int>();
			TypeOrderedSequence = new List<int>();
			InstanceOrderedSequence = new List<int>();
			MethodOrderedSequence = new List<int>();
		}

		public void Unordered(int sequence)
		{
			lock (UnorderedSequence)
				UnorderedSequence.Add(sequence);
		}

		public void TypeOrdered(int sequence)
		{
			if (_currentThread != null)
				throw new InvalidOperationException("");

			_currentThread = Thread.CurrentThread;
			try
			{
				TypeOrderedSequence.Add(sequence);
			}
			finally
			{
				_currentThread = null;
			}
		}

		public void InstanceOrdered(int sequence)
		{
			InstanceOrderedSequence.Add(sequence);
		}

		public void MethodOrdered(int sequence)
		{
			MethodOrderedSequence.Add(sequence);
		}
	}
}