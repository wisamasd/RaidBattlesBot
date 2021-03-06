﻿using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using RaidBattlesBot.Model;
using Team23.TelegramSkeleton;
using Telegram.Bot.Types;
using Poll = RaidBattlesBot.Model.Poll;

namespace RaidBattlesBot.Handlers
{
  public class ChosenInlineResultHandler : IChosenInlineResultHandler
  {
    private readonly RaidBattlesContext myContext;
    private readonly RaidService myRaidService;
    private readonly IUrlHelper myUrlHelper;
    private readonly PoGoToolsClient myPoGoToolsClient;

    public ChosenInlineResultHandler(RaidBattlesContext context, RaidService raidService, IUrlHelper urlHelper, PoGoToolsClient poGoToolsClient)
    {
      myContext = context;
      myRaidService = raidService;
      myUrlHelper = urlHelper;
      myPoGoToolsClient = poGoToolsClient;
    }

    public async Task<bool?> Handle(ChosenInlineResult data, object context = default, CancellationToken cancellationToken = default)
    {
      var resultParts = data.ResultId.Split(':');
      switch (resultParts[0])
      {
        case PollEx.InlineIdPrefix:
          if (!PollEx.TryGetPollId(resultParts.ElementAtOrDefault(1), out var pollId, out var format))
            return null;

          var pollMessage = await myRaidService.GetOrCreatePollAndMessage(new PollMessage(data) { PollId = pollId }, myUrlHelper, format, cancellationToken);
          if (pollMessage != null)
          {
            if (pollMessage.Poll is Poll poll &&  (poll.Portal?.Guid ?? poll.PortalId) is string guid)
            {
              await myPoGoToolsClient.UpdateWayspot(guid, poll.ExRaidGym ? Wayspot.ExRaidGym : Wayspot.Gym, cancellationToken);
            }

            return true;
          }
          
          return false;
      }

      return null;
    }
  }
}