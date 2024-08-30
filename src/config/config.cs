using CounterStrikeSharp.API.Core;
using System.Text.Json.Serialization;

namespace Parachute;

public class Config : BasePluginConfig
{
    [JsonPropertyName("css_parachute_fallspeed")] public float Fallspeed { get; set; } = 85;
    [JsonPropertyName("css_parachute_linear")] public bool Linear { get; set; } = true;
    [JsonPropertyName("css_parachute_model")] public string Model { get; set; } = "models/props_survival/parachute/chute.vmdl";
    [JsonPropertyName("css_parachute_decrease")] public float Decrease { get; set; } = 15;
    [JsonPropertyName("css_admin_flag")] public string AdminFlag { get; set; } = string.Empty;
    [JsonPropertyName("css_disable_when_carrying_hostage")] public bool DisableWhenCarryingHostage { get; set; } = false;
}