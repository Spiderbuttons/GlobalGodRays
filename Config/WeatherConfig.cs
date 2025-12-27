using System;
using System.Collections.Generic;
using System.Linq;
using GlobalGodRays.APIs;
using GlobalGodRays.Converters;
using Newtonsoft.Json;
using StardewModdingAPI;
using StardewModdingAPI.Utilities;
using StardewValley.TokenizableStrings;

namespace GlobalGodRays.Config;

public class WeatherConfigWithGenericToggle : WeatherConfig
{
    public bool UseGenericSettings { get; set; } = true;
}

public class WeatherConfig
{
    [JsonIgnore]
    public IGenericModConfigMenuApi ConfigAPI => ModEntry.GenericModConfigMenuApi!;
    
    [JsonIgnore]
    public ICloudySkiesApi? CloudySkiesApi => ModEntry.CloudySkiesApi;
    
    public KeybindList ToggleLocationKey { get; set; } = new(SButton.None);
    public bool DisableGodRays { get; set; }
    public float RayScale { get; set; } = 0.65f;
    public float RayIntensity { get; set; } = 4f;
    public float RayAnimationSpeed { get; set; } = 20f;
    
    public float RayOpacityModifier { get; set; } = 1.2f;
    public bool FadeUnderClouds { get; set; } = true;

    public bool OnlyWhenSunny { get; set; } = true;

    [JsonConverter(typeof(WeatherConfigConverter))]
    public Dictionary<string, WeatherConfigWithGenericToggle> WeatherSpecificConfigs { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    
    public bool ShouldSerializeToggleLocationKey()
    {
        return GetType() != typeof(WeatherConfigWithGenericToggle);
    }
    
    public bool ShouldSerializeDisableGodRays()
    {
        return GetType() != typeof(WeatherConfigWithGenericToggle) || DisableGodRays;
    }
    
    public bool ShouldSerializeRayScale()
    {
        return GetType() != typeof(WeatherConfigWithGenericToggle) || Math.Abs(RayScale - 0.65f) > 0.001f;
    }
    
    public bool ShouldSerializeRayIntensity()
    {
        return GetType() != typeof(WeatherConfigWithGenericToggle) || Math.Abs(RayIntensity - 4f) > 0.001f;
    }
    
    public bool ShouldSerializeRayAnimationSpeed()
    {
        return GetType() != typeof(WeatherConfigWithGenericToggle) || Math.Abs(RayAnimationSpeed - 20f) > 0.001f;
    }
    
    public bool ShouldSerializeRayOpacityModifier()
    {
        return GetType() != typeof(WeatherConfigWithGenericToggle) || Math.Abs(RayOpacityModifier - 1f) > 0.001f;
    }
    
    public bool ShouldSerializeFadeUnderClouds()
    {
        return GetType() != typeof(WeatherConfigWithGenericToggle) || !FadeUnderClouds;
    }
    
    public bool ShouldSerializeOnlyWhenSunny()
    {
        return GetType() != typeof(WeatherConfigWithGenericToggle);
    }
    
    public bool ShouldSerializeWeatherSpecificConfigs()
    {
        return GetType() != typeof(WeatherConfigWithGenericToggle);
    }

    public WeatherConfig()
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
        FadeUnderClouds = true;
        WeatherSpecificConfigs.Clear();
    }

    private void SwapToPage(string pageId)
    {
        ConfigAPI.AddPage(ModEntry.Manifest, pageId);
    }

    public void SetupConfig()
    {
        ConfigAPI.Register(
            mod: ModEntry.Manifest,
            reset: Init,
            save: () =>
            {
                ModEntry.ModHelper.WriteConfig(this);
            });
        
        ConfigAPI.AddKeybindList(
            mod: ModEntry.Manifest,
            name: i18n.ToggleLocationKeyName,
            tooltip: i18n.ToggleLocationKeyTooltip,
            getValue: () => ToggleLocationKey,
            setValue: value => ToggleLocationKey = value
        );
        
        ConfigAPI.AddPageLink(
            mod: ModEntry.Manifest,
            pageId: "GenericSettings",
            text: () => $"{i18n.Generic()} {i18n.WeatherSettings()}",
            tooltip: i18n.GenericPageTooltip
        );
        
        ConfigAPI.AddPageLink(
            mod: ModEntry.Manifest,
            pageId: "VanillaSettings",
            text: () => $"{i18n.Vanilla()} {i18n.WeatherSettings()}",
            tooltip: i18n.VanillaPageTooltip
        );

        if (CloudySkiesApi is not null)
        {
            ConfigAPI.AddPageLink(
                mod: ModEntry.Manifest,
                pageId: "ModdedSettings",
                text: () => $"{i18n.Modded()} {i18n.WeatherSettings()}",
                tooltip: i18n.ModdedPageTooltip
            );
        }

        SetupGeneric();
        SetupVanilla();
        if (CloudySkiesApi is not null) SetupModded();
    }
    
    

    private void SetupGeneric()
    {
        SwapToPage("GenericSettings");
        
        ConfigAPI.AddBoolOption(
            mod: ModEntry.Manifest,
            name: i18n.OnlyWhenSunnyName,
            tooltip: i18n.OnlyWhenSunnyTooltip,
            getValue: () => OnlyWhenSunny,
            setValue: value => OnlyWhenSunny = value
        );
        AddOptions(this);
    }

