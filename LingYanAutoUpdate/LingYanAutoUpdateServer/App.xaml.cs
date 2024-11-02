using LingYanAutoUpdateServer;
using System.Windows;

namespace LingYanAutoUpdateServerServer
{
    /// <summary>
    /// App.xaml 的交互逻辑
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            var arguments = e.Args;          
            AutoUpdateHelper.SettingConfig(arguments[0], arguments[1], arguments[2], arguments[3], arguments[4]);
        }
    }
}
