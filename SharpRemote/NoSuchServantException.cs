namespace SharpRemote
{
	public class NoSuchServantException : RemotingException
	{
		public NoSuchServantException(ulong objectId)
			: base(string.Format("No such servant: {0}", objectId))
		{}
	}
}