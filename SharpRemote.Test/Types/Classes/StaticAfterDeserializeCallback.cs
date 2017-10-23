using System.Runtime.Serialization;
using SharpRemote.Attributes;

namespace SharpRemote.Test.Types.Classes
{
	[DataContract]
	public sealed class StaticAfterDeserializeCallback
	{
		[AfterDeserialize]
		public static void AfterDeserialize()
		{

		}
	}
}