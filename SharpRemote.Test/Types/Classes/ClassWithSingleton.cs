using System.Runtime.Serialization;

namespace SharpRemote.Test.Types.Classes
{
	[DataContract]
	public sealed class ClassWithSingleton
	{
		[DataMember] public Singleton That;

		#region Public Methods

		public override bool Equals(object obj)
		{
			if (ReferenceEquals(null, obj)) return false;
			if (ReferenceEquals(this, obj)) return true;
			return obj is ClassWithSingleton && Equals((ClassWithSingleton) obj);
		}

		public override int GetHashCode()
		{
			return (That != null ? That.GetHashCode() : 0);
		}

		#endregion

		private bool Equals(ClassWithSingleton other)
		{
			return Equals(That, other.That);
		}
	}
}