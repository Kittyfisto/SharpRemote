using System;
using System.Reflection;
using System.Reflection.Emit;
using SharpRemote.CodeGeneration.Remoting;

namespace SharpRemote.CodeGeneration
{
	/// <summary>
	///     The default <see cref="ICodeGenerator" /> implementation which generates code on-demand
	///     using IL.Emit.
	/// </summary>
	public sealed class CodeGenerator
		: ICodeGenerator
	{
		private readonly ProxyCreator _proxyCreator;
		private readonly ServantCreator _servantCreator;

		/// <summary>
		/// 
		/// </summary>
		/// <param name="serializer"></param>
		public CodeGenerator(Serializer serializer)
		{
			_proxyCreator = new ProxyCreator(serializer.Module, serializer);
			_servantCreator = new ServantCreator(serializer.Module, serializer);
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="customTypeResolver">Type resolver that should be used instead of <see cref="TypeResolver"/></param>
		public CodeGenerator(ITypeResolver customTypeResolver = null)
		{
			var assemblyName = new AssemblyName("SharpRemote.GeneratedCode");
			var assembly = AppDomain.CurrentDomain.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.RunAndSave);
			var moduleName = assemblyName.Name + ".dll";
			var module = assembly.DefineDynamicModule(moduleName);

			var serializer = new Serializer(module, customTypeResolver);
			_proxyCreator = new ProxyCreator(module, serializer);
			_servantCreator = new ServantCreator(module, serializer);
		}

		/// <inheritdoc />
		public Type GenerateServant<T>()
		{
			return _servantCreator.GenerateServant<T>();
		}

		/// <inheritdoc />
		public IServant CreateServant<T>(IRemotingEndPoint endPoint, IEndPointChannel channel, ulong objectId, T subject)
		{
			return _servantCreator.CreateServant(endPoint, channel, objectId, subject);
		}

		/// <inheritdoc />
		public Type GenerateProxy<T>()
		{
			return _proxyCreator.GenerateProxy<T>();
		}

		/// <inheritdoc />
		public T CreateProxy<T>(IRemotingEndPoint endPoint, IEndPointChannel channel, ulong objectId)
		{
			return _proxyCreator.CreateProxy<T>(endPoint, channel, objectId);
		}
	}
}