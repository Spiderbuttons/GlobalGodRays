using GenericModConfigMenu;
using GlobalGodRays.Config;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewModdingAPI.Events;

namespace GlobalGodRays
{
    internal sealed class ModEntry : Mod
    {
        internal static IModHelper ModHelper { get; private set; } = null!;
        internal static ModConfig Config { get; private set; } = null!;

        private static RayManager? RayManager { get; set; }

        public override void Entry(IModHelper helper)
        {
            i18n.Init(helper.Translation);
            ModHelper = helper;
            Config = helper.ReadConfig<ModConfig>();

            Helper.Events.Content.AssetRequested += OnAssetRequested;
            Helper.Events.GameLoop.GameLaunched += OnGameLaunched;
            Helper.Events.GameLoop.SaveLoaded += OnSaveLoaded;
            Helper.Events.GameLoop.ReturnedToTitle += OnReturnedToTitle;
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
            var configMenu = Helper.ModRegistry.GetApi<IGenericModConfigMenuApi>("spacechase0.GenericModConfigMenu");
            if (configMenu != null) Config.SetupConfig(configMenu, ModManifest, Helper);
        }
    }
}