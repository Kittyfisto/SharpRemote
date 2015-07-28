using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using SharpRemote.Tasks;

namespace SharpRemote.CodeGeneration.Remoting
{
	internal sealed class ServantCompiler
		: Compiler
	{
		private readonly List<KeyValuePair<EventInfo, MethodInfo>> _eventInvocationMethods;
		private readonly Dictionary<MethodInfo, FieldBuilder> _perMethodSchedulers;
		private readonly ModuleBuilder _module;
		private readonly FieldBuilder _subject;
		private readonly TypeBuilder _typeBuilder;
		private FieldBuilder _perTypeScheduler;
		private FieldBuilder _perObjectScheduler;

		public ServantCompiler(Serializer serializer,
		                       ModuleBuilder module,
		                       string subjectTypeName,
		                       Type interfaceType)
			: base(serializer, interfaceType)
		{
			if (module == null) throw new ArgumentNullException("module");
			if (subjectTypeName == null) throw new ArgumentNullException("subjectTypeName");

			_module = module;

			_typeBuilder = _module.DefineType(subjectTypeName, TypeAttributes.Class, typeof (object));
			_typeBuilder.AddInterfaceImplementation(typeof (IServant));

			_perMethodSchedulers = new Dictionary<MethodInfo, FieldBuilder>();

			_eventInvocationMethods = new List<KeyValuePair<EventInfo, MethodInfo>>();

			_subject = _typeBuilder.DefineField("_subject", interfaceType, FieldAttributes.Private | FieldAttributes.InitOnly);
			ObjectId = _typeBuilder.DefineField("_objectId", typeof (ulong), FieldAttributes.Private | FieldAttributes.InitOnly);
			EndPoint = _typeBuilder.DefineField("_endPoint", typeof (IRemotingEndPoint),
			                                    FieldAttributes.Private | FieldAttributes.InitOnly);
			Channel = _typeBuilder.DefineField("_channel", typeof (IEndPointChannel),
			                                   FieldAttributes.Private | FieldAttributes.InitOnly);
			Serializer = _typeBuilder.DefineField("_serializer", typeof (ISerializer),
			                                      FieldAttributes.Private | FieldAttributes.InitOnly);
		}

		private MethodInfo[] AllMethods
		{
			get
			{
				MethodInfo[] allEventMethods =
					InterfaceType.GetEvents(BindingFlags.FlattenHierarchy | BindingFlags.Instance | BindingFlags.Public)
					             .SelectMany(x => new[] {x.AddMethod, x.RemoveMethod})
					             .ToArray();

				return InterfaceType
					.GetMethods(BindingFlags.FlattenHierarchy | BindingFlags.Instance | BindingFlags.Public)
					.Concat(
						InterfaceType.GetInterfaces()
						             .SelectMany(
							             x => x.GetMethods(BindingFlags.FlattenHierarchy | BindingFlags.Instance | BindingFlags.Public)))
					.Where(x => !allEventMethods.Contains(x))
					.OrderBy(x => x.Name)
					.ToArray();
			}
		}

		private void GenerateGetSerializer()
		{
			MethodBuilder method = _typeBuilder.DefineMethod("get_Serializer",
			                                                 MethodAttributes.Public | MethodAttributes.Virtual |
			                                                 MethodAttributes.Final, typeof (ISerializer), null);
			ILGenerator gen = method.GetILGenerator();

			gen.Emit(OpCodes.Ldarg_0);
			gen.Emit(OpCodes.Ldfld, Serializer);
			gen.Emit(OpCodes.Ret);

			_typeBuilder.DefineMethodOverride(method, Methods.GrainGetSerializer);
		}

		private void GenerateGetObjectId()
		{
			MethodBuilder method = _typeBuilder.DefineMethod("get_Id",
			                                                 MethodAttributes.Public | MethodAttributes.Virtual |
			                                                 MethodAttributes.Final, typeof (ulong), null);
			ILGenerator gen = method.GetILGenerator();

			gen.Emit(OpCodes.Ldarg_0);
			gen.Emit(OpCodes.Ldfld, ObjectId);
			gen.Emit(OpCodes.Ret);

			_typeBuilder.DefineMethodOverride(method, Methods.GrainGetObjectId);
		}

		public Type Generate()
		{
			GenerateCctor();
			GenerateEvents();
			GenerateCtor();
			GenerateGetObjectId();
			GenerateGetSerializer();
			GenerateGetSubject();
			GenerateInvoke();
			GenerateGetTaskScheduler();
			GenerateInterfaceType();

			Type proxyType = _typeBuilder.CreateType();
			return proxyType;
		}

		private void GenerateInterfaceType()
		{
			MethodBuilder getInterfaceType = _typeBuilder.DefineMethod("get_InterfaceType",
			                                                           MethodAttributes.Public | MethodAttributes.Virtual |
			                                                           MethodAttributes.Final
			                                                           , typeof (Type), null);
			ILGenerator gen = getInterfaceType.GetILGenerator();
			gen.Emit(OpCodes.Ldtoken, InterfaceType);
			gen.Emit(OpCodes.Call, Methods.TypeGetTypeFromHandle);
			gen.Emit(OpCodes.Ret);

			_typeBuilder.DefineMethodOverride(getInterfaceType, Methods.GrainGetInterfaceType);
		}

		private void GenerateEvents()
		{
			// For every event we have to compile a method that essentially does the same that the proxy compiler
			// does for interface methods: serialize the arguments into a stream and then call IEndPointChannel.Invoke
			EventInfo[] allEvents = InterfaceType.GetEvents();
			foreach (EventInfo @event in allEvents)
			{
				GenerateEvent(@event);
			}
		}

		private void GenerateEvent(EventInfo @event)
		{
			Type delegateType = @event.EventHandlerType;
			MethodInfo methodInfo = delegateType.GetMethod("Invoke");

			string methodName = string.Format("On{0}", @event.Name);
			ParameterInfo[] parameters = methodInfo.GetParameters();
			Type returnType = typeof (void);
			MethodBuilder method = _typeBuilder.DefineMethod(methodName,
			                                                 MethodAttributes.Public,
			                                                 returnType,
			                                                 parameters.Select(x => x.ParameterType).ToArray());

			GenerateMethodInvocation(method, InterfaceType.FullName, @event.Name, parameters, method);

			_eventInvocationMethods.Add(new KeyValuePair<EventInfo, MethodInfo>(@event, method));
		}

		private void GenerateGetSubject()
		{
			MethodBuilder method = _typeBuilder.DefineMethod("get_Subject",
			                                                 MethodAttributes.Public | MethodAttributes.Virtual |
			                                                 MethodAttributes.Final,
			                                                 typeof (object),
			                                                 null);

			ILGenerator gen = method.GetILGenerator();
			gen.Emit(OpCodes.Ldarg_0);
			gen.Emit(OpCodes.Ldfld, _subject);
			gen.Emit(OpCodes.Ret);

			_typeBuilder.DefineMethodOverride(method, Methods.ServantGetSubject);
		}

		private void GenerateGetTaskScheduler()
		{
			MethodBuilder method = _typeBuilder.DefineMethod("GetTaskScheduler",
			                                                 MethodAttributes.Public | MethodAttributes.Final | MethodAttributes.Virtual,
			                                                 typeof (SerialTaskScheduler),
			                                                 new[] {typeof (string)});

			ILGenerator gen = method.GetILGenerator();

			LocalBuilder name = gen.DeclareLocal(typeof (string));
			Label @throw = gen.DefineLabel();
			Label @ret = gen.DefineLabel();

			// if (method == null) goto ret
			gen.Emit(OpCodes.Ldarg_1);
			gen.Emit(OpCodes.Stloc, name);
			gen.Emit(OpCodes.Ldloc, name);
			gen.Emit(OpCodes.Brfalse, @throw);

			MethodInfo[] allMethods = AllMethods;

			var labels = new Label[allMethods.Length];
			int index = 0;
			foreach (MethodInfo methodInfo in allMethods)
			{
				gen.Emit(OpCodes.Ldloc, name);
				gen.Emit(OpCodes.Ldstr, methodInfo.Name);
				gen.Emit(OpCodes.Call, Methods.StringEquality);

				Label @true = gen.DefineLabel();
				labels[index++] = @true;
				gen.Emit(OpCodes.Brtrue, @true);
			}

			gen.Emit(OpCodes.Br, @throw);

			for (int i = 0; i < allMethods.Length; ++i)
			{
				MethodInfo methodInfo = allMethods[i];
				Label label = labels[i];

				gen.MarkLabel(label);
				var attribute = methodInfo.GetCustomAttribute<InvokeAttribute>();
				var strategy = attribute != null ? attribute.DispatchingStrategy : Dispatch.DoNotSerialize;

				switch (strategy)
				{
					case Dispatch.DoNotSerialize:
						gen.Emit(OpCodes.Ldnull);
						break;

					case Dispatch.SerializePerMethod:
						gen.Emit(OpCodes.Ldarg_0);
						gen.Emit(OpCodes.Ldfld, _perMethodSchedulers[methodInfo]);
						break;

					case Dispatch.SerializePerObject:
						gen.Emit(OpCodes.Ldarg_0);
						gen.Emit(OpCodes.Ldfld, _perObjectScheduler);
						break;

					case Dispatch.SerializePerType:
						gen.Emit(OpCodes.Ldsfld, _perTypeScheduler);
						break;

					default:
						throw new InvalidEnumArgumentException("InvokeAttribute.DispatchingStrategy", (int)strategy, typeof(Dispatch));
				}

				gen.Emit(OpCodes.Br, @ret);
			}

			gen.MarkLabel(@throw);
			gen.Emit(OpCodes.Ldstr, "Method '{0}' not found");
			gen.Emit(OpCodes.Ldarg_1);
			gen.Emit(OpCodes.Call, Methods.StringFormatOneObject);
			gen.Emit(OpCodes.Newobj, Methods.ArgumentExceptionCtor);
			gen.Emit(OpCodes.Throw);

			gen.MarkLabel(@ret);
			gen.Emit(OpCodes.Ret);

			_typeBuilder.DefineMethodOverride(method, Methods.GrainGetTaskScheduler);
		}

		private void GenerateInvoke()
		{
			MethodBuilder method = _typeBuilder.DefineMethod("Invoke",
			                                                 MethodAttributes.Public | MethodAttributes.Final | MethodAttributes.Virtual,
			                                                 typeof (void),
			                                                 new[]
				                                                 {
					                                                 typeof (string),
					                                                 typeof (BinaryReader),
					                                                 typeof (BinaryWriter)
				                                                 });

			ILGenerator gen = method.GetILGenerator();

			LocalBuilder name = gen.DeclareLocal(typeof (string));
			Label @throw = gen.DefineLabel();
			Label @ret = gen.DefineLabel();

			// if (method == null) goto ret
			gen.Emit(OpCodes.Ldarg_1);
			gen.Emit(OpCodes.Stloc, name);
			gen.Emit(OpCodes.Ldloc, name);
			gen.Emit(OpCodes.Brfalse, @throw);

			MethodInfo[] allMethods = AllMethods;

			var labels = new Label[allMethods.Length];
			int index = 0;
			foreach (MethodInfo methodInfo in allMethods)
			{
				gen.Emit(OpCodes.Ldloc, name);
				gen.Emit(OpCodes.Ldstr, methodInfo.Name);
				gen.Emit(OpCodes.Call, Methods.StringEquality);

				Label @true = gen.DefineLabel();
				labels[index++] = @true;
				gen.Emit(OpCodes.Brtrue, @true);
			}

			gen.Emit(OpCodes.Br, @throw);

			for (int i = 0; i < allMethods.Length; ++i)
			{
				MethodInfo methodInfo = allMethods[i];
				Label label = labels[i];
				MethodInfo extractingMethod = CompileExtractingMethod(methodInfo);

				gen.MarkLabel(label);
				gen.Emit(OpCodes.Ldarg_0);
				gen.Emit(OpCodes.Ldarg_2);
				gen.Emit(OpCodes.Ldarg_3);
				gen.Emit(OpCodes.Call, extractingMethod);

				gen.Emit(OpCodes.Br, @ret);
			}

			gen.MarkLabel(@throw);
			gen.Emit(OpCodes.Ldstr, "Method '{0}' not found");
			gen.Emit(OpCodes.Ldarg_1);
			gen.Emit(OpCodes.Call, Methods.StringFormatOneObject);
			gen.Emit(OpCodes.Newobj, Methods.ArgumentExceptionCtor);
			gen.Emit(OpCodes.Throw);

			gen.MarkLabel(@ret);
			gen.Emit(OpCodes.Ldarg_3);
			gen.Emit(OpCodes.Call, Methods.BinaryWriterFlush);
			gen.Emit(OpCodes.Ret);

			_typeBuilder.DefineMethodOverride(method, Methods.GrainInvoke);
		}

		private MethodInfo CompileExtractingMethod(MethodInfo originalMethod)
		{
			MethodBuilder method = _typeBuilder.DefineMethod(originalMethod.Name, MethodAttributes.Private,
			                                                 typeof (void),
			                                                 new[]
				                                                 {
					                                                 typeof (BinaryReader),
					                                                 typeof (BinaryWriter)
				                                                 });

			ILGenerator gen = method.GetILGenerator();

			gen.Emit(OpCodes.Ldarg_0);
			gen.Emit(OpCodes.Ldfld, _subject);
			ExtractArgumentsAndCallMethod(gen,
			                              originalMethod,
			                              () => gen.Emit(OpCodes.Ldarg_1),
			                              () => gen.Emit(OpCodes.Ldarg_2));

			gen.Emit(OpCodes.Ret);
			return method;
		}

		private void GenerateCctor()
		{
			var serializePerType = AllMethods.Where(x => x.GetCustomAttribute<InvokeAttribute>() != null &&
			                                             x.GetCustomAttribute<InvokeAttribute>().DispatchingStrategy ==
			                                             Dispatch.SerializePerType)
														 .ToList();
			if (serializePerType.Count == 0)
				return;

			_perTypeScheduler = _typeBuilder.DefineField(
				"PerTypeScheduler",
				typeof (SerialTaskScheduler),
				FieldAttributes.Static | FieldAttributes.Private | FieldAttributes.InitOnly
				);
			var builder = _typeBuilder.DefineTypeInitializer();
			var gen = builder.GetILGenerator();
			gen.Emit(OpCodes.Ldc_I4_0);
			gen.Emit(OpCodes.Newobj, Methods.SerialTaskSchedulerCtor);
			gen.Emit(OpCodes.Stsfld, _perTypeScheduler);
			gen.Emit(OpCodes.Ret);
		}

		private void GenerateCtor()
		{
			ConstructorBuilder builder = _typeBuilder.DefineConstructor(MethodAttributes.Public, CallingConventions.Standard,
			                                                            new[]
				                                                            {
					                                                            typeof (ulong),
					                                                            typeof (IRemotingEndPoint),
					                                                            typeof (IEndPointChannel),
					                                                            typeof (ISerializer),
					                                                            InterfaceType
				                                                            });

			ILGenerator gen = builder.GetILGenerator();
			gen.Emit(OpCodes.Ldarg_0);
			gen.Emit(OpCodes.Call, Methods.ObjectCtor);

			gen.Emit(OpCodes.Ldarg_0);
			gen.Emit(OpCodes.Ldarg_1);
			gen.Emit(OpCodes.Stfld, ObjectId);

			gen.Emit(OpCodes.Ldarg_0);
			gen.Emit(OpCodes.Ldarg_2);
			gen.Emit(OpCodes.Stfld, EndPoint);

			gen.Emit(OpCodes.Ldarg_0);
			gen.Emit(OpCodes.Ldarg_3);
			gen.Emit(OpCodes.Stfld, Channel);

			gen.Emit(OpCodes.Ldarg_0);
			gen.Emit(OpCodes.Ldarg, 4);
			gen.Emit(OpCodes.Stfld, Serializer);

			gen.Emit(OpCodes.Ldarg_0);
			gen.Emit(OpCodes.Ldarg, 5);
			gen.Emit(OpCodes.Stfld, _subject);

			foreach (var pair in _eventInvocationMethods)
			{
				MethodInfo eventAddMethod = pair.Key.AddMethod;
				Type delegateType = pair.Key.EventHandlerType;
				MethodInfo onEventMethod = pair.Value;

				AddOnFireEvent(gen, eventAddMethod, delegateType, onEventMethod);
			}

			var serializePerObject = AllMethods.Where(x => x.GetCustomAttribute<InvokeAttribute>() != null &&
														 x.GetCustomAttribute<InvokeAttribute>().DispatchingStrategy ==
														 Dispatch.SerializePerObject)
														 .ToList();
			if (serializePerObject.Count > 0)
			{
				_perObjectScheduler = _typeBuilder.DefineField("_perObjectScheduler", typeof (SerialTaskScheduler),
				                                               FieldAttributes.Private | FieldAttributes.InitOnly);

				gen.Emit(OpCodes.Ldarg_0);
				gen.Emit(OpCodes.Ldc_I4_0);
				gen.Emit(OpCodes.Newobj, Methods.SerialTaskSchedulerCtor);
				gen.Emit(OpCodes.Stfld, _perObjectScheduler);
			}

			var serializePerMethod = AllMethods.Where(x => x.GetCustomAttribute<InvokeAttribute>() != null &&
			                                               x.GetCustomAttribute<InvokeAttribute>().DispatchingStrategy ==
			                                               Dispatch.SerializePerMethod)
			                                   .ToList();
			foreach (var method in serializePerMethod)
			{
				var scheduler = _typeBuilder.DefineField(string.Format("_{0}", method.Name),
					typeof(SerialTaskScheduler),
					FieldAttributes.Private | FieldAttributes.InitOnly);

				gen.Emit(OpCodes.Ldarg_0);
				gen.Emit(OpCodes.Ldc_I4_0);
				gen.Emit(OpCodes.Newobj, Methods.SerialTaskSchedulerCtor);
				gen.Emit(OpCodes.Stfld, scheduler);

				_perMethodSchedulers.Add(method, scheduler);
			}

			gen.Emit(OpCodes.Ret);
		}

		private void AddOnFireEvent(ILGenerator gen, MethodInfo eventAddMethod, Type delegateType, MethodInfo onEventMethod)
		{
			// We need to find the constructor of the Action/Delegate that we're creating....
			ConstructorInfo ctor = delegateType.GetConstructor(new[] {typeof (object), typeof (IntPtr)});
			if (ctor == null)
				throw new NotImplementedException(
					string.Format("Could not find a suitable constructor for delegate '{0}' with an (object, IntPtr) signature",
					              delegateType));

			gen.Emit(OpCodes.Ldarg_0);
			gen.Emit(OpCodes.Ldfld, _subject);
			gen.Emit(OpCodes.Ldarg_0);
			gen.Emit(OpCodes.Ldftn, onEventMethod);
			gen.Emit(OpCodes.Newobj, ctor);
			gen.Emit(OpCodes.Callvirt, eventAddMethod);
		}
	}
}