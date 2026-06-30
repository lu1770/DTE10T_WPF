using log4net;
using log4net.Config;
using System;
using System.IO;

namespace DTE10T_WPF
{
    public static class Logger
    {
        private static readonly ILog _log = LogManager.GetLogger(typeof(Logger));

        static Logger()
        {
            var configFile = new FileInfo("log4net.config");
            XmlConfigurator.Configure(configFile);
        }

        public static void Debug(string message)
        {
            _log.Debug(message);
        }

        public static void Debug(string format, params object[] args)
        {
            _log.DebugFormat(format, args);
        }

        public static void Info(string message)
        {
            _log.Info(message);
        }

        public static void Info(string format, params object[] args)
        {
            _log.InfoFormat(format, args);
        }

        public static void Warn(string message)
        {
            _log.Warn(message);
        }

        public static void Warn(string format, params object[] args)
        {
            _log.WarnFormat(format, args);
        }

        public static void Error(string message)
        {
            _log.Error(message);
        }

        public static void Error(string format, params object[] args)
        {
            _log.ErrorFormat(format, args);
        }

        public static void Error(string message, Exception exception)
        {
            if(exception is TimeoutException || 
               exception is System.IO.IOException ||
               exception.Message.Contains("timed out", StringComparison.OrdinalIgnoreCase))
            {
                _log.Error($"{message}");
            }
            else
            {
                _log.Error(message, exception);
            }
        }

        public static void Fatal(string message)
        {
            _log.Fatal(message);
        }

        public static void Fatal(string format, params object[] args)
        {
            _log.FatalFormat(format, args);
        }
    }
}