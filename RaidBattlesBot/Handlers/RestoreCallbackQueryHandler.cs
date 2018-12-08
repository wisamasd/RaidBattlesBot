﻿using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using RaidBattlesBot.Model;
using Team23.TelegramSkeleton;
using Telegram.Bot.Types;

namespace RaidBattlesBot.Handlers
{
  [CallbackQueryHandler(DataPrefix = "restore")]
  public class RestoreCallbackQueryHandler : ICallbackQueryHandler<object>
  {
    private readonly RaidBattlesContext myContext;
    private readonly RaidService myRaidService;
    private readonly IUrlHelper myUrlHelper;
    private readonly ChatInfo myChatInfo;

    public RestoreCallbackQueryHandler(RaidBattlesContext context, RaidService raidService, IUrlHelper urlHelper, ChatInfo chatInfo)
    {
      myContext = context;
      myRaidService = raidService;
      myUrlHelper = urlHelper;
      myChatInfo = chatInfo;
    }

    public async Task<(string, bool, string)> Handle(CallbackQuery data, object context = default, CancellationToken cancellationToken = default)
    {
      var callback = data.Data.Split(':');
      if (callback[0] != "restore")
        return (null, false, null);
      
      if (!int.TryParse(callback.ElementAtOrDefault(1) ?? "", NumberStyles.Integer, CultureInfo.InvariantCulture, out var pollId))
        return ("Голование подготавливается. Повторите позже", true, null);

      var poll = (await myRaidService.GetOrCreatePollAndMessage(new PollMessage(data) { PollId = pollId }, myUrlHelper, cancellationToken))?.Poll;

      if (poll == null)
        return ("Голосование не найдено", true, null);

      var user = data.From;

      if (!await myChatInfo.CandEditPoll(poll.Owner, user.Id, cancellationToken))
        return ("Вы не можете возобновить голосование", true, null);

      poll.Cancelled = false;
      var changed = myContext.SaveChanges() > 0;
      if (changed)
      {
        await myRaidService.UpdatePoll(poll, myUrlHelper, cancellationToken);
      }

      return (changed ? "Голосование возобновлено" : "Голование уже возобновлено", false, null);
    }
  }
}