using System.Runtime.Serialization;
using SharpRemote.Attributes;

namespace SharpRemote.Test.Types.Classes
{
	[DataContract]
	public sealed class AfterDeserializeCallbackWithParameters
	{
		[AfterDeserialize]
		public void AfterDeserialize(int a, float b, double c)
		{ }
	}
}