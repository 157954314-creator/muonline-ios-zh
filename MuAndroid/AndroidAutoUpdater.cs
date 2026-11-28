#nullable enable
using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using Android.Provider;
using Android.Util;
using Android.Views;
using Android.Widget;
using System;
using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace MuAndroid
{
    /// <summary>
    /// Checks GitHub Release android-latest/version.json on startup and offers
    /// one-tap in-app download + Android package installer launch.
    /// </summary>
    public static class AndroidAutoUpdater
    {
        private const string Tag = "MuAndroidUpdater";
        private const string VersionManifestUrl =
            "https://github.com/157954314-creator/muonline-ios-zh/releases/download/android-latest/version.json";

        private static int _checkedOnce;

        private sealed class RemoteVersionInfo
        {
            [JsonPropertyName("versionCode")]
            public long VersionCode { get; set; }

            [JsonPropertyName("versionName")]
            public string? VersionName { get; set; }

            [JsonPropertyName("apkUrl")]
            public string? ApkUrl { get; set; }

            [JsonPropertyName("notes")]
            public string? Notes { get; set; }
        }

        public static void CheckInBackground(Activity activity)
        {
            if (Interlocked.Exchange(ref _checkedOnce, 1) != 0)
                return;

            _ = Task.Run(() => CheckAndPromptAsync(activity));
        }

        private static async Task CheckAndPromptAsync(Activity activity)
        {
            try
            {
                var (currentCode, currentName) = GetInstalledVersion(activity);
                if (currentCode <= 0)
                    return;

                using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(8) };
                http.DefaultRequestHeaders.UserAgent.ParseAdd("MuMono-Android-Updater/1.0");

                string manifestBase = !string.IsNullOrWhiteSpace(Client.Main.MuGame.AppSettings?.MobileUpdateManifestUrl)
                    ? Client.Main.MuGame.AppSettings.MobileUpdateManifestUrl
                    : VersionManifestUrl;
                string sep = manifestBase.Contains('?') ? "&" : "?";
                string url = $"{manifestBase}{sep}t={DateTimeOffset.UtcNow.ToUnixTimeSeconds()}";
                string json = await http.GetStringAsync(url).ConfigureAwait(false);
                var remote = JsonSerializer.Deserialize<RemoteVersionInfo>(json);

                if (remote == null || string.IsNullOrWhiteSpace(remote.ApkUrl))
                    return;

                Log.Info(Tag, $"Current: v{currentName} ({currentCode}), Remote: v{remote.VersionName} ({remote.VersionCode})");

                if (remote.VersionCode <= currentCode)
                    return;

                activity.RunOnUiThread(() =>
                {
                    if (activity.IsFinishing || activity.IsDestroyed)
                        return;

                    ShowUpdatePrompt(activity, currentName, currentCode, remote);
                });
            }
            catch (Exception ex)
            {
                // Silent fail on offline / timeout — never block gameplay.
                Log.Warn(Tag, $"Update check skipped: {ex.Message}");
            }
        }

        private static (long VersionCode, string VersionName) GetInstalledVersion(Context context)
        {
            try
            {
                var pm = context.PackageManager;
                if (pm == null || string.IsNullOrEmpty(context.PackageName))
                    return (0, "1.0");

#pragma warning disable CA1422
                PackageInfo? info = Build.VERSION.SdkInt >= BuildVersionCodes.Tiramisu
                    ? pm.GetPackageInfo(context.PackageName, PackageManager.PackageInfoFlags.Of(0L))
                    : pm.GetPackageInfo(context.PackageName, 0);
#pragma warning restore CA1422

                if (info == null)
                    return (0, "1.0");

                long code = Build.VERSION.SdkInt >= BuildVersionCodes.P
                    ? info.LongVersionCode
#pragma warning disable CS0618
                    : info.VersionCode;
#pragma warning restore CS0618

                return (code, info.VersionName ?? "1.0");
            }
            catch
            {
                return (0, "1.0");
            }
        }

        private static void ShowUpdatePrompt(
            Activity activity,
            string currentName,
            long currentCode,
            RemoteVersionInfo remote)
        {
            string title = $"发现新版本 v{remote.VersionName ?? remote.VersionCode.ToString()}";
            string notes = string.IsNullOrWhiteSpace(remote.Notes) ? "优化游戏体验并修复已知问题。" : remote.Notes.Trim();
            string message = $"当前版本：v{currentName} (Build {currentCode})\n" +
                             $"最新版本：v{remote.VersionName} (Build {remote.VersionCode})\n\n" +
                             $"更新内容：\n{notes}\n\n" +
                             $"更新不会清除已下载的游戏资源和服务器设置。";

            var builder = new AlertDialog.Builder(activity);
            builder.SetTitle(title);
            builder.SetMessage(message);
            builder.SetCancelable(true);
            builder.SetPositiveButton("立即更新", (_, _) => StartDownloadAndInstall(activity, remote));
            builder.SetNegativeButton("稍后再说", (_, _) => { });
            builder.Show();
        }

        private static void StartDownloadAndInstall(Activity activity, RemoteVersionInfo remote)
        {
            var cts = new CancellationTokenSource();

            var container = new LinearLayout(activity)
            {
                Orientation = Orientation.Vertical
            };
            int pad = (int)(20 * activity.Resources!.DisplayMetrics!.Density);
            container.SetPadding(pad, pad / 2, pad, pad / 2);

            var statusText = new TextView(activity)
            {
                Text = "正在连接服务器..."
            };
            container.AddView(statusText);

            var progressBar = new ProgressBar(activity, null, Android.Resource.Attribute.ProgressBarStyleHorizontal)
            {
                Indeterminate = false,
                Max = 100,
                Progress = 0
            };
            var barParams = new LinearLayout.LayoutParams(
                ViewGroup.LayoutParams.MatchParent,
                ViewGroup.LayoutParams.WrapContent);
            barParams.TopMargin = pad / 2;
            container.AddView(progressBar, barParams);

            var dialog = new AlertDialog.Builder(activity)
                .SetTitle($"正在下载 v{remote.VersionName}")
                .SetView(container)
                .SetCancelable(false)
                .SetNegativeButton("取消", (_, _) => cts.Cancel())
                .Create()!;

            dialog.Show();

            _ = Task.Run(async () =>
            {
                string updateDir = Path.Combine(activity.CacheDir!.AbsolutePath, "updates");
                Directory.CreateDirectory(updateDir);
                string apkPath = Path.Combine(updateDir, "MuMono-update.apk");
                string tempPath = apkPath + ".part";

                try
                {
                    using var http = new HttpClient { Timeout = TimeSpan.FromMinutes(10) };
                    http.DefaultRequestHeaders.UserAgent.ParseAdd("MuMono-Android-Updater/1.0");

                    using var resp = await http.GetAsync(remote.ApkUrl, HttpCompletionOption.ResponseHeadersRead, cts.Token).ConfigureAwait(false);
                    resp.EnsureSuccessStatusCode();

                    long totalBytes = resp.Content.Headers.ContentLength ?? -1;
                    await using var src = await resp.Content.ReadAsStreamAsync(cts.Token).ConfigureAwait(false);
                    await using (var dst = new FileStream(tempPath, FileMode.Create, FileAccess.Write, FileShare.None, 65536, true))
                    {
                        byte[] buffer = new byte[65536];
                        long downloaded = 0;
                        long lastUiTick = 0;
                        int read;
                        while ((read = await src.ReadAsync(buffer, cts.Token).ConfigureAwait(false)) > 0)
                        {
                            await dst.WriteAsync(buffer.AsMemory(0, read), cts.Token).ConfigureAwait(false);
                            downloaded += read;

                            long now = System.Environment.TickCount64;
                            if (now - lastUiTick >= 150)
                            {
                                lastUiTick = now;
                                long dlSnapshot = downloaded;
                                activity.RunOnUiThread(() =>
                                {
                                    if (totalBytes > 0)
                                    {
                                        int pct = (int)Math.Clamp(dlSnapshot * 100L / totalBytes, 0, 100);
                                        progressBar.Indeterminate = false;
                                        progressBar.Progress = pct;
                                        statusText.Text = $"已下载 {dlSnapshot / 1048576.0:F1} MB / {totalBytes / 1048576.0:F1} MB ({pct}%)";
                                    }
                                    else
                                    {
                                        progressBar.Indeterminate = true;
                                        statusText.Text = $"已下载 {dlSnapshot / 1048576.0:F1} MB...";
                                    }
                                });
                            }
                        }
                    }

                    if (File.Exists(apkPath))
                        File.Delete(apkPath);
                    File.Move(tempPath, apkPath);

                    activity.RunOnUiThread(() =>
                    {
                        try { dialog.Dismiss(); } catch { }
                        LaunchPackageInstaller(activity);
                    });
                }
                catch (System.OperationCanceledException)
                {
                    try { if (File.Exists(tempPath)) File.Delete(tempPath); } catch { }
                    activity.RunOnUiThread(() =>
                    {
                        try { dialog.Dismiss(); } catch { }
                        Toast.MakeText(activity, "已取消更新下载", ToastLength.Short)?.Show();
                    });
                }
                catch (Exception ex)
                {
                    try { if (File.Exists(tempPath)) File.Delete(tempPath); } catch { }
                    Log.Error(Tag, $"Update download failed: {ex}");
                    activity.RunOnUiThread(() =>
                    {
                        try { dialog.Dismiss(); } catch { }
                        new AlertDialog.Builder(activity)
                            .SetTitle("更新下载失败")
                            .SetMessage($"下载新版本失败：{ex.Message}\n\n请检查网络后重试。")
                            .SetPositiveButton("重试", (_, _) => StartDownloadAndInstall(activity, remote))
                            .SetNegativeButton("关闭", (_, _) => { })
                            .Show();
                    });
                }
            });
        }

        private static void LaunchPackageInstaller(Activity activity)
        {
            try
            {
                if (Build.VERSION.SdkInt >= BuildVersionCodes.O &&
                    activity.PackageManager != null &&
                    !activity.PackageManager.CanRequestPackageInstalls())
                {
                    new AlertDialog.Builder(activity)
                        .SetTitle("需要允许安装权限")
                        .SetMessage("安装包已下载完成。请在系统设置中开启「允许来自此来源的应用」，返回后即可自动安装更新。")
                        .SetPositiveButton("去开启", (_, _) =>
                        {
                            var settingsIntent = new Intent(Settings.ActionManageUnknownAppSources,
                                Android.Net.Uri.Parse($"package:{activity.PackageName}"));
                            settingsIntent.AddFlags(ActivityFlags.NewTask);
                            activity.StartActivity(settingsIntent);
                        })
                        .SetNegativeButton("直接尝试安装", (_, _) => StartInstallIntent(activity))
                        .Show();
                    return;
                }

                StartInstallIntent(activity);
            }
            catch (Exception ex)
            {
                Log.Error(Tag, $"Failed to launch installer: {ex}");
                Toast.MakeText(activity, $"拉起安装程序失败: {ex.Message}", ToastLength.Long)?.Show();
            }
        }

        private static void StartInstallIntent(Activity activity)
        {
            var apkUri = ApkFileProvider.GetUpdateApkUri();
            var intent = new Intent(Intent.ActionView);
            intent.SetDataAndType(apkUri, "application/vnd.android.package-archive");
            intent.AddFlags(ActivityFlags.GrantReadUriPermission | ActivityFlags.NewTask);
            activity.StartActivity(intent);
        }
    }
}
