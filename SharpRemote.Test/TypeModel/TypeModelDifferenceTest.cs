using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using FluentAssertions;
using NUnit.Framework;
using SharpRemote.CodeGeneration;
using SharpRemote.Test.Types.Interfaces.PrimitiveTypes;

namespace SharpRemote.Test.TypeModel
{
	[TestFixture]
	public sealed class TypeModelDifferenceTest
	{
		[Test]
		public void TestMissingMethod()
		{
			var differences = FindDifferences<IVoidMethod, INoMethod>();
			differences.Should().Contain(x => x is MissingMethod);
			differences.Should().HaveCount(1);
		}

		[Test]
		public void TestDifferentReturnType()
		{
			var differences = FindDifferences<IVoidMethod, IInt32Method>();
			differences.Should().Contain(x => x is ParameterTypeMismatch);
			differences.Should().HaveCount(1);
		}

		[Test]
		public void TestTooMuchParameters()
		{
			var differences = FindDifferences<IVoidMethod, IVoidMethodDoubleParameter>();
			differences.Should().Contain(x => x is ParameterCountMismatch);
			differences.Should().HaveCount(1);
		}

		[Test]
		public void TestNotEnoughParameters()
		{
			var differences = FindDifferences<IVoidMethodDoubleParameter, IVoidMethod>();
			differences.Should().Contain(x => x is ParameterCountMismatch);
			differences.Should().HaveCount(1);
		}

		[Test]
		public void TestDifferentParameterType()
		{
			var differences = FindDifferences<IVoidMethodDoubleParameter, IVoidMethodInt32Parameter>();
			differences.Should().Contain(x => x is ParameterTypeMismatch);
			differences.Should().HaveCount(1);
		}

		[Pure]
		private IReadOnlyList<ITypeModelDifference> FindDifferences<TExpected, TActual>()
		{
			return FindDifferences(typeof(TExpected), typeof(TActual));
		}

		[Pure]
		private IReadOnlyList<ITypeModelDifference> FindDifferences(Type expected, Type actual)
		{
			var expectedTypeModel = new SharpRemote.TypeModel();
			var expectedType = expectedTypeModel.Add(expected);

			var actualTypeModel = new SharpRemote.TypeModel();
			var actualType = actualTypeModel.Add(actual);

			var typeResolver = new TestResolver();
			typeResolver.Add(expected.AssemblyQualifiedName, actual);
			actualTypeModel.TryResolveTypes(typeResolver);

			return ((TypeDescription) expectedType).FindDifferences((TypeDescription) actualType).ToList();
			//return expectedTypeModel.FindDifferences(actualTypeModel);
		}

		internal class TestResolver
			: ITypeResolver
		{
			private readonly Dictionary<string, Type> _types;

			public TestResolver()
			{
				_types = new Dictionary<string, Type>();
			}

			#region Implementation of ITypeResolver

			public Type GetType(string assemblyQualifiedTypeName)
			{
				Type type;
				if (_types.TryGetValue(assemblyQualifiedTypeName, out type))
					return type;

				return TypeResolver.GetType(assemblyQualifiedTypeName);
			}

			#endregion

			public void Add(string assemblyQualifiedName, Type type)
			{
				_types.Add(assemblyQualifiedName, type);
			}
		}
	}
}
