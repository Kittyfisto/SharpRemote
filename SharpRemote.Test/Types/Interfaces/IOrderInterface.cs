namespace SharpRemote.Test.Types.Interfaces
{
	public interface IOrderInterface
	{
		void Unordered(int sequence);

		[Invoke(Dispatch.SerializePerType)]
		void TypeOrdered(int sequence);

		[Invoke(Dispatch.SerializePerObject)]
		void InstanceOrdered(int sequence);

		[Invoke(Dispatch.SerializePerMethod)]
		void MethodOrdered(int sequence);
	}
}