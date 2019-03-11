﻿// <auto-generated />
using System;
using LocusPocusBot.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace LocusPocusBot.Migrations
{
    [DbContext(typeof(BotContext))]
    [Migration("20190311155845_CreateLogsTable")]
    partial class CreateLogsTable
    {
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "2.2.2-servicing-10034")
                .HasAnnotation("Relational:MaxIdentifierLength", 64);

            modelBuilder.Entity("LocusPocusBot.Data.ChatEntity", b =>
                {
                    b.Property<long>("Id");

                    b.Property<string>("FirstName");

                    b.Property<string>("LastName");

                    b.Property<string>("Title");

                    b.Property<string>("Type");

                    b.Property<DateTime>("UpdatedAt");

                    b.Property<string>("Username");

                    b.HasKey("Id");

                    b.ToTable("Chats");
                });

            modelBuilder.Entity("LocusPocusBot.Data.LogEntity", b =>
                {
                    b.Property<uint>("Id")
                        .ValueGeneratedOnAdd();

                    b.Property<DateTime>("At");

                    b.Property<int>("AvailabilityType");

                    b.Property<long>("ChatId");

                    b.Property<string>("Department")
                        .IsRequired();

                    b.Property<int>("RequestType");

                    b.HasKey("Id");

                    b.HasIndex("ChatId");

                    b.ToTable("Logs");
                });

            modelBuilder.Entity("LocusPocusBot.Data.LogEntity", b =>
                {
                    b.HasOne("LocusPocusBot.Data.ChatEntity", "Chat")
                        .WithMany()
                        .HasForeignKey("ChatId")
                        .OnDelete(DeleteBehavior.Cascade);
                });
#pragma warning restore 612, 618
        }
    }
}
