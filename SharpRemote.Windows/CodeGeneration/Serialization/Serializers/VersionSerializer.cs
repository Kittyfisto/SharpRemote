using System;
using System.Reflection;
using System.Reflection.Emit;

namespace SharpRemote.CodeGeneration.Serialization.Serializers
{
	internal sealed class VersionSerializer
		: AbstractTypeSerializer
	{
		private readonly ConstructorInfo _ctor;
		private readonly MethodInfo _getMajor;
		private readonly MethodInfo _getMinor;
		private readonly MethodInfo _getBuild;
		private readonly MethodInfo _getRevision;

		public VersionSerializer()
		{
			_ctor = typeof (Version).GetConstructor(new[] {typeof (int), typeof (int), typeof (int), typeof (int)});
			_getMajor = typeof(Version).GetProperty("Major").GetMethod;
			_getMinor = typeof(Version).GetProperty("Minor").GetMethod;
			_getBuild = typeof(Version).GetProperty("Build").GetMethod;
			_getRevision = typeof(Version).GetProperty("Revision").GetMethod;
		}

		public override bool Supports(Type type)
		{
			return type == typeof (Version);
		}

		public override void EmitWriteValue(ILGenerator gen, Serializer serializerCompiler, Action loadWriter,
		                                    Action loadValue,
		                                    Action loadValueAddress, Action loadSerializer, Type type,
		                                    bool valueCanBeNull = true)
		{
			loadWriter();
			loadValue();
			gen.Emit(OpCodes.Call, _getMajor);
			gen.Emit(OpCodes.Call, Methods.WriteInt32);

			loadWriter();
			loadValue();
			gen.Emit(OpCodes.Call, _getMinor);
			gen.Emit(OpCodes.Call, Methods.WriteInt32);

			loadWriter();
			loadValue();
			gen.Emit(OpCodes.Call, _getBuild);
			gen.Emit(OpCodes.Call, Methods.WriteInt32);

			loadWriter();
			loadValue();
			gen.Emit(OpCodes.Call, _getRevision);
			gen.Emit(OpCodes.Call, Methods.WriteInt32);
		}

		public override void EmitReadValue(ILGenerator gen, Serializer serializerCompiler, Action loadReader,
		                                   Action loadSerializer, Type type,
		                                   bool valueCanBeNull = true)
		{
			loadReader();
			gen.Emit(OpCodes.Call, Methods.ReadInt32);

			loadReader();
			gen.Emit(OpCodes.Call, Methods.ReadInt32);

			loadReader();
			gen.Emit(OpCodes.Call, Methods.ReadInt32);

			loadReader();
			gen.Emit(OpCodes.Call, Methods.ReadInt32);

			gen.Emit(OpCodes.Newobj, _ctor);
		}
	}
}