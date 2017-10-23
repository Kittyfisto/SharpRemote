using System;
using System.Runtime.Serialization;
using SharpRemote.Attributes;

namespace SharpRemote.Test.Types.Classes
{
	[DataContract]
	public sealed class SingletonWithAfterDeserializeCallback
	{
		[SingletonFactoryMethod]
		public static SingletonWithAfterDeserializeCallback SingletonFactoryMethod()
		{
			throw new NotImplementedException();
		}

		[AfterDeserialize]
		public static void AfterDeserialize()
		{

		}
	}
}