using System;
using System.Collections.Generic;
using System.Linq;
using GlobalGodRays.Helpers;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.BellsAndWhistles;
using StardewValley.Extensions;

namespace GlobalGodRays;

public class RayManager : IDisposable
{
    private static Dictionary<string, bool>? _locationOverrides;
    private static Dictionary<string, bool> LocationOverrides {
        get {
            if (_locationOverrides is null)
            {
                try
                {
                    _locationOverrides = ModEntry.ModHelper.ModContent.Load<Dictionary<string, bool>>("location_overrides.json");
                }
                catch
                {
                    _locationOverrides = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);
                }
            }
            return _locationOverrides;
        }
    }
    
    private bool ShouldDrawRays => Game1.currentLocation is not null && (LocationOverrides.TryGetValue(Game1.currentLocation.Name, out bool isEnabled)
                                       ? isEnabled
                                       : Game1.currentLocation.IsOutdoors);

    private Texture2D? _vanillaRayTexture;
    private Texture2D VanillaRayTexture => _vanillaRayTexture ??= Game1.content.Load<Texture2D>("Spiderbuttons.GodRays/LightRays");
    private Texture2D? _hiResRayTexture;
    private Texture2D HiResRayTexture => _hiResRayTexture ??= Game1.content.Load<Texture2D>("Spiderbuttons.GodRays/HiResRays");
    
    private int RaySeed;

    /* This controls how large the rays are drawn on screen. Larger rays are also more transparent. */
    private float DefaultRayScale => RayStyle.Equals("Vanilla") ? 0.65f : 0.25f;
    private float RayScale => ModEntry.Config.RayScale;
    
    /* This controls how dense and numerous the light rays are. */
    private float LightrayIntensity => ModEntry.Config.RayIntensity;
    
    /* This controls how fast the god rays go through their animation. */
    private float RayAnimationSpeed => ModEntry.Config.RayAnimationSpeed;
    
    private static readonly Color DaytimeColour = new(255, 255, 255);
    private static readonly Color EarlySunsetColour = new(255, 229, 138);
    private static readonly Color LateSunsetColour = new(238, 108, 69);
    private static readonly Color NightColour = Color.White;

    private Color RayColour = DaytimeColour;
    
    private string RayStyle => ModEntry.Config.RayStyle;
    private Texture2D RayTexture => RayStyle.Equals("Vanilla") ? VanillaRayTexture : HiResRayTexture;

    private float MORNING_ANGLE => RayStyle.Equals("Vanilla") ? 0f : 45f;
    private float NOON_ANGLE => RayStyle.Equals("Vanilla") ? -27f : 0f;
    private float NIGHT_ANGLE => RayStyle.Equals("Vanilla") ? -27f * 2 : -MORNING_ANGLE;

    private int MorningTime => 600;
    private int NoonTime => 1200;
    private int EarlySunsetTime => Game1.getStartingToGetDarkTime(Game1.currentLocation);
    private int SunsetTime => Game1.getModeratelyDarkTime(Game1.currentLocation);
    private int NightTime => Game1.getTrulyDarkTime(Game1.currentLocation);
    private int CurrentTime => Game1.timeOfDay + (int)((float)Game1.gameTimeInterval / MillisecondsPerMinute % 10f);
    
    
    private int MillisecondsPerMinute => Game1.realMilliSecondsPerGameMinute + Game1.currentLocation?.ExtraMillisecondsPerInGameMinute ?? 0;
    private int MillisecondsToNextMinute => MillisecondsPerMinute - Game1.gameTimeInterval % MillisecondsPerMinute;
    
    private float ProgressToNoon => Utility.CalculateMinutesBetweenTimes(MorningTime, CurrentTime) / (float)Utility.CalculateMinutesBetweenTimes(MorningTime, NoonTime);
    private float ProgressToNight => Utility.CalculateMinutesBetweenTimes(NoonTime, CurrentTime) / (float)Utility.CalculateMinutesBetweenTimes(NoonTime, NightTime);

    private float TimeOpacityFactor = 1f;
    private float TimeBasedRotation;

    private bool IsInCloudCover;
    private float CloudCoverOpacityFactor = 1f;

    public RayManager()
    {
        RaySeed = (int)Game1.currentGameTime.TotalGameTime.TotalMilliseconds;
        
        ModEntry.ModHelper.Events.Input.ButtonPressed += OnButtonPressed;
        ModEntry.ModHelper.Events.GameLoop.UpdateTicked += UpdateValues;
        ModEntry.ModHelper.Events.Display.RenderedWorld += OnRenderedWorld;
        ModEntry.ModHelper.Events.Player.Warped += OnWarped;
        ModEntry.ModHelper.Events.Content.AssetsInvalidated += OnAssetsInvalidated;

        UpdateValues(null, null);
    }

    private void OnButtonPressed(object? sender, ButtonPressedEventArgs e)
    {
        if (!ModEntry.Config.ToggleLocationKey.JustPressed() || Game1.currentLocation is not { } location) return;
        
        bool normallyAllowed = location.IsOutdoors;
        Game1.hudMessages.RemoveWhere(msg => msg.message is "Godrays enabled in this location." or "Godrays disabled in this location.");
        if (LocationOverrides.ContainsKey(location.Name))
        {
            bool isEnabled = !LocationOverrides[location.Name];
            LocationOverrides.Remove(location.Name);
            Game1.addHUDMessage(new HUDMessage($"Godrays {(isEnabled ? "enabled" : "disabled")} in this location.", isEnabled ? HUDMessage.newQuest_type : HUDMessage.error_type));
        }
        else
        {
            LocationOverrides.Add(location.Name, !normallyAllowed);
            bool isEnabled = LocationOverrides[location.Name];
            Game1.addHUDMessage(new HUDMessage($"Godrays {(isEnabled ? "enabled" : "disabled")} in this location.", isEnabled ? HUDMessage.newQuest_type : HUDMessage.error_type));
        }
        ModEntry.ModHelper.Data.WriteJsonFile("location_overrides.json", LocationOverrides.Any() ? LocationOverrides : null);
    }

    public void UpdateValues(object? sender, UpdateTickedEventArgs? e)
    {
        if (!ShouldDrawRays) return;
        
        CheckForCloudCover();
        UpdateTimeBasedOpacity();
        UpdateRayColour();
        UpdateAngleOfRays();
    }

    private void CheckForCloudCover()
    {
        if (Game1.currentLocation is not { IsOutdoors: true } location) return;
        
        IsInCloudCover = location.critters.Any(c =>
        {
            if (c is not Cloud cloud) return false;
            var cloudBB = cloud.getBoundingBox(0, 0);
            /* The following block compensates for a fucked up vanilla bug where sometimes... the clouds... are WRONG. */
            if (cloud is { horizontalFlip: true, verticalFlip: true })
            {
                cloudBB.Offset(-Cloud.width * cloud.zoom, 0);
                cloudBB.Offset(0, -Cloud.height * cloud.zoom);
            }
            /* ------------------------------------------------------------------------------------------------------- */
            cloudBB.Inflate(-6 * cloud.zoom, -6 * cloud.zoom);
            return cloudBB.Intersects(Game1.player.GetBoundingBox());
        });
        
        switch (IsInCloudCover)
        {
            case true when CloudCoverOpacityFactor > 0f:
                CloudCoverOpacityFactor -= 0.03f;
                CloudCoverOpacityFactor = Math.Max(CloudCoverOpacityFactor, 0f);
                break;
            case false when CloudCoverOpacityFactor < 1f:
                CloudCoverOpacityFactor += 0.03f;
                CloudCoverOpacityFactor = Math.Min(CloudCoverOpacityFactor, 1f);
                break;
        }
    }

    private void UpdateAngleOfRays()
    {
        /* The angle of the rays should change according to time of day, with noon making them point straight down. */
        if (CurrentTime <= NoonTime)
        {
            TimeBasedRotation = MathHelper.ToRadians(Utility.Lerp(MORNING_ANGLE, NOON_ANGLE, ProgressToNoon));
            TimeBasedRotation += MathHelper.ToRadians((NOON_ANGLE - MORNING_ANGLE) / Utility.CalculateMinutesBetweenTimes(MorningTime, NoonTime) * (MillisecondsPerMinute - MillisecondsToNextMinute) / MillisecondsPerMinute);
        } else {
            TimeBasedRotation = MathHelper.ToRadians(Utility.Lerp(NOON_ANGLE, NIGHT_ANGLE, ProgressToNight));
            TimeBasedRotation += MathHelper.ToRadians((NIGHT_ANGLE - NOON_ANGLE) / Utility.CalculateMinutesBetweenTimes(NoonTime, NightTime) * (MillisecondsPerMinute - MillisecondsToNextMinute) / MillisecondsPerMinute);
        }
        /* -------------------------------------------------------------------------------------------------------- */
    }

    private void UpdateTimeBasedOpacity()
    {
        /* We want the lights to fade out as it gets darker out. */
        float sunsetProgress = 0f;
        if (CurrentTime >= NightTime) sunsetProgress = 1f;
        else if (CurrentTime < SunsetTime) sunsetProgress = 0f;
        else if (CurrentTime >= SunsetTime && CurrentTime < NightTime)
        {
            sunsetProgress = Utility.CalculateMinutesBetweenTimes(SunsetTime, CurrentTime) / (float)Utility.CalculateMinutesBetweenTimes(SunsetTime, NightTime);
            sunsetProgress += (MillisecondsPerMinute - MillisecondsToNextMinute) / (float)(MillisecondsPerMinute * Utility.CalculateMinutesBetweenTimes(SunsetTime, NightTime));
        }
        
        TimeOpacityFactor = Utility.Lerp(1f, 0f, sunsetProgress);
        /* ----------------------------------------------------- */
    }

    private void UpdateRayColour()
    {
        switch (CurrentTime)
        {
            case var time when time < EarlySunsetTime - 100:
                RayColour = DaytimeColour;
                break;
            case var time when time >= EarlySunsetTime - 100 && time < EarlySunsetTime:
                float progressToSunrise = Utility.CalculateMinutesBetweenTimes(time, EarlySunsetTime - 100) / (float)Utility.CalculateMinutesBetweenTimes(EarlySunsetTime, EarlySunsetTime - 100);
                RayColour = Color.Lerp(DaytimeColour, EarlySunsetColour, progressToSunrise);
                break;
            case var time when time >= EarlySunsetTime && time < SunsetTime:
                float progressToSunset = Utility.CalculateMinutesBetweenTimes(time, EarlySunsetTime) / (float)Utility.CalculateMinutesBetweenTimes(SunsetTime, EarlySunsetTime);
                RayColour = Color.Lerp(EarlySunsetColour, LateSunsetColour, progressToSunset);
                break;
            case var time when time >= SunsetTime && time < NightTime:
                float progressToNight = Utility.CalculateMinutesBetweenTimes(time, SunsetTime) / (float)Utility.CalculateMinutesBetweenTimes(NightTime, SunsetTime);
                RayColour = Color.Lerp(LateSunsetColour, NightColour, progressToNight);
                break;
            default:
                RayColour = NightColour;
                break;
        }
    }
    
    private double easeInOutQuad(float x)
    {
        return x < 0.5 ? 2 * x * x : 1 - Math.Pow(-2 * x + 2, 2) / 2;
    }

    private void OnRenderedWorld(object? sender, RenderedWorldEventArgs e)
    {
        if (!ShouldDrawRays) return;
        
        SpriteBatch b = e.SpriteBatch;
        Random random = Utility.CreateRandom(RaySeed);
        Color drawColour = RayColour;
        
        /* Don't know where the magic numbers come from here. */
        float zoomFactor = Game1.graphics.GraphicsDevice.Viewport.Height * 0.6f / 128f;
        int minRays = -(int)(128f / zoomFactor);
        int maxRays = Game1.currentLocation.Map.DisplayWidth / (int)(32f / LightrayIntensity * zoomFactor);
        /* -------------------------------------------------- */

        float rayScaleMultiplier = RayScale;
        /* As rayScaleMultiplier grows, the rays should become more transparent to avoid filling the screen with ugly white blob. */
        float scaleOpacityFactor = 1f;
        if (rayScaleMultiplier > DefaultRayScale) // TODO: Make this based on the percentage of the screen the rays take up instead of an arbitrarily chosen default value.
        {
            scaleOpacityFactor = 1f - Math.Abs(rayScaleMultiplier - DefaultRayScale);
            scaleOpacityFactor = Utility.Clamp(scaleOpacityFactor, 0.5f, 1f);
            drawColour *= scaleOpacityFactor / (RayStyle is "Vanilla" ? 0.75f : 1f);
        }
        /* ---------------------------------------------------------------------------------------------------------------------- */

        for (int i = minRays; i < maxRays; i++)
        {
            float deg = (float)Game1.currentGameTime.TotalGameTime.TotalSeconds * RayAnimationSpeed;

            /* These two lines add some random variation to each ray's movement. */
            deg *= Utility.RandomFloat(0.75f, 1f, random);
            deg *= Utility.RandomFloat(0.2f, 5f, random);
            /* Otherwise, they'd all fade in and fade out at exactly the same time. */
            
            /* These lines control the rays fading in and out as they move from right to left. */
            deg %= 360f;
            deg /= 2;
            float rad = MathHelper.ToRadians(deg);
            Color rayColour = drawColour;
            rayColour *= Utility.Clamp((float)easeInOutQuad(rad), 0f, 1f);
            rayColour *= Math.Clamp(Utility.RandomFloat(0.25f, 0.5f, random) + scaleOpacityFactor, 0f, 1f);
            /* ------------------------------------------------------------------------------- */
            
            /* Multiply by our time-based opacity factor from earlier to make the lights fade out as night approaches. */
            rayColour *= TimeOpacityFactor;
            
            /* And also multiply it by our configurable opacity multiplier, in case all these calculations are not to a user's tastes. */
            rayColour *= ModEntry.Config.RayOpacityModifier;

            /* This also makes the rays fade out when the player is standing beneath a cloud. */
            if (ModEntry.Config.FadeUnderClouds) rayColour *= CloudCoverOpacityFactor;
            // TODO: Scale the CloudCoverOpacityFactor based on how much of the screen real-estate the rays take up. Rays taking up the whole screen should probably not be hidden by a cloud that takes up only a small fraction of the screen.
            
            /* First we offset each ray by a random amount so they're not all stacked or immediately side by side. */
            float offset = Utility.Lerp(0f - Utility.RandomFloat(24f, 32f, random), 0f, deg / 360f);

            int chosenRay = random.Next(0, RayStyle.Equals("Vanilla") ? 2 : 3);
            Rectangle sourceRect = RayStyle switch
            {
                "Vanilla" => new Rectangle(128 * chosenRay, 0, 128, 128),
                _ => chosenRay switch
                {
                    0 => new Rectangle(230, 0, 100, 850),
                    1 => new Rectangle(575, 0, 215, 1015),
                    _ => new Rectangle(1065, 0, 630, 1000),
                },
            };

            float nearFinalDrawScale = zoomFactor * rayScaleMultiplier * Utility.RandomFloat(0.85f, 1.15f, random);
            float finalDrawScale = nearFinalDrawScale;
            
            /* Then we offset them further depending on where the viewport is on the map, so the rays stay in the same relative location. */
            float followFactor = 1.05f;
            float horizontalMovementOffset = Game1.viewport.X / zoomFactor / followFactor;
            
            /* Vertical movement makes them move weird unless we compensate by scrolling the animation side to side to cancel it out. */
            float morningVerticalOffset = Game1.viewport.Y / zoomFactor / followFactor;
            float noonVerticalOffset = 0f;
            float nightVerticalOffset = -(Game1.viewport.Y / zoomFactor / followFactor);
            float currentVerticalOffset;
            
            if (CurrentTime < NoonTime)
            {
                currentVerticalOffset = Utility.Lerp(morningVerticalOffset, noonVerticalOffset, ProgressToNoon);
                currentVerticalOffset -= (morningVerticalOffset - noonVerticalOffset) / Utility.CalculateMinutesBetweenTimes(MorningTime, NoonTime) * (MillisecondsPerMinute - MillisecondsToNextMinute) / MillisecondsPerMinute;
            } else if (CurrentTime > NoonTime) {
                currentVerticalOffset = Utility.Lerp(noonVerticalOffset, nightVerticalOffset, ProgressToNight);
                currentVerticalOffset += (nightVerticalOffset - noonVerticalOffset) / Utility.CalculateMinutesBetweenTimes(NoonTime, NightTime) * (MillisecondsPerMinute - MillisecondsToNextMinute) / MillisecondsPerMinute;
            } else {
                currentVerticalOffset = noonVerticalOffset;
            }
            /* The smaller the rays, the less the position of the viewport influences all of the above scrolling compensation. */
            float percentageOfViewportHeight = sourceRect.Height * finalDrawScale / Game1.graphics.GraphicsDevice.Viewport.Height;
            if (percentageOfViewportHeight < 1f)
            {
                currentVerticalOffset *= percentageOfViewportHeight / 1.5f;
                horizontalMovementOffset *= percentageOfViewportHeight / 1.5f;
            }

            offset += horizontalMovementOffset + currentVerticalOffset;
            
            float rayXPosition = (i * (sourceRect.Width / 4f / LightrayIntensity) - offset) * zoomFactor;
            float rayYPosition = Utility.RandomFloat(0f, -32f * zoomFactor, random);
            
            /* This will wrap the rays around to the other edge of the map if they're drawn too far off-screen. */
            while (rayXPosition < -sourceRect.Width * finalDrawScale)
            {
                rayXPosition += Game1.currentLocation.Map.DisplayWidth + sourceRect.Width * finalDrawScale;
            }
            /* Otherwise, the horizontal offset above will mean fewer rays the further down in the map you are. */
            
            /* Where we're going, we don't need alpha blend mode... */
            b.End();
            b.Begin(SpriteSortMode.Deferred, BlendState.Additive, SamplerState.PointClamp);
            b.Draw(
                texture: RayTexture,
                position: new Vector2(rayXPosition, rayYPosition),
                sourceRectangle: sourceRect,
                color: rayColour,
                rotation: TimeBasedRotation,
                origin: RayStyle.Equals("Vanilla") ? new Vector2(sourceRect.Width / 2f, 0) : new Vector2(sourceRect.Width / 2f, 96f * finalDrawScale),
                scale: new Vector2(finalDrawScale, finalDrawScale),
                effects: SpriteEffects.None,
                layerDepth: 1f
            );
            b.End();
            b.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp);
            /* ...until we get to the end and need alpha blend mode. */
        }
    }

    private void OnWarped(object? sender, WarpedEventArgs e)
    {
        RaySeed = (int)Game1.currentGameTime.TotalGameTime.TotalMilliseconds;
        Game1.hudMessages.RemoveWhere(msg => msg.message is "Godrays enabled in this location." or "Godrays disabled in this location.");
    }
    
    private void OnAssetsInvalidated(object? sender, AssetsInvalidatedEventArgs e)
    {
        /* I really doubt anyone is ever gonna retexture these, but... you never know, I guess. */
        if (e.NamesWithoutLocale.Any(a => a.IsEquivalentTo("Spiderbuttons.GodRays/LightRays")))
        {
            _vanillaRayTexture = null;
        }
        
        if (e.NamesWithoutLocale.Any(a => a.IsEquivalentTo("Spiderbuttons.GodRays/HiResRays")))
        {
            _hiResRayTexture = null;
        }
        /* ------------------------------------------------------------------------------------ */
    }
    
    public void Dispose()
    {
        ModEntry.ModHelper.Events.Input.ButtonPressed -= OnButtonPressed;
        ModEntry.ModHelper.Events.GameLoop.UpdateTicked -= UpdateValues;
        ModEntry.ModHelper.Events.Display.RenderedWorld -= OnRenderedWorld;
        ModEntry.ModHelper.Events.Player.Warped -= OnWarped;
        ModEntry.ModHelper.Events.Content.AssetsInvalidated -= OnAssetsInvalidated;
        GC.SuppressFinalize(this);
    }
}