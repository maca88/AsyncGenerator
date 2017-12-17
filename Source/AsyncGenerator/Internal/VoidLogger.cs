using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AsyncGenerator.Configuration;
using AsyncGenerator.Core.Configuration;
using AsyncGenerator.Core.Logging;

namespace AsyncGenerator.Internal
{
	internal class VoidLogger : ILogger
	{
		public void Debug(string message)
		{
		}

		public void Info(string message)
		{
		}

		public void Warn(string message)
		{
		}

		public void Error(string message)
		{
		}
	}
}
