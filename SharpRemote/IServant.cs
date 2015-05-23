namespace SharpRemote
{
	public interface IServant
		: IGrain
	{
		/// <summary>
		/// The subject who's methods are being invoked.
		/// </summary>
		object Subject { get; }
	}
}