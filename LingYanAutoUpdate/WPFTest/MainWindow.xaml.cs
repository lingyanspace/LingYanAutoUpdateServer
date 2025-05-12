using LingYanAutoUpdate;
using System.Windows;

namespace WPFTest
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            string updateURL = "https://dynamicapi.lingyanspace.com/UnauthorizedFolderHost/UpgradeProxy/44999483368408069/45059113250456581/升级程序自身处理.zip";
            LingYanAutoUpdateManager.Setting("测试升级",updateURL, "my.txt", "1.0", "2.0");
            LingYanAutoUpdateManager.ToRun();
        }
    }
}