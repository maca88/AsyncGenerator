using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AsyncGenerator.TestCases;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace AsyncGenerator.Tests.Diagnostics.Input
{
	public class EnumerableWarning
	{
		public void Enumerable()
		{
			var enumerable = System.Linq.Enumerable.Range(1, 10)
				.Where(o => SimpleFile.Write(""))
				.Select(o => SimpleFile.Write(o.ToString()));
		}

		public void EnumerableQueryExpression()
		{
			var enumerable =
				from o in System.Linq.Enumerable.Range(1, 10)
				where SimpleFile.Write("")
				select SimpleFile.Write(o.ToString());
		}

		public void EnumerableQueryExpressionNested()
		{
			LocalFunction();

			void LocalFunction()
			{
				var enumerable =
					from o in System.Linq.Enumerable.Range(1, 10)
					where SimpleFile.Write("")
					select SimpleFile.Write(o.ToString());
			}
		}
	}
}
