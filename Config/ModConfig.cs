using GenericModConfigMenu;
using StardewModdingAPI;

namespace GlobalGodRays.Config;

public sealed class ModConfig
{
    public string RayStyle { get; set; } = "Vanilla";
    public float RayScale { get; set; } = 0.25f;
    public float RayIntensity { get; set; } = 4f;
    public float RayAnimationSpeed { get; set; } = 20f;
    
    public float RayOpacityModifier { get; set; } = 1f;

    public ModConfig()
    {
        Init();
    }

    private void Init()
    {
        RayStyle = "Vanilla";
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
        
        configMenu.AddTextOption(
            mod: ModManifest,
            name: () => "Ray Style",
            tooltip: () => "Changes the texture used for drawing the godrays. If you change this, you will likely want to adjust the Ray Scale setting as well.",
            getValue: () => RayStyle,
            setValue: value =>
            {
                RayStyle = value;
                RayScale = value is "Vanilla" ? 0.65f : 0.25f;
                ModEntry.RayManager?.UpdateValues(null, null);
            },
            allowedValues: ["Vanilla", "HiRes"]
        );

        configMenu.AddNumberOption(
            mod: ModManifest,
            name: () => "Ray Scale",
            tooltip: () => "Adjusts the size of the godrays. Higher values make them cover more of the screen. Lower values make them cover less of the screen. Rays are also more transparent the bigger they are than the default value.",
            getValue: () => RayScale,
            setValue: value => RayScale = value,
            min: 0.1f,
            max: 3.0f,
            interval: 0.05f,
            formatValue: value =>
            {
                float baseScale = RayStyle is "Vanilla" ? 0.65f : 0.25f;
                float percentage = value / baseScale * 100f;
                return $"{percentage:0}%";
            });

        configMenu.AddNumberOption(
            mod: ModManifest,
            name: () => "Ray Intensity",
            tooltip: () =>
                "Adjusts the density and number of the godrays. Higher values means more godrays that are closer together. Lower values means fewer godrays that are further apart.",
            getValue: () => RayIntensity,
            setValue: value => RayIntensity = value,
            min: 1f,
            max: 32f,
            interval: 1f,
            formatValue: value =>
            {
                float baseIntensity = 4f;
                float percentage = value / baseIntensity * 100f;
                return $"{percentage:0}%";
            });
        
        configMenu.AddNumberOption(
            mod: ModManifest,
            name: () => "Ray Animation Speed",
            tooltip: () => "Adjusts how fast the godrays move across the screen. Higher values make them move faster. Lower values make them move slower.",
            getValue: () => RayAnimationSpeed,
            setValue: value => RayAnimationSpeed = value,
            min: 1f,
            max: 100f,
            interval: 1f,
            formatValue: value =>
            {
                float baseSpeed = 20f;
                float percentage = value / baseSpeed * 100f;
                return $"{percentage:0}%";
            });
        
        configMenu.AddNumberOption(
            mod: ModManifest,
            name: () => "Ray Opacity Multiplier",
            tooltip: () => "Adjusts the overall opacity of the godrays. Higher values make them more opaque. Lower values make them more transparent.",
            getValue: () => RayOpacityModifier,
            setValue: value => RayOpacityModifier = value,
            min: 0.1f,
            max: 3f,
            interval: 0.1f,
            formatValue: value =>
            {
                float percentage = value * 100f;
                return $"{percentage:0}%";
            }
        );
    }
}