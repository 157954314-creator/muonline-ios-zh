using Client.Main;

#if DEBUG
Constants.DataPath = @"D:\MU_OpenSource_Lab\MuMono_Data\S20\Data";
#endif
if (System.Environment.GetEnvironmentVariable("MU_DATA_PATH") is { Length: > 0 } dataPath)
    Constants.DataPath = dataPath;

using var game = new MuGame();
game.Run();
