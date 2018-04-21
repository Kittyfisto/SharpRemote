using System;
using System.Threading.Tasks;

namespace SharpRemote.Test
{
	public static class TaskEx
	{
		public static Task Failed(Exception e)
		{
			return Failed<int>(e);
		}

		public static Task<T> Failed<T>(Exception e)
		{
			var source = new TaskCompletionSource<T>();
			source.SetException(e);
			return source.Task;
		}
	}
}