    private void SetupVanilla()
    {
        ConfigAPI.AddPage(
            mod: ModEntry.Manifest,
            pageId: "VanillaSettings",
            pageTitle: () => $"{i18n.Vanilla()} {i18n.WeatherSettings()}"
        );
        
        foreach (var weather in RayManager.VanillaWeatherIds)
        {
            bool weatherIsBad = weather is "storm" or "rain" or "greenrain" or "snow";
            
            ConfigAPI.AddPageLink(
                mod: ModEntry.Manifest,
                pageId: $"Weather_{weather}",
                text: () => i18n.GetByKey($"Weather_{weather}")
            );
            
            ConfigAPI.AddPage(
                mod: ModEntry.Manifest,
                pageId: $"Weather_{weather}",
                pageTitle: () => weather
            );
            
            WeatherSpecificConfigs.TryAdd(weather, new WeatherConfigWithGenericToggle()
            {
                UseGenericSettings = !weatherIsBad,
                DisableGodRays = weatherIsBad
            });
            
            ConfigAPI.AddBoolOption(
                mod: ModEntry.Manifest,
                name: i18n.UseGenericSettingsName,
                tooltip: i18n.UseGenericSettingsTooltip,
                getValue: () => WeatherSpecificConfigs[weather].UseGenericSettings,
                setValue: value => WeatherSpecificConfigs[weather].UseGenericSettings = value
            );
            
            AddOptions(WeatherSpecificConfigs[weather]);
            SwapToPage("VanillaSettings");
        }
    }
    
    private void SetupModded()
    {
        ConfigAPI.AddPage(
            mod: ModEntry.Manifest,
            pageId: "ModdedSettings",
            pageTitle: () => $"{i18n.Modded()} {i18n.WeatherSettings()}"
        );
        
        var weathers = CloudySkiesApi!.GetAllCustomWeather();
        if (!weathers.Any())
        {
            ConfigAPI.AddParagraph(
                mod: ModEntry.Manifest,
                text: i18n.NoModdedWeathers
            );
            return;
        }
        
        foreach (var weather in CloudySkiesApi!.GetAllCustomWeather())
        {
            ConfigAPI.AddPageLink(
                mod: ModEntry.Manifest,
                pageId: $"Weather_{weather.Id}",
                text: () => TokenParser.ParseText(weather.DisplayName)
            );
            
            ConfigAPI.AddPage(
                mod: ModEntry.Manifest,
                pageId: $"Weather_{weather.Id}",
                pageTitle: () => TokenParser.ParseText(weather.DisplayName)
            );
            
            WeatherSpecificConfigs.TryAdd(weather.Id, new WeatherConfigWithGenericToggle());
            
            ConfigAPI.AddBoolOption(
                mod: ModEntry.Manifest,
                name: i18n.UseGenericSettingsName,
                tooltip: i18n.UseGenericSettingsTooltip,
                getValue: () => WeatherSpecificConfigs[weather.Id].UseGenericSettings,
                setValue: value => WeatherSpecificConfigs[weather.Id].UseGenericSettings = value
            );
            
            AddOptions(WeatherSpecificConfigs[weather.Id]);
            SwapToPage("ModdedSettings");
        }
    }

    private void AddOptions(WeatherConfig config)
    {
        ConfigAPI.AddBoolOption(
            mod: ModEntry.Manifest,
            name: i18n.DisableRaysName,
            tooltip: i18n.DisableRaysTooltip,
            getValue: () => config.DisableGodRays,
            setValue: value => config.DisableGodRays = value
        );
        
        ConfigAPI.AddNumberOption(
            mod: ModEntry.Manifest,
            name: i18n.RayScaleName,
            tooltip: i18n.RayScaleTooltip,
            getValue: () => config.RayScale,
            setValue: value => config.RayScale = value,
            min: 0.1f,
            max: 1.0f,
            interval: 0.01f,
            formatValue: value => $"{value * 100f:0}%");

        ConfigAPI.AddNumberOption(
            mod: ModEntry.Manifest,
            name: i18n.RayIntensityName,
            tooltip: i18n.RayIntensityTooltip,
            getValue: () => config.RayIntensity,
            setValue: value => config.RayIntensity = value,
            min: 1f,
            max: 32f,
            interval: 1f
        );
        
        ConfigAPI.AddNumberOption(
            mod: ModEntry.Manifest,
            name: i18n.RayAnimationSpeedName,
            tooltip: i18n.RayAnimationSpeedTooltip,
            getValue: () => config.RayAnimationSpeed,
            setValue: value => config.RayAnimationSpeed = value,
            min: 1f,
            max: 100f,
            interval: 1f
        );
        
        ConfigAPI.AddNumberOption(
            mod: ModEntry.Manifest,
            name: i18n.RayOpacityModifierName,
            tooltip: i18n.RayOpacityModifierTooltip,
            getValue: () => config.RayOpacityModifier,
            setValue: value => config.RayOpacityModifier = value,
            min: 0.1f,
            max: 3f,
            interval: 0.1f,
            formatValue: value => $"{value}x"
        );
        
        ConfigAPI.AddBoolOption(
            mod: ModEntry.Manifest,
            name: i18n.FadeUnderCloudsName,
            tooltip: i18n.FadeUnderCloudsTooltip,
            getValue: () => config.FadeUnderClouds,
            setValue: value => config.FadeUnderClouds = value
        );
    }
}