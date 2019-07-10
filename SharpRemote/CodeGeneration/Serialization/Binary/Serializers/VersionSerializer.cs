using System;
using System.Reflection;
using System.Reflection.Emit;

namespace SharpRemote.CodeGeneration.Serialization.Binary.Serializers
{
	internal sealed class VersionSerializer
		: AbstractTypeSerializer
	{
		private readonly MethodInfo _getMajor;
		private readonly MethodInfo _getMinor;
		private readonly MethodInfo _getBuild;
		private readonly MethodInfo _getRevision;
		private readonly ConstructorInfo _ctorMajorMinor;
		private readonly ConstructorInfo _ctorMajorMinorBuild;
		private readonly ConstructorInfo _ctorMajorMinorBuildRevision;

		public VersionSerializer()
		{
			_ctorMajorMinor = typeof(Version).GetConstructor(new[] { typeof(int), typeof(int) });
			_ctorMajorMinorBuild = typeof(Version).GetConstructor(new[] { typeof(int), typeof(int), typeof(int) });
			_ctorMajorMinorBuildRevision = typeof(Version).GetConstructor(new[] { typeof(int), typeof(int), typeof(int), typeof(int) });

			_getMajor = typeof(Version).GetProperty("Major").GetMethod;
			_getMinor = typeof(Version).GetProperty("Minor").GetMethod;
			_getBuild = typeof(Version).GetProperty("Build").GetMethod;
			_getRevision = typeof(Version).GetProperty("Revision").GetMethod;
		}

		public override bool Supports(Type type)
		{
			return type == typeof (Version);
		}

		public override void EmitWriteValue(ILGenerator gen,
		                                    ISerializerCompiler serializerCompiler,
		                                    Action loadWriter,
		                                    Action loadValue,
		                                    Action loadValueAddress,
		                                    Action loadSerializer,
		                                    Action loadRemotingEndPoint,
		                                    Type type,
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

		public override void EmitReadValue(ILGenerator gen,
		                                   ISerializerCompiler serializerCompiler,
		                                   Action loadReader,
		                                   Action loadSerializer,
		                                   Action loadRemotingEndPoint,
		                                   Type type,
		                                   bool valueCanBeNull = true)
		{
			var major = gen.DeclareLocal(typeof(int));
			loadReader();
			gen.Emit(OpCodes.Call, Methods.ReadInt32);
			gen.Emit(OpCodes.Stloc, major);

			var minor = gen.DeclareLocal(typeof(int));
			loadReader();
			gen.Emit(OpCodes.Call, Methods.ReadInt32);
			gen.Emit(OpCodes.Stloc, minor);

			var build = gen.DeclareLocal(typeof(int));
			loadReader();
			gen.Emit(OpCodes.Call, Methods.ReadInt32);
			gen.Emit(OpCodes.Stloc, build);

			var revision = gen.DeclareLocal(typeof(int));
			loadReader();
			gen.Emit(OpCodes.Call, Methods.ReadInt32);
			gen.Emit(OpCodes.Stloc, revision);

			var end = gen.DefineLabel();
			var majorMinor = gen.DefineLabel();
			gen.Emit(OpCodes.Ldloc, build);
			gen.Emit(OpCodes.Ldc_I4_0);
			gen.Emit(OpCodes.Blt, majorMinor);

			var majorMinorBuild = gen.DefineLabel();
			gen.Emit(OpCodes.Ldloc, revision);
			gen.Emit(OpCodes.Ldc_I4_0);
			gen.Emit(OpCodes.Blt, majorMinorBuild);

			var all = gen.DefineLabel();
			gen.Emit(OpCodes.Br, all);

			// Thanks for not making this easy...

			gen.MarkLabel(majorMinor);
			gen.Emit(OpCodes.Ldloc, major);
			gen.Emit(OpCodes.Ldloc, minor);
			gen.Emit(OpCodes.Newobj, _ctorMajorMinor);
			gen.Emit(OpCodes.Br, end);

			gen.MarkLabel(majorMinorBuild);
			gen.Emit(OpCodes.Ldloc, major);
			gen.Emit(OpCodes.Ldloc, minor);
			gen.Emit(OpCodes.Ldloc, build);
			gen.Emit(OpCodes.Newobj, _ctorMajorMinorBuild);
			gen.Emit(OpCodes.Br, end);

			gen.MarkLabel(all);
			gen.Emit(OpCodes.Ldloc, major);
			gen.Emit(OpCodes.Ldloc, minor);
			gen.Emit(OpCodes.Ldloc, build);
			gen.Emit(OpCodes.Ldloc, revision);
			gen.Emit(OpCodes.Newobj, _ctorMajorMinorBuildRevision);
			gen.Emit(OpCodes.Br, end);

			gen.MarkLabel(end);
		}
	}
}