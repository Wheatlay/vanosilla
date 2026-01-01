using System;
using System.Net;
using System.Threading.Tasks;
using PhoenixLib.Logging;
using Plugin.Raids.Const;
using Qmmands;
using WingsAPI.Scripting.LUA;
using WingsAPI.Scripting.ScriptManager;
using WingsEmu.Commands.Checks;
using WingsEmu.Commands.Entities;
using WingsEmu.DTOs.Account;
using WingsEmu.Game.Extensions;
using WingsEmu.Game.Networking;
using WingsEmu.Game.Raids;

namespace Plugin.Raids.Commands;

[Name("OwnerRaids")]
[Description("Module related to Raids files management commands.")]
[RequireAuthority(AuthorityType.Root)]
public class RaidAdminCommandsModule : SaltyModuleBase
{
    private readonly ScriptFactoryConfiguration _configuration;
    private readonly IRaidScriptManager _raidScriptManager;

    public RaidAdminCommandsModule(IRaidScriptManager raidScriptManager, ScriptFactoryConfiguration configuration)
    {
        _raidScriptManager = raidScriptManager;
        _configuration = configuration;
    }

    [Command("download-raids", "download-raid", "raid-dl")]
    public async Task<SaltyCommandResult> DownloadRaid(string raidName, [Remainder] string raidUrl)
    {
        try
        {
            using var webClient = new WebClient();
            webClient.DownloadFile(raidUrl, _configuration.RaidsDirectory + '/' + raidName + ".lua");
        }
        catch (Exception e)
        {
            Log.Error($"[DOWNLOAD_RAID] {raidName} {raidUrl}", e);
            Context.Player.SendErrorChatMessage($"Couldn't download {raidUrl}");
            return new SaltyCommandResult(false);
        }

        try
        {
            _raidScriptManager.Load();
            Context.Player.SendSuccessChatMessage("Raids reloaded, check your console output!");
        }
        catch (Exception e)
        {
            Log.Error("[RELOAD_RAIDS]", e);
            Context.Player.SendErrorChatMessage("Couldn't reload raids! :(");
            return new SaltyCommandResult(false);
        }

        return new SaltyCommandResult(true);
    }

    [Command("raid-objective")]
    public async Task<SaltyCommandResult> CompleteObjectives()
    {
        IClientSession session = Context.Player;

        if (session.PlayerEntity.Raid?.Instance == null)
        {
            return new SaltyCommandResult(false);
        }

        if (!session.PlayerEntity.Raid.Instance.RaidSubInstances.TryGetValue(session.CurrentMapInstance.Id, out RaidSubInstance subInstance))
        {
            return new SaltyCommandResult(false);
        }

        await subInstance.TriggerEvents(RaidConstEventKeys.ObjectivesCompleted);

        return new SaltyCommandResult(true, "Done.");
    }

    [Command("reload-raids", "reloadraids", "raids-reload")]
    public async Task<SaltyCommandResult> ReloadRaids()
    {
        try
        {
            _raidScriptManager.Load();
            Context.Player.SendSuccessChatMessage("Raids reloaded, check your console output!");
        }
        catch (Exception e)
        {
            Log.Error("[RELOAD_RAIDS]", e);
            Context.Player.SendErrorChatMessage("Couldn't reload raids! :(");
            return new SaltyCommandResult(false);
        }

        return new SaltyCommandResult(true);
    }
}