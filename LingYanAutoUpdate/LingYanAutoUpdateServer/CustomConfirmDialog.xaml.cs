using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Animation;

namespace LingYanAutoUpdateServer
{
    public partial class CustomConfirmDialog : Window
    {
        private TaskCompletionSource<bool> _tcs;
        private bool _isClosing = false;

        public CustomConfirmDialog(string message, string title)
        {
            InitializeComponent();
            TitleBlock.Text = title;
            MsgBlock.Text = message;
            YesBtn.Click += (s, e) => { CloseWithResult(true); };
            NoBtn.Click += (s, e) => { CloseWithResult(false); };
        }

        public static Task<bool> ShowAsync(string message, string title)
        {
            var dialog = new CustomConfirmDialog(message, title);
            dialog._tcs = new TaskCompletionSource<bool>();
            dialog.Owner = Application.Current.MainWindow;
            dialog.Show();
            return dialog._tcs.Task;
        }

        private void CloseWithResult(bool result)
        {
            if (_isClosing) return;
            _isClosing = true;
            var sb = (Storyboard)this.Resources["FadeOutStoryboard"];
            sb.Completed += (s, e) =>
            {
                _tcs?.TrySetResult(result);
                this.Close();
            };
            sb.Begin(this);
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);
            if (e.Key == Key.Escape)
            {
                CloseWithResult(false);
            }
        }

        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                this.DragMove();
            }
        }
    }
}
