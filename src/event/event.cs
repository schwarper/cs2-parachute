using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Modules.Utils;
using static Parachute.Config_Config;

namespace Parachute;

public partial class Parachute : BasePlugin
{
    public static void OnServerPrecacheResources(ResourceManifest manifest)
    {
        manifest.AddResource(Config.Model);
    }

    public void OnFakeConVarChanged()
    {
        sv_parachute.ValueChanged += (_, value) =>
        {
            if (!value)
            {
                foreach ((CCSPlayerController player, Player playerData) in PlayerDataList)
                {
                    if (playerData.Flying)
                    {
                        if (player.PlayerPawn.Value is CCSPlayerPawn playerPawn)
                        {
                            playerPawn.GravityScale = 1.0f;
                        }

                        RemoveParachute(player, playerData, false);
                    }
                }
            }
        };
    }

    [GameEventHandler]
    public HookResult OnPlayerConnect(EventPlayerConnectFull @event, GameEventInfo info)
    {
        if (@event.Userid is not CCSPlayerController player || player.IsBot)
        {
            return HookResult.Continue;
        }

        PlayerDataList.TryAdd(player, new Player());
        return HookResult.Continue;
    }

    [GameEventHandler]
    public HookResult OnPlayerDisconnect(EventPlayerDisconnect @event, GameEventInfo info)
    {
        if (@event.Userid is not CCSPlayerController player || player.IsBot)
        {
            return HookResult.Continue;
        }

        RemoveParachute(player, null, true);
        return HookResult.Continue;
    }

    [GameEventHandler]
    public HookResult OnPlayerDeath(EventPlayerDeath @event, GameEventInfo info)
    {
        if (@event.Userid is not CCSPlayerController player || player.IsBot)
        {
            return HookResult.Continue;
        }

        RemoveParachute(player, null, false);
        return HookResult.Continue;
    }
}