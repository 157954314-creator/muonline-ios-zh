using System;
using System.IO;
using Client.Data.Texture;
using Client.Main;
using Client.Main.Content;
using Foundation;
using UIKit;

namespace MuIos
{
    [Register("AppDelegate")]
    class Program : UIApplicationDelegate
    {
        private static MuGame _game;

        private static string DocumentsDir =>
            Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);

        private static string EnsureIosConfig()
        {
            string dst = Path.Combine(DocumentsDir, "appsettings.json");
            if (!File.Exists(dst))
            {
                try
                {
                    string bundled = Path.Combine(NSBundle.MainBundle.BundlePath, "appsettings.json");
                    if (File.Exists(bundled))
                    {
                        Directory.CreateDirectory(DocumentsDir);
                        File.Copy(bundled, dst, overwrite: false);
                    }
                }
                catch (Exception ex)
                {
                    SaveCrashLog($"Failed to copy appsettings.json: {ex}");
                }
            }
            return dst;
        }

        private static void SaveCrashLog(string text)
        {
            try
            {
                Directory.CreateDirectory(DocumentsDir);
                string logPath = Path.Combine(DocumentsDir, "MuIos_crash.txt");
                File.AppendAllText(logPath, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}]\n{text}\n\n");
            }
            catch
            {
                // Best-effort crash logging into Files-visible Documents folder.
            }
        }

        private static void ApplyIosDefaults()
        {
            Constants.DRAW_GRASS = false;
            Constants.ENABLE_DYNAMIC_LIGHTS = false;
            Constants.ENABLE_DYNAMIC_LIGHTING_SHADER = true;
            Constants.ENABLE_TERRAIN_GPU_LIGHTING = false;
            Constants.OPTIMIZE_FOR_INTEGRATED_GPU = true;
            Constants.ENABLE_LOW_QUALITY_IN_LOGIN_SCENE = true;
            Constants.ENABLE_ITEM_MATERIAL_SHADER = true;
            Constants.ENABLE_MONSTER_MATERIAL_SHADER = true;
            Constants.ENABLE_WEAPON_TRAIL = false;
            Constants.HIGH_QUALITY_TEXTURES = false;
            Constants.RENDER_SCALE = 0.85f;
            Constants.DYNAMIC_LIGHT_UPDATE_FPS = 30;
            Constants.FOV_SCALE = 0.85f;
        }

        internal static void RunGame()
        {
            UIApplication.SharedApplication.IdleTimerDisabled = true;

            Constants.SETTINGS_PATH = EnsureIosConfig();
            Constants.DataPath = Path.Combine(DocumentsDir, "Data");
            Directory.CreateDirectory(Constants.DataPath);

            ApplyIosDefaults();

            // iOS OpenGL ES does not support desktop S3TC/DXT compressed textures directly.
            TextureLoader.Instance.CustomDecompressFunction = (textureInfo) =>
            {
                if (textureInfo.Format == TextureSurfaceFormat.Dxt1)
                    return DxtDecoder.DecompressDXT1(textureInfo.Data, textureInfo.Width, textureInfo.Height);
                if (textureInfo.Format == TextureSurfaceFormat.Dxt3)
                    return DxtDecoder.DecompressDXT3(textureInfo.Data, textureInfo.Width, textureInfo.Height);
                if (textureInfo.Format == TextureSurfaceFormat.Dxt5)
                    return DxtDecoder.DecompressDXT5(textureInfo.Data, textureInfo.Width, textureInfo.Height);

                throw new NotSupportedException($"Unsupported DXT format for iOS decompression: {textureInfo.Format}");
            };

            AppDomain.CurrentDomain.UnhandledException += (_, e) =>
            {
                if (e.ExceptionObject is Exception ex)
                    SaveCrashLog($"UNHANDLED EXCEPTION:\n{ex}");
            };

            try
            {
                _game = new MuGame();
                _game.Run();
            }
            catch (Exception ex)
            {
                SaveCrashLog($"STARTUP EXCEPTION:\n{ex}");
                throw;
            }
        }

        static void Main(string[] args)
        {
            UIApplication.Main(args, null, typeof(Program));
        }

        public override void FinishedLaunching(UIApplication app)
        {
            RunGame();
        }
    }
}
