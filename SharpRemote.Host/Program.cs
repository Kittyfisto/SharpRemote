using SharpRemote.Hosting;

namespace SharpRemote.Host
{
	internal class Program
	{
		private static void Main(string[] args)
		{
			using (var silo = new ProcessSiloServer(args))
			{
				silo.Run();
			}
		}
	}
}