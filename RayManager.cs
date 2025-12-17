using System;
using System.Linq;
using GlobalGodRays.Helpers;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;

namespace GlobalGodRays;

public class RayManager : IDisposable
{
    private Texture2D? _rayTexture;
    private Texture2D RayTexture => _rayTexture ??= Game1.content.Load<Texture2D>("Spiderbuttons.GodRays/LightRays");
    private Texture2D? _hiResRayTexture;
    private Texture2D HiResRayTexture => _hiResRayTexture ??= Game1.content.Load<Texture2D>("Spiderbuttons.GodRays/HiResRays");
    
    private int RaySeed;

    /* This controls how large the rays are drawn on screen. Larger rays are also more transparent. */
    private float DefaultRayScale => ModEntry.Config.RayScale;
    
    /* This controls how dense and numerous the light rays are. */
    private float LightrayIntensity => ModEntry.Config.RayIntensity;
    
    /* This controls how fast the god rays go through their animation. */
    private float RayAnimationSpeed => ModEntry.Config.RayAnimationSpeed;
    
    private static readonly Color DaytimeColour = new(255, 255, 255);
    private static readonly Color EarlySunsetColour = new(255, 229, 138);
    private static readonly Color LateSunsetColour = new(238, 108, 69);
    private static readonly Color NightColour = Color.White;

    private Color RayColour = DaytimeColour;

    private const float MORNING_ANGLE = 45f;
    private const float NOON_ANGLE = 0f;
    private const float NIGHT_ANGLE = -MORNING_ANGLE;

    private int MorningTime => 600;
    private int NoonTime => 1200;
    private int EarlySunsetTime => Game1.getStartingToGetDarkTime(Game1.currentLocation);
    private int SunsetTime => Game1.getModeratelyDarkTime(Game1.currentLocation);
    private int NightTime => Game1.getTrulyDarkTime(Game1.currentLocation);

    private float TimeOpacityFactor = 1f;
    private float TimeBasedRotation = MORNING_ANGLE;

    public RayManager()
    {
        RaySeed = (int)Game1.currentGameTime.TotalGameTime.TotalMilliseconds;
        ModEntry.ModHelper.Events.Input.ButtonPressed += OnButtonPressed;
        ModEntry.ModHelper.Events.GameLoop.TimeChanged += OnTimeChanged;
        ModEntry.ModHelper.Events.Display.RenderedWorld += OnRenderedWorld;
        ModEntry.ModHelper.Events.Player.Warped += OnWarped;
        ModEntry.ModHelper.Events.Content.AssetsInvalidated += OnAssetsInvalidated;
        
        UpdateAngleOfRays();
        UpdateTimeBasedOpacity();
        UpdateRayColour();
    }

    private void OnButtonPressed(object? sender, ButtonPressedEventArgs e)
    {
        if (e.Button is SButton.OemPlus)
        {
            ModEntry.Config.RayAnimationSpeed += 10f;
        } else if (e.Button is SButton.OemMinus)
        {
            ModEntry.Config.RayAnimationSpeed -= 10f;
        }
        ModEntry.Config.RayAnimationSpeed = MathHelper.Clamp(RayAnimationSpeed, 0f, 1000f);
    }

    private void UpdateAngleOfRays()
    {
        /* The angle of the rays should change according to time of day, with noon making them point straight down. */
        if (Game1.timeOfDay <= NoonTime)
        {
            float progressToNoon = Utility.CalculateMinutesBetweenTimes(Game1.timeOfDay, MorningTime) / (float)Utility.CalculateMinutesBetweenTimes(NoonTime, MorningTime);
            TimeBasedRotation = MathHelper.ToRadians(Utility.Lerp(MORNING_ANGLE, NOON_ANGLE, progressToNoon));
        } else {
            float progressToNight = Utility.CalculateMinutesBetweenTimes(Game1.timeOfDay, NoonTime) / (float)Utility.CalculateMinutesBetweenTimes(NightTime, NoonTime);
            TimeBasedRotation = MathHelper.ToRadians(Utility.Lerp(NOON_ANGLE, NIGHT_ANGLE, progressToNight));
        }
        /* -------------------------------------------------------------------------------------------------------- */
    }

    private void UpdateTimeBasedOpacity()
    {
        /* We want the lights to fade out as it gets darker out. */
        float progressToDark = 0f;
        if (Game1.timeOfDay >= NightTime) progressToDark = 1f;
        else if (Game1.timeOfDay < SunsetTime) progressToDark = 0f;
        else if (Game1.timeOfDay >= SunsetTime && Game1.timeOfDay < NightTime)
        {
            progressToDark = (Game1.timeOfDay - SunsetTime) / (float)(NightTime - SunsetTime);
        }
        TimeOpacityFactor = Utility.Lerp(1f, 0f, progressToDark);
        /* ----------------------------------------------------- */
    }

