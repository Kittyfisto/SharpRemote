using System;

namespace SharpRemote.Clock
{
	/// <summary>
	/// The interface responsible to translate <see cref="DateTime"/> values from a remote clock into <see cref="DateTime"/>
	/// values of a local clock.
	/// </summary>
	public interface ITimer
	{
		/// <summary>
		/// The absolute drift between the two clocks that would be present without correction
		/// since this timer has been created.
		/// </summary>
		TimeSpan AbsoluteDrift { get; }

		/// <summary>
		/// Adds a time-measurement 
		/// </summary>
		/// <param name="localMinimum"></param>
		/// <param name="remote"></param>
		/// <param name="localMaximum"></param>
		void AddTimeMeasurement(DateTime localMinimum, DateTime remote, DateTime localMaximum);

		/// <summary>
		/// Translates the given <see cref="DateTime"/> value from the remote source
		/// into a local <see cref="DateTime"/> value so that both values represent
		/// the same point in time (but account for the fact that both sources may use different clocks).
		/// </summary>
		/// <param name="remoteTime"></param>
		/// <param name="accuracy"></param>
		/// <returns></returns>
		DateTime ToLocalTime(DateTime remoteTime, out TimeSpan accuracy);
	}
}