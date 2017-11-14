using System;
using System.Reflection;
using System.Reflection.Emit;
using System.Xml;

namespace SharpRemote.CodeGeneration.Serialization.Xml
{
	internal sealed class XmlReadValueMethodCompiler
		: AbstractReadValueMethodCompiler
	{
		private static readonly MethodInfo XmlReaderRead;
		private static readonly MethodInfo XmlReaderGetName;
		private static readonly MethodInfo XmlReaderReadElementContentAsString;
		private static readonly MethodInfo XmlReaderMoveToAttributeByName;
		private static readonly MethodInfo XmlSerializerReadDecimal;
		private static readonly MethodInfo XmlSerializerReadString;
		private static readonly MethodInfo XmlSerializerReadByte;
		private static readonly MethodInfo XmlSerializerReadSByte;
		private static readonly MethodInfo XmlSerializerReadInt16;
		private static readonly MethodInfo XmlSerializerReadUInt16;
		private static readonly MethodInfo XmlSerializerReadInt32;
		private static readonly MethodInfo XmlSerializerReadUInt32;
		private static readonly MethodInfo XmlSerializerReadInt64;
		private static readonly MethodInfo XmlSerializerReadUInt64;
		private static readonly MethodInfo XmlSerializerReadSingle;
		private static readonly MethodInfo XmlSerializerReadDouble;
		private static readonly ConstructorInfo XmlParseExceptionCtor;
		private static readonly MethodInfo XmlLineInfoGetLineNumber;
		private static readonly MethodInfo XmlLineInfoGetLinePosition;

		static XmlReadValueMethodCompiler()
		{
			XmlReaderRead = typeof(XmlReader).GetMethod(nameof(XmlReader.Read));
			XmlReaderGetName = typeof(XmlReader).GetProperty(nameof(XmlReader.Name)).GetMethod;
			XmlReaderReadElementContentAsString = typeof(XmlReader).GetMethod(nameof(XmlReader.ReadElementContentAsString), new Type[0]);
			XmlReaderMoveToAttributeByName = typeof(XmlReader).GetMethod(nameof(XmlReader.MoveToAttribute), new [] {typeof(string)});
			XmlSerializerReadDecimal = typeof(XmlSerializer).GetMethod(nameof(XmlSerializer.ReadValueAsDecimal));
			XmlSerializerReadString = typeof(XmlSerializer).GetMethod(nameof(XmlSerializer.ReadValueAsString));
			XmlSerializerReadByte = typeof(XmlSerializer).GetMethod(nameof(XmlSerializer.ReadValueAsByte));
			XmlSerializerReadSByte = typeof(XmlSerializer).GetMethod(nameof(XmlSerializer.ReadValueAsSByte));
			XmlSerializerReadInt16 = typeof(XmlSerializer).GetMethod(nameof(XmlSerializer.ReadValueAsInt16));
			XmlSerializerReadUInt16 = typeof(XmlSerializer).GetMethod(nameof(XmlSerializer.ReadValueAsUInt16));
			XmlSerializerReadInt32 = typeof(XmlSerializer).GetMethod(nameof(XmlSerializer.ReadValueAsInt32));
			XmlSerializerReadUInt32 = typeof(XmlSerializer).GetMethod(nameof(XmlSerializer.ReadValueAsUInt32));
			XmlSerializerReadInt64 = typeof(XmlSerializer).GetMethod(nameof(XmlSerializer.ReadValueAsInt64));
			XmlSerializerReadUInt64 = typeof(XmlSerializer).GetMethod(nameof(XmlSerializer.ReadValueAsUInt64));
			XmlSerializerReadSingle = typeof(XmlSerializer).GetMethod(nameof(XmlSerializer.ReadValueAsSingle));
			XmlSerializerReadDouble = typeof(XmlSerializer).GetMethod(nameof(XmlSerializer.ReadValueAsDouble));
			XmlLineInfoGetLineNumber = typeof(IXmlLineInfo).GetProperty(nameof(IXmlLineInfo.LineNumber)).GetMethod;
			XmlLineInfoGetLinePosition = typeof(IXmlLineInfo).GetProperty(nameof(IXmlLineInfo.LinePosition)).GetMethod;
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
			gen.Emit(OpCodes.Ldarg_0);
			gen.Emit(OpCodes.Call, XmlSerializerReadByte);
		}

		protected override void EmitReadSByte(ILGenerator gen)
		{
			gen.Emit(OpCodes.Ldarg_0);
			gen.Emit(OpCodes.Call, XmlSerializerReadSByte);
		}

		protected override void EmitReadUInt16(ILGenerator gen)
		{
			gen.Emit(OpCodes.Ldarg_0);
			gen.Emit(OpCodes.Call, XmlSerializerReadUInt16);
		}

		protected override void EmitReadInt16(ILGenerator gen)
		{
			gen.Emit(OpCodes.Ldarg_0);
			gen.Emit(OpCodes.Call, XmlSerializerReadInt16);
		}

		protected override void EmitReadUInt32(ILGenerator gen)
		{
			gen.Emit(OpCodes.Ldarg_0);
			gen.Emit(OpCodes.Call, XmlSerializerReadUInt32);
		}

		protected override void EmitReadInt32(ILGenerator gen)
		{
			gen.Emit(OpCodes.Ldarg_0);
			gen.Emit(OpCodes.Call, XmlSerializerReadInt32);
		}

		protected override void EmitReadUInt64(ILGenerator gen)
		{
			gen.Emit(OpCodes.Ldarg_0);
			gen.Emit(OpCodes.Call, XmlSerializerReadUInt64);
		}

		protected override void EmitReadInt64(ILGenerator gen)
		{
			gen.Emit(OpCodes.Ldarg_0);
			gen.Emit(OpCodes.Call, XmlSerializerReadInt64);
		}

		protected override void EmitReadDecimal(ILGenerator gen)
		{
			gen.Emit(OpCodes.Ldarg_0);
			gen.Emit(OpCodes.Call, XmlSerializerReadDecimal);
		}

		protected override void EmitReadFloat(ILGenerator gen)
		{
			gen.Emit(OpCodes.Ldarg_0);
			gen.Emit(OpCodes.Call, XmlSerializerReadSingle);
		}

		protected override void EmitReadDouble(ILGenerator gen)
		{
			gen.Emit(OpCodes.Ldarg_0);
			gen.Emit(OpCodes.Call, XmlSerializerReadDouble);
		}

		protected override void EmitReadString(ILGenerator gen)
		{
			gen.Emit(OpCodes.Ldarg_0);
			gen.Emit(OpCodes.Call, XmlSerializerReadString);
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