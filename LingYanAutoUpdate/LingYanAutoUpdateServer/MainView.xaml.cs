using System.Windows.Input;
using System.Windows; // 新增

namespace LingYanAutoUpdateServer
{
    /// <summary>
    /// MainView.xaml 的交互逻辑
    /// </summary>
    public partial class MainView
    {
        public MainView()
        {
            InitializeComponent();
            this.DataContext = new MainViewModel();
        }

        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ButtonState == MouseButtonState.Pressed)
            {
                this.DragMove();
            }
        }

        // 新增关闭按钮事件
        private async void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            bool confirm = await CustomConfirmDialog.ShowAsync("确定要关闭升级程序吗？", "确认");
            if (confirm)
                this.Close();
        }       
    }
}
