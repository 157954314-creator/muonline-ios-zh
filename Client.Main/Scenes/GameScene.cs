// GameScene.cs
using Client.Main.Controls;
using Client.Main.Controls.UI;
using Client.Main.Controls.UI.Game;
using Client.Main.Models;
using Client.Main.Objects.Player;
using Client.Main.Worlds;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using MUnique.OpenMU.Network.Packets.ServerToClient;
using Client.Main.Objects;
using Client.Main.Objects.Effects;
using Client.Main.Objects.Effects.Skills;
using Client.Main.Core.Utilities;
using Client.Main.Networking.PacketHandling.Handlers; // For CharacterClassNumber
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Client.Main.Controls.UI.Game.Inventory;
using Client.Main.Controls.UI.Game.Guild;
using Client.Main.Controls.UI.Game.Friends;
using Client.Main.Controls.UI.Game.Map;
using Client.Main.Controls.UI.Game.Party;
using Client.Main.Controls.UI.Game.PauseMenu;
using Client.Main.Controls.UI.Loading;
using Client.Main.Controls.UI.Game.Character;
using Client.Main.Controls.UI.Game.Trade;
using Client.Main.Controls.UI.Game.Quest;
using Microsoft.Xna.Framework.Graphics;
using Client.Main.Networking;
using System.Reflection;
using System.Diagnostics.CodeAnalysis;
using Client.Main.Controls.UI.Game.Buffs;
using Client.Main.Controls.UI.Game.Hud;
using Client.Main.Controls.UI.Game.MuHelper;
using Client.Main.Controls.UI.Game.Command;
using MUnique.OpenMU.Network.Packets;
using Client.Main.Controllers;
using Client.Main.Helpers;
using Client.Main.Content;

namespace Client.Main.Scenes
{
    public class GameScene : BaseScene
    {
        // ──────────────────────────── Fields ────────────────────────────
        private readonly HeroObject _hero;
        private double _lastHeartbeatMs;
        private const double HeartbeatIntervalMs = 1000.0;
        private ModernBottomHud _modernHud;
        private BottomBarControl _classicBottomBar;
        private TouchActionButtonsControl _classicTouchActions;
        private TouchMenuControl _classicTouchMenu;
        private VirtualJoystickControl _classicJoystick;
        private SkillImprintControl _classicSkillImprint;
        private PotionImprintControl _classicPotionImprint;
        private Controls.UI.Game.Skills.MasterSkillTreeControl _masteryTree;
        private MasteryTreeControl _classicMasteryTree;
        private EquipmentDurabilityHud _equipmentDurabilityHud;
        private GameSceneMapController _mapController;
        private MapListControl _mapListControl;
        private ChatLogWindow _chatLog;
        private MoveCommandWindow _moveCommandWindow;
        public MoveCommandWindow MoveCommandWindow => _moveCommandWindow;
        private ChatInputBoxControl _chatInput;
        private InventoryControl _inventoryControl;
        internal PlayerShopControl PlayerShop { get; private set; }
        internal OwnPlayerShopControl OwnPlayerShop { get; private set; }
        internal GuildInfoWindow GuildInfo { get; private set; }
        internal GuildCreateWindow GuildCreate { get; private set; }
        internal FriendListWindow FriendList { get; private set; }
        internal LetterReadWindow LetterRead { get; private set; }
        internal LetterComposeWindow LetterCompose { get; private set; }
        internal MiniGameScoreWindow MiniGameScore { get; private set; }
        private Client.Main.Core.Client.GuildManager _guildManager;
        private Client.Main.Core.Client.FriendManager _friendManager;
        private Controls.UI.NotificationManager _notificationManager;
        private PartyPanelControl _partyPanel;
        private readonly (string Name, CharacterClassNumber Class, ushort Level, byte[] Appearance) _characterInfo;
        private CharacterInfoWindowControl _characterInfoWindow;
        private MiniMapControl _miniMap;
        private ILogger _logger = MuGame.AppLoggerFactory?.CreateLogger<GameScene>() ?? NullLogger<GameScene>.Instance;
        private LabelControl _pingLabel; // Displays current ping
        private LabelControl _fpsLabel; // Displays current FPS independently of DebugPanel
        private double _pingTimer = 0;
        private double _fpsTimer = 0;
        private int? _lastPingValue = null;
        private int _lastFpsValue = -1;
        private PauseMenuControl _pauseMenu; // ESC menu
        // (SkillQuickSlot removed — replaced by ModernBottomHud)
        private Controls.UI.Game.Skills.SkillSelectionPanel _skillSelectionPanel; // Skill selection panel (independent)
        private bool _modernSkillSelectionWasVisible;
        private bool _modernSkillSelectionWasInteractive;
        private bool _modernMasteryWasVisible;
        private bool _modernMasteryWasInteractive;
        private bool _uiThemeVisibilityInitialized;
        private UiThemeId _lastAppliedUiTheme;
        private CurrentLocationControl _currentLocationControl; // Current map + coordinates (top-left)
        private ActiveBuffsPanel _activeBuffsPanel; // Active buffs display (top-left corner)
        private MuHelperController _muHelper;
        private MuHelperBarControl _muHelperBar;
        private MuHelperWindow _muHelperWindow;
        private PartyWindow _partyWindow;
        private CommandWindow _commandWindow;
        private Client.Main.Effects.BuffEffectController _buffEffectController;
        internal Client.Main.Effects.BuffEffectController BuffEffects => _buffEffectController;
        private Texture2D _backgroundTexture;
        private LoadingScreenView _loadingView;
        private GameSceneSkillController _skillController;
        private GameSceneNotificationController _notificationController;
        private GameScenePlayerMenuController _playerMenuController;
        private GameSceneHotkeys _hotkeys;
        private GameSceneScopeImportController _scopeImportController;
        private GameSceneObjectEditorController _objectEditorController;
        private GameSceneDuelController _duelController;
        private GameSceneChatController _chatController;
        private GameSceneUiPreloadController _uiPreloadController;
        private GameSceneWindowCloseController _windowCloseController;
        private Task _sceneShellInitializationTask;
        private Task _firstPresentedFramePreparationTask;
        private LoadingScreenControl _initialLoadingScreen;
        private bool _sceneShellInitialized;
        private Action _pendingWorldActivation;
        private TaskCompletionSource<bool> _pendingWorldActivationCompletion;
        private bool _pendingWorldActivationScheduled;
        private bool _pendingWorldActivationCleansLoadingUi;
        private string _pendingWorldActivationName;
        private bool _initialWorldActivationCooldown;
        private bool _initialWorldLoadInProgress = true;

        // Performance optimization fields - track object IDs for O(1) lookups
        // ───────────────────────── Properties ─────────────────────────
        public HeroObject Hero => _hero;
        public ChatLogWindow ChatLog => _chatLog;
        public bool IsPkModeEnabled { get; private set; }
        public ushort LockedPvpTargetId { get; private set; }
        public bool IsJoystickActive => _classicJoystick?.IsActive == true;
        public Vector2 JoystickWorldDirection => _classicJoystick?.CurrentWorldDirection ?? Vector2.Zero;
        public void PickupNearestItem() => _hotkeys?.TriggerPickupNearestItem();

        public bool CanAttackPlayer(PlayerObject targetPlayer)
        {
            if (targetPlayer == null || targetPlayer == _hero || targetPlayer.IsDead || targetPlayer.World != World)
                return false;
            if (MuGame.Network?.GetPartyManager()?.IsMember(targetPlayer.NetworkId) == true)
                return false;
            if (_duelController?.IsDuelAttackTarget(targetPlayer) == true)
                return true;
            var kb = MuGame.Instance.Keyboard;
            return IsPkModeEnabled || kb.IsKeyDown(Keys.LeftControl) || kb.IsKeyDown(Keys.RightControl);
        }

        public void TogglePkMode()
        {
            IsPkModeEnabled = !IsPkModeEnabled;
            if (!IsPkModeEnabled)
                LockedPvpTargetId = 0;

            string msg = IsPkModeEnabled
                ? (Localization.Loc.IsChinese ? "已开启 [PK 强攻] 模式（自动补血护航已激活）" : "PK Mode ON (Auto-Potion Escort active)")
                : (Localization.Loc.IsChinese ? "已切回 [和平] 模式" : "Peace Mode ON");
            _chatLog?.AddMessage("System", msg, MessageType.System);
        }

        public PlayerObject GetLockedOrNearestPvpTarget(float maxTiles = 13f)
        {
            if (World?.Players == null || _hero == null)
                return null;

            var players = World.Players;
            float maxLockSq = 16f * 16f;
            if (LockedPvpTargetId != 0)
            {
                for (int i = 0; i < players.Count; i++)
                {
                    var p = players[i];
                    if (p != null && p != _hero && !p.IsDead && p.World == World &&
                        p.NetworkId == LockedPvpTargetId &&
                        MuGame.Network?.GetPartyManager()?.IsMember(p.NetworkId) != true &&
                        Vector2.DistanceSquared(_hero.Location, p.Location) <= maxLockSq)
                    {
                        return p;
                    }
                }
                LockedPvpTargetId = 0;
            }

            PlayerObject nearest = null;
            float bestSq = maxTiles * maxTiles;
            for (int i = 0; i < players.Count; i++)
            {
                var p = players[i];
                if (p == null || p == _hero || p.IsDead || p.World != World)
                    continue;
                if (MuGame.Network?.GetPartyManager()?.IsMember(p.NetworkId) == true)
                    continue;
                float d = Vector2.DistanceSquared(_hero.Location, p.Location);
                if (d < bestSq)
                {
                    bestSq = d;
                    nearest = p;
                }
            }
            return nearest;
        }

