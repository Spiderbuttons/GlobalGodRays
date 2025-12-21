using System.Text;
using GenericModConfigMenu;
using GlobalGodRays.Config;
using GlobalGodRays.Helpers;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;

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
            Helper.Events.GameLoop.ReturnedToTitle += OnReturnedToTitle;
            Helper.Events.GameLoop.DayStarted += OnDayStarted;
        }

        private void OnReturnedToTitle(object? sender, ReturnedToTitleEventArgs e)
        {
            RayManager?.Dispose();
            RayManager = null;
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
            LogCommWarning();
        }

        private void LogCommWarning()
        {
            StringBuilder commWarning = new StringBuilder();
            commWarning.AppendLine();
            commWarning.AppendLine($@"/* ----------------------------------------------------------------- *\");
            commWarning.AppendLine($@"/*                                                                   *\");
            commWarning.AppendLine($@"/*    This is a commissioned mod that has not yet been paid for!     *\");
            commWarning.AppendLine($@"/*                                                                   *\");
            commWarning.AppendLine($@"/* ----------------------------------------------------------------- *\");
            Log.Alert(commWarning.ToString());
        }

        private void OnDayStarted(object? sender, DayStartedEventArgs e)
        {
            LogCommWarning();
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

            if (e.Button is SButton.F5)
            {
                for (int i = 0; i < 50; i++) Game1.currentLocation.addClouds(1, true);
            }
        }
    }
}