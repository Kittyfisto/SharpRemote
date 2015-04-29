﻿using System;
using System.IO;

namespace SharpRemote
{
	public interface ISerializer
	{
		void RegisterType<T>();
		void WriteObject(BinaryWriter writer, object value);
		object ReadObject(BinaryReader reader);

		Exception ReadException(BinaryReader reader);
		void WriteException(BinaryWriter writer, Exception e);
	}
}