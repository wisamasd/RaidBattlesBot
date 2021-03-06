﻿using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using RaidBattlesBot.Model;
using Telegram.Bot.Types;

namespace RaidBattlesBot.Handlers
{
  [CallbackQueryHandler(DataPrefix = ID)]
  public class AdjustCallbackQueryHandler : ICallbackQueryHandler
  {
    public const string ID = "adjust";
    
    private readonly RaidBattlesContext myContext;
    private readonly RaidService myRaidService;
    private readonly IUrlHelper myUrlHelper;
    private readonly ChatInfo myChatInfo;

    public AdjustCallbackQueryHandler(RaidBattlesContext context, RaidService raidService, IUrlHelper urlHelper, ChatInfo chatInfo)
    {
      myContext = context;
      myRaidService = raidService;
      myUrlHelper = urlHelper;
      myChatInfo = chatInfo;
    }

    public async Task<(string, bool, string)> Handle(CallbackQuery data, object context = default, CancellationToken cancellationToken = default)
    {
      var callback = data.Data.Split(':');
      if (callback[0] != "adjust")
        return (null, false, null);
      
      if (!PollEx.TryGetPollId(callback.ElementAtOrDefault(1), out var pollId, out var format))
        return ("Голование подготавливается. Повторите позже", true, null);

      if (!int.TryParse(callback.ElementAtOrDefault(2) ?? "", NumberStyles.Integer, CultureInfo.InvariantCulture, out var offset))
        return ("", false, null);

      var poll = (await myRaidService.GetOrCreatePollAndMessage(new PollMessage(data) { PollId = pollId }, myUrlHelper, format, cancellationToken))?.Poll;

      if (poll == null)
        return ("Голосование не найдено", true, null);

      var user = data.From;

      if (!await myChatInfo.CandEditPoll(poll.Owner, user.Id, cancellationToken))
        return ("Вы не можете редактировать голосование", true, null);

      poll.Time = poll.Time?.AddMinutes(offset);
      if (poll.Time > poll.Raid?.RaidBossEndTime)
        return ($"В {poll.Time:t} рейд уже закончится", true, null);

      var changed = await myContext.SaveChangesAsync(cancellationToken) > 0;
      if (changed)
      {
        await myRaidService.UpdatePoll(poll, myUrlHelper, cancellationToken);
      }

      return ($"Голосование установлено на {poll.Time:t}", false, null);
    }
  }
}