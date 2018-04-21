using System;
using System.Reflection;
using System.Reflection.Emit;

namespace SharpRemote.CodeGeneration.FaultTolerance
{
	/// <summary>
	///     Responsible for creating proxy objects implementing a specified interface,
	///     forwarding method calls to another object while attempting to hide failures
	///     from the caller.
	/// </summary>
	public sealed class ProxyCreator
	{
		private readonly ProxyTypeStorage _storage;

		/// <summary>
		/// </summary>
		public ProxyCreator()
			: this(CreateModule())
		{
		}

		/// <summary>
		/// </summary>
		/// <param name="moduleBuilder"></param>
		/// <exception cref="ArgumentNullException"></exception>
		public ProxyCreator(ModuleBuilder moduleBuilder)
		{
			if (moduleBuilder == null)
				throw new ArgumentNullException(nameof(moduleBuilder));

			_storage = new ProxyTypeStorage(moduleBuilder);
		}

		private static ModuleBuilder CreateModule()
		{
			var assemblyName = new AssemblyName("SharpRemote.GeneratedCode.FaultTolerance");
			var assembly = AppDomain.CurrentDomain.DefineDynamicAssembly(assemblyName,
			                                                             AssemblyBuilderAccess.RunAndSave);
			var moduleName = assemblyName.Name + ".dll";
			var module = assembly.DefineDynamicModule(moduleName);
			return module;
		}

		/// <summary>
		/// 
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="subject"></param>
		/// <returns></returns>
		public IProxyFactory<T> PrepareProxyFor<T>(T subject) where T : class
		{
			var type = typeof(T);
			if (!type.IsInterface)
				throw new ArgumentException();

			return new ProxyFactory<T>(_storage, subject);
		}
	}
}