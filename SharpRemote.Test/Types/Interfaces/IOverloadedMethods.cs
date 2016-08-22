namespace SharpRemote.Test.Types.Interfaces
{
	public interface IOverloadedMethods
	{
		void Print(string value);
		void Print(object fmt, params object[] values);
	}
}