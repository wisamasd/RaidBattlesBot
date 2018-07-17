﻿using System;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using RaidBattlesBot.Model;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.InlineQueryResults;

namespace RaidBattlesBot.Handlers
{
  [InlineQueryHandler]
  public class GeneralInlineQueryHandler : IInlineQueryHandler
  {
    public const string SwitchToGymParameter = "linkToGym";

    private readonly ITelegramBotClient myBot;
    private readonly IUrlHelper myUrlHelper;
    private readonly UserInfo myUserInfo;
    private readonly ShareInlineQueryHandler myShareInlineQueryHandler;
    private readonly RaidService myRaidService;
    private readonly IngressClient myIngressClient;
    private readonly RaidBattlesContext myDb;

    public GeneralInlineQueryHandler(ITelegramBotClient bot, IUrlHelper urlHelper, UserInfo userInfo, ShareInlineQueryHandler shareInlineQueryHandler, RaidService raidService, IngressClient ingressClient, RaidBattlesContext db)
    {
      myBot = bot;
      myUrlHelper = urlHelper;
      myUserInfo = userInfo;
      myShareInlineQueryHandler = shareInlineQueryHandler;
      myRaidService = raidService;
      myIngressClient = ingressClient;
      myDb = db;
    }

    public async Task<bool?> Handle(InlineQuery data, object context = default, CancellationToken cancellationToken = default)
    {
      InlineQueryResultBase[] inlineQueryResults;

      string query = null;
      Portal portal = null;
      string switchPmParameter = null;
      foreach (var queryPart in data.Query.Split(' ', StringSplitOptions.RemoveEmptyEntries))
      {
        switch (queryPart)
        {
          case string _ when queryPart.StartsWith(GymInlineQueryHandler.PREFIX):
            var guid = queryPart.Substring(GymInlineQueryHandler.PREFIX.Length);
            var portalGuid = PortalEx.DecodeGuid(guid);
            portal = await myIngressClient.Get(portalGuid, data.Location, cancellationToken);
            break;
          default:
            query += (query == null ? default(char?) : ' ') + queryPart;
            break;
        }
      }
      if (string.IsNullOrWhiteSpace(data.Query)) // check whole query for sharing branch
      {
        inlineQueryResults = await myShareInlineQueryHandler.GetActivePolls(data.From, cancellationToken);
      }
      else
      {
        var pollId = await myRaidService.GetPollId(new Poll(data) { Title = query, Portal = portal }, cancellationToken);
        switchPmParameter = portal == null ? $"{SwitchToGymParameter}{pollId}" : null;
        inlineQueryResults = await Task.WhenAll(
          VoteEnumEx.AllowedVoteFormats
            .Select((_, i) => new Poll
            {
              Id = pollId + i,
              Title = query,
              AllowedVotes = _,
              Portal = portal
            })
            .Select(async fakePoll => new InlineQueryResultArticle($"create:{fakePoll.Id}", fakePoll.GetTitle(myUrlHelper),
              new InputTextMessageContent((await fakePoll.GetMessageText(myUrlHelper, myUserInfo, RaidEx.ParseMode, cancellationToken)).ToString())
              {
                ParseMode = RaidEx.ParseMode,
                DisableWebPagePreview = fakePoll.DisableWebPreview()
              })
              {
                Description = fakePoll.AllowedVotes?.Format(new StringBuilder("Создать голосование ")).ToString(),
                HideUrl = true,
                ThumbUrl = fakePoll.GetThumbUrl(myUrlHelper).ToString(),
                ReplyMarkup = fakePoll.GetReplyMarkup()
            }));
      }

      await myBot.AnswerInlineQueryAsync(data.Id, inlineQueryResults,
        switchPmText: switchPmParameter != null ? "Привязатать голосование к гиму" : null, switchPmParameter: switchPmParameter,
        cacheTime: 0, cancellationToken: cancellationToken);

      await myDb.SaveChangesAsync(cancellationToken);
      return true;
    }
  }
}