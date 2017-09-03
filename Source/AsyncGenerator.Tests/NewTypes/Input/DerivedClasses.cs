using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AsyncGenerator.TestCases;

namespace AsyncGenerator.Tests.NewTypes.Input
{
	public class BaseClass
	{
		private bool? _lastWrite;
		public bool Test;

		public virtual string Content => "";

		public bool Write()
		{
			if (_lastWrite.HasValue)
			{
				return _lastWrite.Value;
			}
			_lastWrite = SimpleFile.Write(Content);
			return _lastWrite.Value;
		}
	}

	public class DerivedClasses : BaseClass
	{
		public bool DerivedTest;

		public override string Content => "Test";
	}
}
