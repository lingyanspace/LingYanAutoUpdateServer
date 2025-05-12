using LingYanAutoUpdateServerServer;
using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;

namespace LingYanAutoUpdateServer
{
    public class MainViewModel : BasePropertyChanged
    {
        private string _WindowTitle;

        public string WindowTitle
        {
            get { return _WindowTitle; }
            set { _WindowTitle = value; this.OnPropertyChanged(); }
        }

        private string _LocalVersion;

        public string LocalVersion
        {
            get { return _LocalVersion; }
            set { _LocalVersion = value; this.OnPropertyChanged(); }
        }
        private string _ServerVersion;

        public string ServerVersion
        {
            get { return _ServerVersion; }
            set { _ServerVersion = value; this.OnPropertyChanged(); }
        }
        private string _CurrentDecription;

        public string CurrentDecription
        {
            get { return _CurrentDecription; }
            set { _CurrentDecription = value; this.OnPropertyChanged(); }
        }
        private DownloadModel _DownloadModelInter;

        public DownloadModel DownloadModelInter
        {
            get { return _DownloadModelInter; }
            set { _DownloadModelInter = value; this.OnPropertyChanged(); }
        }
        private ExtractModel _ExtractModelInter;

        public ExtractModel ExtractModelInter
        {
            get { return _ExtractModelInter; }
            set { _ExtractModelInter = value; this.OnPropertyChanged(); }
        }

        public MainViewModel()
        {
            this.DownloadModelInter = new DownloadModel();
            this.ExtractModelInter = new ExtractModel();
            this.LocalVersion = App.AutoUpdateModel?.LocalVersion??"0.0";
            this.ServerVersion = App.AutoUpdateModel?.ServerVersion??"0.1";
            this.WindowTitle = App.AutoUpdateModel?.TitleName ?? "云端在线升级";
            this.CurrentDecription = "等待更新...";
            ViewInitEndAction(async () =>
            {
                await ToDownloadCommandMethod();
            });
        }
        public void ViewInitEndAction(Action action, double time = 0.3)
        {
            DispatcherTimer startTimer = new DispatcherTimer(DispatcherPriority.Normal);
            startTimer.Interval = TimeSpan.FromSeconds(0.3); // 设置定时器间隔，这里设置为1秒
            startTimer.Tick += (sender, e) =>
            {
                startTimer.Stop();
                action.Invoke();
            };
            startTimer.Start();
        }
        private async Task ToDownloadCommandMethod()
        {
            try
            {
                Application.Current.Dispatcher.Invoke(() => this.CurrentDecription = "下载升级包...");
                var downloadAction = new Action<double, double, double, double>((t1, t2, t3, t4) =>
                {
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        this.DownloadModelInter.CuurentProgress = t1;
                        this.DownloadModelInter.HasDownloadValue = t2;
                        this.DownloadModelInter.TotalDownloadValue = t3;
                        this.DownloadModelInter.DownloadSpeed = t4;
                    });
                });
                var localUrl = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, App.AutoUpdateModel.UpdatePackageZipUrl.Split('/').LastOrDefault());
                var downloadReuslt = await AutoUpdateHelper.DownloadSingleFile(downloadAction, App.AutoUpdateModel.UpdatePackageZipUrl, localUrl);
                Application.Current.Dispatcher.Invoke(() => this.CurrentDecription = "解压升级包...");
                var extractUpdateZip = new Action<string, int, int, double>((str, i1, i2, d) =>
                {
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        this.ExtractModelInter.CurrentFileName = str;
                        this.ExtractModelInter.CurrentFileIndex = i1;
                        this.ExtractModelInter.CurrentExtractProgress = d;
                        this.ExtractModelInter.TotalFiles = i2;
                    });
                });
                var descrptionAction = new Action<string>((str) =>
                {
                    Application.Current.Dispatcher.Invoke(() => this.CurrentDecription = str);
                });
                await Task.Run(async () =>
                {
                    await AutoUpdateHelper.UpdateMainApp(extractUpdateZip, descrptionAction, App.AutoUpdateModel.RestartApp, localUrl, App.AutoUpdateModel.LocalVersionDir, App.AutoUpdateModel.ServerVersion);
                });

                // 升级成功通知
                ToastNotification.Show("升级成功", "通知", true);
                Application.Current.Dispatcher.Invoke(() =>
                {
                    Application.Current.MainWindow?.Close();
                });
            }
            catch (Exception ex)
            {
                // 升级失败通知
                ToastNotification.Show("升级失败", "通知", false);
            }
            finally
            {
                //Environment.Exit(0);
            }
        }
    }
}
