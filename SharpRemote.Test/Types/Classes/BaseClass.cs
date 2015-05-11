using System.Runtime.Serialization;

namespace SharpRemote.Test.Types.Classes
{
	[DataContract]
	public abstract class BaseClass
	{
		[DataMember]
		public double A;
	}
}