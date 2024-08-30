using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Cvars;
using CounterStrikeSharp.API.Modules.Utils;
using static CounterStrikeSharp.API.Core.Listeners;
using static Parachute.Config_Config;

namespace Parachute;

public partial class Parachute : BasePlugin
{
    public override string ModuleName => "Parachute";
    public override string ModuleVersion => "0.0.3";
    public override string ModuleAuthor => "schwarper";

    public class Player
    {
        public CDynamicProp? Model;
        public bool Flying;
    }

    public readonly Dictionary<CCSPlayerController, Player> PlayerDataList = [];
    public FakeConVar<bool> sv_parachute = new("sv_parachute", "Enable/Disable Parachute", true);

    public override void Load(bool hotReload)
    {
        Config_Config.Load();
        OnFakeConVarChanged();
        RegisterFakeConVars(sv_parachute);
        RegisterListener<OnTick>(OnTick);

        if (!string.IsNullOrEmpty(Config.Model))
        {
            RegisterListener<OnServerPrecacheResources>(OnServerPrecacheResources);
        }

        if (hotReload)
        {
            List<CCSPlayerController> players = Utilities.GetPlayers().Where(player => !player.IsBot).ToList();

            foreach (CCSPlayerController? player in players)
            {
                PlayerDataList.TryAdd(player, new Player());
            }

        }
    }
    public override void Unload(bool hotReload)
    {
        if (!string.IsNullOrEmpty(Config.Model))
        {
            RemoveListener<OnServerPrecacheResources>(OnServerPrecacheResources);
        }

        RemoveListener<OnTick>(OnTick);
    }

    private void OnTick()
    {
        if (sv_parachute.Value == false || PlayerDataList.Count == 0)
        {
            return;
        }

        bool parachuteModelEnable = !string.IsNullOrEmpty(Config.Model);
        bool adminFlagEnable = !string.IsNullOrEmpty(Config.AdminFlag);

        foreach ((CCSPlayerController player, Player playerData) in PlayerDataList)
        {
            CCSPlayerPawn? playerPawn = player.PlayerPawn.Value;

            if (
                !player.PawnIsAlive ||
                (adminFlagEnable && !AdminManager.PlayerHasPermissions(player, Config.AdminFlag)) ||
                playerPawn == null
            )
            {
                continue;
            }

            if (player.Buttons.HasFlag(PlayerButtons.Use) && !playerPawn.GroundEntity.IsValid && (!Config.DisableWhenCarryingHostage || playerPawn.HostageServices?.CarriedHostageProp.Value == null))
            {
                Vector velocity = playerPawn.AbsVelocity;

                if (velocity.Z >= 0.0)
                {
                    playerPawn.GravityScale = 1.0f;
                    continue;
                }

                if (parachuteModelEnable && playerData.Model == null)
                {
                    playerData.Model = CreateParachute(playerPawn);
                }

                if ((velocity.Z >= Config.Fallspeed && Config.Linear) || Config.Decrease == 0.0f)
                {
                    velocity.Z = Config.Fallspeed;
                }
                else
                {
                    velocity.Z += Config.Decrease;
                }

                if (!playerData.Flying)
                {
                    playerPawn.GravityScale = 0.1f;
                    playerData.Flying = true;
                }
            }
            else if (playerData.Flying)
            {
                RemoveParachute(player, playerData, false);
                playerData.Model = null;
                playerData.Flying = false;
                playerPawn.GravityScale = 1.0f;
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
        entity.AcceptInput("FollowEntity", playerPawn, playerPawn, "!activator");

        return entity;
    }

    private void RemoveParachute(CCSPlayerController player, Player? playerData, bool removePlayer)
    {
        if (playerData == null && !PlayerDataList.TryGetValue(player, out playerData))
        {
            return;
        }

        if (playerData.Model?.IsValid == true)
        {
            playerData.Model.Remove();
            playerData.Model = null;
        }

        playerData.Model = null;
        playerData.Flying = false;

        if (removePlayer)
        {
            PlayerDataList.Remove(player);
        }
    }
}
