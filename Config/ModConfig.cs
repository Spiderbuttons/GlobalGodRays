using GenericModConfigMenu;
using StardewModdingAPI;
using StardewModdingAPI.Utilities;

namespace GlobalGodRays.Config;

public sealed class ModConfig
{
    public KeybindList ToggleLocationKey { get; set; } = new(SButton.None);
    public float RayScale { get; set; } = 0.25f;
    public float RayIntensity { get; set; } = 4f;
    public float RayAnimationSpeed { get; set; } = 20f;
    
    public float RayOpacityModifier { get; set; } = 1f;
    public bool FadeUnderClouds { get; set; } = true;

    public ModConfig()
    {
        Init();
    }

    private void Init()
    {
        ToggleLocationKey = new KeybindList(SButton.None);
        RayScale = 0.65f;
        RayIntensity = 4f;
        RayAnimationSpeed = 20f;
        RayOpacityModifier = 1f;
    }

    public void SetupConfig(IGenericModConfigMenuApi configMenu, IManifest ModManifest, IModHelper Helper)
    {
        configMenu.Register(
            mod: ModManifest,
            reset: Init,
            save: () => Helper.WriteConfig(this)
        );
        
        configMenu.AddKeybindList(
            mod: ModManifest,
            name: () => "Toggle Location Godrays",
            tooltip: () => "Press this key to toggle whether godrays are shown in the current location.",
            getValue: () => ToggleLocationKey,
            setValue: value => ToggleLocationKey = value
        );

        configMenu.AddNumberOption(
            mod: ModManifest,
            name: () => "Ray Scale Multiplier",
            tooltip: () => "Adjusts the size of the godrays. Higher values make them cover more of the screen. Lower values make them cover less of the screen. Rays are also more transparent the bigger they are than the default value.",
            getValue: () => RayScale,
            setValue: value => RayScale = value,
            min: 0.1f,
            max: 1.0f,
            interval: 0.01f,
            formatValue: value => $"{value * 100f:0}%");

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
        
        configMenu.AddNumberOption(
            mod: ModManifest,
            name: () => "Ray Opacity Multiplier",
            tooltip: () => "Adjusts the overall opacity of the godrays. Higher values make them more opaque. Lower values make them more transparent.",
            getValue: () => RayOpacityModifier,
            setValue: value => RayOpacityModifier = value,
            min: 0.1f,
            max: 3f,
            interval: 0.1f,
            formatValue: value => $"{value}x"
        );
        
        configMenu.AddBoolOption(
            mod: ModManifest,
            name: () => "Fade Under Clouds",
            tooltip: () => "If enabled, godrays will fade out when standing under a cloud shadow.",
            getValue: () => FadeUnderClouds,
            setValue: value => FadeUnderClouds = value
        );
    }
}