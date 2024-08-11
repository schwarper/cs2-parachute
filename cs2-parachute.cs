using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Utils;
using System.Text.Json.Serialization;
using static CounterStrikeSharp.API.Core.Listeners;

namespace Parachute;

public class Config : BasePluginConfig
{
    [JsonPropertyName("css_parachute_fallspeed")] public float Fallspeed { get; set; } = 100;
    [JsonPropertyName("css_parachute_linear")] public bool Linear { get; set; } = true;
    [JsonPropertyName("css_parachute_model")] public string Model { get; set; } = "models/props_survival/parachute/chute.vmdl";
    [JsonPropertyName("css_parachute_decrease")] public float Decrease { get; set; } = 50;
    [JsonPropertyName("css_admin_flag")] public string AdminFlag { get; set; } = string.Empty;
}

public class Parachute : BasePlugin, IPluginConfig<Config>
{
    public override string ModuleName => "Parachute";
    public override string ModuleVersion => "0.0.1";
    public override string ModuleAuthor => "schwarper";

    public Config Config { get; set; } = new Config();
    public readonly Dictionary<CCSPlayerController, CDynamicProp?> PlayerDataList = [];

    public override void Load(bool hotReload)
    {
        RegisterEventHandler<EventPlayerConnectFull>(OnPlayerConnect);
        RegisterEventHandler<EventPlayerDisconnect>(OnPlayerDisconnect);
        RegisterEventHandler<EventPlayerDeath>(OnPlayerDeath);

        if (!string.IsNullOrEmpty(Config.Model))
        {
            RegisterListener<OnServerPrecacheResources>(OnServerPrecacheResources);
        }

        RegisterListener<OnTick>(OnTick);
    }
    public override void Unload(bool hotReload)
    {
        if (!string.IsNullOrEmpty(Config.Model))
        {
            RemoveListener<OnServerPrecacheResources>(OnServerPrecacheResources);
        }

        RemoveListener<OnTick>(OnTick);
    }
    public void OnConfigParsed(Config config)
    {
        config.Fallspeed *= -1.0f;
        Config = config;
    }
    private HookResult OnPlayerConnect(EventPlayerConnectFull @event, GameEventInfo info)
    {
        CCSPlayerController? player = @event.Userid;

        if (player == null)
        {
            return HookResult.Continue;
        }

        PlayerDataList.TryAdd(player, null);
        return HookResult.Continue;
    }
    private HookResult OnPlayerDisconnect(EventPlayerDisconnect @event, GameEventInfo info)
    {
        CCSPlayerController? player = @event.Userid;

        if (player == null)
        {
            return HookResult.Continue;
        }

        RemoveParachute(player, true);
        return HookResult.Continue;
    }
    private HookResult OnPlayerDeath(EventPlayerDeath @event, GameEventInfo info)
    {
        CCSPlayerController? player = @event.Userid;

        if (player == null)
        {
            return HookResult.Continue;
        }

        RemoveParachute(player, false);
        return HookResult.Continue;
    }
    private void OnServerPrecacheResources(ResourceManifest manifest)
    {
        manifest.AddResource(Config.Model);
    }
    private void OnTick()
    {
        bool parachuteModelEnable = !string.IsNullOrEmpty(Config.Model);

        foreach (KeyValuePair<CCSPlayerController, CDynamicProp?> kvp in PlayerDataList)
        {
            CCSPlayerController player = kvp.Key;
            CCSPlayerPawn? playerPawn = player.PlayerPawn.Value;

            if (
                !player.PawnIsAlive ||
                !string.IsNullOrEmpty(Config.AdminFlag) && !AdminManager.PlayerHasPermissions(player, Config.AdminFlag) ||
                playerPawn == null
            )
            {
                continue;
            }

            CDynamicProp? entity = kvp.Value;

            if (player.Buttons.HasFlag(PlayerButtons.Use) && !playerPawn.GroundEntity.IsValid)
            {
                Vector velocity = playerPawn.AbsVelocity;

                if (velocity.Z >= 0.0)
                {
                    continue;
                }

                if (parachuteModelEnable && entity == null)
                {
                    PlayerDataList[player] = CreateParachute(playerPawn);
                }

                bool isFallSpeed = false;

                if (velocity.Z >= Config.Fallspeed)
                {
                    isFallSpeed = true;
                }

                if (isFallSpeed && Config.Linear || Config.Decrease == 0.0f)
                {
                    velocity.Z = Config.Fallspeed;
                }
                else
                {
                    velocity.Z += Config.Decrease;
                }

                playerPawn.GravityScale = 0.01f;
            }
            else
            {
                entity?.Remove();
                PlayerDataList[player] = null;

                if (playerPawn.GravityScale == 0.01f)
                {
                    playerPawn.GravityScale = 1.0f;
                }
            }
        }
    }

    private CDynamicProp? CreateParachute(CCSPlayerPawn playerPawn)
    {
        CDynamicProp? entity = Utilities.CreateEntityByName<CDynamicProp>("prop_dynamic_override");

        if (entity == null || !entity.IsValid)
        {
            return null;
        }

        entity.Teleport(playerPawn.AbsOrigin);
        entity.DispatchSpawn();
        entity.SetModel(Config.Model);
        entity.AcceptInput("SetParent", playerPawn, playerPawn, "!activator");

        return entity;
    }

    private void RemoveParachute(CCSPlayerController player, bool removePlayer)
    {
        if (PlayerDataList.TryGetValue(player, out CDynamicProp? entity) && entity != null)
        {
            entity.Remove();
            PlayerDataList[player] = null;

            if (removePlayer)
            {
                PlayerDataList.Remove(player);
            }
        }
    }
}
