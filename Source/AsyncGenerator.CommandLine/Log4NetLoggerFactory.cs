using AsyncGenerator.Core.Logging;
using log4net;

namespace AsyncGenerator.Logging
{
	internal class Log4NetLoggerFactory : ILoggerFactory
	{
		public ILogger GetLogger(string name)
		{
			var logRepository = LogManager.GetRepository(typeof(Log4NetLoggerFactory).Assembly);
			var logger = LogManager.GetLogger(logRepository.Name, name);
			return new Log4NetLogger(logger);
		}
	}
}
