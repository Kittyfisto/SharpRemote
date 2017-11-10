using System.Reflection.Emit;

namespace SharpRemote.CodeGeneration.Serialization.Xml
{
	internal sealed class XmlWriteValueMethodCompiler
		: AbstractWriteValueMethodCompiler
	{
		public XmlWriteValueMethodCompiler(CompilationContext context) : base(context)
		{
		}

		protected override void EmitWriteHasValue(ILGenerator generator)
		{
			throw new System.NotImplementedException();
		}

		protected override void EmitWriteNoValue(ILGenerator generator)
		{
			throw new System.NotImplementedException();
		}
	}
}