        public PlayerObject CyclePvpTarget(float maxTiles = 15f)
        {
            if (World?.Players == null || _hero == null)
                return null;

            var candidates = new List<PlayerObject>();
            float maxSq = maxTiles * maxTiles;
            var players = World.Players;
            for (int i = 0; i < players.Count; i++)
            {
                var p = players[i];
                if (p == null || p == _hero || p.IsDead || p.World != World)
                    continue;
                if (MuGame.Network?.GetPartyManager()?.IsMember(p.NetworkId) == true)
                    continue;
                if (Vector2.DistanceSquared(_hero.Location, p.Location) <= maxSq)
                    candidates.Add(p);
            }

            if (candidates.Count == 0)
            {
                LockedPvpTargetId = 0;
                _chatLog?.AddMessage("System", Localization.Loc.IsChinese ? "附近没有可锁定的玩家。" : "No players nearby to lock.", MessageType.System);
                return null;
            }

            candidates.Sort((a, b) =>
                Vector2.DistanceSquared(_hero.Location, a.Location)
                    .CompareTo(Vector2.DistanceSquared(_hero.Location, b.Location)));

            int nextIdx = 0;
            if (LockedPvpTargetId != 0)
            {
                for (int i = 0; i < candidates.Count; i++)
                {
                    if (candidates[i].NetworkId == LockedPvpTargetId)
                    {
                        nextIdx = (i + 1) % candidates.Count;
                        break;
                    }
                }
            }

            PlayerObject chosen = candidates[nextIdx];
            LockedPvpTargetId = chosen.NetworkId;
            if (!IsPkModeEnabled && _duelController?.IsDuelAttackTarget(chosen) != true)
                IsPkModeEnabled = true;

            int distTiles = (int)MathF.Round(Vector2.Distance(_hero.Location, chosen.Location));
            _chatLog?.AddMessage("System",
                Localization.Loc.IsChinese ? $"已锁定对手：{chosen.Name}（{distTiles} 格）" : $"Locked target: {chosen.Name} ({distTiles}m)",
                MessageType.System);
            return chosen;
        }
        public InventoryControl InventoryControl => _inventoryControl;
        public TradeControl TradeControl => TradeControl.Instance;
        public PauseMenuControl PauseMenu => _pauseMenu;
        internal ModernBottomHud ModernHud => _modernHud;
        internal GameSceneSkillController SkillController => _skillController;
        internal MuHelperController MuHelper => _muHelper;
        internal MuHelperWindow MuHelperWindow => _muHelperWindow;
        internal PartyWindow PartyWindow => _partyWindow;
        internal CommandWindow CommandWindow => _commandWindow;
        internal Controls.UI.Game.Skills.MasterSkillTreeControl MasteryTree => _masteryTree;
        internal MasteryTreeControl ClassicMasteryTree => _classicMasteryTree;

        public override bool CanRenderWhileInitializing => true;

        public static readonly IReadOnlyDictionary<byte, Type> MapWorldRegistry = DiscoverWorlds();

        [UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "World registry uses reflection; trimming is not supported for scene discovery.")]
        private static IReadOnlyDictionary<byte, Type> DiscoverWorlds()
        {
            var registry = new Dictionary<byte, Type>();
            var worldTypes = Assembly.GetExecutingAssembly().GetTypes()
                .Where(t => t.IsClass && !t.IsAbstract && typeof(WalkableWorldControl).IsAssignableFrom(t));

            foreach (var type in worldTypes)
            {
                var attr = type.GetCustomAttribute<WorldInfoAttribute>();
                if (attr != null)
                {
                    if (!registry.TryAdd((byte)attr.MapId, type))
                    {
                        // A duplicate MapId makes the registry pick an arbitrary class and leaves
                        // the intended map unreachable (it falls back to Lorencia), so surface it.
                        MuGame.AppLoggerFactory?.CreateLogger<GameScene>().LogWarning(
                            "Duplicate WorldInfo MapId {MapId}: {Type} ignored, {Existing} already registered.",
                            attr.MapId, type.Name, registry[(byte)attr.MapId].Name);
                    }
                }
            }
            return registry;
        }

        // ──────────────────────── Constructors ────────────────────────
        public GameScene((string Name, CharacterClassNumber Class, ushort Level, byte[] Appearance) characterInfo)
        {
            _characterInfo = characterInfo;
            _logger?.LogDebug(
                "GameScene shell created for Character: {Name} ({Class})",
                _characterInfo.Name,
                _characterInfo.Class);

            // Keep the constructor intentionally small. Scene construction happens inside a
            // main-thread dispatcher action, so building the full HUD here previously made
            // HandleEnteredGame block the game for roughly 150 ms.
            _hero = new HeroObject(new AppearanceData(characterInfo.Appearance));
        }

        public override Task PrepareForFirstPresentedFrameAsync()
        {
            _firstPresentedFramePreparationTask ??= PrepareFirstPresentedFrameCoreAsync();
            return _firstPresentedFramePreparationTask;
        }

        private async Task PrepareFirstPresentedFrameCoreAsync()
        {
            // Build only scene-owned loading resources here. Shared/singleton game controls are
            // attached after the previous GameScene has been disposed, so a GameScene-to-GameScene
            // fallback cannot detach or reset controls which already belong to the new scene.
            if (_backgroundTexture == null)
            {
                try
                {
                    _backgroundTexture = MuGame.Instance.Content.Load<Texture2D>("Background");
                }
                catch (Exception ex)
                {
                    _logger?.LogDebug("[GameScene] Background load failed: {Message}", ex.Message);
                }
            }

            if (_loadingView == null)
            {
                _loadingView = new LoadingScreenView { FallbackTexture = _backgroundTexture };
                var mapId = MuGame.Network?.GetCharacterState()?.MapId;
                if (mapId.HasValue && MapWorldRegistry.TryGetValue((byte)mapId.Value, out Type destination))
                    _loadingView.SetDestination(destination);
                _loadingView.Progress = 0.01f;
                _loadingView.StatusText = "Preparing game interface...";
                Controls.Add(_loadingView);
                await LoadingScreenView.PreloadAsync(_loadingView.Art);
            }

            if (_initialLoadingScreen == null)
            {
                _initialLoadingScreen = new LoadingScreenControl
                {
                    Visible = true,
                    Message = "Preparing game interface...",
                    Progress = 0.01f
                };
                Controls.Add(_initialLoadingScreen);
            }

            if (_loadingView.Status == GameControlStatus.NonInitialized)
                await _loadingView.Initialize();

            if (_initialLoadingScreen.Status == GameControlStatus.NonInitialized)
                await _initialLoadingScreen.Initialize();
        }

        public override async Task InitializeWithProgressReporting(Action<string, float> progressCallback)
        {
            await PrepareForFirstPresentedFrameAsync();

            Action<string, float> effectiveProgressCallback = progressCallback ?? UpdateLoadProgress;
            _sceneShellInitializationTask ??= InitializeSceneShellAsync(effectiveProgressCallback);
            await _sceneShellInitializationTask;

            await MuGame.YieldToNextFrameAsync(
                "GameScene.InitializeControls",
                MainThreadDispatcher.WorkPriority.High);
            await base.InitializeWithProgressReporting(effectiveProgressCallback);
        }

