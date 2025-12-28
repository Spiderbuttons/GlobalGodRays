using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using StardewValley.Network;

namespace GlobalGodRays.APIs;

/// <summary>
///     This is the public API surface of Cloudy Skies.
/// </summary>
public interface ICloudySkiesApi
{
	/// <summary>
	///     Enumerate all the custom weather conditions we know about.
	/// </summary>
	IEnumerable<IWeatherData> GetAllCustomWeather();
}

/// <summary>
///     The data resource for any given custom weather type. This is, for
///     the time being, read-only in the API. Modify the data resource if
///     you want to change the weather, please!
/// </summary>
public interface IWeatherData
{
	/// <summary>
	///     This weather condition's unique Id. This is the Id that you
	///     would check with game state queries, etc.
	/// </summary>
	string Id { get; set; }

	/// <summary>
	///     A display name to show the player when this weather condition
	///     should be referenced by name. This is a tokenizable string.
	/// </summary>
	string DisplayName { get; set; }

	/// <summary>
	///     Controls the value of <see cref="LocationWeather.IsRaining" />.
	/// </summary>
	bool IsRaining { get; set; }

	/// <summary>
	///     Controls the value of <see cref="LocationWeather.IsSnowing" />.
	/// </summary>
	bool IsSnowing { get; set; }

	/// <summary>
	///     Controls the value of <see cref="LocationWeather.IsLightning" />.
	/// </summary>
	bool IsLightning { get; set; }

	/// <summary>
	///     Controls the value of <see cref="LocationWeather.IsDebrisWeather" />.
	/// </summary>
	bool IsDebrisWeather { get; set; }

	/// <summary>
	///     Controls the value of <see cref="LocationWeather.IsGreenRain" />.
	/// </summary>
	bool IsGreenRain { get; set; }
}