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
                        CCSPlayerPawn? playerPawn = player.PlayerPawn.Value;

                        if (playerPawn != null)
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
        CCSPlayerController? player = @event.Userid;

        if (player?.IsBot ?? true)
        {
            return HookResult.Continue;
        }

        PlayerDataList.TryAdd(player, new Player());
        return HookResult.Continue;
    }

    [GameEventHandler]
    public HookResult OnPlayerDisconnect(EventPlayerDisconnect @event, GameEventInfo info)
    {
        CCSPlayerController? player = @event.Userid;

        if (player?.IsBot ?? true)
        {
            return HookResult.Continue;
        }

        RemoveParachute(player, null, true);
        return HookResult.Continue;
    }

    [GameEventHandler]
    public HookResult OnPlayerDeath(EventPlayerDeath @event, GameEventInfo info)
    {
        CCSPlayerController? player = @event.Userid;

        if (player?.IsBot ?? true)
        {
            return HookResult.Continue;
        }

        RemoveParachute(player, null, false);
        return HookResult.Continue;
    }
}