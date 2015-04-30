﻿using System;
using System.Runtime.Serialization;

namespace SharpRemote
{
	[Serializable]
	public class RemotingException
		: SystemException
	{
		public RemotingException()
		{}

		public RemotingException(SerializationInfo info, StreamingContext context)
			: base(info, context)
		{}

		public RemotingException(string message)
			: base(message)
		{}
	}
}