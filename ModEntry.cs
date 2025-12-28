using GlobalGodRays.APIs;
using GlobalGodRays.Config;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewModdingAPI.Events;

namespace GlobalGodRays
{
    internal sealed class ModEntry : Mod
    {
        internal static IManifest Manifest { get; private set; } = null!;
        internal static IModHelper ModHelper { get; private set; } = null!;
        internal static WeatherConfig Config { get; private set; } = null!;

        internal static RayManager? RayManager { get; set; }
        
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
            Helper.Events.Input.ButtonPressed += OnButtonPressed;
        }

        private void OnReturnedToTitle(object? sender, ReturnedToTitleEventArgs e)
        {
            RayManager?.Dispose();
            RayManager = null;
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
            RayManager = new RayManager();
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
            if (GenericModConfigMenuApi != null) Config?.SetupConfig();
        }

        private void OnButtonPressed(object? sender, ButtonPressedEventArgs e)
        {
            if (e.Button is SButton.F2)
            {
                GenericModConfigMenuApi?.Unregister(ModManifest);
                SetupConfig();
            }
        }
    }
}