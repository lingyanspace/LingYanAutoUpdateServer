using Newtonsoft.Json;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace LingYanAutoUpdate
{
    public class LingYanAutoUpdateManager
    {
        private static AutoUpdateModel AutoUpdateModelV { get; set; }
        /// <summary>
        /// 设置更新信息
        /// </summary>
        /// <param name="windowTitle">窗口标题</param>
        /// <param name="updateZipUrl">升级ZIP压缩包http/https路径</param>
        /// <param name="localversionDir"></param>
        /// <param name="localVersion"></param>
        /// <param name="updateVersion"></param>
        public static void Setting(string windowTitle, string updateZipUrl, string localversionDir, string localVersion, string updateVersion)
        {
            AutoUpdateModelV = new AutoUpdateModel();
            AutoUpdateModelV.TitleName = windowTitle;
            AutoUpdateModelV.UpdatePackageZipUrl = updateZipUrl;
            AutoUpdateModelV.RestartApp = Process.GetCurrentProcess().MainModule.FileName;
            AutoUpdateModelV.LocalVersionDir = localversionDir;
            AutoUpdateModelV.LocalVersion = localVersion;
            AutoUpdateModelV.ServerVersion = updateVersion;
        }
        public static void ToRun()
        {
            var entityValue = JsonConvert.SerializeObject(AutoUpdateModelV);
            var startApp = Directory.GetFiles(Path.GetDirectoryName(AppDomain.CurrentDomain.BaseDirectory), "LingYanAutoUpdateServer.exe", SearchOption.AllDirectories).FirstOrDefault();
            var startAppDir = Path.GetDirectoryName(startApp);
            if (File.Exists(Path.Combine(startAppDir, "LingYanAutoUpdate.Temp")))
            {
                File.Delete(Path.Combine(startAppDir, "LingYanAutoUpdate.Temp"));
            }
            File.WriteAllText(Path.Combine(startAppDir, "LingYanAutoUpdate.Temp"), entityValue);
            Process process = new Process();//创建进程对象    
            ProcessStartInfo startInfo = new ProcessStartInfo(startApp);
            process.StartInfo = startInfo;
            process.Start();
        }

    }
}