        private async Task InitializeSceneShellAsync(Action<string, float> progressCallback)
        {
            void Report(string message, float progress)
            {
                progressCallback?.Invoke(message, progress);

                var loading = _initialLoadingScreen ?? _mapController?.LoadingScreen;
                if (loading != null)
                {
                    loading.Message = message;
                    loading.Progress = progress;
                }

                if (_loadingView != null)
                {
                    _loadingView.StatusText = message;
                    _loadingView.Progress = progress;
                }
            }

            Report("Preparing game interface...", 0.01f);

            // Phase 1: controls required by the loading and messaging paths.
            Controls.Add(NpcShopControl.Instance);
            Controls.Add(VaultControl.Instance);
            Controls.Add(ChaosMixControl.Instance);
            Controls.Add(TradeControl.Instance);
            Controls.Add(QuestDialogControl.Instance);
            Controls.Add(DevilSquareEnterControl.Instance);
            Controls.Add(BloodCastleEnterControl.Instance);
            Controls.Add(BloodCastleTimeControl.Instance);
            Controls.Add(BloodCastleResultControl.Instance);
            PlayerShop = new PlayerShopControl();
            PlayerShop.ShopClosed += (ownerId, ownerName) =>
            {
                var service = MuGame.Network?.GetCharacterService();
                if (service != null) _ = service.SendPlayerShopCloseOtherAsync(ownerId, ownerName);
            };
            PlayerShop.RefreshRequested += (ownerId, ownerName) =>
            {
                var service = MuGame.Network?.GetCharacterService();
                if (service != null) _ = service.SendPlayerStoreListRequestAsync(ownerId, ownerName);
            };
            PlayerShop.BuyRequested += async (ownerId, ownerName, slot) =>
            {
                var service = MuGame.Network?.GetCharacterService();
                if (service == null || !await service.SendPlayerShopItemBuyRequestAsync(ownerId, ownerName, slot))
                {
                    PlayerShop.PurchaseFinished();
                    RequestDialog.ShowInfo("Could not send the purchase request.");
                }
            };
            Controls.Add(PlayerShop);

            _mapListControl = new MapListControl { Visible = false };
            _chatLog = new ChatLogWindow
            {
                X = 5
            };
            Controls.Add(_chatLog);

            _chatInput = new ChatInputBoxControl(_chatLog, MuGame.AppLoggerFactory)
            {
                X = 5
            };
            Controls.Add(_chatInput);
            ApplyChatThemeLayout();
            _duelController = new GameSceneDuelController(this, _chatLog, _logger);

            _notificationManager = new Controls.UI.NotificationManager();
            Controls.Add(_notificationManager);
            _notificationManager.BringToFront();
            _notificationController = new GameSceneNotificationController(_notificationManager, _chatLog);
            _notificationController.AddPending(ChatMessageHandler.TakePendingServerMessages());
            _scopeImportController = new GameSceneScopeImportController(this, _logger);

            await MuGame.YieldToNextFrameAsync(
                "GameScene.BuildShell.Inventory",
                MainThreadDispatcher.WorkPriority.High);

            // Phase 2: inventory and common windows.
            _inventoryControl = new InventoryControl(MuGame.Network, MuGame.AppLoggerFactory);
            Controls.Add(_inventoryControl);
            _inventoryControl.HookEvents();
            OwnPlayerShop = new OwnPlayerShopControl(MuGame.Network.GetCharacterState());
            Controls.Add(OwnPlayerShop);
            _guildManager = MuGame.Network.GetGuildManager();
            GuildInfo = new GuildInfoWindow(_guildManager, MuGame.Network.GetCharacterState());
            Controls.Add(GuildInfo);
            GuildCreate = new GuildCreateWindow();
            Controls.Add(GuildCreate);
            var friendManager = MuGame.Network.GetFriendManager();
            FriendList = new FriendListWindow(friendManager);
            Controls.Add(FriendList);
            LetterRead = new LetterReadWindow(friendManager);
            Controls.Add(LetterRead);
            LetterCompose = new LetterComposeWindow(friendManager);
            Controls.Add(LetterCompose);
            Controls.Add(new LetterNotifierControl(friendManager));
            MiniGameScore = new MiniGameScoreWindow();
            Controls.Add(MiniGameScore);
            _friendManager = friendManager;
            _friendManager.FriendRequestReceived += OnFriendRequestReceived;
            _friendManager.ChatRoomOpened += OnChatRoomOpened;
            _friendManager.NewLetterReceived += OnNewLetterReceived;
            _friendManager.FriendCameOnline += OnFriendCameOnline;
            _guildManager.GuildMasterDialogRequested += OnGuildMasterDialogRequested;
            _guildManager.GuildJoinRequestReceived += OnGuildJoinRequestReceived;
            _guildManager.GuildCreationDialogRequested += OnGuildCreationDialogRequested;
            _windowCloseController = new GameSceneWindowCloseController(_inventoryControl, _logger);

            _moveCommandWindow = new MoveCommandWindow(MuGame.AppLoggerFactory, MuGame.Network);
            Controls.Add(_moveCommandWindow);
            _moveCommandWindow.MapWarpRequested += OnMapWarpRequested;

            _characterInfoWindow = new CharacterInfoWindowControl { X = 20, Y = 50, Visible = false };
            Controls.Add(_characterInfoWindow);
            _miniMap = new MiniMapControl(this);
            Controls.Add(_miniMap);
            _partyPanel = new PartyPanelControl();
            Controls.Add(_partyPanel);

            _fpsLabel = new LabelControl
            {
                Text = "FPS: --",
                Align = ControlAlign.Top | ControlAlign.Right,
                Margin = new Margin { Top = 5, Right = 5 },
                FontSize = 10,
                TextColor = Color.LightGreen
            };
            Controls.Add(_fpsLabel);

            _pingLabel = new LabelControl
            {
                Text = "Ping: --",
                Align = ControlAlign.Top | ControlAlign.Right,
                Margin = new Margin { Top = 22, Right = 5 },
                FontSize = 10,
                TextColor = Color.White
            };
            Controls.Add(_pingLabel);

            await MuGame.YieldToNextFrameAsync(
                "GameScene.BuildShell.Hud",
                MainThreadDispatcher.WorkPriority.High);

            // Phase 3: HUD and interaction controllers.
            var characterState = MuGame.Network.GetCharacterState();
            _pauseMenu = new PauseMenuControl();
            Controls.Add(_pauseMenu);

            _skillSelectionPanel = new Controls.UI.Game.Skills.SkillSelectionPanel();
            Controls.Add(_skillSelectionPanel);

            _modernHud = new ModernBottomHud(characterState, _skillSelectionPanel);
            Controls.Add(_modernHud);

            // Classic owns a separate HUD tree. The controls are created once and only their
            // visibility changes, so switching themes cannot leave either HUD underneath.
            _classicBottomBar = new BottomBarControl(characterState, _modernHud);
            _classicTouchActions = new TouchActionButtonsControl();
            _classicJoystick = new VirtualJoystickControl();
            _classicSkillImprint = new SkillImprintControl(characterState, _modernHud);
            _classicPotionImprint = new PotionImprintControl(characterState, _modernHud);
            _classicTouchMenu = new TouchMenuControl
            {
                HotbarToHide = _classicTouchActions,
                ImprintPanel = _classicSkillImprint,
                PotionPanel = _classicPotionImprint
            };
            Controls.Add(_classicBottomBar);
            Controls.Add(_classicTouchActions);
            Controls.Add(_classicJoystick);
            Controls.Add(_classicSkillImprint);
            Controls.Add(_classicPotionImprint);
            Controls.Add(_classicTouchMenu);

            _masteryTree = new Controls.UI.Game.Skills.MasterSkillTreeControl();
            _classicMasteryTree = new MasteryTreeControl(characterState);
            Controls.Add(_masteryTree);
            Controls.Add(_classicMasteryTree);
            _equipmentDurabilityHud = new EquipmentDurabilityHud(characterState);
            Controls.Add(_equipmentDurabilityHud);
            _skillController = new GameSceneSkillController(
                this,
                _modernHud,
                _logger,
                _duelController.IsDuelAttackTarget);
            ApplyUiThemeVisibility();

            _currentLocationControl = new CurrentLocationControl(characterState);
            Controls.Add(_currentLocationControl);
            _activeBuffsPanel = new ActiveBuffsPanel(characterState, _currentLocationControl);
            Controls.Add(_activeBuffsPanel);

            _muHelper = new MuHelperController(this, characterState);
            _muHelperWindow = new MuHelperWindow(_muHelper);
            _muHelperBar = new MuHelperBarControl(_muHelper, _currentLocationControl, () => _muHelperWindow.Toggle());
            Controls.Add(_muHelperBar);
            Controls.Add(_muHelperWindow);

            _partyWindow = new PartyWindow();
            Controls.Add(_partyWindow);

            _commandWindow = new CommandWindow(
                this,
                StartWhisperToPlayer,
                _duelController.OnDuelRequestedFromContextMenu);
            Controls.Add(_commandWindow);

            var duelHud = new DuelHudControl(characterState);
            Controls.Add(duelHud);
            Controls.Add(DevilSquareCountdownControl.Instance);

            await MuGame.YieldToNextFrameAsync(
                "GameScene.BuildShell.Controllers",
                MainThreadDispatcher.WorkPriority.High);

            // Phase 4: interaction controllers.
            _playerMenuController = new GameScenePlayerMenuController(
                this,
                StartWhisperToPlayer,
                _duelController.OnDuelRequestedFromContextMenu);
            _playerMenuController.Initialize();
            _objectEditorController = new GameSceneObjectEditorController(this, _logger);
            _objectEditorController.Initialize();
            _hotkeys = new GameSceneHotkeys(
                this,
                _pauseMenu,
                _playerMenuController,
                _moveCommandWindow,
                _inventoryControl,
                _characterInfoWindow,
                _miniMap,
                _chatInput,
                _chatLog,
                _objectEditorController,
                _logger);

            await MuGame.YieldToNextFrameAsync(
                "GameScene.BuildShell.LoadingInfrastructure",
                MainThreadDispatcher.WorkPriority.High);

            // Phase 5: complete the loading infrastructure prepared before scene activation.
            // The background and progress controls already exist, so no active frame can expose
            // only the cleared render target while this heavier shell is being assembled.
            _mapController = new GameSceneMapController(
                this,
                _modernHud,
                _loadingView,
                _chatLog,
                _chatInput,
                _mapListControl,
                DebugPanel,
                Cursor,
                _scopeImportController,
                _logger,
                _initialLoadingScreen);
            _initialLoadingScreen = null;
            _mapController.EnsureLoadingScreen();
            _chatController = new GameSceneChatController(_mapController, _duelController, _chatLog, _logger);
            _chatInput.MessageSendRequested += _chatController.OnChatMessageSendRequested;
            _uiPreloadController = new GameSceneUiPreloadController(this, _logger);

            await MuGame.YieldToNextFrameAsync(
                "GameScene.BuildShell.Ordering",
                MainThreadDispatcher.WorkPriority.High);

            // Phase 6: z-order changes are separated because BringToFront mutates the controls
            // collection and repeatedly recalculates ordering.
            _fpsLabel.BringToFront();
            _pingLabel.BringToFront();
            _chatInput.BringToFront();
            _pauseMenu.BringToFront();
            _modernHud.BringToFront();
            _classicBottomBar.BringToFront();
            _classicTouchActions.BringToFront();
            _classicJoystick.BringToFront();
            _classicSkillImprint.BringToFront();
            _classicPotionImprint.BringToFront();
            _classicMasteryTree.BringToFront();
            _classicTouchMenu.BringToFront();
            _equipmentDurabilityHud.BringToFront();
            _currentLocationControl.BringToFront();
            _activeBuffsPanel.BringToFront();
            _muHelperBar.BringToFront();
            duelHud.BringToFront();
            DevilSquareCountdownControl.Instance.BringToFront();
            DebugPanel.BringToFront();
            Cursor.BringToFront();

            // Subscribe after the complete UI tree exists. ThemeChanged is synchronous, so
            // this makes control layout updates finish before the scene repositions dependent
            // controls such as the chat log and input box.
            UiThemeManager.ThemeChanged += HandleUiThemeChanged;
            _sceneShellInitialized = true;
            Report("Game interface prepared.", 0.04f);

            // Optional assets are deliberately not awaited by the scene transition.
            _ = _uiPreloadController.StartPreloadAsync();
        }

