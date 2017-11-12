using System.Reflection;
using System.Reflection.Emit;
using System.Xml;

namespace SharpRemote.CodeGeneration.Serialization.Xml
{
	internal sealed class XmlReadObjectMethodCompiler
		: AbstractReadObjectMethodCompiler
	{
		private static readonly MethodInfo XmlReaderHasValue;

		static XmlReadObjectMethodCompiler()
		{
			XmlReaderHasValue = typeof(XmlReader).GetProperty(nameof(XmlReader.HasValue)).GetMethod;
		}

		public XmlReadObjectMethodCompiler(CompilationContext context)
			: base(context)
		{
		}

		protected override void EmitReadIsNull(ILGenerator gen)
		{
			gen.Emit(OpCodes.Ldarg_0);
			gen.Emit(OpCodes.Callvirt, XmlReaderHasValue);
		}
	}
}