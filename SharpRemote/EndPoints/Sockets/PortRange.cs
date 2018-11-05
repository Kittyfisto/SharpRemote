namespace SharpRemote.EndPoints.Sockets
{
	struct PortRange
	{
		public ushort StartPort;
		public ushort NumberOfPorts;

		public PortRange(ushort start, ushort count)
		{
			StartPort = start;
			NumberOfPorts = count;
		}
	}
}