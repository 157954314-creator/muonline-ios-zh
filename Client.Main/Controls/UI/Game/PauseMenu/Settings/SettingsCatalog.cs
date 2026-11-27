using System;
using System.Collections.Generic;
using System.Globalization;
using Client.Main.Controllers;
using Client.Main.Graphics;
using Client.Main.Scenes;
using Microsoft.Xna.Framework;

namespace Client.Main.Controls.UI.Game.PauseMenu.Settings
{
    /// <summary>
    /// Categories and options of the settings window. To add an option, append a SettingToggle / SettingSlider /
    /// SettingChoice to the category builder; side effects that touch the engine belong in <see cref="SettingsActions"/>.
    /// </summary>
    internal static class SettingsCatalog
    {
        private static readonly int[] MsaaSampleChoices = { 0, 2, 4, 8 };

        /// <summary>Every option of every category, with its "Category/Label" key.</summary>
        public static IEnumerable<(string Key, SettingOption Option)> AllOptions()
        {
            foreach (var category in Create(() => { }))
            {
                if (category.Icon == "advanced")
                    continue;   // debug views: never restored
                foreach (var option in category.Build())
                    if (option is not SettingHeader && option.Persist)
                        yield return ($"{category.Name}/{option.Label}", option);
            }
        }

        public static List<SettingsCategory> Create(Action rebuild) => new()
        {
            new SettingsCategory { Name = "显示", Icon = "display", Subtitle = "分辨率、窗口模式与帧率", Build = () => BuildDisplay(rebuild) },
            new SettingsCategory { Name = "画质", Icon = "graphics", Subtitle = "整体画质、贴图与阴影", Build = BuildGraphics },
            new SettingsCategory { Name = "光照", Icon = "lighting", Subtitle = "太阳光、动态光源与材质着色器", Build = BuildLighting },
            new SettingsCategory { Name = "音频", Icon = "audio", Subtitle = "背景音乐与游戏音效", Build = BuildAudio },
            new SettingsCategory { Name = "界面", Icon = "interface", Subtitle = "界面风格、镜头与信息面板", Build = BuildInterface },
            new SettingsCategory { Name = "性能", Icon = "performance", Subtitle = "刷新率与低配硬件优化", Build = BuildPerformance },
            new SettingsCategory { Name = "高级", Icon = "advanced", Subtitle = "开发者与调试视图", Build = BuildAdvanced },
        };

        // --- Categories ---------------------------------------------------------------------------------

        private static List<SettingOption> BuildDisplay(Action rebuild)
        {
            var list = new List<SettingOption> { new SettingHeader { Label = "屏幕" } };

            var settings = MuGame.AppSettings?.Graphics;
            if (settings != null)
            {
                var resolutions = SettingsActions.SupportedResolutions(settings.IsFullScreen);
                var labels = resolutions.ConvertAll(r => $"{r.X} x {r.Y}");
                list.Add(new SettingChoice
                {
                    Label = "分辨率",
                    Persist = false,
                    Description = "窗口或屏幕尺寸，16:9 比例可保持最佳界面观感。",
                    Choices = labels,
                    GetIndex = () => resolutions.FindIndex(r => r.X == settings.Width && r.Y == settings.Height),
                    SetIndex = i => SettingsActions.SetResolution(resolutions[i].X, resolutions[i].Y)
                });
                list.Add(new SettingToggle
                {
                    Label = "全屏模式",
                    Persist = false,
                    Description = "以独占全屏而非窗口模式运行游戏。",
                    Get = () => settings.IsFullScreen,
                    Set = value =>
                    {
                        SettingsActions.SetFullscreen(value);
                        rebuild(); // fullscreen limits the resolution list to adapter modes
                    }
                });
            }

            float[] scales = { 0.375f, 0.5f, 0.6f, 0.75f, 0.9f, 1f, 1.25f, 1.5f, 2f, 3f };
            list.Add(new SettingChoice
            {
                Label = "渲染比例",
                Description = "内部 3D 渲染分辨率。低于 100% 更流畅，高于 100% 超采样更锐利。",
                Choices = Array.ConvertAll(scales, s => (s * 100f).ToString("0.#", CultureInfo.InvariantCulture) + "%"),
                GetIndex = () => Array.FindIndex(scales, s => MathF.Abs(Constants.RENDER_SCALE - s) < 0.01f),
                SetIndex = i => SettingsActions.SetRenderScale(scales[i])
            });

            list.Add(new SettingHeader { Label = "帧率" });
            list.Add(new SettingToggle
            {
                Label = "垂直同步",
                Description = "与显示器刷新率同步，消除画面撕裂。",
                Get = () => !Constants.DISABLE_VSYNC,
                Set = SettingsActions.SetVSync
            });
            list.Add(new SettingToggle
            {
                Label = "无上限帧率",
                Description = "尽可能以最高帧率渲染（将关闭垂直同步）。",
                Get = () => Constants.UNLIMITED_FPS,
                Set = SettingsActions.SetUnlimitedFps
            });
            return list;
        }

