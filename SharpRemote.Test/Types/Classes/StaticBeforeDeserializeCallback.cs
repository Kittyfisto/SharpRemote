using System.Runtime.Serialization;
using SharpRemote.Attributes;

namespace SharpRemote.Test.Types.Classes
{
	[DataContract]
	public sealed class StaticBeforeDeserializeCallback
	{
		[BeforeDeserialize]
		public static void BeforeDeserialize()
		{

		}
	}
}