        public GameScene() : this(GetCharacterInfoFromState())
        {
        }

        public override void AfterLoad()
        {
            base.AfterLoad();
            ApplyChatThemeLayout();
        }

        public GameScene((string Name, CharacterClassNumber Class, ushort Level, byte[] Appearance) characterInfo, NetworkManager networkManager)
            : this(characterInfo)
        {
            // Optionally store networkManager if needed in the future
        }

        private static (string Name, CharacterClassNumber Class, ushort Level, byte[] Appearance) GetCharacterInfoFromState()
        {
            var state = MuGame.Network?.GetCharacterState();
            if (state != null)
            {
                return (state.Name ?? "Unknown", state.Class, state.Level, Array.Empty<byte>());
            }
            return ("Unknown", CharacterClassNumber.DarkKnight, 1, Array.Empty<byte>());
        }

        // ───────────────────── Content Loading (Progressive) ─────────────────────
        private void UpdateLoadProgress(string message, float progress)
        {
            if (MuGame.IsMainThread)
            {
                _mapController?.UpdateLoadProgress(message, progress);
                return;
            }

            MuGame.ScheduleOnMainThread(
                () => _mapController?.UpdateLoadProgress(message, progress),
                MainThreadDispatcher.WorkPriority.High,
                "GameScene.UpdateLoadProgress");
        }

        protected override async Task LoadSceneContentWithProgress(Action<string, float> progressCallback)
        {
            WorldControl worldInstance = null;
            try
            {
                UpdateLoadProgress("Initializing Game Scene...", 0.0f);

                var charState = MuGame.Network?.GetCharacterState();
                if (charState == null)
                {
                    UpdateLoadProgress("Error: CharacterState is null.", 1.0f);
                    _logger?.LogDebug("CharacterState is null in GameScene.Load, cannot proceed.");
                    _modernHud.Visible = false;
                    return;
                }

                // Phase 1: apply the small, data-only hero state.
                UpdateLoadProgress("Setting up hero info...", 0.05f);
                _hero.CharacterClass = _characterInfo.Class;
                _hero.Name = _characterInfo.Name;
                charState.UpdateCoreCharacterInfo(
                    charState.Id,
                    _characterInfo.Name,
                    _characterInfo.Class,
                    _characterInfo.Level,
                    charState.PositionX,
                    charState.PositionY,
                    charState.MapId);
                _hero.NetworkId = charState.Id;
                _hero.Location = new Vector2(charState.PositionX, charState.PositionY);
                if (_windowCloseController != null)
                {
                    _hero.PlayerMoved += _windowCloseController.OnHeroMoved;
                    _hero.PlayerTookDamage += _windowCloseController.OnHeroTookDamage;
                }

                Type initialWorldType = typeof(LorenciaWorld);
                if (MapWorldRegistry.TryGetValue((byte)charState.MapId, out Type mappedType))
                    initialWorldType = mappedType;
                else
                    _logger?.LogWarning("Unknown MapId {MapId}. Defaulting to Lorencia.", charState.MapId);

                await MuGame.YieldToNextFrameAsync(
                    $"GameScene.Load.CreateWorld.{initialWorldType.Name}",
                    MainThreadDispatcher.WorkPriority.Critical);

                // Phase 2: create a hidden world shell. Keeping it hidden prevents the renderer
                // from cold-starting terrain, culling and model buffers before loading completes.
                UpdateLoadProgress($"Creating world: {initialWorldType.Name}...", 0.20f);
                if (World != null)
                {
                    Controls.Remove(World);
                    World.Dispose();
                    World = null;
                }

                worldInstance = (WorldControl)Activator.CreateInstance(initialWorldType);
                worldInstance.Visible = false;
                Controls.Add(worldInstance);
                World = worldInstance;

                if (worldInstance is WalkableWorldControl walkable)
                {
                    walkable.Walker = _hero;
                    _scopeImportController?.EnsureWalkerNetworkId(walkable, charState.Id, "initial world shell");
                }

                _hero.World = worldInstance;

                await MuGame.YieldToNextFrameAsync(
                    $"GameScene.Load.InitializeWorld.{initialWorldType.Name}",
                    MainThreadDispatcher.WorkPriority.Critical);

                // Phase 3: initialize the hidden world. Any unavoidable cold I/O is now isolated
                // to a named transition phase and cannot be combined with hero publication.
                UpdateLoadProgress($"Loading world: {initialWorldType.Name}...", 0.30f);
                await worldInstance.Initialize();
                UpdateLoadProgress($"World {initialWorldType.Name} initialized.", 0.60f);

                await MuGame.YieldToNextFrameAsync(
                    "GameScene.Load.HeroAssets",
                    MainThreadDispatcher.WorkPriority.Critical);

                // Phase 4: load the hero before adding it to the live object collection.
                UpdateLoadProgress("Loading hero assets...", 0.65f);
                if (_hero.Status == GameControlStatus.NonInitialized ||
                    _hero.Status == GameControlStatus.Initializing)
                {
                    await _hero.Load();
                }

                // Asset loading may complete on the thread pool. Marshal back before touching
                // model buffers and split prewarm from publication into separate frames.
                await MuGame.YieldToNextFrameAsync(
                    "GameScene.Load.PrepareHero",
                    MainThreadDispatcher.WorkPriority.Critical);
                _scopeImportController?.EnsureHeroNetworkId(charState.Id, "after hero Load()");
                _hero.SnapToTerrainHeight(updateCamera: false);
                await _hero.PrepareGpuTexturesForFirstFrameAsync();
                _hero.PrepareRenderResourcesForFirstFrame();
                await CharacterSpawnEffect.PreloadAsync();

                await MuGame.YieldToNextFrameAsync(
                    "GameScene.Load.PublishHero",
                    MainThreadDispatcher.WorkPriority.Critical);

                if (!worldInstance.Objects.Contains(_hero))
                {
                    worldInstance.Objects.Add(_hero);
                    CharacterSpawnEffect.Start(_hero);
                }
                if (worldInstance is WalkableWorldControl initializedWalkable)
                    _scopeImportController?.EnsureWalkerNetworkId(initializedWalkable, charState.Id, "after hero publication");

                // Phase 5: queue each scope category in a separate frame. Remote objects load
                // asynchronously and are published only after their own assets are ready.
                UpdateLoadProgress("Importing nearby players...", 0.80f);
                await (_scopeImportController?.ImportPendingRemotePlayersAsync() ?? Task.CompletedTask);
                await MuGame.YieldToNextFrameAsync(
                    "GameScene.Load.ImportNpcsMonsters",
                    MainThreadDispatcher.WorkPriority.High);

                UpdateLoadProgress("Importing nearby NPCs and monsters...", 0.86f);
                await (_scopeImportController?.ImportPendingNpcsMonstersAsync() ?? Task.CompletedTask);
                await MuGame.YieldToNextFrameAsync(
                    "GameScene.Load.ImportDroppedItems",
                    MainThreadDispatcher.WorkPriority.High);

                UpdateLoadProgress("Importing dropped items...", 0.90f);
                await (_scopeImportController?.ImportPendingDroppedItemsAsync() ?? Task.CompletedTask);

                await MuGame.YieldToNextFrameAsync(
                    "GameScene.Load.PrepareVisibility",
                    MainThreadDispatcher.WorkPriority.High);

                // Build the first visibility snapshot while the loading screen is still active.
                // This moves the initial spatial/culling rebuild out of the first gameplay frame.
                await worldInstance.PrepareInitialRenderResourcesAsync(
                    "GameScene.Load.PrewarmModel");

                await MuGame.YieldToNextFrameAsync(
                    "GameScene.Load.PreloadCombatAssets",
                    MainThreadDispatcher.WorkPriority.Low);
                UpdateLoadProgress("Preloading effects and sounds...", 0.96f);
                await PreloadCombatAssetsAsync();
                await MonsterHitEffect.PrewarmRenderingAsync(worldInstance);

                await MuGame.YieldToNextFrameAsync(
                    "GameScene.Load.PrewarmCombatRendering",
                    MainThreadDispatcher.WorkPriority.Low);
                DamageTextObject.PrewarmRendering();

                await MuGame.YieldToNextFrameAsync(
                    "GameScene.Load.Finalize",
                    MainThreadDispatcher.WorkPriority.Critical);

                if (worldInstance is WalkableWorldControl finalWalkable)
                    _scopeImportController?.EnsureWalkerNetworkId(finalWalkable, charState.Id, "final verification");

                _mapController?.UpdateLoadProgress("Preparing first frame...", 0.99f);
                _ = QueueWorldActivationAfterLoadingFrame(() =>
                {
                    _hero.SnapToTerrainHeight();
                    worldInstance.Visible = true;
                    _modernHud.Visible = true;
                    _mapController?.UpdateLoadProgress("Game ready!", 1.0f);
                    ScheduleMapNameUpdateNextFrame("GameScene.UpdateInitialMapName");
                    _ = RefreshMiniMapAsync();
                }, "GameScene.ActivateInitialWorld");

                // Complete this nested async workflow outside the dispatcher action which ran
                // GameScene.Load.Finalize. This prevents parent scene-initialization continuations
                // from being charged to (and executed inside) the same frame-budgeted action.
                await Task.Yield();
            }
            finally
            {
                // Activation owns loading-screen cleanup. On failures there is no queued
                // activation, so release the loading UI immediately.
                if (_pendingWorldActivation == null)
                {
                    _initialWorldLoadInProgress = false;
                    _mapController?.DisposeLoadingScreen();
                    if (_loadingView != null)
                        _loadingView.Visible = false;
                }
            }
        }

