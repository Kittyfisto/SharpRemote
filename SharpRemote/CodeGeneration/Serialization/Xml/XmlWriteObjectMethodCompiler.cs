using System;
using System.Reflection.Emit;

namespace SharpRemote.CodeGeneration.Serialization.Xml
{
	internal sealed class XmlWriteObjectMethodCompiler
		: AbstractWriteObjectMethodCompiler
	{
		public XmlWriteObjectMethodCompiler(CompilationContext context)
			: base(context)
		{
		}

		protected override void EmitWriteNull(ILGenerator generator)
		{}

		protected override void EmitWriteTypeInformation(ILGenerator generator)
		{
			throw new NotImplementedException();
		}
	}
}