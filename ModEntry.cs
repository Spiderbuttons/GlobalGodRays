using GlobalGodRays.APIs;
using GlobalGodRays.Config;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewModdingAPI.Utilities;
using StardewValley;

namespace GlobalGodRays
{
    internal sealed class ModEntry : Mod
    {
        internal static IManifest Manifest { get; private set; } = null!;
        internal static IModHelper ModHelper { get; private set; } = null!;
        internal static WeatherConfig Config { get; private set; } = null!;

        internal static readonly PerScreen<RayManager?> ScreenRayManager = new();
        
        internal static IGenericModConfigMenuApi? GenericModConfigMenuApi { get; set; }
        internal static ICloudySkiesApi? CloudySkiesApi { get; set; }

        public override void Entry(IModHelper helper)
        {
            i18n.Init(helper.Translation);
            Manifest = ModManifest;
            ModHelper = helper;
            Config = helper.ReadConfig<WeatherConfig>();

            Helper.Events.Content.AssetRequested += OnAssetRequested;
            Helper.Events.GameLoop.GameLaunched += OnGameLaunched;
            Helper.Events.GameLoop.SaveLoaded += OnSaveLoaded;
            Helper.Events.GameLoop.ReturnedToTitle += OnReturnedToTitle;
            
            ModHelper.Events.Input.ButtonPressed += PerScreenButtonPressed;
            ModHelper.Events.GameLoop.UpdateTicked += PerScreenUpdateTicked;
            ModHelper.Events.Display.RenderedWorld += PerScreenRenderedWorld;
            ModHelper.Events.Player.Warped += PerScreenWarped;
            ModHelper.Events.GameLoop.DayStarted += PerScreenDayStarted;
            ModHelper.Events.Content.AssetsInvalidated += PerScreenAssetsInvalidated;
        }

        private void PerScreenAssetsInvalidated(object? sender, AssetsInvalidatedEventArgs e)
        {
            ScreenRayManager.Value?.OnAssetsInvalidated(sender, e);
        }

        private void PerScreenDayStarted(object? sender, DayStartedEventArgs e)
        {
            ScreenRayManager.Value?.OnDayStarted(sender, e);
        }

        private void PerScreenWarped(object? sender, WarpedEventArgs e)
        {
            ScreenRayManager.Value?.OnWarped(sender, e);
        }

        private void PerScreenRenderedWorld(object? sender, RenderedWorldEventArgs e)
        {
            ScreenRayManager.Value?.OnRenderedWorld(sender, e);
        }

        private void PerScreenUpdateTicked(object? sender, UpdateTickedEventArgs e)
        {
            ScreenRayManager.Value?.OnUpdateTicked(sender, e);
        }

        private void PerScreenButtonPressed(object? sender, ButtonPressedEventArgs e)
        {
            ScreenRayManager.Value?.OnButtonPressed(sender, e);
        }

        private void OnReturnedToTitle(object? sender, ReturnedToTitleEventArgs e)
        {
            ScreenRayManager.Value = null;
        }

        private void OnAssetRequested(object? sender, AssetRequestedEventArgs e)
        {
            if (e.NameWithoutLocale.IsEquivalentTo(RayManager.ASSET_NAME))
            {
                e.LoadFromModFile<Texture2D>("assets/rays.png", AssetLoadPriority.Medium);
            }
        }

        private void OnSaveLoaded(object? sender, SaveLoadedEventArgs e)
        {
            ScreenRayManager.Value = new RayManager();
        }

        private void OnGameLaunched(object? sender, GameLaunchedEventArgs e)
        {
            CloudySkiesApi ??= Helper.ModRegistry.GetApi<ICloudySkiesApi>("leclair.cloudyskies");
            ModHelper.Events.GameLoop.UpdateTicked += OnUpdateTicked;
        }

        private void OnUpdateTicked(object? sender, UpdateTickedEventArgs e)
        {
            /* Wait for Content Patcher to be ready for us to load data from its packs. */
            if (!e.IsMultipleOf(4)) return;
            
            SetupConfig();
            ModHelper.Events.GameLoop.UpdateTicked -= OnUpdateTicked;
        }

        private void SetupConfig()
        {
            GenericModConfigMenuApi ??= Helper.ModRegistry.GetApi<IGenericModConfigMenuApi>("spacechase0.GenericModConfigMenu");
            if (GenericModConfigMenuApi != null) Config.SetupConfig();
        }
    }
}