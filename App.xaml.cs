using System.Windows;

namespace DTE10T_WPF
{
    public partial class App : Application
    {
        private static MainWindow? _mainWindow;

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            //// 确保只创建一个主窗口实例
            //if (_mainWindow == null)
            //{
            //    _mainWindow = new MainWindow();
            //    _mainWindow.Closed += (sender, args) => _mainWindow = null;
            //}

            //_mainWindow.Show();
            //_mainWindow.Activate();
        }
    }
}
