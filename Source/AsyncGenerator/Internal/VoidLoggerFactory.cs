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
	internal class VoidLoggerFactory : ILoggerFactory
	{
		private static readonly VoidLogger Logger = new VoidLogger();

		public ILogger GetLogger(string name)
		{
			return Logger;
		}
	}
}
