#nullable enable
using System;
using System.Collections.Generic;
using Client.Main.Configuration;
using Client.Main.Core.Client;
using Client.Main.Core.Models;
using Client.Main.Models;
using Client.Main.Scenes;

namespace Client.Main.Networking
{
    /// <summary>
    /// Modular dev/testing quick-login component controlled via appsettings.json (MuOnlineSettings.QuickLogin).
    /// Runs once on startup to auto-select server and submit credentials without touching core login logic.
    /// </summary>
    public static class QuickLoginHelper
    {
        private static List<ServerInfo>? _servers;
        private static bool _serverRequested;
        private static bool _loginSent;
        private static bool _completed;

        private static QuickLoginSettings? Settings => MuGame.AppSettings?.QuickLogin;

        public static bool IsEnabled =>
            Settings is { Enabled: true } s &&
            !string.IsNullOrWhiteSpace(s.Username);

        public static void Attach(MuGame game)
        {
            var network = MuGame.Network;
            if (!IsEnabled || network == null)
                return;

            network.ServerListReceived += (_, servers) => _servers = servers;
            network.LoginFailed += (_, _) => _completed = true; // Stop auto-retry on failure
        }

        public static void OnFrame(MuGame game)
        {
            if (_completed || !IsEnabled)
                return;

            var settings = Settings!;
            var network = MuGame.Network;
            if (network == null)
                return;

            var scene = game.ActiveScene;
            if (scene is SelectCharacterScene or GameScene)
            {
                _completed = true;
                return;
            }

            if (scene is not LoginScene || scene.Status != GameControlStatus.Ready)
                return;

            var state = network.CurrentState;
            if (settings.AutoSelectServer && !_serverRequested &&
                state == ClientConnectionState.ReceivedServerList &&
                _servers is { Count: > 0 })
            {
                _serverRequested = true;
                int idx = Math.Clamp(settings.ServerIndex, 0, _servers.Count - 1);
                _ = network.RequestGameServerConnectionAsync(_servers[idx].ServerId);
            }
            else if (settings.AutoSubmit && !_loginSent &&
                     state == ClientConnectionState.ConnectedToGameServer)
            {
                _loginSent = true;
                _ = network.SendLoginRequestAsync(settings.Username, settings.Password ?? string.Empty);
            }
        }
    }
}