        private static List<SettingOption> BuildGraphics()
        {
            GraphicsQualityPreset[] presets = { GraphicsQualityPreset.Auto, GraphicsQualityPreset.Low, GraphicsQualityPreset.Medium, GraphicsQualityPreset.High };
            Constants.ShadowQuality[] shadows =
            {
                Constants.ShadowQuality.Off, Constants.ShadowQuality.Low, Constants.ShadowQuality.Medium,
                Constants.ShadowQuality.High, Constants.ShadowQuality.Ultra
            };

            return new List<SettingOption>
            {
                new SettingHeader { Label = "画面质量" },
                new SettingChoice
                {
                    Label = "画质预设",
                    Persist = false,
                    Description = "自动检测显卡；低 0.75x、中 0.90x、高 1.00x 渲染比例。",
                    Choices = new[] { "自动", "低", "中", "高" },
                    GetIndex = () => Array.IndexOf(presets, GraphicsQualityManager.UserPreset),
                    SetIndex = i => SettingsActions.ApplyQualityPreset(presets[i])
                },
                new SettingToggle
                {
                    Label = "高清贴图",
                    Description = "为场景与角色启用全分辨率贴图。",
                    Get = () => Constants.HIGH_QUALITY_TEXTURES,
                    Set = v => Constants.HIGH_QUALITY_TEXTURES = v
                },
                new SettingToggle
                {
                    Label = "重制贴图",
                    Persist = false,
                    Description = "为建筑、墙面、地面和水体启用高精重制贴图；关闭则显示原版贴图。",
                    Get = () => Constants.REMASTERED_TEXTURES,
                    Set = SettingsActions.SetRemasteredTextures
                },
                new SettingToggle
                {
                    Label = "抗锯齿 (FXAA)",
                    Description = "使用快速后处理滤镜平滑锯齿边缘。",
                    Get = () => GraphicsManager.Instance.IsFXAAEnabled,
                    Set = v => GraphicsManager.Instance.IsFXAAEnabled = v
                },
                new SettingChoice
                {
                    Label = "多重采样抗锯齿 (MSAA)",
                    Description = "硬件级模型与地形边缘平滑，比 FXAA 更锐利；4x 性价比最佳，8x 显卡开销较高。",
                    Choices = new[] { "关闭", "2x", "4x", "8x" },
                    GetIndex = () => Array.IndexOf(MsaaSampleChoices, Constants.MSAA_SAMPLES),
                    SetIndex = i => Constants.MSAA_SAMPLES = MsaaSampleChoices[Math.Clamp(i, 0, MsaaSampleChoices.Length - 1)]
                },
                new SettingToggle
                {
                    Label = "地表草丛",
                    Description = "在地形上绘制草丛细节。",
                    Get = () => Constants.DRAW_GRASS,
                    Set = SettingsActions.SetGrass
                },
                new SettingToggle
                {
                    Label = "草丛阴影",
                    Description = "角色与建筑阴影同时投射到草叶上，而不仅在地面。",
                    Get = () => GroundShadowMask.Enabled,
                    Set = v => GroundShadowMask.Enabled = v,
                    IsAvailable = () => Constants.DRAW_GRASS
                },
                new SettingToggle
                {
                    Label = "地表防重复纹理",
                    Description = "打散地面贴图的重复平铺感。",
                    Get = () => Constants.TERRAIN_ANTI_TILING,
                    Set = v => Constants.TERRAIN_ANTI_TILING = v
                },
                new SettingToggle
                {
                    Label = "武器拖尾特效",
                    Description = "挥动武器时显示流光拖尾。",
                    Get = () => Constants.ENABLE_WEAPON_TRAIL,
                    Set = v => Constants.ENABLE_WEAPON_TRAIL = v
                },
                new SettingToggle
                {
                    Label = "菜单场景特效",
                    Description = "登录和选角界面的辉光、水波、薄雾、萤火与星光特效。",
                    Get = () => Constants.ENABLE_MENU_SCENE_EFFECTS,
                    Set = v => Constants.ENABLE_MENU_SCENE_EFFECTS = v
                },
                new SettingToggle
                {
                    Label = "场景泛光 (Bloom)",
                    Description = "为火焰、魔法与灯火添加柔和泛光及轻度调色暗角。",
                    Get = () => Constants.ENABLE_WORLD_BLOOM,
                    Set = v => Constants.ENABLE_WORLD_BLOOM = v
                },
                new SettingToggle
                {
                    Label = "地图氛围调色",
                    Description = "按地图呈现专属色调（勇者大陆暖调、冰风谷冷调、亚特兰蒂斯青调等，需开启泛光）。",
                    Get = () => Constants.ENABLE_MAP_COLOR_GRADE,
                    Set = v => Constants.ENABLE_MAP_COLOR_GRADE = v
                },
                new SettingToggle
                {
                    Label = "环境光遮蔽 (SSAO)",
                    Description = "加深模型接缝与着地处暗部立体感（需开启泛光）。",
                    Get = () => Constants.ENABLE_SSAO,
                    Set = v => Constants.ENABLE_SSAO = v
                },
                new SettingSlider
                {
                    Label = "SSAO 强度",
                    Description = "控制环境光遮蔽的暗化程度，默认 100%，0% 将跳过计算。",
                    Min = 0f,
                    Max = 200f,
                    Step = 10f,
                    Get = () => Constants.SSAO_STRENGTH * 100f,
                    Set = v => Constants.SSAO_STRENGTH = v / 100f
                },
                new SettingToggle
                {
                    Label = "高度雾效",
                    Description = "在支持雾效的地图山谷与地牢中渲染低空薄雾。",
                    Get = () => Constants.ENABLE_HEIGHT_FOG,
                    Set = v => Constants.ENABLE_HEIGHT_FOG = v
                },
                new SettingSlider
                {
                    Label = "角色轮廓光",
                    Description = "为角色和怪物边缘添加柔和轮廓光，使其在背景中更醒目。",
                    Min = 0f,
                    Max = 100f,
                    Step = 5f,
                    Get = () => Constants.RIM_LIGHT_STRENGTH * 100f,
                    Set = v => Constants.RIM_LIGHT_STRENGTH = v / 100f
                },
                new SettingHeader { Label = "阴影" },
                new SettingChoice
                {
                    Label = "阴影质量",
                    Description = "阴影贴图精度与距离：低 512px、中/高 1024px、极高 2048px。",
                    Choices = new[] { "关闭", "低", "中", "高", "极高" },
                    GetIndex = () => Array.IndexOf(shadows, Constants.GetCurrentShadowQuality()),
                    SetIndex = i =>
                    {
                        Constants.ApplyShadowQualityPreset(shadows[i]);
                        SettingsActions.OnShadowSettingChanged();
                    }
                },
                new SettingToggle
                {
                    Label = "实时阴影贴图",
                    Description = "启用太阳投射的实时阴影。若阴影原为关闭将自动切至中等。",
                    Get = () => Constants.ENABLE_SHADOW_MAPPING,
                    Set = v =>
                    {
                        Constants.ENABLE_SHADOW_MAPPING = v;
                        if (v && Constants.GetCurrentShadowQuality() == Constants.ShadowQuality.Off)
                            Constants.ApplyShadowQualityPreset(Constants.ShadowQuality.Medium);
                        SettingsActions.OnShadowSettingChanged();
                    }
                },
                new SettingToggle
                {
                    Label = "怪物完整模型阴影",
                    Persist = false,
                    Description = "始终为怪物渲染完整模型阴影而非简易圆形脚底影。",
                    Get = () => MuGame.AppSettings?.Graphics?.ForceMonsterMeshShadows == true,
                    Set = v =>
                    {
                        var graphics = MuGame.AppSettings?.Graphics;
                        if (graphics == null)
                            return;
                        graphics.ForceMonsterMeshShadows = v;
                        MuGame.PersistMonsterShadowMode(v);
                        SettingsActions.OnShadowSettingChanged();
                    }
                },
            };
        }

