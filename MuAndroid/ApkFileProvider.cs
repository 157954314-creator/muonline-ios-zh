#nullable enable
using Android.Content;
using Android.Database;
using Android.OS;
using System.IO;

namespace MuAndroid
{
    /// <summary>
    /// Minimal zero-dependency ContentProvider to expose the downloaded update APK
    /// to the Android package installer via content:// URI (API 24+ compatible).
    /// </summary>
    [ContentProvider(
        new[] { "MuMono.MuMono.apkprovider" },
        Exported = false,
        GrantUriPermissions = true)]
    public sealed class ApkFileProvider : ContentProvider
    {
        public const string Authority = "MuMono.MuMono.apkprovider";

        public static Android.Net.Uri GetUpdateApkUri() =>
            Android.Net.Uri.Parse($"content://{Authority}/update.apk")!;

        public override bool OnCreate() => true;

        public override string? GetType(Android.Net.Uri uri) =>
            "application/vnd.android.package-archive";

        public override ParcelFileDescriptor? OpenFile(Android.Net.Uri uri, string mode)
        {
            var ctx = Context;
            if (ctx?.CacheDir == null)
                throw new Java.IO.FileNotFoundException("CacheDir unavailable");

            string apkPath = Path.Combine(ctx.CacheDir.AbsolutePath, "updates", "MuMono-update.apk");
            var javaFile = new Java.IO.File(apkPath);
            if (!javaFile.Exists())
                throw new Java.IO.FileNotFoundException($"Update APK not found: {apkPath}");

            return ParcelFileDescriptor.Open(javaFile, ParcelFileMode.ReadOnly);
        }

        public override ICursor? Query(Android.Net.Uri uri, string[]? projection, string? selection, string[]? selectionArgs, string? sortOrder) => null;
        public override Android.Net.Uri? Insert(Android.Net.Uri uri, ContentValues? values) => null;
        public override int Delete(Android.Net.Uri uri, string? selection, string[]? selectionArgs) => 0;
        public override int Update(Android.Net.Uri uri, ContentValues? values, string? selection, string[]? selectionArgs) => 0;
    }
}
