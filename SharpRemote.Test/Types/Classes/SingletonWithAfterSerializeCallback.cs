using System;
using System.Runtime.Serialization;
using SharpRemote.Attributes;

namespace SharpRemote.Test.Types.Classes
{
	[DataContract]
	public sealed class SingletonWithAfterSerializeCallback
	{
		[SingletonFactoryMethod]
		public static SingletonWithAfterSerializeCallback SingletonFactoryMethod()
		{
			throw new NotImplementedException();
		}

		[AfterSerialize]
		public static void AfterSerialize()
		{

		}
	}
}