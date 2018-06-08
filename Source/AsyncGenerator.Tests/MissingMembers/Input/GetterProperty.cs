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

		[Obsolete]
		Task<bool> GetWriteSuccess4Async();

		[Obsolete("Obsolete attribute should be copied to concrete implementation")]
		Task<bool> GetWriteSuccess5Async();

		[Obsolete("Obsolete async interface")]
		Task<bool> GetWriteSuccess6Async();
	}
#endif

	public partial interface IGetterProperty
	{
		bool WriteSuccess { get; }

		bool WriteSuccess2 { get; }

		bool WriteSuccess4 { get; }

		bool WriteSuccess5 { get; }

		[Obsolete("Obsolete sync interface")]
		bool WriteSuccess6 { get; }
	}

	public class GetterProperty : IGetterProperty
	{
		public bool WriteSuccess { get { return SimpleFile.Write(""); } }

		public bool WriteSuccess2 => SimpleFile.Write("2");

		public bool WriteSuccess3 { get => SimpleFile.Write("3"); }

		public bool WriteSuccess4 => true;

		public bool WriteSuccess5 => false;

		[Obsolete("Obsolete sync")]
		public bool WriteSuccess6 => true;
	}
}