        private static List<SettingOption> BuildLighting() => new()
        {
            new SettingHeader { Label = "太阳光" },
            new SettingToggle
            {
                Label = "太阳光照",
                Description = "对世界启用方向性太阳光照。",
                Get = () => Constants.SUN_ENABLED,
                Set = v => Constants.SUN_ENABLED = v
            },
            new SettingToggle
            {
                Label = "昼夜循环",
                Description = "跟随现实时间自动移动太阳角度。",
                Get = () => Constants.ENABLE_DAY_NIGHT_CYCLE,
                Set = v =>
                {
                    Constants.ENABLE_DAY_NIGHT_CYCLE = v;
                    if (!v)
                        SunCycleManager.ResetToDefault();
                }
            },
            new SettingChoice
            {
                Label = "日照方向",
                Description = "太阳光照射而来的方向。",
                Choices = new[] { "西", "东" },
                GetIndex = () => Constants.SUN_DIRECTION.X >= 0f ? 1 : 0,
                SetIndex = i =>
                {
                    var dir = Constants.SUN_DIRECTION;
                    if (dir.LengthSquared() < 0.0001f)
                        dir = new Vector3(1f, 0f, -0.6f);
                    dir.X = Math.Abs(dir.X) * (i == 1 ? 1f : -1f);
                    Constants.SUN_DIRECTION = dir;
                }
            },
            new SettingSlider
            {
                Label = "阳光强度",
                Description = "调整太阳光照亮度。",
                Get = () => Constants.SUN_STRENGTH * 100f,
                Set = v => Constants.SUN_STRENGTH = MathHelper.Clamp(v, 0f, 200f) / 100f,
                Max = 200f,
                Step = 5f
            },
            new SettingSlider
            {
                Label = "阴影暗度",
                Description = "调整太阳未照到区域的暗部程度。",
                Get = () => Constants.SUN_SHADOW_STRENGTH * 100f,
                Set = v => Constants.SUN_SHADOW_STRENGTH = MathHelper.Clamp(v, 0f, 100f) / 100f,
                Step = 5f
            },
            new SettingHeader { Label = "光源与材质" },
            new SettingToggle
            {
                Label = "动态光源",
                Description = "火把、技能与特效照亮周围环境。",
                Get = () => Constants.ENABLE_DYNAMIC_LIGHTS,
                Set = v => Constants.ENABLE_DYNAMIC_LIGHTS = v
            },
            new SettingToggle
            {
                Label = "GPU 光照着色器",
                Description = "在 GPU 上逐像素计算动态光照（地形 GPU 光照的前置项）。",
                Get = () => Constants.ENABLE_DYNAMIC_LIGHTING_SHADER,
                Set = v =>
                {
                    Constants.ENABLE_DYNAMIC_LIGHTING_SHADER = v;
                    if (!v)
                        Constants.ENABLE_TERRAIN_GPU_LIGHTING = false;
                }
            },
            new SettingToggle
            {
                Label = "地形 GPU 光照",
                Description = "使用 GPU 着色器照亮地形（需先开启 GPU 光照着色器）。",
                Get = () => Constants.ENABLE_TERRAIN_GPU_LIGHTING,
                Set = v => Constants.ENABLE_TERRAIN_GPU_LIGHTING = v,
                IsAvailable = () => Constants.ENABLE_DYNAMIC_LIGHTING_SHADER
            },
            new SettingToggle
            {
                Label = "装备流光材质",
                Description = "卓越与强化装备的金属光泽和流光效果。",
                Get = () => Constants.ENABLE_ITEM_MATERIAL_SHADER,
                Set = v => Constants.ENABLE_ITEM_MATERIAL_SHADER = v
            },
            new SettingToggle
            {
                Label = "怪物材质着色",
                Description = "为怪物启用材质光影效果。",
                Get = () => Constants.ENABLE_MONSTER_MATERIAL_SHADER,
                Set = v => Constants.ENABLE_MONSTER_MATERIAL_SHADER = v
            },
        };