        public override async Task Load()
        {
            // This method is called by BaseScene.Initialize() if LoadSceneContentWithProgress is not overridden,
            // OR if the overridden method calls base.Load().
            // For GameScene, we want the progressive loading, so we'll call it from here if this Load is hit.
            // However, with the new structure, InitializeWithProgressReporting should call LoadSceneContentWithProgress directly.
            // This is a fallback / ensures old paths might still work or for clarity.
            if (Status == GameControlStatus.Initializing) // Check if we are already in the new init flow
            {
                await LoadSceneContentWithProgress(UpdateLoadProgress);
            }
            else
            {
                // Fallback to old behavior or log a warning
                _logger?.LogDebug("GameScene.Load() called outside of InitializeWithProgressReporting flow. Consider refactoring.");
                await base.Load(); // Which is empty in BaseScene, then calls derived GameScene's old Load logic
            }
        }

        private async void OnMapWarpRequested(int mapIndex, string mapDisplayName)
        {
            _logger?.LogDebug($"Player requested warp to map index: {mapIndex}");
            var mapName = mapDisplayName;
            _chatLog.AddMessage("System", $"Warping to {mapName} (ID {mapIndex})...", MessageType.System);

            try
            {
                await MuGame.Network.SendWarpRequestAsync((ushort)mapIndex);
            }
            catch (Exception ex)
            {
                _logger?.LogDebug(ex, $"Error sending warp request for map index {mapIndex}.");
                _chatLog.AddMessage("System", $"Error warping: {ex.Message}", MessageType.Error);
            }
        }

