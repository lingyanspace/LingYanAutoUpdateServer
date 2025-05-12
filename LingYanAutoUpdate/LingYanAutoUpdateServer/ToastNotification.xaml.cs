using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Threading;
using System.Windows.Media.Animation;

namespace LingYanAutoUpdateServer
{
    public partial class ToastNotification : Window
    {
        public string IconText { get; set; }
        public Brush IconColor { get; set; }

        private bool _isClosing = false;
        private DispatcherTimer _closeTimer;
        private int _countdownSeconds = 3; // 默认3秒

        public ToastNotification(string message, string title, bool isSuccess = false)
        {
            InitializeComponent();
            TitleBlock.Text = title;
            MsgBlock.Text = message;

            // 设置图标和颜色
            if (isSuccess)
            {
                IconText = "✔";
                IconColor = new SolidColorBrush(Color.FromRgb(34, 167, 242)); // #22A7F2
            }
            else
            {
                IconText = "⚠";
                IconColor = new SolidColorBrush(Color.FromRgb(231, 76, 60)); // #E74C3C
            }
            IconBlock.Text = IconText;
            IconBlock.Foreground = IconColor;

            // 倒计时提示初始化
            CountdownBlock.Visibility = Visibility.Visible;
            CountdownBlock.Text = $"{_countdownSeconds}秒后自动关闭";

            Loaded += ToastNotification_Loaded;
            Closing += ToastNotification_Closing;
        }

        private void ToastNotification_Loaded(object sender, RoutedEventArgs e)
        {
            // 右下角定位
            var workArea = SystemParameters.WorkArea;
            this.Left = workArea.Right - this.Width - 20;
            this.Top = workArea.Bottom - this.Height - 20;

            // 启动倒计时
            CountdownBlock.Text = $"{_countdownSeconds}秒后自动关闭";
            CountdownBlock.Visibility = Visibility.Visible;

            _closeTimer = new DispatcherTimer();
            _closeTimer.Interval = TimeSpan.FromSeconds(1);
            _closeTimer.Tick += CloseTimer_Tick;
            _closeTimer.Start();
        }

        private void ToastNotification_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (IconBlock.Text== "✔")
            {
                AutoUpdateHelper.CoverSelf();
                //Environment.Exit(0);
            }
            // 动画关闭已由XAML Storyboard处理
        }
       
        private void CloseTimer_Tick(object sender, EventArgs e)
        {
            _countdownSeconds--;
            if (_countdownSeconds > 0)
            {
                CountdownBlock.Text = $"{_countdownSeconds}s后自动关闭";
            }
            else
            {
                _closeTimer.Stop();
                // 播放淡出动画
                var fadeOut = (Storyboard)this.Resources["FadeOutStoryboard"];
                fadeOut.Completed += FadeOut_Completed;
                fadeOut.Begin(this);
            }
        }

        private void FadeOut_Completed(object sender, EventArgs e)
        {
            this.Close();
        }

        // 新增支持成功/失败的静态方法
        public static void Show(string message, string title = "通知", bool isSuccess = false)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                foreach (Window win in Application.Current.Windows)
                {
                    if (win is ToastNotification tn) tn.Close();
                }
                var toast = new ToastNotification(message, title, isSuccess);
                toast.Show();
            });
        }

        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                this.DragMove();
            }
        }
        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            // 手动关闭时也播放淡出动画
            var fadeOut = (Storyboard)this.Resources["FadeOutStoryboard"];
            fadeOut.Completed += FadeOut_Completed;
            fadeOut.Begin(this);
        }
    }
}

