using GenericModConfigMenu;
using HarmonyLib;
using GlobalGodRays.Config;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewModdingAPI.Events;

namespace GlobalGodRays
{
    internal sealed class ModEntry : Mod
    {
        internal static IModHelper ModHelper { get; set; } = null!;
        internal static IMonitor ModMonitor { get; set; } = null!;
        internal static ModConfig Config { get; set; } = null!;
        
        public static RayManager? RayManager { get; set; }

        public override void Entry(IModHelper helper)
        {
            i18n.Init(helper.Translation);
            ModHelper = helper;
            ModMonitor = Monitor;
            Config = helper.ReadConfig<ModConfig>();

            Helper.Events.Content.AssetRequested += OnAssetRequested;
            Helper.Events.Input.ButtonPressed += OnButtonPressed;
            Helper.Events.GameLoop.GameLaunched += OnGameLaunched;
            Helper.Events.GameLoop.SaveLoaded += OnSaveLoaded;
        }

        private void OnAssetRequested(object? sender, AssetRequestedEventArgs e)
        {
            if (e.NameWithoutLocale.IsEquivalentTo("Spiderbuttons.GodRays/LightRays"))
            {
                e.LoadFromModFile<Texture2D>("assets/LightRays.png", AssetLoadPriority.Medium);
            }

            if (e.NameWithoutLocale.IsEquivalentTo("Spiderbuttons.GodRays/HiResRays"))
            {
                e.LoadFromModFile<Texture2D>("assets/HiResRays.png", AssetLoadPriority.Medium);
            }
        }

        private void OnSaveLoaded(object? sender, SaveLoadedEventArgs e)
        {
            RayManager = new RayManager();
        }

        private void OnGameLaunched(object? sender, GameLaunchedEventArgs e)
        {
            var configMenu = Helper.ModRegistry.GetApi<IGenericModConfigMenuApi>("spacechase0.GenericModConfigMenu");
            if (configMenu != null) Config.SetupConfig(configMenu, ModManifest, Helper);
        }

        private void OnButtonPressed(object? sender, ButtonPressedEventArgs e)
        {
            if (!Context.IsWorldReady)
                return;

            if (e.Button is SButton.F3)
            {
                RayManager?.Dispose();
                RayManager = new RayManager();
            }
        }
    }
}