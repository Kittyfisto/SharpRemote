using System;
using System.Reflection;
using System.Reflection.Emit;
using System.Xml;

namespace SharpRemote.CodeGeneration.Serialization.Xml
{
	internal sealed class XmlWriteValueMethodCompiler
		: AbstractWriteValueMethodCompiler
	{
		private static readonly MethodInfo XmlWriterWriteStartElement;
		private static readonly MethodInfo XmlWriterWriteEndElement;
		private static readonly MethodInfo XmlWriterWriteStringValue;
		private static readonly MethodInfo XmlWriterWriteAttributeString;
		private static readonly MethodInfo XmlSerializerWriteDecimal;
		private static readonly MethodInfo DecimalToString;
		private readonly CompilationContext _context;

		static XmlWriteValueMethodCompiler()
		{
			XmlWriterWriteStartElement = typeof(XmlWriter).GetMethod(nameof(XmlWriter.WriteStartElement), new [] {typeof(string)});
			XmlWriterWriteEndElement = typeof(XmlWriter).GetMethod(nameof(XmlWriter.WriteEndElement));
			XmlWriterWriteStringValue = typeof(XmlWriter).GetMethod(nameof(XmlWriter.WriteValue), new[] {typeof(string)});
			XmlWriterWriteAttributeString = typeof(XmlWriter).GetMethod(nameof(XmlWriter.WriteAttributeString), new [] {typeof(string), typeof(string)});
			XmlSerializerWriteDecimal = typeof(XmlSerializer).GetMethod(nameof(XmlSerializer.WriteValue), new [] {typeof(XmlWriter), typeof(decimal)});
			DecimalToString = typeof(decimal).GetMethod(nameof(decimal.ToString), new [] {typeof(IFormatProvider)});
		}

		public XmlWriteValueMethodCompiler(CompilationContext context) : base(context)
		{
			_context = context;
		}

		protected override void EmitWriteHint(ILGenerator generator, ByReferenceHint hint)
		{ }

		protected override void EmitBeginWriteField(ILGenerator gen, FieldDescription field)
		{
			// XmlWriter.WriteStartElement("Field")
			gen.Emit(OpCodes.Ldarg_0);
			gen.Emit(OpCodes.Ldstr, XmlSerializer.FieldElementName);
			gen.Emit(OpCodes.Callvirt, XmlWriterWriteStartElement);
			gen.Emit(OpCodes.Ldarg_0);
			gen.Emit(OpCodes.Ldstr, XmlSerializer.NameAttributeName);
			gen.Emit(OpCodes.Ldstr, field.Name);
			gen.Emit(OpCodes.Callvirt, XmlWriterWriteAttributeString);
		}

		protected override void EmitEndWriteField(ILGenerator gen, FieldDescription field)
		{
			// XmlWriter.WriteEndElement()
			gen.Emit(OpCodes.Ldarg_0);
			gen.Emit(OpCodes.Callvirt, XmlWriterWriteEndElement);
		}

		protected override void EmitBeginWriteProperty(ILGenerator gen, PropertyDescription property)
		{
			// XmlWriter.WriteEndElement()
			gen.Emit(OpCodes.Ldarg_0);
			gen.Emit(OpCodes.Ldstr, XmlSerializer.PropertyElementName);
			gen.Emit(OpCodes.Callvirt, XmlWriterWriteStartElement);
			gen.Emit(OpCodes.Ldarg_0);
			gen.Emit(OpCodes.Ldstr, XmlSerializer.NameAttributeName);
			gen.Emit(OpCodes.Ldstr, property.Name);
			gen.Emit(OpCodes.Callvirt, XmlWriterWriteAttributeString);
		}

		protected override void EmitEndWriteProperty(ILGenerator gen, PropertyDescription property)
		{
			// XmlWriter.WriteEndElement()
			gen.Emit(OpCodes.Ldarg_0);
			gen.Emit(OpCodes.Callvirt, XmlWriterWriteEndElement);
		}

		protected override void EmitWriteByte(ILGenerator gen, Action loadValue)
		{
			throw new NotImplementedException();
		}

		protected override void EmitWriteSByte(ILGenerator gen, Action loadValue)
		{
			throw new NotImplementedException();
		}

		protected override void EmitWriteUShort(ILGenerator gen, Action loadValue)
		{
			throw new NotImplementedException();
		}

		protected override void EmitWriteShort(ILGenerator gen, Action loadValue)
		{
			throw new NotImplementedException();
		}

		protected override void EmitWriteUInt(ILGenerator gen, Action loadValue)
		{
			throw new NotImplementedException();
		}

		protected override void EmitWriteInt(ILGenerator gen, Action loadValue)
		{
			throw new NotImplementedException();
		}

		protected override void EmitWriteULong(ILGenerator gen, Action loadValue)
		{
			throw new NotImplementedException();
		}

		protected override void EmitWriteLong(ILGenerator gen, Action loadValue)
		{
			throw new NotImplementedException();
		}

		protected override void EmitWriteDecimal(ILGenerator gen, IMemberDescription member, Action loadMember, Action loadMemberAddress)
		{
			// XmlSerializer.WriteValue(decimal)
			gen.Emit(OpCodes.Ldarg_0);
			loadMember();
			gen.Emit(OpCodes.Call, XmlSerializerWriteDecimal);
		}

		protected override void EmitWriteFloat(ILGenerator gen, Action loadValue)
		{
			throw new NotImplementedException();
		}

		protected override void EmitWriteDouble(ILGenerator gen, Action loadValue)
		{
			throw new NotImplementedException();
		}

		protected override void EmitWriteString(ILGenerator gen, Action loadValue)
		{
			throw new NotImplementedException();
		}

		protected override void EmitWriteObjectId(ILGenerator generator, LocalBuilder proxy)
		{}
	}
}