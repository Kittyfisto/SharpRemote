using System.Reflection;
using System.Xml;

namespace SharpRemote.CodeGeneration.Serialization.Xml
{
	internal sealed class XmlWriteObjectMethodCompiler
		: AbstractWriteObjectMethodCompiler
	{
		private static readonly MethodInfo XmlWriterWriteAttributeString;
		private readonly CompilationContext _context;

		static XmlWriteObjectMethodCompiler()
		{
			XmlWriterWriteAttributeString = typeof(XmlWriter).GetMethod(nameof(XmlWriter.WriteAttributeString), new[] { typeof(string), typeof(string) });
		}

		public XmlWriteObjectMethodCompiler(CompilationContext context)
			: base(context)
		{
			_context = context;
		}

		//protected override void EmitWriteNull(ILGenerator generator)
		//{
		//	// We don't require a special marker to find that out...
		//}
		//
		//protected override void EmitWriteTypeInformation(ILGenerator generator)
		//{
		//	// XmlWriter.WriteAttributeString("Type", "...");
		//	generator.Emit(OpCodes.Ldarg_0);
		//	generator.Emit(OpCodes.Ldstr, XmlMethodCallWriter.ArgumentTypeAttributeName);
		//	generator.Emit(OpCodes.Ldstr, _context.TypeDescription.AssemblyQualifiedName);
		//	generator.Emit(OpCodes.Callvirt, XmlWriterWriteAttributeString);
		//}
	}
}