using CounterStrikeSharp.API;
using System.Reflection;
using Tomlyn;
using Tomlyn.Model;

namespace Parachute;

public static class Config_Config
{
    public static Cfg Config { get; set; } = new Cfg();

    public static void Load()
    {
        string assemblyName = Assembly.GetExecutingAssembly().GetName().Name ?? string.Empty;

        string configPath = Path.Combine(Server.GameDirectory,
            "csgo",
            "addons",
            "counterstrikesharp",
            "configs",
            "plugins",
            assemblyName,
            "config.toml"
        );

        if (!File.Exists(configPath))
        {
            throw new FileNotFoundException($"Configuration file not found: {configPath}");
        }

        string configText = File.ReadAllText(configPath);
        TomlTable model = Toml.ToModel(configText);

        TomlTable settingsTable = (TomlTable)model["Settings"];
        Config.Fallspeed = float.Parse(settingsTable["FallSpeed"].ToString()!) * -1.0f;
        Config.Linear = bool.Parse(settingsTable["Linear"].ToString()!);
        Config.Model = settingsTable["Model"].ToString()!;
        Config.Decrease = float.Parse(settingsTable["Decrease"].ToString()!);
        Config.AdminFlag = settingsTable["AdminFlag"].ToString()!;
        Config.DisableWhenCarryingHostage = bool.Parse(settingsTable["DisableWhenCarryingHostage"].ToString()!);
    }

    public class Cfg
    {
        public float Fallspeed { get; set; } = 85;
        public bool Linear { get; set; } = true;
        public string Model { get; set; } = "models/props_survival/parachute/chute.vmdl";
        public float Decrease { get; set; } = 15;
        public string AdminFlag { get; set; } = string.Empty;
        public bool DisableWhenCarryingHostage { get; set; } = false;
    }
}