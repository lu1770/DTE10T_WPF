using log4net;
using log4net.Config;
using System.IO;
using System.Windows;

namespace DTE10T_WPF
{
    public partial class App : Application
    {
        private static readonly ILog _log = LogManager.GetLogger(typeof(App));

        protected override void OnStartup(StartupEventArgs e)
        {
            var configFile = new FileInfo("log4net.config");
            XmlConfigurator.Configure(configFile);
            
            _log.Info("DTE10T_WPF 应用程序启动");
            
            base.OnStartup(e);
        }

        protected override void OnExit(ExitEventArgs e)
        {
            _log.Info("DTE10T_WPF 应用程序退出");
            base.OnExit(e);
        }
    }
}