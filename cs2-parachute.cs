using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Utils;
using static Parachute.Parachute;

namespace Parachute;

public class Config : BasePluginConfig
{
    public Settings Settings { get; set; } = new();
}

public class Parachute : BasePlugin, IPluginConfig<Config>
{
    public override string ModuleName => "Parachute";
    public override string ModuleVersion => "0.0.6";
    public override string ModuleAuthor => "schwarper";

    public class PlayerData
    {
        public CDynamicProp? Entity;
        public bool Flying;
    }

    public class Settings
    {
        public float FallSpeed { get; set; } = 85;
        public bool Linear { get; set; } = true;
        public string Model { get; set; } = "models/props_survival/parachute/chute.vmdl";
        public float Decrease { get; set; } = 15;
        public string AdminFlag { get; set; } = string.Empty;
        public bool DisableWhenCarryingHostage { get; set; } = false;
    }

    private readonly Dictionary<IntPtr, PlayerData> _playerDatas = [];
    public Config Config { get; set; } = new();

    public void OnConfigParsed(Config config)
    {
        config.Settings.FallSpeed *= -1.0f;
        Config = config;
    }

    public override void Load(bool hotReload)
    {
        if (hotReload)
        {
            List<CCSPlayerController> players = Utilities.GetPlayers();
            foreach (CCSPlayerController player in players)
                _playerDatas[player.Handle] = new();
        }
    }

    [GameEventHandler]
    public HookResult OnPlayerConnect(EventPlayerConnectFull @event, GameEventInfo info)
    {
        if (@event.Userid is not CCSPlayerController player)
            return HookResult.Continue;

        _playerDatas[player.Handle] = new();
        return HookResult.Continue;
    }

    [GameEventHandler]
    public HookResult OnPlayerDisconnect(EventPlayerDisconnect @event, GameEventInfo info)
    {
        if (@event.Userid is not CCSPlayerController player)
            return HookResult.Continue;

        RemoveParachute(player);
        _playerDatas.Remove(player.Handle);
        return HookResult.Continue;
    }

    [GameEventHandler(HookMode.Pre)]
    public HookResult OnPlayerSpawn(EventPlayerSpawn @event, GameEventInfo info)
    {
        if (@event.Userid is not CCSPlayerController player)
            return HookResult.Continue;

        RemoveParachute(player);
        _playerDatas[player.Handle] = new();
        return HookResult.Continue;
    }

    [GameEventHandler(HookMode.Pre)]
    public HookResult OnPlayerDeath(EventPlayerDeath @event, GameEventInfo info)
    {
        if (@event.Userid is not CCSPlayerController player)
            return HookResult.Continue;

        RemoveParachute(player);
        _playerDatas[player.Handle] = new();
        return HookResult.Continue;
    }

    [ListenerHandler<Listeners.OnServerPrecacheResources>()]
    public void OnServerPrecacheResources(ResourceManifest manifest)
    {
        if (!string.IsNullOrEmpty(Config.Settings.Model))
            manifest.AddResource(Config.Settings.Model);
    }

    [ListenerHandler<Listeners.OnTick>()]
    public void OnTick()
    {
        bool hasParachuteModel = !string.IsNullOrEmpty(Config.Settings.Model);
        bool requiresAdminFlag = !string.IsNullOrEmpty(Config.Settings.AdminFlag);

        foreach ((IntPtr handle, PlayerData playerData) in _playerDatas)
        {
            if (new CCSPlayerController(handle) is not { } player ||
                player.PlayerPawn.Value is not { } playerPawn ||
                playerPawn.LifeState != (int)LifeState_t.LIFE_ALIVE ||
                (requiresAdminFlag && !AdminManager.PlayerHasPermissions(player, Config.Settings.AdminFlag)))
            {
                continue;
            }

            if (player.Buttons.HasFlag(PlayerButtons.Use) && !playerPawn.GroundEntity.IsValid && (!Config.Settings.DisableWhenCarryingHostage || playerPawn.HostageServices?.CarriedHostageProp.Value == null))
            {
                Vector velocity = playerPawn.AbsVelocity;

                if (velocity.Z >= 0.0)
                {
                    playerPawn.GravityScale = 1.0f;
                    continue;
                }

                if (hasParachuteModel && playerData.Entity == null)
                {
                    playerData.Entity = CreateParachute(playerPawn);
                }

                velocity.Z = (velocity.Z >= Config.Settings.FallSpeed && Config.Settings.Linear) || Config.Settings.Decrease == 0.0f
                    ? Config.Settings.FallSpeed
                    : velocity.Z + Config.Settings.Decrease;

                if (!playerData.Flying)
                {
                    playerPawn.GravityScale = 0.1f;
                    playerData.Flying = true;
                }
            }
            else if (playerData.Flying)
            {
                RemoveParachute(player);
                playerData.Entity = null;
                playerData.Flying = false;
                playerPawn.GravityScale = 1.0f;
            }
        }
    }

    private CDynamicProp? CreateParachute(CCSPlayerPawn playerPawn)
    {
        CDynamicProp? entity = Utilities.CreateEntityByName<CDynamicProp>("prop_dynamic_override");
        if (entity?.IsValid is not true)
            return null;

        entity.Teleport(playerPawn.AbsOrigin);
        entity.DispatchSpawn();
        entity.SetModel(Config.Settings.Model);
        entity.AcceptInput("SetParent", playerPawn, playerPawn, "!activator");
        return entity;
    }

    private void RemoveParachute(CCSPlayerController player)
    {
        if (_playerDatas.TryGetValue(player.Handle, out PlayerData? playerData) && playerData.Entity?.IsValid is true)
            playerData.Entity.Remove();
    }
}