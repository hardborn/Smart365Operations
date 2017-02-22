using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using log4net;
using Prism.Logging;

namespace Smart365Operation.Modules.Log4NetLogger
{
    public class Log4NetLogger : ILoggerFacade
    {
        private readonly ILog _logger = LogManager.GetLogger(typeof(Log4NetLogger));

        //public Log4NetLogger()
        //{
        //    log4net.Config.XmlConfigurator.ConfigureAndWatch(
        //        new System.IO.FileInfo("Log4Net.config"));
        //}
        public void Log(string message, Category category, Priority priority)
        {
            switch (category)
            {
                case Category.Debug:
                    _logger.Debug(message);
                    break;
                case Category.Warn:
                    _logger.Warn(message);
                    break;
                case Category.Exception:
                    _logger.Error(message);
                    break;
                case Category.Info:
                    _logger.Info(message);
                    break;
            }
        }
    }
}