    private void UpdateRayColour()
    {
        switch (Game1.timeOfDay)
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

    private void OnTimeChanged(object? sender, TimeChangedEventArgs e)
    {
        UpdateAngleOfRays();
        UpdateTimeBasedOpacity();
        UpdateRayColour();
    }
    
    private double easeInOutQuad(float x)
    {
        return x < 0.5 ? 2 * x * x : 1 - Math.Pow(-2 * x + 2, 2) / 2;
    }

    private void OnRenderedWorld(object? sender, RenderedWorldEventArgs e)
    {
        if (Game1.currentLocation is not { IsOutdoors: true } location) return;
        
        SpriteBatch b = e.SpriteBatch;
        Random random = Utility.CreateRandom(RaySeed);
        Color drawColour = RayColour;
        
        /* Don't know where the magic numbers come from here. */
        float zoomFactor = Game1.graphics.GraphicsDevice.Viewport.Height * 0.6f / 128f;
        int minRays = -(int)(128f / zoomFactor);
        int maxRays = location.Map.DisplayWidth / (int)(32f / LightrayIntensity * zoomFactor);
        /* -------------------------------------------------- */

        float rayScaleMultiplier = DefaultRayScale;
        /* As rayScaleMultiplier grows, the rays should become more transparent to avoid filling the screen with ugly white blob. */
        float scaleOpacityFactor = 1f;
        if (rayScaleMultiplier > DefaultRayScale)
        {
            scaleOpacityFactor = 1f - Math.Abs(rayScaleMultiplier - DefaultRayScale);
            scaleOpacityFactor = Utility.Clamp(scaleOpacityFactor, 0.5f, 1f);
            drawColour *= scaleOpacityFactor;
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
            
            /* First we offset each ray by a random amount so they're not all stacked or immediately side by side. */
            float offset = Utility.Lerp(0f - Utility.RandomFloat(24f, 32f, random), 0f, deg / 360f);
            
            /* Then we offset them further depending on where the viewport is on the map, so the rays stay in the same relative location. */
            offset += Game1.viewport.X / zoomFactor / 1.15f;
            // offset -= Game1.viewport.Y / zoomFactor / 1.15f; TODO: At 6am this should be positive, at 6pm it should be negative. Lerping?
            // if ((i * (32 / lightRayIntensity) - offset) * zoomFactor < 0) offset = location.Map.DisplayWidth - offset;
            /* -------------------------------------------------------------------------------------------------------------------------- */

            int chosenRay = random.Next(0, 3);
            Rectangle sourceRect = chosenRay switch 
            {
                0 => new Rectangle(230, 0, 100, 850),
                1 => new Rectangle(575, 0, 215, 1015),
                _ => new Rectangle(1065, 0, 630, 1000),
            };
            
            float nearFinalDrawScale = zoomFactor * rayScaleMultiplier * Utility.RandomFloat(0.85f, 1.15f, random);
            float finalDrawScale = nearFinalDrawScale * Game1.graphics.GraphicsDevice.Viewport.Height / sourceRect.Height;
            
            /* Where we're going, we don't need alpha blend mode... */
            b.End();
            b.Begin(SpriteSortMode.Deferred, BlendState.Additive, SamplerState.PointClamp);
            b.Draw(
                texture: HiResRayTexture,
                position: new Vector2((i * (sourceRect.Width / 4f / LightrayIntensity) - offset) * zoomFactor, Utility.RandomFloat(0f, -32f * zoomFactor, random)),
                sourceRectangle: sourceRect,
                color: rayColour,
                rotation: TimeBasedRotation,
                origin: new Vector2(sourceRect.Width / 2f, 64f),
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
    }
    
    private void OnAssetsInvalidated(object? sender, AssetsInvalidatedEventArgs e)
    {
        /* I really doubt anyone is ever gonna retexture these, but... you never know, I guess. */
        if (e.NamesWithoutLocale.Any(a => a.IsEquivalentTo("Spiderbuttons.GodRays/LightRays")))
        {
            _rayTexture = null;
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
        ModEntry.ModHelper.Events.GameLoop.TimeChanged -= OnTimeChanged;
        ModEntry.ModHelper.Events.Display.RenderedWorld -= OnRenderedWorld;
        ModEntry.ModHelper.Events.Player.Warped -= OnWarped;
        ModEntry.ModHelper.Events.Content.AssetsInvalidated -= OnAssetsInvalidated;
        GC.SuppressFinalize(this);
    }
}