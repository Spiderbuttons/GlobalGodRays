using System;
using System.Collections.Generic;
using System.Linq;
using GlobalGodRays.Config;
using Newtonsoft.Json;

namespace GlobalGodRays.Converters;

public class WeatherConfigConverter : JsonConverter

{
    public override bool CanConvert(Type objectType)
    {
        return objectType == typeof(Dictionary<string,WeatherConfigWithGenericToggle>);
    }

    public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
    {
        serializer.Serialize(writer, ((Dictionary<string,WeatherConfigWithGenericToggle>)value!).Where(weather => !weather.Value.UseGenericSettings || !weather.Value.DoAllPropertiesMatchDefault()).ToDictionary(weather => weather.Key, weather => weather.Value));
    }

    public override bool CanRead => false;

    public override object ReadJson(JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer)
    {
        throw new NotImplementedException();
    }
}