using AsyncGenerator.Core.Logging;
using log4net;

namespace AsyncGenerator.Logging
{
	internal class Log4NetLogger : ILogger
	{
		private readonly ILog _logger;

		public Log4NetLogger(ILog logger)
		{
			_logger = logger;
		}

		public void Debug(string message)
		{
			_logger.Debug(message);
		}

		public void Info(string message)
		{
			_logger.Info(message);
		}

		public void Warn(string message)
		{
			_logger.Warn(message);
		}

		public void Error(string message)
		{
			_logger.Error(message);
		}
	}
}
