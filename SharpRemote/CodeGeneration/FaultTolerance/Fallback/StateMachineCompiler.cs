using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace SharpRemote.CodeGeneration.FaultTolerance.Fallback
{
	/// <summary>
	///     Responsible for creating a state machine capable of intercepting failed tasks and delegating
	///     calls to another new task, in case of failure.
	/// </summary>
	internal sealed class StateMachineCompiler
	{
		private readonly FieldBuilder _fallbackField;
		private readonly Type _interfaceType;
		private readonly FieldBuilder _taskCompletionSource;
		private readonly TypeBuilder _stateMachine;
		private readonly Type _taskCompletionSourceType;
		private readonly FieldBuilder _originalTask;
		private readonly FieldBuilder _fallbackTask;
		private readonly Type _taskType;
		private readonly FieldBuilder _taskField;
		private readonly MethodInfo _taskContinueWith;
		private readonly Type _taskReturnType;
		private readonly bool _hasReturnValue;
		private readonly MethodInfo _taskCompletionSourceSetResult;
		private readonly List<FieldInfo> _arguments;
		private readonly MethodInfo _originalMethod;
		private readonly MethodInfo _taskCompletionSourceSetException;
		private readonly MethodInfo _taskCompletionSourceSetExceptions;
		private readonly MethodInfo _taskGetAwaiter;
		private readonly Type _awaiterType;
		private readonly MethodInfo _awaiterOnCompleted;
		private readonly MethodInfo _taskGetResult;
		private MethodInfo _taskGetException;

		public StateMachineCompiler(TypeBuilder typeBuilder,
		                            ITypeDescription interfaceDescription,
		                            IMethodDescription methodDescription)
		{
			_originalMethod = methodDescription.Method;
			_taskType = methodDescription.ReturnType.Type;
			_taskGetAwaiter = _taskType.GetMethod(nameof(Task.GetAwaiter));
			_taskGetException = _taskType.GetProperty(nameof(Task.Exception)).GetMethod;

			_awaiterType = GetAwaiterType(_taskType);
			_awaiterOnCompleted = _awaiterType.GetMethod(nameof(TaskAwaiter.OnCompleted),
			                                               new[] {typeof(Action)});

			_taskReturnType = GetTaskReturnType(_taskType);
			_hasReturnValue = _taskReturnType != typeof(void);
			if (_hasReturnValue)
			{
				_taskGetResult = _taskType.GetProperty(nameof(Task<int>.Result)).GetMethod;
			}

			_interfaceType = interfaceDescription.Type;
			_taskCompletionSourceType = GetTaskCompletionSourceType(_taskType);
			_taskCompletionSourceSetResult = _taskCompletionSourceType.GetMethod(nameof(TaskCompletionSource<int>.SetResult));
			_taskCompletionSourceSetException = _taskCompletionSourceType.GetMethod(nameof(TaskCompletionSource<int>.SetException),
			                                                                        new []{typeof(Exception)});
			_taskCompletionSourceSetExceptions= _taskCompletionSourceType.GetMethod(nameof(TaskCompletionSource<int>.SetException),
			                                                                        new []{typeof(IEnumerable<Exception>)});

			var name = string.Format("{0}_StateMachine", methodDescription.Name);
			_stateMachine = typeBuilder.DefineNestedType(name);

			_originalTask =
				_stateMachine.DefineField("_originalTask", _taskType, FieldAttributes.Private | FieldAttributes.InitOnly);
			_fallbackTask = _stateMachine.DefineField("_fallbackTask", _taskType, FieldAttributes.Private);
			_fallbackField =
				_stateMachine.DefineField("_fallback", _interfaceType, FieldAttributes.Private | FieldAttributes.InitOnly);
			_taskCompletionSource = _stateMachine.DefineField("_taskCompletionSource", _taskCompletionSourceType,
			                                         FieldAttributes.Private | FieldAttributes.InitOnly);
			_taskField = _stateMachine.DefineField("Task", _taskType,
			                                       FieldAttributes.Public | FieldAttributes.InitOnly);

			_taskContinueWith = _taskType.GetMethod(nameof(Task.ContinueWith),
			                                        new[] {_taskType, typeof(TaskContinuationOptions)});

			_arguments = new List<FieldInfo>(methodDescription.Parameters.Count);
			foreach (var parameter in methodDescription.Parameters)
			{
				var field = _stateMachine.DefineField(parameter.Name,
				                                      parameter.ParameterType.Type,
				                                      FieldAttributes.Private | FieldAttributes.InitOnly);
				_arguments.Add(field);
			}
		}

		public ConstructorInfo Compile(out FieldInfo taskField)
		{
			var failMethodCall = CreateFailMethodCall();
			var onFallbackCompleted = CreateOnFallbackCompleted(failMethodCall);
			var invokeFallback = CreateInvokeFallback(onFallbackCompleted, failMethodCall);
			var onSubjectCompleted = CreateOnSubjectCompleted(invokeFallback);
			var constructor = CreateConstructor(onSubjectCompleted);
			_stateMachine.CreateType();

			taskField = _taskField;
			return constructor;
		}

		private MethodInfo CreateFailMethodCall()
		{
			var method = _stateMachine.DefineMethod("FailMethodCall",
			                                        MethodAttributes.Private,
			                                        CallingConventions.Standard | CallingConventions.HasThis,
			                                        typeof(void),
			                                        new[] {typeof(Exception)});

			var gen = method.GetILGenerator();

			var aggregateException = gen.DeclareLocal(typeof(AggregateException));
			var innerExceptions = gen.DeclareLocal(typeof(Exception[]));

			var end = gen.DefineLabel();
			var notAggregateException = gen.DefineLabel();

			// var aggregateException = e as AggregateException
			gen.Emit(OpCodes.Ldarg_1);
			gen.Emit(OpCodes.Isinst, typeof(AggregateException));
			gen.Emit(OpCodes.Stloc, aggregateException);

			// if (aggregateException != null)
			gen.Emit(OpCodes.Ldloc, aggregateException);
			gen.Emit(OpCodes.Ldnull);
			gen.Emit(OpCodes.Cgt_Un);
			gen.Emit(OpCodes.Brfalse_S, notAggregateException);

			// var exceptions = aggregateException.InnerExceptions;
			var aggregateExceptionGetInnerExceptions = typeof(AggregateException)
			                                           .GetProperty(nameof(AggregateException.InnerExceptions))
			                                           .GetMethod;
			gen.Emit(OpCodes.Ldloc, aggregateException);
			gen.Emit(OpCodes.Callvirt, aggregateExceptionGetInnerExceptions);
			gen.Emit(OpCodes.Stloc, innerExceptions);

			gen.Emit(OpCodes.Ldarg_0);
			gen.Emit(OpCodes.Ldfld, _taskCompletionSource);
			gen.Emit(OpCodes.Ldloc, innerExceptions);
			gen.Emit(OpCodes.Callvirt, _taskCompletionSourceSetExceptions);
			gen.Emit(OpCodes.Br_S, end);

			gen.MarkLabel(notAggregateException);
			gen.Emit(OpCodes.Ldarg_0);
			gen.Emit(OpCodes.Ldfld, _taskCompletionSource);
			gen.Emit(OpCodes.Ldarg_1);
			gen.Emit(OpCodes.Call, _taskCompletionSourceSetException);

			gen.MarkLabel(end);
			gen.Emit(OpCodes.Ret);

			return method;
		}

		private MethodInfo CreateOnFallbackCompleted(MethodInfo failMethodCall)
		{
			var method = _stateMachine.DefineMethod("OnFallbackCompleted",
			                                        MethodAttributes.Private,
			                                        typeof(void),
			                                        new Type[0]);

			var gen = method.GetILGenerator();
			var exception = gen.DeclareLocal(typeof(Exception));

			// try {...
			gen.BeginExceptionBlock();
			if (_hasReturnValue)
			{
				// var exception = _fallbackTask.Exception
				//gen.Emit(OpCodes.Ldarg_0);
				//gen.Emit(OpCodes.Ldfld, _fallbackTask);
				//gen.Emit(OpCodes.Callvirt, _taskGetException);
				//gen.Emit(OpCodes.Stloc, exception);

				// if (exception != null) FailMethodCall(exception)

				// _taskCompletionSource.SetResult(_fallbackTask.Result);
				gen.Emit(OpCodes.Ldarg_0);
				gen.Emit(OpCodes.Ldfld, _taskCompletionSource);
				gen.Emit(OpCodes.Ldarg_0);
				gen.Emit(OpCodes.Ldfld, _fallbackTask);
				gen.Emit(OpCodes.Callvirt, _taskGetResult);
				gen.Emit(OpCodes.Call, _taskCompletionSourceSetResult);
			}
			else
			{
				// _fallbackTask.Wait()
				gen.Emit(OpCodes.Ldarg_0);
				gen.Emit(OpCodes.Ldfld, _fallbackTask);
				gen.Emit(OpCodes.Callvirt, Methods.TaskWait);

				// _taskCompletionSource.SetResult(42);
				gen.Emit(OpCodes.Ldarg_0);
				gen.Emit(OpCodes.Ldfld, _taskCompletionSource);
				gen.Emit(OpCodes.Ldc_I4, 42);
				gen.Emit(OpCodes.Call, _taskCompletionSourceSetResult);
			}

			// cach(Exception e) { FailMethodCall(e); }
			gen.BeginCatchBlock(typeof(Exception));
			gen.Emit(OpCodes.Stloc, exception);
			gen.Emit(OpCodes.Ldarg_0);
			gen.Emit(OpCodes.Ldloc, exception);
			gen.Emit(OpCodes.Call, failMethodCall);


			gen.EndExceptionBlock();

			gen.Emit(OpCodes.Ret);

			return method;
		}

		private MethodInfo CreateInvokeFallback(MethodInfo onFallbackCompleted, MethodInfo failMethodCall)
		{
			var method = _stateMachine.DefineMethod("InvokeFallback",
			                                        MethodAttributes.Private,
			                                        typeof(void),
			                                        new Type[0]);

			var gen = method.GetILGenerator();
			var exception = gen.DeclareLocal(typeof(Exception));

			// try { ...
			gen.BeginExceptionBlock();

			// _fallbackTask = _fallback.Do(....)
			gen.Emit(OpCodes.Ldarg_0);

			gen.Emit(OpCodes.Ldarg_0);
			gen.Emit(OpCodes.Ldfld, _fallbackField);

			for (int i = 0; i < _arguments.Count; ++i)
			{
				gen.Emit(OpCodes.Ldarg_0);
				gen.Emit(OpCodes.Ldfld, _arguments[i]);
			}

			gen.Emit(OpCodes.Callvirt, _originalMethod);
			gen.Emit(OpCodes.Stfld, _fallbackTask);

			// var awaiter = _fallbackTask.GetAwaiter();
			gen.Emit(OpCodes.Ldarg_0);
			gen.Emit(OpCodes.Ldfld, _fallbackTask);
			var awaiter = gen.DeclareLocal(_awaiterType);
			gen.Emit(OpCodes.Callvirt, _taskGetAwaiter);
			gen.Emit(OpCodes.Stloc, awaiter);
			
			// awaiter.ContinueWith(OnFallbackCompleted)
			var actionCtor = typeof(Action).GetConstructor(new[] {typeof(object), typeof(IntPtr)});
			gen.Emit(OpCodes.Ldloca, awaiter);
			gen.Emit(OpCodes.Ldarg_0);
			gen.Emit(OpCodes.Ldftn, onFallbackCompleted);
			gen.Emit(OpCodes.Newobj, actionCtor);
			gen.Emit(OpCodes.Call, _awaiterOnCompleted);

			// cach(Exception e) { FailMethodCall(e); }
			gen.BeginCatchBlock(typeof(Exception));
			gen.Emit(OpCodes.Stloc, exception);
			gen.Emit(OpCodes.Ldarg_0);
			gen.Emit(OpCodes.Ldloc, exception);
			gen.Emit(OpCodes.Call, failMethodCall);

			gen.EndExceptionBlock();

			gen.Emit(OpCodes.Ret);

			return method;
		}

		private MethodInfo CreateOnSubjectCompleted(MethodInfo invokeFallback)
		{
			var method = _stateMachine.DefineMethod("OnSubjectCompleted",
			                                        MethodAttributes.Private,
			                                        typeof(void),
			                                        new Type[0]);

			var gen = method.GetILGenerator();

			// try { ... }
			gen.BeginExceptionBlock();

			if (_hasReturnValue)
			{
				// _taskCompletionSource.SetResult(_originalTask.Result);
				gen.Emit(OpCodes.Ldarg_0);
				gen.Emit(OpCodes.Ldfld, _taskCompletionSource);

				gen.Emit(OpCodes.Ldarg_0);
				gen.Emit(OpCodes.Ldfld, _originalTask);
				gen.Emit(OpCodes.Callvirt, _taskGetResult);

				gen.Emit(OpCodes.Callvirt, _taskCompletionSourceSetResult);
			}
			else
			{
				// _originalTask.Wait()
				// _taskCompletionSource.SetResult(42)
				var taskWait = _taskType.GetMethod(nameof(Task<int>.Wait), new Type[0]);
				gen.Emit(OpCodes.Ldarg_0);
				gen.Emit(OpCodes.Ldfld, _originalTask);
				gen.Emit(OpCodes.Callvirt, taskWait);

				gen.Emit(OpCodes.Ldarg_0);
				gen.Emit(OpCodes.Ldfld, _taskCompletionSource);
				gen.Emit(OpCodes.Ldc_I4, 42);
				gen.Emit(OpCodes.Callvirt, _taskCompletionSourceSetResult);
			}

			gen.BeginCatchBlock(typeof(Exception));
			// catch(Exception) { InvokeFallback(); }
			gen.Emit(OpCodes.Pop);
			gen.Emit(OpCodes.Ldarg_0);
			gen.Emit(OpCodes.Call, invokeFallback);

			gen.EndExceptionBlock();

			gen.Emit(OpCodes.Ret);

			return method;
		}

		private ConstructorInfo CreateConstructor(MethodInfo taskCallback)
		{
			var constructorArguments = new List<Type>(_arguments.Count + 2);
			constructorArguments.Add(_taskType);
			constructorArguments.Add(_interfaceType);
			constructorArguments.AddRange(_arguments.Select(x => x.FieldType));

			var constructor = _stateMachine.DefineConstructor(MethodAttributes.Public,
			                                                  CallingConventions.Standard | CallingConventions.HasThis,
			                                                  constructorArguments.ToArray());

			var gen = constructor.GetILGenerator();

			// _originalTask = task
			gen.Emit(OpCodes.Ldarg_0);
			gen.Emit(OpCodes.Ldarg_1);
			gen.Emit(OpCodes.Stfld, _originalTask);

			// _fallback = fallback
			gen.Emit(OpCodes.Ldarg_0);
			gen.Emit(OpCodes.Ldarg_2);
			gen.Emit(OpCodes.Stfld, _fallbackField);

			// _argumentN = argumentN
			for (int i = 0; i < _arguments.Count; ++i)
			{
				gen.Emit(OpCodes.Ldarg_0);
				gen.Emit(OpCodes.Ldarg, 3 + i);
				gen.Emit(OpCodes.Stfld, _arguments[i]);
			}

			// _taskCompletionSource = new TaskCompletionSource<>()
			// _task = _taskCompletionSource.Task
			gen.Emit(OpCodes.Ldarg_0);
			gen.Emit(OpCodes.Newobj, _taskCompletionSourceType.GetConstructor(new Type[0]));
			gen.Emit(OpCodes.Stfld, _taskCompletionSource);

			var sourceGetTask = _taskCompletionSourceType.GetProperty(nameof(TaskCompletionSource<int>.Task))
			                                             .GetMethod;
			gen.Emit(OpCodes.Ldarg_0);
			gen.Emit(OpCodes.Ldarg_0);
			gen.Emit(OpCodes.Ldfld, _taskCompletionSource);
			gen.Emit(OpCodes.Callvirt, sourceGetTask);
			gen.Emit(OpCodes.Stfld, _taskField);

			// task.GetAwaiter().ContinueWith(OnSubjectCompleted)
			var awaiter = gen.DeclareLocal(_awaiterType);
			gen.Emit(OpCodes.Ldarg_1);
			gen.Emit(OpCodes.Callvirt, _taskGetAwaiter);
			gen.Emit(OpCodes.Stloc, awaiter);

			gen.Emit(OpCodes.Ldloca, awaiter);
			var actionCtor = typeof(Action).GetConstructor(new[] {typeof(object), typeof(IntPtr)});
			gen.Emit(OpCodes.Ldarg_0);
			gen.Emit(OpCodes.Ldftn, taskCallback);
			gen.Emit(OpCodes.Newobj, actionCtor);
			gen.Emit(OpCodes.Callvirt, _awaiterOnCompleted);

			gen.Emit(OpCodes.Ret);


			return constructor;
		}

		private static Type GetAwaiterType(Type taskType)
		{
			if (taskType.IsGenericType) return typeof(TaskAwaiter<>).MakeGenericType(taskType.GetGenericArguments()[0]);

			return typeof(TaskAwaiter);
		}

		private static Type GetTaskCompletionSourceType(Type taskType)
		{
			if (taskType.IsGenericType) return typeof(TaskCompletionSource<>).MakeGenericType(taskType.GetGenericArguments()[0]);

			return typeof(TaskCompletionSource<int>);
		}

		[Pure]
		private static Type GetTaskReturnType(Type taskType)
		{
			if (taskType.IsGenericType)
				return taskType.GetGenericArguments()[0];

			return typeof(void);
		}
	}
}