        // ─────────────────── Map Change Logic (Remains largely the same) ───────────────────
        public async Task ChangeMap([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)] Type worldType)
        {
            if (_mapController != null)
            {
                await _mapController.ChangeMap(worldType);
            }
        }

        public async Task ChangeMap<T>() where T : WalkableWorldControl, new()
        {
            await ChangeMap(typeof(T));
        }

        // ─────────────────── Notification Handling ───────────────────
        public void ShowNotificationMessage(ServerMessage.MessageType messageType, string message)
        {
            _notificationController?.Enqueue(messageType, message);
        }

        // ─────────────────────────── Update Loop ───────────────────────────
        private void UpdateLowHealthHeartbeat(GameTime gameTime)
        {
            var state = MuGame.Network?.GetCharacterState();
            if (state == null || state.MaximumHealth == 0 || state.CurrentHealth == 0)
                return;

            if (state.CurrentHealth / (float)state.MaximumHealth >= 0.2f)
                return;

            double nowMs = gameTime.TotalGameTime.TotalMilliseconds;
            if (nowMs - _lastHeartbeatMs < HeartbeatIntervalMs)
                return;

            _lastHeartbeatMs = nowMs;
            SoundController.Instance.PlayBuffer("Sound/pHeartBeat.wav");
        }

        public override void Update(GameTime gameTime)
        {
            if (Status != GameControlStatus.Ready)
            {
                _mapController?.UpdateLoading(gameTime);
                return;
            }

            if (_initialWorldLoadInProgress ||
                _mapController?.IsChangingWorld == true ||
                _pendingWorldActivation != null ||
                _initialWorldActivationCooldown ||
                World == null ||
                !World.Visible ||
                World.Status != GameControlStatus.Ready)
            {
                _mapController?.UpdateLoading(gameTime);
                return;
            }

            var currentKeyboardState = MuGame.Instance.Keyboard;
            var previousKeyboardState = MuGame.Instance.PrevKeyboard;

            Objects.Effects.Joints.SourceParticleWind.Update(FPSCounter.Instance.FPS_ANIMATION_FACTOR);
            base.Update(gameTime);
            if (Status != GameControlStatus.Ready)
                return;

            UpdateLowHealthHeartbeat(gameTime);

            long buffsStarted = UpdatePassProfiler.Start();
            if (_buffEffectController == null && MuGame.Network != null)
                _buffEffectController = new Client.Main.Effects.BuffEffectController(
                    MuGame.Network.GetBuffManager(), MuGame.AppLoggerFactory ?? NullLoggerFactory.Instance);
            MuGame.Network?.UpdateBuffs();
            _buffEffectController?.EnsureLocalManaShield(this, _hero.NetworkId);
            MuGame.Network?.GetCharacterState()?.ExpireActiveBuffs();
            _hotkeys?.HandleGlobal(currentKeyboardState, previousKeyboardState);
            UpdatePassProfiler.AddGameBuffs(buffsStarted);

            long notificationsStarted = UpdatePassProfiler.Start();
            _notificationManager?.Update(gameTime);
            _notificationController?.ProcessPending();
            UpdatePassProfiler.AddGameNotifications(notificationsStarted);

            long scopeStarted = UpdatePassProfiler.Start();
            if (World is WalkableWorldControl walkableWorld)
                ScopeHandler.PumpNpcSpawnQueue(walkableWorld);
            UpdatePassProfiler.AddGameScopePump(scopeStarted);

            if (World == null || World.Status != GameControlStatus.Ready)
            {
                _playerMenuController?.ResetOnWorldUnavailable();
                _skillController?.ClearPending();
                return;
            }

            long interactionStarted = UpdatePassProfiler.Start();
            var uiMouse = MuGame.Instance.UiMouseState;
            var prevUiMouse = MuGame.Instance.PrevUiMouseState;

            long playerMenuStarted = UpdatePassProfiler.Start();
            _playerMenuController?.Update(gameTime, currentKeyboardState, uiMouse, prevUiMouse);
            UpdatePassProfiler.AddGamePlayerMenu(playerMenuStarted);

            long skillUpdateStarted = UpdatePassProfiler.Start();
            _skillController?.Update();
            _muHelper?.Update(gameTime);
            UpdatePassProfiler.AddGameSkillUpdate(skillUpdateStarted);

            long attackInputStarted = UpdatePassProfiler.Start();
            // Handle attack clicks on monsters with proper validation
            if (!IsMouseInputConsumedThisFrame &&
                !WorldHoverSystem.IsAltPressed() &&
                MuGame.Instance.Mouse.LeftButton == ButtonState.Pressed &&
                MuGame.Instance.PrevMouseState.LeftButton == ButtonState.Released) // Fresh press
            {
                MonsterObject hoveredAttackMonster = WorldHoverSystem.FindBestLiveMonster(
                    World.VisibleObjects,
                    MuGame.Instance.MouseRay,
                    World);

                if (hoveredAttackMonster != null &&
                    Hero != null &&
                    !Hero.IsDead && // Don't attack if player is dead
                    Vector2.Distance(Hero.Location, hoveredAttackMonster.Location) <= Hero.GetAttackRangeTiles()) // Check range
                {
                    Hero.Attack(hoveredAttackMonster);
                    SetMouseInputConsumed(); // Consume the click
                }
            }

            // Handle attack clicks on players (duel opponent, PK mode, or Ctrl force-attack)
            if (!IsMouseInputConsumedThisFrame &&
                MouseHoverObject is PlayerObject targetPlayer &&
                CanAttackPlayer(targetPlayer) &&
                MuGame.Instance.Mouse.LeftButton == ButtonState.Pressed &&
                MuGame.Instance.PrevMouseState.LeftButton == ButtonState.Released) // Fresh press
            {
                if (Hero != null &&
                    !Hero.IsDead &&
                    !targetPlayer.IsDead &&
                    targetPlayer.World == World)
                {
                    Hero.Attack(targetPlayer);
                    SetMouseInputConsumed();
                }
            }

            UpdatePassProfiler.AddGameAttackInput(attackInputStarted);

            // Handle skill usage with right-click. These paths are measured independently so
            // the next runtime trace can identify packet/effect cold starts precisely.
            long rightClickSkillStarted = UpdatePassProfiler.Start();
            _skillController?.HandleRightClickSkillUsage();
            UpdatePassProfiler.AddGameRightClickSkill(rightClickSkillStarted);

            long hotkeysStarted = UpdatePassProfiler.Start();
            _hotkeys?.HandleInWorld(currentKeyboardState, previousKeyboardState);
            UpdatePassProfiler.AddGameHotkeys(hotkeysStarted);
            UpdatePassProfiler.AddGameInteraction(interactionStarted);

            long housekeepingStarted = UpdatePassProfiler.Start();
            // Update ping every 5 seconds to reduce network overhead
            _pingTimer += gameTime.ElapsedGameTime.TotalSeconds;
            if (_pingTimer >= 5.0)
            {
                _pingTimer = 0;
                _ = UpdatePingAsync();
            }

            // Keep this separate from DebugPanel so it is always available.
            _fpsTimer += gameTime.ElapsedGameTime.TotalSeconds;
            if (_fpsTimer >= 0.25)
            {
                _fpsTimer = 0;
                UpdateFpsLabel();
            }
            UpdatePassProfiler.AddGameHousekeeping(housekeepingStarted);
        }

        internal void ScheduleMapNameUpdateNextFrame(string actionName)
        {
            _ = UpdateMapNameNextFrameAsync(actionName);
        }

        internal Task RefreshMiniMapAsync()
        {
            return _miniMap != null && World != null
                ? _miniMap.LoadContentForWorld(World.WorldIndex)
                : Task.CompletedTask;
        }

        private async Task UpdateMapNameNextFrameAsync(string actionName)
        {
            await MuGame.YieldToNextFrameAsync(
                string.IsNullOrWhiteSpace(actionName) ? "GameScene.UpdateMapName" : actionName,
                MainThreadDispatcher.WorkPriority.High);
            _mapController?.UpdateMapName();
        }

        internal Task QueueWorldActivationAfterLoadingFrame(
            Action activation,
            string actionName,
            bool cleanupLoadingUi = true)
        {
            ArgumentNullException.ThrowIfNull(activation);
            if (_pendingWorldActivation != null)
                throw new InvalidOperationException("A world activation is already pending.");

            _pendingWorldActivation = activation;
            _pendingWorldActivationScheduled = false;
            _pendingWorldActivationCleansLoadingUi = cleanupLoadingUi;
            _pendingWorldActivationName = string.IsNullOrWhiteSpace(actionName)
                ? "GameScene.ActivateWorldAfterLoadingFrame"
                : actionName;
            _pendingWorldActivationCompletion = new TaskCompletionSource<bool>(
                TaskCreationOptions.RunContinuationsAsynchronously);
            return _pendingWorldActivationCompletion.Task;
        }

        private void SchedulePendingWorldActivation()
        {
            if (_pendingWorldActivation == null || _pendingWorldActivationScheduled)
                return;

            _pendingWorldActivationScheduled = true;
            MuGame.ScheduleOnMainThread(() =>
            {
                Action activation = _pendingWorldActivation;
                TaskCompletionSource<bool> completion = _pendingWorldActivationCompletion;
                try
                {
                    activation?.Invoke();
                    if (_pendingWorldActivationCleansLoadingUi)
                        _initialWorldActivationCooldown = true;
                    completion?.TrySetResult(true);
                }
                catch (Exception ex)
                {
                    completion?.TrySetException(ex);
                    throw;
                }
                finally
                {
                    _pendingWorldActivation = null;
                    _pendingWorldActivationCompletion = null;
                    _pendingWorldActivationScheduled = false;
                    _pendingWorldActivationCleansLoadingUi = false;
                    _pendingWorldActivationName = null;
                }
            }, MainThreadDispatcher.WorkPriority.Critical, _pendingWorldActivationName);
        }

        // ─────────────────────────── Draw Loop ───────────────────────────
        private bool IsWorldReadyToDraw => !_initialWorldLoadInProgress &&
            _mapController?.IsChangingWorld != true && _pendingWorldActivation == null &&
            !_initialWorldActivationCooldown && World != null && World.Visible &&
            World.Status == GameControlStatus.Ready;

        internal void PrepareShadowMapForDraw()
        {
            if (_sceneShellInitialized && IsWorldReadyToDraw)
                World.PrepareShadowMapForDraw();
        }

        public override void Draw(GameTime gameTime)
        {
            if (!_sceneShellInitialized)
            {
                GraphicsDevice.Clear(new Color(12, 12, 20));

                var initialLoading = _initialLoadingScreen ?? _mapController?.LoadingScreen;
                if (_loadingView != null)
                {
                    _loadingView.Progress = initialLoading?.Progress ?? _loadingView.Progress;
                    _loadingView.StatusText = initialLoading?.Message ?? "Preparing game interface...";
                    _loadingView.Visible = true;
                    DrawLoadingView(_loadingView, gameTime);
                }
                return;
            }

            if (!IsWorldReadyToDraw)
            {
                GraphicsDevice.Clear(new Color(12, 12, 20));
                var loading = _mapController?.LoadingScreen;
                _loadingView.Progress = loading?.Progress ?? 0f;
                _loadingView.StatusText = loading?.Message ?? "Loading...";
                _loadingView.Visible = true;
                DrawLoadingView(_loadingView, gameTime);
                SchedulePendingWorldActivation();
                if (_initialWorldActivationCooldown && _pendingWorldActivation == null)
                {
                    _initialWorldActivationCooldown = false;
                    _initialWorldLoadInProgress = false;
                    _mapController?.DisposeLoadingScreen();
                    _loadingView.Visible = false;
                }
                return;
            }

            // BaseScene draws the world and then the UI. A preliminary UI pass is
            // overwritten by the world and repeats all inventory/HUD rendering work.
            base.Draw(gameTime);
        }

        protected override void DrawInterface(GameTime gameTime)
        {
            base.DrawInterface(gameTime);

            // Final top-most pass: draw dragged item previews above all UI windows
            using (new SpriteBatchScope(
                       GraphicsManager.Instance.Sprite,
                       SpriteSortMode.Deferred,
                       BlendState.AlphaBlend,
                       SamplerState.LinearClamp,
                       DepthStencilState.None,
                       transform: UiScaler.SpriteTransform))
            {
                var sprite = GraphicsManager.Instance.Sprite;
                DrawPvpLockOverlay(sprite);
                _inventoryControl?._pickedItemRenderer?.Draw(sprite, gameTime);
                VaultControl.Instance?.DrawPickedPreview(sprite, gameTime);
                ChaosMixControl.Instance?.DrawPickedPreview(sprite, gameTime);
                TradeControl.Instance?.DrawPickedPreview(sprite, gameTime);
                DrawPerformanceOverlay(gameTime);
            }
        }

        private void DrawPerformanceOverlay(GameTime gameTime)
        {
            _fpsLabel?.Draw(gameTime);
            _pingLabel?.Draw(gameTime);
        }

        private void DrawPvpLockOverlay(SpriteBatch sprite)
        {
            if (sprite == null || World == null || _hero == null)
                return;

            PlayerObject locked = null;
            if (LockedPvpTargetId != 0 && World.Players != null)
            {
                var players = World.Players;
                for (int i = 0; i < players.Count; i++)
                {
                    var p = players[i];
                    if (p != null && !p.IsDead && p.World == World && p.NetworkId == LockedPvpTargetId)
                    {
                        locked = p;
                        break;
                    }
                }
                if (locked == null)
                    LockedPvpTargetId = 0;
            }

            if (locked == null)
                return;

            var pixel = GraphicsManager.Instance.Pixel;
            var font = GraphicsManager.Instance.SmallFont ?? GraphicsManager.Instance.Font;
            if (pixel == null || font == null)
                return;

            // 1. World-projected crimson target marker around locked opponent's feet
            var cam = Graphics.Camera.Instance;
            if (cam != null)
            {
                Vector3 footWorld = locked.WorldPosition.Translation + new Vector3(0f, 0f, 12f);
                Vector3 projected = GraphicsDevice.Viewport.Project(
                    footWorld, cam.Projection, cam.View, Matrix.Identity);
                if (projected.Z is >= 0f and <= 1f)
                {
                    Point vp = UiScaler.ToVirtual(new Point((int)projected.X, (int)projected.Y));
                    var ringRect = new Rectangle(vp.X - 28, vp.Y - 12, 56, 24);
                    Color lockCol = new Color(255, 64, 64);
                    sprite.Draw(pixel, new Rectangle(ringRect.X, ringRect.Y, ringRect.Width, 2), lockCol * 0.9f);
                    sprite.Draw(pixel, new Rectangle(ringRect.X, ringRect.Bottom - 2, ringRect.Width, 2), lockCol * 0.9f);
                    sprite.Draw(pixel, new Rectangle(ringRect.X, ringRect.Y, 2, ringRect.Height), lockCol * 0.9f);
                    sprite.Draw(pixel, new Rectangle(ringRect.Right - 2, ringRect.Y, 2, ringRect.Height), lockCol * 0.9f);
                }
            }

            // 2. Top-center PK Lock Pill
            int dist = (int)MathF.Round(Vector2.Distance(_hero.Location, locked.Location));
            string label = Localization.Loc.IsChinese
                ? $"PK 锁定：{locked.Name}  ({dist}格)"
                : $"LOCKED: {locked.Name} ({dist}m)";
            const float scale = 0.72f;
            Vector2 sz = font.MeasureString(label) * scale;
            int pw = (int)sz.X + 28;
            int ph = 28;
            int px = (UiScaler.VirtualSize.X - pw) / 2;
            int py = 10;
            var pill = new Rectangle(px, py, pw, ph);
            sprite.Draw(pixel, pill, new Color(36, 8, 12) * 0.86f);
            sprite.Draw(pixel, new Rectangle(pill.X, pill.Y, pill.Width, 2), new Color(235, 65, 65));
            sprite.Draw(pixel, new Rectangle(pill.X, pill.Bottom - 1, pill.Width, 1), new Color(160, 40, 40));
            Vector2 tp = new(px + (pw - sz.X) / 2f, py + (ph - sz.Y) / 2f);
            sprite.DrawString(font, label, tp + Vector2.One, Color.Black * 0.85f, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
            sprite.DrawString(font, label, tp, new Color(255, 215, 195), 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
        }


        private Task PreloadCombatAssetsAsync()
        {
            // Move reflection-based skill effect discovery out of the first combat packet.
            SkillVisualEffectRegistry.Initialize();

            return Task.WhenAll(
                // Upload common impact textures while the loading screen is active, so
                // the first hit does not pay for decoding or creating GPU resources.
                MonsterHitEffect.PreloadAsync(),
                MonsterHitSparkEffect.PreloadAsync(),
                BloodStainEffect.PreloadAsync(),
                ArrowProjectileEffect.PreloadMonsterBombAsync(),
                BMDLoader.Instance.Prepare("Skill/Blizzard.bmd"),
                BMDLoader.Instance.Prepare("Skill/Circle01.bmd"),
                TextureLoader.Instance.PrepareAndGetTexture(MonsterFireRainEffect.FireTexturePath),
                SoundController.Instance.PreloadSoundAsync("Sound/eExplosion.wav"),
                SoundController.Instance.PreloadSoundAsync("Sound/eMeteorite.wav"),
                SoundController.Instance.PreloadSoundAsync("Sound/iron_attack1.wav"),
                SoundController.Instance.PreloadSoundAsync("Sound/mGoblinAttack1.wav"),
                SoundController.Instance.PreloadSoundAsync("Sound/mGoblinAttack2.wav"),
                SoundController.Instance.PreloadSoundAsync("Sound/pDropItem.wav"),
                SoundController.Instance.PreloadSoundAsync("Sound/pDropMoney.wav"),
                SoundController.Instance.PreloadSoundAsync("Sound/eGem.wav"),
                SoundController.Instance.PreloadSoundAsync("Sound/Jewel_Sound.wav"),
                SoundController.Instance.PreloadSoundAsync("Sound/pGetItem.wav"),
                SoundController.Instance.PreloadSoundAsync("Sound/pWalk(Grass).wav"),
                SoundController.Instance.PreloadSoundAsync("Sound/pWalk(Snow).wav"),
                SoundController.Instance.PreloadSoundAsync("Sound/pWalk(Soil).wav"),
                SoundController.Instance.PreloadSoundAsync("Sound/pSwim.wav"),
                SoundController.Instance.PreloadSoundAsync("Sound/mHomord1.wav"),
                SoundController.Instance.PreloadSoundAsync("Sound/mHomordAttack1.wav"),
                SoundController.Instance.PreloadSoundAsync("Sound/mHomordDie.wav"));
        }

        private async Task UpdatePingAsync()
        {
            if (MuGame.Network == null)
                return;

            // System.Net Ping may perform an expensive synchronous first-use setup. Keep that
            // work away from the game thread and only publish the final value through the
            // dispatcher.
            int? ping = await Task.Run(
                async () => await MuGame.Network.PingServerAsync().ConfigureAwait(false))
                .ConfigureAwait(false);
            MuGame.ScheduleOnMainThread(() =>
            {
                if (_pingLabel == null)
                    return;

                if (ping == _lastPingValue)
                    return;

                _lastPingValue = ping;
                _pingLabel.Text = ping.HasValue ? $"Ping: {ping.Value} ms" : "Ping: --";
            });
        }

        private void UpdateFpsLabel()
        {
            if (_fpsLabel == null)
                return;

            int fps = (int)FPSCounter.Instance.FPS_AVG;
            if (fps == _lastFpsValue)
                return;

            _lastFpsValue = fps;
            _fpsLabel.Text = $"FPS: {fps}";
        }

        private void StartWhisperToPlayer(string playerName)
        {
            if (string.IsNullOrWhiteSpace(playerName) || _chatInput == null)
            {
                return;
            }

            _chatInput.StartWhisperTo(playerName);
        }

        internal void SetWorldInternal(WorldControl world)
        {
            World = world;
        }

        internal void NotifyLocalSkillAnimation(ushort skillId)
        {
            _skillController?.NotifyLocalSkillAnimation(skillId);
        }

        private void HandleUiThemeChanged(object sender, UiThemeChangedEventArgs e)
        {
            ApplyChatThemeLayout();
            ApplyUiThemeVisibility();
        }

        private void ApplyChatThemeLayout()
        {
            if (_chatLog == null || _chatInput == null)
                return;

            bool classic = !ChatUiTheme.UseModernLayout;
            bool mobile = OperatingSystem.IsAndroid() || OperatingSystem.IsIOS();
            if (classic || mobile)
            {
                // Place compact chat/system log below top-left location & MU Helper bar
                // so it never overlaps the lower-left virtual movement joystick or HP orb.
                _chatLog.X = 12;
                _chatLog.Y = 44;
                _chatInput.X = 12;
                _chatInput.Y = UiScaler.VirtualSize.Y - UiThemeManager.Current.Metrics.ChatInputSize.Y - 8;
            }
            else
            {
                _chatLog.X = 5;
                _chatLog.Y = UiScaler.VirtualSize.Y - 160 - ChatInputBoxControl.CHATBOX_HEIGHT;
                _chatInput.X = 5;
                _chatInput.Y = UiScaler.VirtualSize.Y - 65 - ChatInputBoxControl.CHATBOX_HEIGHT;
            }
        }

        private void ApplyUiThemeVisibility()
        {
            UiThemeId currentTheme = UiThemeManager.CurrentId;
            bool classic = currentTheme == UiThemeId.Classic;
            bool mobileTouchOverlay = OperatingSystem.IsAndroid() || OperatingSystem.IsIOS();
            bool showTouchControls = classic || mobileTouchOverlay;
            bool enteringTheme = !_uiThemeVisibilityInitialized || _lastAppliedUiTheme != currentTheme;
            bool pauseMenuWasVisible = _pauseMenu?.Visible == true;

            if (enteringTheme && classic)
            {
                _modernSkillSelectionWasVisible = _skillSelectionPanel?.Visible == true;
                _modernSkillSelectionWasInteractive = _skillSelectionPanel?.Interactive == true;
                _modernMasteryWasVisible = _masteryTree?.Visible == true;
                _modernMasteryWasInteractive = _masteryTree?.Interactive == true;
            }

            // Keep the Modern control alive as the shared quick-slot state owner, but never
            // let it draw or consume pointer input while Classic is active.
            if (_modernHud != null)
            {
                _modernHud.Visible = true;
                _modernHud.Interactive = !classic;
            }

            // Durability warnings belong to both themes and remain a click-through overlay.
            if (_equipmentDurabilityHud != null)
            {
                _equipmentDurabilityHud.Visible = true;
                _equipmentDurabilityHud.Interactive = false;
            }

            if (_skillSelectionPanel != null)
            {
                if (classic)
                {
                    _skillSelectionPanel.Visible = false;
                    _skillSelectionPanel.Interactive = false;
                }
                else
                {
                    bool restoreModernState = enteringTheme && _uiThemeVisibilityInitialized;
                    _skillSelectionPanel.Visible = restoreModernState
                        ? _modernSkillSelectionWasVisible
                        : _skillSelectionPanel.Visible;
                    _skillSelectionPanel.Interactive = restoreModernState
                        ? _modernSkillSelectionWasInteractive
                        : _skillSelectionPanel.Visible;
                }
            }
            if (_masteryTree != null)
            {
                if (classic)
                {
                    _masteryTree.Visible = false;
                    _masteryTree.Interactive = false;
                }
                else
                {
                    bool restoreModernState = enteringTheme && _uiThemeVisibilityInitialized;
                    _masteryTree.Visible = restoreModernState
                        ? _modernMasteryWasVisible
                        : _masteryTree.Visible;
                    _masteryTree.Interactive = restoreModernState
                        ? _modernMasteryWasInteractive
                        : _masteryTree.Visible;
                }
            }
            if (_classicMasteryTree != null && !classic)
            {
                _classicMasteryTree.Visible = false;
                _classicMasteryTree.Interactive = false;
            }

            if (_classicBottomBar != null)
            {
                _classicBottomBar.Visible = classic;
                _classicBottomBar.Interactive = classic;
            }
            if (_classicTouchActions != null)
            {
                _classicTouchActions.Visible = showTouchControls;
                _classicTouchActions.Interactive = showTouchControls;
                _classicTouchActions.MasterAlpha = showTouchControls ? 1f : 0f;
            }
            if (_classicJoystick != null)
            {
                _classicJoystick.Visible = showTouchControls;
                _classicJoystick.Interactive = showTouchControls;
            }
            if (_classicTouchMenu != null)
            {
                _classicTouchMenu.Visible = classic;
                _classicTouchMenu.Interactive = classic;
                if (!classic)
                    _classicTouchMenu.Close();
            }
            if (_classicSkillImprint != null)
            {
                if (!classic)
                {
                    _classicSkillImprint.Visible = false;
                    _classicSkillImprint.Interactive = false;
                }
                else
                {
                    _classicSkillImprint.Interactive = _classicSkillImprint.Visible;
                }
            }
            if (_classicPotionImprint != null)
            {
                if (!classic)
                {
                    _classicPotionImprint.Visible = false;
                    _classicPotionImprint.Interactive = false;
                }
                else
                {
                    _classicPotionImprint.Interactive = _classicPotionImprint.Visible;
                }
            }

            if (classic)
            {
                if (Status == GameControlStatus.Ready)
                    _ = EnsureClassicAssetsAsync();
                _classicBottomBar?.BringToFront();
                _classicTouchActions?.BringToFront();
                _classicJoystick?.BringToFront();
                _classicSkillImprint?.BringToFront();
                _classicPotionImprint?.BringToFront();
                _classicMasteryTree?.BringToFront();
                _classicTouchMenu?.BringToFront();
                _equipmentDurabilityHud?.BringToFront();
                _chatLog?.BringToFront();
                _chatInput?.BringToFront();
            }
            else if (mobileTouchOverlay)
            {
                if (Status == GameControlStatus.Ready)
                    _ = EnsureMobileTouchAssetsAsync();
                _classicTouchActions?.BringToFront();
                _classicJoystick?.BringToFront();
                _equipmentDurabilityHud?.BringToFront();
                _chatLog?.BringToFront();
                _chatInput?.BringToFront();
            }

            // The theme option lives inside the pause menu. Reordering the replacement HUD
            // must not put it above an already open settings/modal surface.
            if (pauseMenuWasVisible)
                _pauseMenu?.BringToFront();

            _lastAppliedUiTheme = currentTheme;
            _uiThemeVisibilityInitialized = true;
        }

        private async Task EnsureMobileTouchAssetsAsync()
        {
            try
            {
                await Task.WhenAll(
                    _classicTouchActions?.Load() ?? Task.CompletedTask,
                    _classicJoystick?.Load() ?? Task.CompletedTask);
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "Failed to load mobile touch overlay assets.");
            }
        }

        private async Task EnsureClassicAssetsAsync()
        {
            try
            {
                await Task.WhenAll(
                    _classicBottomBar?.Load() ?? Task.CompletedTask,
                    _classicTouchActions?.Load() ?? Task.CompletedTask,
                    _classicJoystick?.Load() ?? Task.CompletedTask,
                    _classicSkillImprint?.Load() ?? Task.CompletedTask,
                    _classicPotionImprint?.Load() ?? Task.CompletedTask,
                    _classicMasteryTree?.Load() ?? Task.CompletedTask,
                    _classicTouchMenu?.Load() ?? Task.CompletedTask);
            }
            catch (Exception ex)
            {
                _logger?.LogDebug(ex, "Classic UI asset warm-up failed; controls will use procedural fallbacks.");
            }
        }

        /// <summary>Guild master NPC dialog (0x54), as UIGuildMaster.cpp: proceed to the creation window or cancel.</summary>
        private void OnGuildMasterDialogRequested()
        {
            var service = MuGame.Network?.GetCharacterService();
            if (service == null) return;
            RequestDialog.Show(
                "Do you want to create a guild?",
                onAccept: () => _ = AcceptGuildMasterDialogAsync(service),
                onReject: () => _ = service.SendGuildMasterAnswerAsync(false));
        }

        /// <summary>
        /// OpenMU only shows the creation dialog when the player is back in the EnteredWorld state
        /// (GuildMasterAnswerAction), while talking to the NPC leaves it in NpcDialogOpened. Closing the
        /// NPC dialog first (0x31) makes the "create" answer work; the original server accepted it directly.
        /// </summary>
        private static async System.Threading.Tasks.Task AcceptGuildMasterDialogAsync(Client.Main.Networking.Services.CharacterService service)
        {
            await service.SendCloseNpcRequestAsync();
            await service.SendGuildMasterAnswerAsync(true);
        }

        private void OnGuildCreationDialogRequested() => GuildCreate?.Show();

        /// <summary>GuildJoinRequest (0x50): another player asks to join the own guild.</summary>
        private void OnGuildJoinRequestReceived(ushort requesterId, string requesterName)
        {
            var service = MuGame.Network?.GetCharacterService();
            if (service == null) return;
            RequestDialog.Show(
                $"{requesterName} wants to join your guild.",
                onAccept: () => _ = service.SendGuildJoinResponseAsync(true, requesterId),
                onReject: () => _ = service.SendGuildJoinResponseAsync(false, requesterId));
        }

        private readonly System.Collections.Generic.List<ChatRoomWindow> _chatRooms = new();

        private void OnFriendRequestReceived(string requester)
        {
            var service = MuGame.Network?.GetCharacterService();
            if (service == null) return;
            RequestDialog.Show(
                $"{requester} wants to add you as a friend.",
                onAccept: () => _ = service.SendFriendAddResponseAsync(true, requester),
                onReject: () => _ = service.SendFriendAddResponseAsync(false, requester));
        }

        private void OnChatRoomOpened(Client.Main.Core.Client.ChatRoomInvite invite)
            => OpenChatRoom(invite.Host, invite.RoomId, invite.Token, invite.FriendName);

        // Original alert sounds (ZzzOpenData.cpp: SOUND_FRIEND_MAIL_ALERT / SOUND_FRIEND_LOGIN_ALERT).
        private void OnNewLetterReceived(Client.Main.Core.Client.LetterEntry letter)
            => SoundController.Instance.PlayBuffer("Sound/iFMailAlert.wav");

        private void OnFriendCameOnline(string name)
            => SoundController.Instance.PlayBuffer("Sound/iFLogInAlert.wav");

        /// <summary>
        /// ChatRoomConnectionInfo (0xCA): connects to the chat server and opens a chat window for the room
        /// (or shows the existing one).
        /// </summary>
        internal async void OpenChatRoom(string host, ushort roomId, uint token, string friendName)
        {
            _chatRooms.RemoveAll(w => w.Status == Models.GameControlStatus.Disposed || w.Parent == null);
            foreach (var existing in _chatRooms)
            {
                if (existing.RoomId == roomId)
                {
                    existing.Show();
                    return;
                }
            }

            var session = new Client.Main.Networking.ChatRoomSession(MuGame.AppLoggerFactory, roomId, token, friendName);
            string ownName = MuGame.Network?.GetCharacterState()?.Name ?? string.Empty;
            var window = new ChatRoomWindow(session, ownName);
            Controls.Add(window);
            _chatRooms.Add(window);
            window.Show();

            if (!await session.ConnectAsync(host))
                ChatLog?.AddMessage("System", $"Could not connect to the chat server ({host}).", Models.MessageType.Error);
        }

        public override void Dispose()
        {
            _muHelper?.Stop();
            _buffEffectController?.Dispose();
            _buffEffectController = null;
            UiThemeManager.ThemeChanged -= HandleUiThemeChanged;
            if (_friendManager != null)
            {
                _friendManager.FriendRequestReceived -= OnFriendRequestReceived;
                _friendManager.ChatRoomOpened -= OnChatRoomOpened;
                _friendManager.NewLetterReceived -= OnNewLetterReceived;
                _friendManager.FriendCameOnline -= OnFriendCameOnline;
            }
            foreach (var room in _chatRooms.ToArray())
                room.Hide();
            if (_guildManager != null)
            {
                _guildManager.GuildMasterDialogRequested -= OnGuildMasterDialogRequested;
                _guildManager.GuildJoinRequestReceived -= OnGuildJoinRequestReceived;
                _guildManager.GuildCreationDialogRequested -= OnGuildCreationDialogRequested;
            }
            _pendingWorldActivation = null;
            _pendingWorldActivationScheduled = false;
            _initialWorldActivationCooldown = false;
            _pendingWorldActivationCompletion?.TrySetCanceled();
            _pendingWorldActivationCompletion = null;

            if (_hero != null)
            {
                if (_windowCloseController != null)
                {
                    _hero.PlayerMoved -= _windowCloseController.OnHeroMoved;
                    _hero.PlayerTookDamage -= _windowCloseController.OnHeroTookDamage;
                }
            }
            base.Dispose();
        }
    }
}
