using System.Runtime.Serialization;

namespace SharpRemote.Test.CodeGeneration.Types.Structs
{
	[DataContract]
	public abstract class BaseClass
	{
		[DataMember]
		public double A;
	}
}