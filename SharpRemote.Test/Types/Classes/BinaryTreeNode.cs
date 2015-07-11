using System.Runtime.Serialization;

namespace SharpRemote.Test.Types.Classes
{
	[DataContract]
	public sealed class BinaryTreeNode
	{
		[DataMember] public BinaryTreeNode Left;

		[DataMember] public BinaryTreeNode Right;
		[DataMember] public double Value;

		public bool Equals(BinaryTreeNode other)
		{
			if (ReferenceEquals(null, other)) return false;
			if (ReferenceEquals(this, other)) return true;
			return Equals(Value, other.Value) && Equals(Left, other.Left) && Equals(Right, other.Right);
		}

		public override bool Equals(object obj)
		{
			if (ReferenceEquals(null, obj)) return false;
			if (ReferenceEquals(this, obj)) return true;
			return obj is BinaryTreeNode && Equals((BinaryTreeNode) obj);
		}

		public override int GetHashCode()
		{
			unchecked
			{
				int hashCode = Value.GetHashCode();
				hashCode = (hashCode*397) ^ (Left != null ? Left.GetHashCode() : 0);
				hashCode = (hashCode*397) ^ (Right != null ? Right.GetHashCode() : 0);
				return hashCode;
			}
		}

		public static bool operator ==(BinaryTreeNode left, BinaryTreeNode right)
		{
			return Equals(left, right);
		}

		public static bool operator !=(BinaryTreeNode left, BinaryTreeNode right)
		{
			return !Equals(left, right);
		}
	}
}