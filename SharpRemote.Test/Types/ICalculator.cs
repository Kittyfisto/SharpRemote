using System;
using System.Diagnostics.Contracts;

namespace SharpRemote.Test.Types
{
	public interface ICalculator
		: IDisposable
	{
		bool IsDisposed { get; }

		[Pure]
		double Add(double x, double y);
	}
}