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
            LingYanAutoUpdateManager.Setting("https://www.lingyanspace.com/SoftFile/灵燕空间_UpdatePackage_24674736379266053.zip",
                LingYanAutoUpdateManager.GetRestartApp(), "my.txt", "1.0", "2.0");
            LingYanAutoUpdateManager.ToRun();
        }
    }
}