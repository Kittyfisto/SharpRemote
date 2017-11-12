using System;
using System.Reflection;
using System.Reflection.Emit;
using System.Xml;

namespace SharpRemote.CodeGeneration.Serialization.Xml
{
	internal sealed class XmlReadValueMethodCompiler
		: AbstractReadValueMethodCompiler
	{
		private static readonly MethodInfo XmlReaderMoveToContent;
		private static readonly MethodInfo XmlReaderRead;
		private static readonly MethodInfo XmlReaderGetName;
		private static readonly MethodInfo XmlReaderReadElementContentAsString;
		private static readonly MethodInfo XmlReaderMoveToAttributeByName;
		private static readonly MethodInfo XmlSerializerReadDecimal;
		private static readonly MethodInfo DecimalParse;
		private static readonly ConstructorInfo XmlParseExceptionCtor;
		private static readonly MethodInfo XmlLineInfoGetLineNumber;
		private static readonly MethodInfo XmlLineInfoGetLinePosition;

		static XmlReadValueMethodCompiler()
		{
			XmlReaderRead = typeof(XmlReader).GetMethod(nameof(XmlReader.Read));
			XmlReaderGetName = typeof(XmlReader).GetProperty(nameof(XmlReader.Name)).GetMethod;
			XmlReaderMoveToContent = typeof(XmlReader).GetMethod(nameof(XmlReader.MoveToContent));
			XmlReaderReadElementContentAsString = typeof(XmlReader).GetMethod(nameof(XmlReader.ReadElementContentAsString), new Type[0]);
			XmlReaderMoveToAttributeByName = typeof(XmlReader).GetMethod(nameof(XmlReader.MoveToAttribute), new [] {typeof(string)});
			XmlSerializerReadDecimal = typeof(XmlSerializer).GetMethod(nameof(XmlSerializer.ReadValueAsDecimal));
			XmlLineInfoGetLineNumber = typeof(IXmlLineInfo).GetProperty(nameof(IXmlLineInfo.LineNumber)).GetMethod;
			XmlLineInfoGetLinePosition = typeof(IXmlLineInfo).GetProperty(nameof(IXmlLineInfo.LinePosition)).GetMethod;
			DecimalParse = typeof(decimal).GetMethod(nameof(decimal.Parse), new [] {typeof(string), typeof(IFormatProvider)});
			XmlParseExceptionCtor = typeof(XmlParseException).GetConstructor(new [] {typeof(string), typeof(int), typeof(int), typeof(Exception)});
		}

		public XmlReadValueMethodCompiler(CompilationContext context)
			: base(context)
		{
		}

		protected override void EmitEndReadProperty(ILGenerator gen, PropertyDescription property)
		{
			throw new NotImplementedException();
		}

		protected override void EmitBeginRead(ILGenerator gen)
		{
			gen.Emit(OpCodes.Ldarg_0);
			gen.Emit(OpCodes.Callvirt, XmlReaderRead);
			gen.Emit(OpCodes.Pop);
		}

		protected override void EmitEndRead(ILGenerator gen)
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
			gen.Emit(OpCodes.Call, XmlSerializerReadDecimal);
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

		protected override void EmitBeginReadField(ILGenerator gen, FieldDescription field)
		{
			var correctField = gen.DefineLabel();
			var actualElementName = gen.DeclareLocal(typeof(string));

			// If reader.Name == FieldElementName goto correctField
			gen.Emit(OpCodes.Ldarg_0);
			gen.Emit(OpCodes.Callvirt, XmlReaderGetName);
			gen.Emit(OpCodes.Stloc, actualElementName);
			gen.Emit(OpCodes.Ldloc, actualElementName);
			gen.Emit(OpCodes.Ldstr, XmlSerializer.FieldElementName);
			gen.Emit(OpCodes.Call, StringEquals);
			gen.Emit(OpCodes.Brtrue_S, correctField);
			// throw new XmlParseException
			EmitThrowXmlParseException(gen, actualElementName);

			gen.MarkLabel(correctField);
			gen.Emit(OpCodes.Ldarg_0);
			gen.Emit(OpCodes.Ldstr, field.Name);
			gen.Emit(OpCodes.Callvirt, XmlReaderMoveToAttributeByName);
			gen.Emit(OpCodes.Pop);
		}

		private void EmitThrowXmlParseException(ILGenerator gen, LocalBuilder actualElementName)
		{
			gen.Emit(OpCodes.Ldstr, "Expected to find element '"+ XmlSerializer.FieldElementName + "', but found '{0}' instead!");
			gen.Emit(OpCodes.Ldloc, actualElementName);
			gen.Emit(OpCodes.Call, StringFormatObject);
			gen.Emit(OpCodes.Ldarg_0);
			gen.Emit(OpCodes.Castclass, typeof(IXmlLineInfo));
			gen.Emit(OpCodes.Callvirt, XmlLineInfoGetLineNumber);
			gen.Emit(OpCodes.Ldarg_0);
			gen.Emit(OpCodes.Castclass, typeof(IXmlLineInfo));
			gen.Emit(OpCodes.Callvirt, XmlLineInfoGetLinePosition);
			gen.Emit(OpCodes.Ldnull);
			gen.Emit(OpCodes.Newobj, XmlParseExceptionCtor);
			gen.Emit(OpCodes.Throw);
		}

		protected override void EmitEndReadField(ILGenerator gen, FieldDescription field)
		{
			gen.Emit(OpCodes.Ldarg_0);
			gen.Emit(OpCodes.Callvirt, XmlReaderRead);
			gen.Emit(OpCodes.Pop);
		}

		protected override void EmitBeginReadProperty(ILGenerator gen, PropertyDescription property)
		{
			throw new NotImplementedException();
		}

		protected override void EmitReadHintAndGrainId(ILGenerator generator)
		{
		}
	}
}