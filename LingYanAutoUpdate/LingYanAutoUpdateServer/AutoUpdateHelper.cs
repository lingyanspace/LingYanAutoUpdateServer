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
        internal static readonly HttpClient SharedHttpClient = new HttpClient();

        internal static async Task<string> SaveLocalFileAsync(this string filePath, MemoryStream memoryStream)
        {
            using (var fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None, 4096, true))
            {
                memoryStream.Position = 0;
                await memoryStream.CopyToAsync(fileStream);
            }
            return filePath;
        }

        private const int BufferSize = 262144;
        private const int SpeedSampleCount = 5;

        /// <summary>
        /// 支持断点续传的文件下载，边下边写，极致性能与健壮性
        /// </summary>
        /// <param name="client"></param>
        /// <param name="requestUri"></param>
        /// <param name="progress"></param>
        /// <param name="cancellationToken"></param>
        /// <param name="localFile"></param>
        /// <returns>返回最终文件路径</returns>
        internal static async Task<string> DownloadFileWithResumeAsync(
            this HttpClient client,
            Uri requestUri,
            IProgress<HttpDownloadProgress> progress,
            CancellationToken cancellationToken,
            string localFile)
        {
            if (client == null) throw new ArgumentNullException(nameof(client));
            if (string.IsNullOrWhiteSpace(localFile)) throw new ArgumentNullException(nameof(localFile));

            long existingLength = 0;
            if (File.Exists(localFile))
            {
                var fileInfo = new FileInfo(localFile);
                existingLength = fileInfo.Length;
            }

            var request = new HttpRequestMessage(HttpMethod.Get, requestUri);
            if (existingLength > 0)
                request.Headers.Range = new System.Net.Http.Headers.RangeHeaderValue(existingLength, null);

            using (var responseMessage = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false))
            {
                responseMessage.EnsureSuccessStatusCode();
                var content = responseMessage.Content;
                if (content == null) return localFile;

                var headers = content.Headers;
                var contentLength = headers.ContentLength;
                var totalBytesToReceive = contentLength.HasValue ? (ulong)(contentLength.Value + existingLength) : (ulong?)null;

                var downloadProgress = new HttpDownloadProgress
                {
                    BytesReceived = (ulong)existingLength,
                    TotalBytesToReceive = totalBytesToReceive
                };
                progress?.Report(downloadProgress);

                DateTime lastReportTime = DateTime.UtcNow;
                ulong lastBytesReceived = downloadProgress.BytesReceived;
                Queue<double> speedSamples = new Queue<double>();

                // 文件写入模式：存在则追加，否则新建
                FileMode fileMode = existingLength > 0 ? FileMode.Append : FileMode.Create;
                using (var responseStream = await content.ReadAsStreamAsync().ConfigureAwait(false))
                using (var fileStream = new FileStream(localFile, fileMode, FileAccess.Write, FileShare.None, BufferSize, true))
                {
                    // 如果是断点续传，确保文件流指针在末尾
                    if (existingLength > 0)
                        fileStream.Seek(0, SeekOrigin.End);

                    var buffer = new byte[BufferSize];
                    int bytesRead;
                    while ((bytesRead = await responseStream.ReadAsync(buffer, 0, BufferSize, cancellationToken).ConfigureAwait(false)) > 0)
                    {
                        await fileStream.WriteAsync(buffer, 0, bytesRead, cancellationToken);
                        downloadProgress.BytesReceived += (ulong)bytesRead;

                        DateTime currentTime = DateTime.UtcNow;
                        var timeSpan = currentTime - lastReportTime;
                        if (timeSpan.TotalSeconds > 0.2)
                        {
                            var bytesSinceLastReport = downloadProgress.BytesReceived - lastBytesReceived;
                            double speed = bytesSinceLastReport / timeSpan.TotalSeconds;
                            speedSamples.Enqueue(speed);
                            if (speedSamples.Count > SpeedSampleCount) speedSamples.Dequeue();
                            downloadProgress.DownloadSpeed = speedSamples.Average();

                            lastReportTime = currentTime;
                            lastBytesReceived = downloadProgress.BytesReceived;
                        }
                        progress?.Report(downloadProgress);
                    }
                }
                downloadProgress.DownloadSpeed = 0;
                progress?.Report(downloadProgress);

                return localFile;
            }
        }
    }

    public struct HttpDownloadProgress
    {
        public ulong BytesReceived { get; set; }
        public double DownloadSpeed { get; set; }
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
            var name = Path.GetFileNameWithoutExtension(App.AutoUpdateModel?.RestartApp ?? "");
            int currentProcessId = Process.GetCurrentProcess().Id;
            Process[] processes = Process.GetProcessesByName(name)
                .Where(p => p.Id != currentProcessId).ToArray();
            while (mainClose < 10 && processes.Any(a => !a.HasExited))
            {
                mainClose++;
                foreach (var process in processes)
                {
                    if (!process.HasExited)
                    {
                        try
                        {
                            process.CloseMainWindow();
                            process.WaitForExit(1000);
                            if (!process.HasExited)
                                process.Kill();
                        }
                        catch (Exception ex)
                        {
                            // TODO: 日志记录 ex
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
                    double percent = totalSize > 0 ? hasDownloadSize / totalSize * 100.0 : 0;
                    action.Invoke(percent, hasDownloadSize / 1024 / 1024, totalSize / 1024 / 1024, p.DownloadSpeed / 1024 / 1024);
                }
            });
            try
            {
                await AutoUpdateHelperExtension.SharedHttpClient.DownloadFileWithResumeAsync(
                    new Uri(networkUrl), progress, CancellationToken.None, localUrl);
            }
            catch (Exception ex)
            {
                // TODO: 日志记录 ex
                throw;
            }
            return true;
        }

        internal static async Task UpdateMainApp(
            Action<string, int, int, double> action,
            Action<string> descryptionAction,
            string startApp,
            string updatezipFile,
            string localVersionFile,
            string autoVersion)
        {
            descryptionAction.Invoke("开始升级流程");
            if (string.IsNullOrWhiteSpace(startApp) ||
                string.IsNullOrWhiteSpace(updatezipFile) ||
                string.IsNullOrWhiteSpace(localVersionFile) ||
                string.IsNullOrWhiteSpace(autoVersion) ||
                action == null ||
                descryptionAction == null)
            {
                descryptionAction.Invoke("参数无效");
                throw new ArgumentException("参数无效");
            }

            int maxAttempts = 10;
            int attempt = 0;
            bool success = false;
            string startAppDir = Path.GetDirectoryName(startApp);
            while (attempt < maxAttempts && !success)
            {
                if (!File.Exists(updatezipFile) || !Directory.Exists(startAppDir))
                {
                    descryptionAction.Invoke("升级包或目标目录不存在");
                    break;
                }

                try
                {
                    descryptionAction.Invoke("开始解压升级包");
                    using (ZipArchive archive = ZipFile.OpenRead(updatezipFile))
                    {
                        int totalFiles = archive.Entries.Count;
                        int currentFile = 0;
                        foreach (ZipArchiveEntry entry in archive.Entries)
                        {
                            currentFile++;
                            action.Invoke(entry.Name, currentFile, totalFiles, (double)currentFile / totalFiles * 100);
                            string destinationPath = Path.Combine(startAppDir, entry.FullName);
                            string destinationDir = Path.GetDirectoryName(destinationPath);
                            if (entry.FullName.EndsWith("/") || entry.FullName.EndsWith("\\"))
                            {
                                if (!Directory.Exists(destinationPath))
                                {
                                    Directory.CreateDirectory(destinationPath);
                                    descryptionAction.Invoke($"创建目录: {entry.FullName}");
                                }
                            }
                            else
                            {
                                if (!Directory.Exists(destinationDir))
                                {
                                    Directory.CreateDirectory(destinationDir);
                                    descryptionAction.Invoke($"创建目录: {destinationDir}");
                                }
                                // 文件覆盖前先尝试删除，防止占用
                                if (File.Exists(destinationPath))
                                {
                                    descryptionAction.Invoke($"准备删除旧文件: {entry.FullName}");
                                    if (entry.FullName.ToLower().Contains("UpdateAppFloder/".ToLower()))
                                    {
                                        descryptionAction.Invoke($"检测到升级包文件: {entry.FullName}");
                                        descryptionAction.Invoke($"写入临时新文件: {entry.FullName+".newtemp"}");
                                        using (var entryStream = entry.Open())
                                        using (var fileStream = new FileStream(destinationPath + ".newtemp", FileMode.Create, FileAccess.Write, FileShare.None, 4096, true))
                                        {
                                            await entryStream.CopyToAsync(fileStream);
                                        }
                                        continue;
                                    }
                                    try
                                    {
                                        File.Delete(destinationPath);
                                        descryptionAction.Invoke($"已删除旧文件: {entry.FullName}");
                                    }
                                    catch
                                    {
                                        descryptionAction.Invoke($"删除旧文件失败: {destinationPath}");
                                        continue;
                                    }
                                }
                                descryptionAction.Invoke($"写入新文件: {entry.FullName}");
                                using (var entryStream = entry.Open())
                                using (var fileStream = new FileStream(destinationPath, FileMode.Create, FileAccess.Write, FileShare.None, 4096, true))
                                {
                                    await entryStream.CopyToAsync(fileStream);
                                }
                            }
                        }
                    }
                    descryptionAction.Invoke("解压完成");
                    success = true;
                }
                catch (Exception ex)
                {
                    attempt++;
                    descryptionAction.Invoke($"解压升级包失败, 尝试 {attempt}/{maxAttempts}");
                    if (attempt < maxAttempts)
                        await Task.Delay(200);
                }
            }

            try
            {
                descryptionAction.Invoke($"写入{Path.GetFileName(localVersionFile)}最新版本");
                await Task.Delay(200);

                if (File.Exists(localVersionFile))
                {
                    try { File.Delete(localVersionFile); }
                    catch (Exception ex)
                    {
                        descryptionAction.Invoke($"删除旧版本文件失败: {localVersionFile}, {ex}");
                    }
                }
                File.WriteAllText(localVersionFile, autoVersion);

                descryptionAction.Invoke($"删除升级包");
                await Task.Delay(200);
                if (File.Exists(updatezipFile))
                {
                    try { File.Delete(updatezipFile); }
                    catch (Exception ex)
                    {
                        descryptionAction.Invoke($"删除升级包失败: {updatezipFile}, {ex}");
                    }
                }

                descryptionAction.Invoke($"删除进程传参文件");
                await Task.Delay(200);
                var tempFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "LingYanAutoUpdate.Temp");
                if (File.Exists(tempFile))
                {
                    try { File.Delete(tempFile); }
                    catch (Exception ex)
                    {
                        descryptionAction.Invoke($"删除临时文件失败: {tempFile}, {ex}");
                    }
                }

                descryptionAction.Invoke($"重启主程序");
                await Task.Delay(200);

                if (!File.Exists(startApp))
                {
                    descryptionAction.Invoke($"启动文件不存在: {startApp}");
                    return;
                }

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
                descryptionAction.Invoke("升级流程完成");
            }
            catch
            {
                descryptionAction.Invoke("升级流程失败");
                throw;
            }
        }

        internal static void CoverSelf()
        {
            string batPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "cover_update.bat");
            using (var sw = new StreamWriter(batPath, false, System.Text.Encoding.Default))
            {
                sw.WriteLine("@echo off");
                sw.WriteLine("setlocal enabledelayedexpansion");
                // 强制杀死进程
                sw.WriteLine("taskkill /f /im LingYanAutoUpdateServer.exe >nul 2>nul");
                sw.WriteLine(":wait");
                sw.WriteLine("tasklist | find /i \"LingYanAutoUpdateServer.exe\" >nul");
                sw.WriteLine("if not errorlevel 1 (");
                sw.WriteLine("    timeout /t 1 >nul");
                sw.WriteLine("    goto wait");
                sw.WriteLine(")");
                sw.WriteLine("for /r \"%~dp0\" %%f in (*.newtemp) do (");
                sw.WriteLine("    set \"target=%%~dpnf\"");
                sw.WriteLine("    move /Y \"%%f\" \"!target!\"");
                sw.WriteLine(")");
                // sw.WriteLine("start \"\" \"你的主程序.exe\"");
                // 用cmd /c延迟删除自身
                sw.WriteLine("cmd /c \"ping 127.0.0.1 -n 2 >nul & del \"%~f0\"\"");
            }
            Process.Start(new ProcessStartInfo
            {
                FileName = batPath,
                WindowStyle = ProcessWindowStyle.Hidden
            });
            Environment.Exit(0);
        }
    }
}