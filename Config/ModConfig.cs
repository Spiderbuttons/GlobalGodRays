using GenericModConfigMenu;
using StardewModdingAPI;

namespace GlobalGodRays.Config;

public sealed class ModConfig
{
    public float RayScale { get; set; } = 0.25f;
    public float RayIntensity { get; set; } = 4f;
    public float RayAnimationSpeed { get; set; } = 20f;

    public ModConfig()
    {
        Init();
    }

    private void Init()
    {
        RayScale = 0.25f;
        RayIntensity = 4f;
        RayAnimationSpeed = 20f;
    }

    public void SetupConfig(IGenericModConfigMenuApi configMenu, IManifest ModManifest, IModHelper Helper)
    {
        configMenu.Register(
            mod: ModManifest,
            reset: Init,
            save: () => Helper.WriteConfig(this)
        );

        configMenu.AddNumberOption(
            mod: ModManifest,
            name: () => "Ray Scale",
            tooltip: () => "Adjusts the size of the godrays. Higher values make them cover more of the screen. Lower values make them cover less of the screen. Rays are also more transparent the bigger they are than the default value.",
            getValue: () => RayScale,
            setValue: value => RayScale = value,
            min: 0.1f,
            max: 3.0f,
            interval: 0.05f
        );

        configMenu.AddNumberOption(
            mod: ModManifest,
            name: () => "Ray Intensity",
            tooltip: () =>
                "Adjusts the density and number of the godrays. Higher values means more godrays that are closer together. Lower values means fewer godrays that are further apart.",
            getValue: () => RayIntensity,
            setValue: value => RayIntensity = value,
            min: 1f,
            max: 32f,
            interval: 1f
        );
        
        configMenu.AddNumberOption(
            mod: ModManifest,
            name: () => "Ray Animation Speed",
            tooltip: () => "Adjusts how fast the godrays move across the screen. Higher values make them move faster. Lower values make them move slower.",
            getValue: () => RayAnimationSpeed,
            setValue: value => RayAnimationSpeed = value,
            min: 1f,
            max: 100f,
            interval: 1f
        );
    }
}