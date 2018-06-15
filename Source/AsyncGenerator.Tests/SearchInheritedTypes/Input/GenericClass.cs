using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AsyncGenerator.Tests.SearchInheritedTypes.Input
{
	public class GenericClass
	{
		public GenericClass<int> TestDerived(GenericClass<int> item)
		{
			return item.AsDerivedType<GenericClass<int>>();
		}

		public GenericClass<int> TestBase(GenericClass<int> item)
		{
			return item.AsBaseType<GenericClass<int>>();
		}
	}

	public class GenericClass<T> : GenericClassBase<T, int>
	{
		public override TType AsBaseType<TType>()
		{
			return base.AsBaseType<TType>();
		}

		public override TType AsDerivedType<TType>()
		{
			return base.AsDerivedType<TType>();
		}
	}

	public class GenericClassBase<T, T2>
	{
		public virtual TType AsBaseType<TType>() where TType : GenericClassBase<T, T2>
		{
			return this as TType;
		}

		public virtual TType AsDerivedType<TType>() where TType : GenericClass<T>
		{
			return this as TType;
		}
	}
}
