using System.Runtime.Serialization;

namespace SharpRemote.Test.Types.Interfaces.Classes
{
	[DataContract]
	public abstract class BaseClass
	{
		[DataMember]
		public double A;
	}
}