        private static List<SettingOption> BuildAudio() => new()
        {
            new SettingHeader { Label = "音乐" },
            new SettingToggle
            {
                Label = "背景音乐",
                Description = "播放当前地图的背景音乐。",
                Get = () => Constants.BACKGROUND_MUSIC,
                Set = v =>
                {
                    Constants.BACKGROUND_MUSIC = v;
                    SettingsActions.ApplyBackgroundMusicSetting(v);
                }
            },
            new SettingSlider
            {
                Label = "音乐音量",
                Description = "调节背景音乐的音量大小。",
                Get = () => Constants.BACKGROUND_MUSIC_VOLUME,
                Set = v =>
                {
                    Constants.BACKGROUND_MUSIC_VOLUME = v;
                    if (Constants.BACKGROUND_MUSIC)
                        SoundController.Instance.ApplyBackgroundMusicVolume();
                },
                Step = 5f,
                IsAvailable = () => Constants.BACKGROUND_MUSIC
            },
            new SettingHeader { Label = "音效" },
            new SettingToggle
            {
                Label = "游戏音效",
                Description = "战斗、技能、环境与界面交互音效。",
                Get = () => Constants.SOUND_EFFECTS,
                Set = v =>
                {
                    Constants.SOUND_EFFECTS = v;
                    SoundController.Instance.ApplySoundEffectsVolume();
                }
            },
            new SettingSlider
            {
                Label = "音效音量",
                Description = "调节所有游戏音效的音量大小。",
                Get = () => Constants.SOUND_EFFECTS_VOLUME,
                Set = v =>
                {
                    Constants.SOUND_EFFECTS_VOLUME = v;
                    SoundController.Instance.ApplySoundEffectsVolume();
                },
                Step = 5f,
                IsAvailable = () => Constants.SOUND_EFFECTS
            },
        };

