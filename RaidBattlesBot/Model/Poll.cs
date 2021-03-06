﻿using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using DelegateDecompiler;
using Telegram.Bot.Types;

namespace RaidBattlesBot.Model
{
  public class Poll : ITrackable
  {
    public Poll() {  }

    public Poll(Message message)
    {
      Owner = message.From?.Id ?? message.Chat.Id;
    }

    public Poll(InlineQuery inlineQuery)
    {
      Owner = inlineQuery.From?.Id;
      Title = inlineQuery.Query;
    }

    public int Id { get; set; }

    public int? RaidId { get; set; }
    public Raid Raid { get; set; }

    public string PortalId { get; set; }
    public Portal Portal { get; set; }
    
    /// <remarks>Chat Id (equals user Id for private chat with bot)</remarks>
    public long? Owner { get; set; }
    public string Title { get; set; }
    public DateTimeOffset? Time { get; set; }
    public DateTimeOffset? Modified { get; set; }
    public bool Cancelled { get; set; }
    public VoteEnum? AllowedVotes { get; set; }
    public bool ExRaidGym { get; set; }
    private static readonly TimeSpan LiveTime = TimeSpan.FromHours(1);

    [Computed, NotMapped]
    public DateTimeOffset? EndTime => Raid != null ? Raid.RaidBossEndTime : Modified?.Add(LiveTime);


    public List<PollMessage> Messages { get; set; }
    public List<Vote> Votes { get; set; }
  }
}