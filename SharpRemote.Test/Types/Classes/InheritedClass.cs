using System.Runtime.Serialization;

namespace SharpRemote.Test.Types.Classes
{
	[DataContract]
	public sealed class InheritedClass
		: NonSealedClass
	{
		/// <summary>
		/// 
		/// </summary>
		[DataMember]
		public new long Value1 { get; set; }
	}
}