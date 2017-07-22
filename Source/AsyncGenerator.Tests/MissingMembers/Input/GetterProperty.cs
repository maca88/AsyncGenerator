using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using AsyncGenerator.TestCases;

namespace AsyncGenerator.Tests.MissingMembers.Input
{
#if TEST
	public partial interface IGetterProperty
	{
		Task<bool> GetWriteSuccessAsync();

		Task<bool> GetWriteSuccess2Async();

		Task<bool> GetWriteSuccess3Async();
	}
#endif

	public partial interface IGetterProperty
	{
		bool WriteSuccess { get; }

		bool WriteSuccess2 { get; }
	}

	public class GetterProperty : IGetterProperty
	{
		public bool WriteSuccess { get { return SimpleFile.Write(""); } }

		public bool WriteSuccess2 => SimpleFile.Write("2");

		public bool WriteSuccess3 { get => SimpleFile.Write("3"); }
	}
}
