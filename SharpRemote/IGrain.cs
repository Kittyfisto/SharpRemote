namespace SharpRemote
{
	public interface IGrain
	{
		ulong ObjectId { get; }
		ISerializer Serializer { get; }
	}
}