        private static List<SettingOption> BuildInterface() => new()
        {
            new SettingHeader { Label = "布局" },
            new SettingChoice
            {
                Label = "界面风格",
                    Persist = false,
                Description = "现代为新版高清界面，经典为传统复古底栏与窗口风格。",
                Choices = new[] { "现代", "经典" },
                GetIndex = () => UiThemeManager.CurrentId == UiThemeId.Classic ? 1 : 0,
                SetIndex = i => UiThemeManager.SetTheme(i == 1 ? UiThemeId.Classic : UiThemeId.Modern)
            },
            new SettingHeader { Label = "移动与镜头" },
            new SettingToggle
            {
                Label = "平滑移动",
                Description = "角色沿平滑曲线行走而不每格停顿；关闭则为经典逐格移动。",
                Get = () => Constants.SMOOTH_PATH_CORNERS,
                Set = v =>
                {
                    Constants.SMOOTH_PATH_CORNERS = v;
                    Constants.CONTINUOUS_PATH_WALK = v;
                }
            },
            new SettingToggle
            {
                Label = "镜头高度平滑",
                Description = "镜头平滑跟随地形起伏，减少画面抖动。",
                Get = () => Constants.SMOOTH_CAMERA_HEIGHT,
                Set = v => Constants.SMOOTH_CAMERA_HEIGHT = v
            },
            new SettingToggle
            {
                Label = "背包镜头拉近",
                Description = "在安全区打开背包时拉近镜头展示角色装备外观。",
                Get = () => Constants.INVENTORY_CAMERA_SHOWCASE,
                Set = v => Constants.INVENTORY_CAMERA_SHOWCASE = v
            },
            new SettingHeader { Label = "信息" },
            new SettingToggle
            {
                Label = "性能统计面板",
                Description = "显示帧率与渲染性能统计信息面板。",
                Get = () => Constants.SHOW_DEBUG_PANEL,
                Set = v =>
                {
                    Constants.SHOW_DEBUG_PANEL = v;
                    SettingsActions.ApplyDebugPanelSetting();
                }
            },
        };

