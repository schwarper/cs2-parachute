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
        string assemblyName = Assembly.GetExecutingAssembly().GetName().Name ?? "";
        string cfgPath = $"{Server.GameDirectory}/csgo/addons/counterstrikesharp/configs/plugins/{assemblyName}";

        LoadConfig($"{cfgPath}/config.toml");
    }

    private static void LoadConfig(string configPath)
    {
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

        Server.PrintToConsole($"FallSpeed set to: {Config.Fallspeed}");
        Server.PrintToConsole($"Linear set to: {Config.Linear}");
        Server.PrintToConsole($"Model set to: {Config.Model}");
        Server.PrintToConsole($"Decrease set to: {Config.Decrease}");
        Server.PrintToConsole($"AdminFlag set to: {Config.AdminFlag}");
        Server.PrintToConsole($"DisableWhenCarryingHostage set to: {Config.DisableWhenCarryingHostage}");
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