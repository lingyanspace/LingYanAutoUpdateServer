using System;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace LingYanAutoUpdate
{
    public class LingYanAutoUpdateManager
    {
        //升级包位置
        public static string NetworkUrl { get; set; }
        //升级完成后启动应用
        public static string StartApp { get; set; }
        //存放版本文件
        public static string LocalVersionUrl { get; set; }
        //当前安装版本
        public static string LocalVersion { get; set; }
        //线上最新版本
        public static string ServerVersion { get; set; }
        public static void Setting(string networkUrl, string restartApp, string localversionUrl, string localversion, string serverVersion)
        {
            NetworkUrl = networkUrl;
            StartApp = restartApp;
            LocalVersionUrl = localversionUrl;
            LocalVersion = localversion;
            ServerVersion = serverVersion;
        }
        public static string GetRestartApp()
        {
            return Process.GetCurrentProcess().MainModule.FileName;
        }
        public static void ToRun()
        {
            var args = string.Join(" ", new string[] { NetworkUrl, StartApp, LocalVersionUrl, LocalVersion, ServerVersion });
            var startApp = Directory.GetFiles(Path.GetDirectoryName(AppDomain.CurrentDomain.BaseDirectory), "LingYanAutoUpdateServer.exe",SearchOption.AllDirectories).FirstOrDefault();
            Process process = new Process();//创建进程对象    
            ProcessStartInfo startInfo = new ProcessStartInfo(startApp, args);
            process.StartInfo = startInfo;
            process.Start();
        }

    }
}