        private static List<SettingOption> BuildPerformance() => new()
        {
            new SettingHeader { Label = "刷新率" },
            PerformanceFpsSlider("动画刷新率", "角色与怪物骨骼动画的更新频率。",
                () => Constants.ANIMATION_UPDATE_FPS,
                (graphics, fps) =>
                {
                    Constants.ANIMATION_UPDATE_FPS = fps;
                    if (graphics != null)
                        graphics.AnimationUpdateFps = fps;
                }),
            PerformanceFpsSlider("光照刷新率", "动态光源的重新计算频率。",
                () => Constants.DYNAMIC_LIGHT_UPDATE_FPS,
                (graphics, fps) =>
                {
                    Constants.DYNAMIC_LIGHT_UPDATE_FPS = fps;
                    if (graphics != null)
                        graphics.DynamicLightUpdateFps = fps;
                }),
            new SettingHeader { Label = "硬件" },
            new SettingToggle
            {
                Label = "核显性能优化",
                Description = "为笔记本与集成显卡启用更轻量的渲染路径。",
                Get = () => Constants.OPTIMIZE_FOR_INTEGRATED_GPU,
                Set = v => Constants.OPTIMIZE_FOR_INTEGRATED_GPU = v
            },
            new SettingToggle
            {
                Label = "动态缓冲池",
                Description = "在帧间复用 GPU 缓冲区以减少卡顿。",
                Get = () => Constants.ENABLE_DYNAMIC_BUFFER_POOL,
                Set = DynamicBufferPool.SetEnabled
            },
            new SettingToggle
            {
                Label = "物品材质动画",
                Description = "播放装备与宝石材质的动态流光效果。",
                Get = () => Constants.ENABLE_ITEM_MATERIAL_ANIMATION,
                Set = v => Constants.ENABLE_ITEM_MATERIAL_ANIMATION = v
            },
            new SettingToggle
            {
                Label = "自动降画质保护",
                Description = "性能下降时允许自动切换至低画质渲染。",
                Get = () => Constants.ENABLE_LOW_QUALITY_SWITCH,
                Set = v => Constants.ENABLE_LOW_QUALITY_SWITCH = v
            },
            new SettingToggle
            {
                Label = "登录界面低画质",
                Description = "以低画质渲染登录背景场景。",
                Get = () => Constants.ENABLE_LOW_QUALITY_IN_LOGIN_SCENE,
                Set = v => Constants.ENABLE_LOW_QUALITY_IN_LOGIN_SCENE = v
            },
        };

        private static List<SettingOption> BuildAdvanced() => new()
        {
            new SettingHeader { Label = "调试视图" },
            new SettingToggle
            {
                Label = "对象包围盒",
                Description = "描边显示场景中所有物体的包围盒。",
                Get = () => Constants.DRAW_BOUNDING_BOXES,
                Set = v => Constants.DRAW_BOUNDING_BOXES = v
            },
            new SettingToggle
            {
                Label = "可交互对象包围盒",
                Description = "仅描边显示可点击交互物体的包围盒。",
                Get = () => Constants.DRAW_BOUNDING_BOXES_INTERACTIVES,
                Set = v => Constants.DRAW_BOUNDING_BOXES_INTERACTIVES = v
            },
            new SettingToggle
            {
                Label = "光照影响范围",
                Description = "可视化显示动态光源的覆盖区域。",
                Get = () => Constants.DEBUG_LIGHTING_AREAS,
                Set = v => Constants.DEBUG_LIGHTING_AREAS = v
            },
        };

        private static SettingSlider PerformanceFpsSlider(string label, string description, Func<int> get,
            Action<Client.Main.Configuration.GraphicsSettings, int> set) => new()
        {
            Label = label,
            Description = description,
            Get = () => get(),
            Set = value =>
            {
                int fps = Constants.ClampPerformanceFps((int)value);
                var graphics = MuGame.AppSettings?.Graphics;
                set(graphics, fps);
                if (graphics != null)
                    MuGame.PersistGraphicsPerformanceCaps(graphics.DynamicLightUpdateFps, graphics.AnimationUpdateFps);
            },
            Min = Constants.MIN_PERFORMANCE_FPS_CAP,
            Max = Constants.MAX_PERFORMANCE_FPS_CAP,
            Step = 1f,
            Suffix = " FPS",
            ApplyOnRelease = true,
            Persist = false
        };
    }
}
