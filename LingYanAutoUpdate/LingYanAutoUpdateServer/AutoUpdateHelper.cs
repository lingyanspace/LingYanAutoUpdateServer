using LingYanAutoUpdateServerServer;
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
    public static class AutoUpdateHelperExtension
    {
        internal static async Task<string> SaveLocalFileAsync(this string filePath, MemoryStream memoryStream)
        {
            using (var fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write))
            {
                await memoryStream.CopyToAsync(fileStream);
            }
            return filePath;
        }
        private const int BufferSize = 262144;
        internal static async Task<byte[]> MyGetByteArrayAsync(this HttpClient client, Uri requestUri, IProgress<HttpDownloadProgress> progress, CancellationToken cancellationToken)
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
    }
    public struct HttpDownloadProgress
    {
        public ulong BytesReceived { get; set; }
        public double DownloadSpeed { get; set; } // in bytes per second
        public ulong? TotalBytesToReceive { get; set; }
    }
    public struct UpdateProgressEventArgs
    {
        public string CurrentFileName { get; set; }
        public int CurrentFile { get; }
        public int TotalFiles { get; }
        public double ProgressPercentage { get; }
    }
    public class AutoUpdateHelper
    {
        public static void SettingConfig()
        {
            int mainClose = 0;
            var name = Path.GetFileNameWithoutExtension(App.AutoUpdateModel.RestartApp);
            Process[] processes = Process.GetProcessesByName(name);
            while (mainClose < 10 && !processes.All(a => a.HasExited))
            {
                mainClose++;
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
                            Console.WriteLine($"关闭主程序失败日志: {ex.Message}");
                        }
                    }
                }
                Thread.Sleep(100);
            }
        }
        internal static async Task<bool> DownloadSingleFile(Action<double, double, double, double> action, string networkUrl, string localUrl)
        {
            var progress = new Progress<HttpDownloadProgress>(p =>
            {
                if (p.TotalBytesToReceive.HasValue)
                {
                    double hasDownloadSize = (double)p.BytesReceived;
                    double totalSize = (double)p.TotalBytesToReceive.Value;
                    double percent = hasDownloadSize / totalSize * 100.0;
                    action.Invoke(percent, hasDownloadSize / 1024 / 1024, totalSize / 1024 / 1024, p.DownloadSpeed / 1024 / 1024);
                }
            });
            var fileBytes = await new HttpClient().MyGetByteArrayAsync(new Uri(networkUrl), progress, CancellationToken.None);
            if (File.Exists(localUrl))
            {
                File.Delete(localUrl);
            }
            await localUrl.SaveLocalFileAsync(new MemoryStream(fileBytes));
            return true;
        }

        internal static async Task UpdateMainApp(Action<string, int, int, double> action, Action<string> descryptionAction, string startApp, string updatezipFile, string localVersionFile, string autoVersion)
        {
            int maxAttempts = 10;
            // 解压升级包
            int attempt = 0;
            bool success = false;
            while (attempt < maxAttempts && !success)
            {
                if (File.Exists(updatezipFile) && Directory.Exists(Path.GetDirectoryName(startApp)))
                {
                    try
                    {
                        using (ZipArchive archive = ZipFile.OpenRead(updatezipFile))
                        {
                            int totalFiles = archive.Entries.Count;
                            int currentFile = 0;

                            foreach (ZipArchiveEntry entry in archive.Entries)
                            {
                                string destinationPath = Path.Combine(Path.GetDirectoryName(startApp), entry.FullName);
                                string destinationDir = Path.GetDirectoryName(destinationPath);
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
                                    if (!Directory.Exists(destinationDir))
                                    {
                                        Directory.CreateDirectory(destinationDir);
                                    }
                                    if (File.Exists(destinationPath))
                                    {
                                        File.Delete(destinationPath);
                                    }
                                    entry.ExtractToFile(destinationPath);
                                }
                                currentFile++;
                                action.Invoke(entry.Name, currentFile, totalFiles, (double)currentFile / totalFiles * 100);
                            }
                        }
                        success = true;
                    }
                    catch (Exception ex)
                    {
                        attempt++;
                        if (attempt < maxAttempts)
                        {
                            await Task.Delay(100);
                        }
                        Console.WriteLine($"解压失败，尝试次数: {attempt}, 错误: {ex.Message}");
                    }
                }
                else
                {
                    break;
                }
            }
            descryptionAction.Invoke($"写入{Path.GetFileName(localVersionFile)}最新版本");
            await Task.Delay(200);
            if (File.Exists(localVersionFile))
            {
                File.Delete(localVersionFile);
            }
            File.WriteAllText(localVersionFile, autoVersion);
            descryptionAction.Invoke($"删除升级包");
            await Task.Delay(200);
            if (File.Exists(updatezipFile))
            {
                File.Delete(updatezipFile);
            }
            descryptionAction.Invoke($"删除进程传参文件");
            await Task.Delay(200);
            if (File.Exists(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "LingYanAutoUpdate.Temp")))
            {
                File.Delete(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "LingYanAutoUpdate.Temp"));
            }
            descryptionAction.Invoke($"重启主程序");
            await Task.Delay(200);
            ProcessStartInfo startInfo = new ProcessStartInfo
            {
                FileName = startApp,
                UseShellExecute = true
            };
            using (Process process = new Process())
            {
                process.StartInfo = startInfo;
                process.Start();
            }
        }
    }
}