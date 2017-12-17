using AsyncGenerator.Core.Logging;
using log4net;

namespace AsyncGenerator.Logging
{
	internal class Log4NetLoggerFactory : ILoggerFactory
	{
		public ILogger GetLogger(string name) => new Log4NetLogger(LogManager.GetLogger(name));
	}
}
