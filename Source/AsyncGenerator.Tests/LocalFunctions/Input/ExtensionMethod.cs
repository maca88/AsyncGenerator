using System.Collections.Generic;
using System.Linq;

namespace AsyncGenerator.Tests.LocalFunctions.Input
{
	public class ExtensionMethod
	{
		public void Test()
		{
			TType GetFirstOrDefault<TType>(TType item) where TType : class
			{
				return (from e in new EnumerableQuery<TType>(new List<TType>())
						where e.Equals(item)
						select e
					).FirstOrDefault();

			}
			var result = GetFirstOrDefault("test");
		}
	}
}
