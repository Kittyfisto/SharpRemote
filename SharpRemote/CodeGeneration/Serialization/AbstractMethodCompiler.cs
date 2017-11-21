using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Reflection.Emit;
using log4net.Core;

namespace SharpRemote.CodeGeneration.Serialization
{
	/// <summary>
	///     Base class for all method compilers.
	/// </summary>
	public abstract class AbstractMethodCompiler
		: IMethodCompiler
	{
		/// <summary>
		/// </summary>
		protected static readonly MethodInfo CultureInfoGetInvariantCulture;

		/// <summary>
		/// </summary>
		protected static readonly MethodInfo StringFormatObject;

		/// <summary>
		/// </summary>
		protected static readonly MethodInfo StringEquals;

		/// <summary>
		///     A list of accessors to special <see cref="log4net.Core.Level" /> values
		///     which shall be regarded as singletons: A serializer shall ensure
		///     that if an object equal to one of the given array is serialized, then
		///     deserialization shall produce the same object (and not a new one).
		/// </summary>
		/// <remarks>
		///     This list will never change in order and new values will only ever be appended
		///     to the bottom.
		/// </remarks>
		protected static readonly IReadOnlyList<Singleton> HardcodedLevels;

		/// <summary>
		///     Provides access to a singleton value which is accessible via static property
		///     or field.
		/// </summary>
		protected struct Singleton
		{
			/// <summary>
			///     The field through which the singleton value can be retrieved,
			///     may be null.
			/// </summary>
			public readonly FieldInfo Field;

			/// <summary>
			///     The property through which the singleton value can be retrieved,
			///     may be null.
			/// </summary>
			public readonly PropertyInfo Property;

			/// <summary>
			///     A name for this singleton value.
			///     Will never change, ever and can be used for serialization, if desired.
			/// </summary>
			public readonly string Name;

			private Singleton(string name, FieldInfo field)
			{
				if (name == null)
					throw new ArgumentNullException(nameof(name));
				if (field == null)
					throw new ArgumentNullException(nameof(field));

				Name = name;
				Field = field;
				Property = null;
			}

			private Singleton(string name, PropertyInfo property)
			{
				if (name == null)
					throw new ArgumentNullException(nameof(name));
				if (property == null)
					throw new ArgumentNullException(nameof(property));

				Name = name;
				Field = null;
				Property = property;
			}

			/// <summary>
			/// 
			/// </summary>
			/// <typeparam name="T"></typeparam>
			/// <param name="fieldName"></param>
			/// <returns></returns>
			public static Singleton FromField<T>(string fieldName)
			{
				return new Singleton(fieldName, typeof(T).GetField(fieldName, BindingFlags.Public | BindingFlags.Static));
			}

			/// <summary>
			/// 
			/// </summary>
			/// <typeparam name="T"></typeparam>
			/// <param name="fieldName"></param>
			/// <returns></returns>
			public static Singleton FromProperty<T>(string fieldName)
			{
				return new Singleton(fieldName, typeof(T).GetProperty(fieldName, BindingFlags.Public | BindingFlags.Static));
			}
		}

		static AbstractMethodCompiler()
		{
			CultureInfoGetInvariantCulture = typeof(CultureInfo).GetProperty(nameof(CultureInfo.InvariantCulture)).GetMethod;
			StringFormatObject = typeof(string).GetMethod(nameof(string.Format), new[] {typeof(string), typeof(object)});
			StringEquals = typeof(string).GetMethod(nameof(string.Equals), new[] {typeof(string)});
			HardcodedLevels = new[]
			{
				Singleton.FromField<Level>(nameof(Level.Debug)),
				Singleton.FromField<Level>(nameof(Level.Alert)),
				Singleton.FromField<Level>(nameof(Level.All)),
				Singleton.FromField<Level>(nameof(Level.Critical)),
				Singleton.FromField<Level>(nameof(Level.Emergency)),
				Singleton.FromField<Level>(nameof(Level.Error)),
				Singleton.FromField<Level>(nameof(Level.Fatal)),
				Singleton.FromField<Level>(nameof(Level.Fine)),
				Singleton.FromField<Level>(nameof(Level.Finer)),
				Singleton.FromField<Level>(nameof(Level.Finest)),
				Singleton.FromField<Level>(nameof(Level.Info)),
				Singleton.FromField<Level>(nameof(Level.Log4Net_Debug)),
				Singleton.FromField<Level>(nameof(Level.Notice)),
				Singleton.FromField<Level>(nameof(Level.Off)),
				Singleton.FromField<Level>(nameof(Level.Severe)),
				Singleton.FromField<Level>(nameof(Level.Trace)),
				Singleton.FromField<Level>(nameof(Level.Verbose)),
				Singleton.FromField<Level>(nameof(Level.Warn))
			};
		}

		/// <inheritdoc />
		public abstract MethodBuilder Method { get; }

		/// <inheritdoc />
		public abstract void Compile(AbstractMethodsCompiler methods,
		                             ISerializationMethodStorage<AbstractMethodsCompiler> methodStorage);
	}
}