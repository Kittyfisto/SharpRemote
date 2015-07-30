namespace ConsoleApplication1
{
	public sealed class Worker
		: IWorker
	{
		public long Work(long value)
		{
			return ~value;
		}
	}
}