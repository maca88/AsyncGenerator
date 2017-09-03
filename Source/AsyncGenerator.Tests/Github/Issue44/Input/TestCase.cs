using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using AsyncGenerator.TestCases;

namespace AsyncGenerator.Tests.Github.Issue44.Input
{
	public interface ICollectionPropertiesMapper<TEntity, TElement>
	{
		void Key(Action<IKeyMapper<TEntity>> keyMapping);
		void Cascade(bool cascadeStyle);
	}

	public interface ISetPropertiesMapper<TEntity, TElement> : ICollectionPropertiesMapper<TEntity, TElement> { }

	public interface ICollectionElementRelation<TElement>
	{
		void OneToMany();
	}

	public interface IColumnsMapper
	{
		void Column(string name);
	}

	public interface IKeyMapper<TEntity> : IColumnsMapper
	{
	}

	public abstract class Animal
	{
		public virtual int Id { get; protected set; }
	}

	public class Family<T> where T : Animal
	{
		public virtual ISet<T> Childs { get; set; }
	}


	public class ClassMapping<TEntity>
	{
		public void Set<TElement>(Expression<Func<TEntity, IEnumerable<TElement>>> property,
			Action<ISetPropertiesMapper<TEntity, TElement>> collectionMapping,
			Action<ICollectionElementRelation<TElement>> mapping)
		{
		}
	}

	public class TestCase
	{
		public FamilyMap<Animal> Map = new FamilyMap<Animal>();

		public void Read()
		{
			SimpleFile.Read();
		}

		public class FamilyMap<T> : ClassMapping<Family<T>> where T : Animal
		{
			public FamilyMap()
			{
				Set(x => x.Childs, cam =>
					{
						cam.Key(km => km.Column("familyId"));
						cam.Cascade(true);
					},
					rel => rel.OneToMany());
			}
		}
	}
}
