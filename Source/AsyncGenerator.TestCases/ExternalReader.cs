using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AsyncGenerator.TestCases
{
	public class ExternalReader
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
}
