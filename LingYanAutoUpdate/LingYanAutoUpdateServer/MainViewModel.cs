﻿using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;

namespace LingYanAutoUpdateServer
{
    public class MainViewModel : BasePropertyChanged
    {
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

        public AsyncRelayCommand ToDownloadCommand { get; set; }
        public MainViewModel()
        {
            this.DownloadModelInter = new DownloadModel();
            this.ToDownloadCommand = new AsyncRelayCommand(ToDownloadCommandMethod);
            this.LocalVersion = AutoUpdateHelper.LocalVersion;
            this.ServerVersion = AutoUpdateHelper.ServerVersion;
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
                this.CurrentDecription = "下载升级包...";
                var downloadAction = new Action<double, double, double, double>((t1, t2, t3, t4) =>
                {
                    this.DownloadModelInter.CuurentProgress = t1;
                    this.DownloadModelInter.HasDownloadValue = t2;
                    this.DownloadModelInter.TotalDownloadValue = t3;
                    this.DownloadModelInter.DownloadSpeed = t4;
                });
                var localUrl = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, AutoUpdateHelper.NetworkUrl.Split('/').LastOrDefault());
                var downloadReuslt = await AutoUpdateHelper.DownloadSingleFile(downloadAction, AutoUpdateHelper.NetworkUrl, localUrl);
                this.CurrentDecription = "解压升级包...";
                await Task.Delay(200);
                await Task.Run(() =>
                {
                    AutoUpdateHelper.UpdateMainApp(AutoUpdateHelper.StartApp, localUrl, AutoUpdateHelper.LocalVersionUrl, AutoUpdateHelper.ServerVersion);
                });

            }
            catch (Exception ex)
            {
                WPFDevelopers.Controls.MessageBox.Show("升级失败", "通知", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                Environment.Exit(0);
            }
        }
    }
}
