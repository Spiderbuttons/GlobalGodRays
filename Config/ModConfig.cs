using GenericModConfigMenu;
using StardewModdingAPI;
using StardewModdingAPI.Utilities;

namespace GlobalGodRays.Config;

public sealed class ModConfig
{
    public KeybindList ToggleLocationKey { get; set; } = new(SButton.None);
    public float RayScale { get; set; } = 0.65f;
    public float RayIntensity { get; set; } = 4f;
    public float RayAnimationSpeed { get; set; } = 20f;
    
    public float RayOpacityModifier { get; set; } = 1.2f;
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
            name: i18n.ToggleLocationKeyName,
            tooltip: i18n.ToggleLocationKeyTooltip,
            getValue: () => ToggleLocationKey,
            setValue: value => ToggleLocationKey = value
        );

        configMenu.AddNumberOption(
            mod: ModManifest,
            name: i18n.RayScaleName,
            tooltip: i18n.RayScaleTooltip,
            getValue: () => RayScale,
            setValue: value => RayScale = value,
            min: 0.1f,
            max: 1.0f,
            interval: 0.01f,
            formatValue: value => $"{value * 100f:0}%");

        configMenu.AddNumberOption(
            mod: ModManifest,
            name: i18n.RayIntensityName,
            tooltip: i18n.RayIntensityTooltip,
            getValue: () => RayIntensity,
            setValue: value => RayIntensity = value,
            min: 1f,
            max: 32f,
            interval: 1f
        );
        
        configMenu.AddNumberOption(
            mod: ModManifest,
            name: i18n.RayAnimationSpeedName,
            tooltip: i18n.RayAnimationSpeedTooltip,
            getValue: () => RayAnimationSpeed,
            setValue: value => RayAnimationSpeed = value,
            min: 1f,
            max: 100f,
            interval: 1f
        );
        
        configMenu.AddNumberOption(
            mod: ModManifest,
            name: i18n.RayOpacityModifierName,
            tooltip: i18n.RayOpacityModifierTooltip,
            getValue: () => RayOpacityModifier,
            setValue: value => RayOpacityModifier = value,
            min: 0.1f,
            max: 3f,
            interval: 0.1f,
            formatValue: value => $"{value}x"
        );
        
        configMenu.AddBoolOption(
            mod: ModManifest,
            name: i18n.FadeUnderCloudsName,
            tooltip: i18n.FadeUnderCloudsTooltip,
            getValue: () => FadeUnderClouds,
            setValue: value => FadeUnderClouds = value
        );
    }
}