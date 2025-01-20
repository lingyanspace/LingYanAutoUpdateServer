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
            string updateURL = "https://dynamicapi.lingyanspace.com/UnauthorizedFolderHost/SoftFile/灵燕空间_UpdatePackage_16231259799815173.zip";
            LingYanAutoUpdateManager.Setting("测试升级",updateURL, "my.txt", "1.0", "2.0");
            LingYanAutoUpdateManager.ToRun();
        }
    }
}