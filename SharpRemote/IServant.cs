using System.IO;

namespace SharpRemote
{
	public interface IServant
		: IGrain
	{
		/// <summary>
		/// The subject who's methods are being invoked.
		/// </summary>
		object Subject { get; }

		/// <summary>
		/// Shall invoke the method named <paramref name="methodName"/> on the <see cref="Subject"/>.
		/// </summary>
		/// <param name="methodName"></param>
		/// <param name="reader"></param>
		/// <param name="writer"></param>
		void InvokeMethod(string methodName, BinaryReader reader, BinaryWriter writer);
	}
}