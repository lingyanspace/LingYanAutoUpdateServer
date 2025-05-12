using LingYanAutoUpdate;
using System.Windows;

namespace WPFTest
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            //LingYanAutoUpdateManager.ToCoverOldUpdateExe();
            base.OnStartup(e);
        }
    }

}
