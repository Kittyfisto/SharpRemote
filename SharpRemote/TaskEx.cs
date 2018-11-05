using System;
using System.Threading;
using System.Threading.Tasks;

namespace SharpRemote
{
	/// <summary>
	///     Extension methods to the awesome <see cref="Task" /> class.
	/// </summary>
	public static class TaskEx
	{
		/// <summary>
		///     Creates a new task which returns the result (or exception)
		///     of the given <paramref name="task" /> in case it finishes
		///     in the given <paramref name="timeout" />. If it does't, then
		///     the returned task throws a <see cref="TimeoutException" />.
		/// </summary>
		/// <param name="task"></param>
		/// <param name="timeout"></param>
		/// <returns></returns>
		public static async Task TimeoutAfter(Task task, TimeSpan timeout)
		{
			var cancellationTokenSource = new CancellationTokenSource();
			var timer = Task.Delay(timeout, cancellationTokenSource.Token);
			var winner = await Task.WhenAny(task, timer);
			if (winner == timer)
				throw new TimeoutException();

			// The timer didn't win so we should cancel it
			cancellationTokenSource.Cancel();
		}

		/// <summary>
		///     Creates a new task which returns the result (or exception)
		///     of the given <paramref name="task" /> in case it finishes
		///     in the given <paramref name="timeout" />. If it does't, then
		///     the returned task throws a <see cref="TimeoutException" />.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="task"></param>
		/// <param name="timeout"></param>
		/// <returns></returns>
		public static async Task<T> TimeoutAfter<T>(Task<T> task, TimeSpan timeout)
		{
			var cancellationTokenSource = new CancellationTokenSource();
			var timer = Task.Delay(timeout, cancellationTokenSource.Token);
			var winner = await Task.WhenAny(task, timer);
			if (winner == timer)
				throw new TimeoutException();

			// The timer didn't win so we should cancel it
			cancellationTokenSource.Cancel();

			return task.Result;
		}
	}
}