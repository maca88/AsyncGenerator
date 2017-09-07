using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AsyncGenerator.TestCases;

namespace AsyncGenerator.Tests.NewTypes.Input
{
	public abstract class InternalReader
	{
		public virtual bool Read()
		{
			SimpleFile.Read();
			return true;
		}

		public virtual async Task<bool> ReadAsync()
		{
			await SimpleFile.ReadAsync();
			return true;
		}
	}

	public class NestedDerivedAsync
	{
		public void Write()
		{
			SimpleFile.Write("");
		}

		public class Nested : InternalReader
		{
			private readonly bool _test;

			public Nested()
			{
				_test = false;
			}

			public override bool Read()
			{
				return _test;
			}
		}

		public class Nested2 : ExternalReader
		{
			private bool Test { get; set; }

			public Nested2()
			{
				Test = false;
			}

			public override bool Read()
			{
				return Test;
			}
		}

		public class NestedBaseCall : InternalReader
		{
			private readonly bool _test;

			public NestedBaseCall()
			{
				_test = false;
			}

			public override bool Read()
			{
				base.Read();
				return _test;
			}
		}

		public class Nested2BaseCall : ExternalReader
		{
			private bool Test { get; set; }

			public Nested2BaseCall()
			{
				Test = false;
			}

			public override bool Read()
			{
				base.Read();
				return Test;
			}
		}

		public class Dummy
		{
			
		}
	}
}
