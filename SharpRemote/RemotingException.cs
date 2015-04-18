using System;

namespace SharpRemote
{
	public class RemotingException
		: SystemException
	{
		public RemotingException()
		{}

		public RemotingException(string message)
			: base(message)
		{}
	}
}