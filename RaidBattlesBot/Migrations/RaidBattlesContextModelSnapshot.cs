﻿// <auto-generated />
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.EntityFrameworkCore.Storage.Internal;
using RaidBattlesBot.Model;
using System;
using Telegram.Bot.Types.Enums;

namespace RaidBattlesBot.Migrations
{
    [DbContext(typeof(RaidBattlesContext))]
    partial class RaidBattlesContextModelSnapshot : ModelSnapshot
    {
        protected override void BuildModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "2.0.1-rtm-125")
                .HasAnnotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn);

            modelBuilder.Entity("RaidBattlesBot.Model.Poll", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd();

                    b.Property<int?>("AllowedVotes");

                    b.Property<bool>("Cancelled");

                    b.Property<DateTimeOffset?>("Modified");

                    b.Property<long?>("Owner");

                    b.Property<int?>("RaidId");

                    b.Property<DateTimeOffset?>("Time");

                    b.Property<string>("Title");

                    b.HasKey("Id");

                    b.HasIndex("RaidId");

                    b.ToTable("Polls");
                });

            modelBuilder.Entity("RaidBattlesBot.Model.PollMessage", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd();

                    b.Property<long?>("ChatId");

                    b.Property<int?>("ChatType");

                    b.Property<string>("InlineMesssageId");

                    b.Property<int?>("MesssageId");

                    b.Property<DateTimeOffset?>("Modified");

                    b.Property<int>("PollId");

                    b.Property<int?>("UserId");

                    b.HasKey("Id");

                    b.HasIndex("PollId");

                    b.ToTable("Messages");
                });

            modelBuilder.Entity("RaidBattlesBot.Model.Raid", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd();

                    b.Property<string>("Description");

                    b.Property<int?>("EggRaidId");

                    b.Property<DateTimeOffset?>("EndTime");

                    b.Property<string>("Gym");

                    b.Property<decimal?>("Lat")
                        .HasColumnType("decimal(18,15)");

                    b.Property<decimal?>("Lon")
                        .HasColumnType("decimal(18,15)");

                    b.Property<DateTimeOffset?>("Modified");

                    b.Property<string>("Move1");

                    b.Property<string>("Move2");

                    b.Property<string>("Name");

                    b.Property<string>("NearByAddress");

                    b.Property<string>("NearByPlaceId");

                    b.Property<int?>("Pokemon");

                    b.Property<string>("PossibleGym");

                    b.Property<int?>("RaidBossLevel");

                    b.Property<DateTimeOffset?>("StartTime");

                    b.Property<string>("Title");

                    b.HasKey("Id");

                    b.HasIndex("EggRaidId")
                        .IsUnique()
                        .HasFilter("[EggRaidId] IS NOT NULL");

                    b.ToTable("Raids");
                });

            modelBuilder.Entity("RaidBattlesBot.Model.Settings", b =>
                {
                    b.Property<long>("Chat");

                    b.Property<int>("DefaultAllowedVotes");

                    b.Property<DateTimeOffset?>("Modified");

                    b.HasKey("Chat");

                    b.ToTable("Settings");
                });

            modelBuilder.Entity("RaidBattlesBot.Model.Vote", b =>
                {
                    b.Property<int>("PollId");

                    b.Property<int>("UserId");

                    b.Property<string>("FirstName");

                    b.Property<string>("LasttName");

                    b.Property<DateTimeOffset?>("Modified");

                    b.Property<int?>("Team");

                    b.Property<string>("Username");

                    b.HasKey("PollId", "UserId");

                    b.ToTable("Votes");
                });

            modelBuilder.Entity("RaidBattlesBot.Model.Poll", b =>
                {
                    b.HasOne("RaidBattlesBot.Model.Raid", "Raid")
                        .WithMany("Polls")
                        .HasForeignKey("RaidId");
                });

            modelBuilder.Entity("RaidBattlesBot.Model.PollMessage", b =>
                {
                    b.HasOne("RaidBattlesBot.Model.Poll", "Poll")
                        .WithMany("Messages")
                        .HasForeignKey("PollId")
                        .OnDelete(DeleteBehavior.Cascade);
                });

            modelBuilder.Entity("RaidBattlesBot.Model.Raid", b =>
                {
                    b.HasOne("RaidBattlesBot.Model.Raid", "EggRaid")
                        .WithOne("PostEggRaid")
                        .HasForeignKey("RaidBattlesBot.Model.Raid", "EggRaidId");
                });

            modelBuilder.Entity("RaidBattlesBot.Model.Vote", b =>
                {
                    b.HasOne("RaidBattlesBot.Model.Poll")
                        .WithMany("Votes")
                        .HasForeignKey("PollId")
                        .OnDelete(DeleteBehavior.Cascade);
                });
#pragma warning restore 612, 618
        }
    }
}
