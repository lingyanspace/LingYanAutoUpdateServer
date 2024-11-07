using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace LingYanAutoUpdateServer
{
    public struct HttpDownloadProgress
    {
        public ulong BytesReceived { get; set; }
        public double DownloadSpeed { get; set; } // in bytes per second
        public ulong? TotalBytesToReceive { get; set; }
    }
    public static class AutoUpdateHelper
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
        public static void SettingConfig(string networkUrl,string restartApp,string localversionUrl,string localversion,string serverVersion)
        { 
            NetworkUrl = networkUrl;
            StartApp = restartApp;
            LocalVersionUrl = localversionUrl;
            LocalVersion = localversion;
            ServerVersion = serverVersion;
            int mainClese = 0;
            var name = Path.GetFileNameWithoutExtension(restartApp);
            Process[] processes = Process.GetProcessesByName(name);
            while (mainClese < 10 && !processes.All(a => a.HasExited))
            {
                mainClese++;
                // 关闭所有进程
                foreach (var process in processes)
                {
                    if (!process.HasExited)
                    {
                        try
                        {
                            process.Kill();
                        }
                        catch (Exception ex)
                        {
                            // 可以在这里处理异常，例如记录日志
                            Console.WriteLine($"Error killing process: {ex.Message}");
                        }
                    }
                }
                Thread.Sleep(100);
            }
        }
        internal static async Task<bool> DownloadSingleFile(Action<double, double, double, double> action, string netWrokUrl, string localUrl)
        {
            var progress = new Progress<HttpDownloadProgress>(p =>
            {
                if (p.TotalBytesToReceive.HasValue)
                {
                    double hasdownloadSize = (double)p.BytesReceived;
                    double totalSize = (double)p.TotalBytesToReceive.Value;
                    double percent = hasdownloadSize / totalSize * 100.0;
                    action.Invoke(percent, hasdownloadSize / 1024 / 1024, totalSize / 1024 / 1024, p.DownloadSpeed / 1024 / 1024);
                }
            });
            var fileBytes = await new HttpClient().GetByteArrayAsync(new Uri(netWrokUrl), progress, CancellationToken.None);
            if (File.Exists(localUrl))
            {
                File.Delete(localUrl);
            }
            await localUrl.SaveLocalFileAsync(new MemoryStream(fileBytes));
            return true;
        }
        internal static async Task<string> SaveLocalFileAsync(this string filePath, MemoryStream memoryStream)
        {
            using (var fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write))
            {
                await memoryStream.CopyToAsync(fileStream);
            }
            return filePath;
        }
        private const int BufferSize = 262144;
        private static async Task<byte[]> GetByteArrayAsync(this HttpClient client, Uri requestUri, IProgress<HttpDownloadProgress> progress, CancellationToken cancellationToken)
        {
            if (client == null)
            {
                throw new ArgumentNullException(nameof(client));
            }

            using (var responseMessage = await client.GetAsync(requestUri, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false))
            {
                responseMessage.EnsureSuccessStatusCode();
                var content = responseMessage.Content;
                if (content == null)
                {
                    return Array.Empty<byte>();
                }

                var headers = content.Headers;
                var contentLength = headers.ContentLength;
                using (var responseStream = await content.ReadAsStreamAsync().ConfigureAwait(false))
                {
                    var buffer = new byte[BufferSize];
                    int bytesRead;
                    var bytes = new List<byte>();
                    var downloadProgress = new HttpDownloadProgress();
                    if (contentLength.HasValue)
                    {
                        downloadProgress.TotalBytesToReceive = (ulong)contentLength.Value;
                    }
                    progress?.Report(downloadProgress);

                    DateTime lastReportTime = DateTime.UtcNow;
                    ulong lastBytesReceived = 0;

                    while ((bytesRead = await responseStream.ReadAsync(buffer, 0, BufferSize, cancellationToken).ConfigureAwait(false)) > 0)
                    {
                        bytes.AddRange(buffer.Take(bytesRead));
                        downloadProgress.BytesReceived += (ulong)bytesRead;
                        DateTime currentTime = DateTime.UtcNow;
                        var timeSpan = currentTime - lastReportTime;
                        if (timeSpan.TotalSeconds > 1 || downloadProgress.DownloadSpeed == 0)
                        {
                            var bytesSinceLastReport = downloadProgress.BytesReceived - lastBytesReceived;
                            downloadProgress.DownloadSpeed = bytesSinceLastReport / timeSpan.TotalSeconds; // bytes per second

                            // Update last report time and bytes received
                            lastReportTime = currentTime;
                            lastBytesReceived = downloadProgress.BytesReceived;
                        }
                        progress?.Report(downloadProgress);
                    }

                    // Set download speed to 0 after completion
                    downloadProgress.DownloadSpeed = 0;
                    progress?.Report(downloadProgress);

                    return bytes.ToArray();
                }
            }
        }
        internal static void UpdateMainApp(string startApp, string updatezipFile, string localVersionFile, string autoVersion)
        {
            int maxAttempts = 10; 
            //解压升级包
            int attempt = 0;
            bool success = false;
            while (attempt < maxAttempts && !success)
            {
                if (File.Exists(updatezipFile) && Directory.Exists(Path.GetDirectoryName(startApp)))
                {
                    try
                    {
                        //ZipFile.ExtractToDirectory(updatezipFile, Path.GetDirectoryName(startApp));
                        using (ZipArchive archive = ZipFile.OpenRead(updatezipFile))
                        {
                            foreach (ZipArchiveEntry entry in archive.Entries)
                            {
                                string destinationPath = Path.Combine(Path.GetDirectoryName(startApp), entry.FullName);
                                string destinationDirectory = Path.GetDirectoryName(destinationPath);
                                // 检查是否为目录（在ZIP文件中，目录名以'/'结尾）
                                if (entry.FullName.EndsWith("/"))
                                {
                                    if (!Directory.Exists(destinationPath))
                                    {
                                        Directory.CreateDirectory(destinationPath);
                                    }
                                }
                                else
                                {
                                    if (!Directory.Exists(destinationDirectory))
                                    {
                                        Directory.CreateDirectory(destinationDirectory);
                                    }
                                    if (File.Exists(destinationPath))
                                    {
                                        File.Delete(destinationPath);
                                    }
                                    entry.ExtractToFile(destinationPath);
                                }
                            }
                        }
                        success = true;
                    }
                    catch (Exception ex)
                    {
                        attempt++;
                        if (attempt < maxAttempts)
                        {
                            Thread.Sleep(100);
                        }
                    }
                }
                else
                {
                    break;
                }
            }
            //存放本地版本
            if (File.Exists(localVersionFile))
            {
                File.Delete(localVersionFile);
            }
            File.WriteAllText(localVersionFile, autoVersion);
            //删除升级包
            if (File.Exists(updatezipFile))
            {
                File.Delete(updatezipFile);
            }
            // 创建一个新的进程信息对象
            ProcessStartInfo startInfo = new ProcessStartInfo
            {
                FileName = startApp,
                UseShellExecute = true
            };
            // 创建一个新的进程
            using (Process process = new Process())
            {
                // 设置进程的启动信息
                process.StartInfo = startInfo;
                // 启动进程
                process.Start();
            }
        }
    }
}
