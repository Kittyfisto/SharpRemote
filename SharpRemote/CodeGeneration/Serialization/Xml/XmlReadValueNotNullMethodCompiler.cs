using System;
using System.Reflection;
using System.Reflection.Emit;
using System.Xml;

namespace SharpRemote.CodeGeneration.Serialization.Xml
{
	internal sealed class XmlReadValueNotNullMethodCompiler
		: AbstractReadValueNotNullMethodCompiler
	{
		private static readonly MethodInfo XmlReaderMoveToContent;
		private static readonly MethodInfo XmlReaderReadElementContentAsString;
		private static readonly MethodInfo DecimalParse;

		static XmlReadValueNotNullMethodCompiler()
		{
			XmlReaderMoveToContent = typeof(XmlReader).GetMethod(nameof(XmlReader.MoveToContent));
			XmlReaderReadElementContentAsString = typeof(XmlReader).GetMethod(nameof(XmlReader.ReadElementContentAsString), new Type[0]);
			DecimalParse = typeof(decimal).GetMethod(nameof(decimal.Parse), new [] {typeof(string), typeof(IFormatProvider)});
		}

		public XmlReadValueNotNullMethodCompiler(CompilationContext context)
			: base(context)
		{
		}

		protected override void EmitReadByte(ILGenerator gen)
		{
		}

		protected override void EmitReadSByte(ILGenerator gen)
		{
		}

		protected override void EmitReadUShort(ILGenerator gen)
		{
		}

		protected override void EmitReadShort(ILGenerator gen)
		{
		}

		protected override void EmitReadUInt(ILGenerator gen)
		{
		}

		protected override void EmitReadInt(ILGenerator gen)
		{
		}

		protected override void EmitReadULong(ILGenerator gen)
		{
		}

		protected override void EmitReadLong(ILGenerator gen)
		{
		}

		protected override void EmitReadDecimal(ILGenerator gen)
		{
			gen.Emit(OpCodes.Ldarg_0);
			gen.Emit(OpCodes.Callvirt, XmlReaderReadElementContentAsString);

			//var tmp = gen.DeclareLocal(typeof(string));
			//gen.Emit(OpCodes.Stloc, tmp);
			//gen.Emit(OpCodes.Ldloc, tmp);
			//gen.EmitWriteLine(tmp);
			//gen.Emit(OpCodes.Ldloc, tmp);

			gen.Emit(OpCodes.Call, CultureInfoGetInvariantCulture);
			gen.Emit(OpCodes.Call, DecimalParse);
		}

		protected override void EmitReadFloat(ILGenerator gen)
		{
		}

		protected override void EmitReadDouble(ILGenerator gen)
		{
		}

		protected override void EmitReadString(ILGenerator gen)
		{
		}

		protected override void EmitBeginReadFieldOrProperty(ILGenerator gen, TypeDescription valueType, string name)
		{
			gen.Emit(OpCodes.Ldarg_0);
			gen.Emit(OpCodes.Callvirt, XmlReaderMoveToContent);
			gen.Emit(OpCodes.Pop);
		}

		protected override void EmitEndReadFieldOrProperty(ILGenerator gen, TypeDescription valueType, string name)
		{
		}

		protected override void EmitReadHintAndGrainId(ILGenerator generator)
		{
		}
	}
}