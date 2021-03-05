﻿// <auto-generated />
using System;
using FeedReader.ServerCore.Datas;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

namespace FeedReader.ServerCore.Migrations
{
    [DbContext(typeof(FeedReaderDbContext))]
    [Migration("20210224171052_RemoveFeedItemHashFromUserFeedItemsTable")]
    partial class RemoveFeedItemHashFromUserFeedItemsTable
    {
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("Relational:MaxIdentifierLength", 63)
                .HasAnnotation("ProductVersion", "5.0.3")
                .HasAnnotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);

            modelBuilder.Entity("FeedReader.ServerCore.Models.Feed", b =>
                {
                    b.Property<string>("Id")
                        .HasMaxLength(64)
                        .HasColumnType("character varying(64)");

                    b.Property<string>("Category")
                        .HasColumnType("text");

                    b.Property<string>("Description")
                        .HasColumnType("text");

                    b.Property<string>("IconUri")
                        .HasColumnType("text");

                    b.Property<DateTime>("LastUpdateTimeInUtc")
                        .HasColumnType("timestamp without time zone");

                    b.Property<string>("Name")
                        .HasColumnType("text");

                    b.Property<DateTime>("RegistrationTimeInUtc")
                        .HasColumnType("timestamp without time zone");

                    b.Property<string>("Uri")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<string>("WebSiteUri")
                        .HasColumnType("text");

                    b.HasKey("Id");

                    b.ToTable("Feeds");
                });

            modelBuilder.Entity("FeedReader.ServerCore.Models.FeedItem", b =>
                {
                    b.Property<string>("Id")
                        .HasMaxLength(64)
                        .HasColumnType("character varying(64)");

                    b.Property<string>("Content")
                        .HasColumnType("text");

                    b.Property<string>("FeedId")
                        .HasColumnType("character varying(64)");

                    b.Property<DateTime>("PublishTimeInUtc")
                        .HasColumnType("timestamp without time zone");

                    b.Property<string>("Summary")
                        .HasColumnType("text");

                    b.Property<string>("Title")
                        .HasColumnType("text");

                    b.Property<string>("TopicPictureUri")
                        .HasColumnType("text");

                    b.Property<string>("Uri")
                        .IsRequired()
                        .HasColumnType("text");

                    b.HasKey("Id");

                    b.HasIndex("FeedId");

                    b.ToTable("FeedItems");
                });

            modelBuilder.Entity("FeedReader.ServerCore.Models.User", b =>
                {
                    b.Property<string>("Id")
                        .HasMaxLength(32)
                        .HasColumnType("character varying(32)");

                    b.Property<DateTime>("LastActiveTimeInUtc")
                        .HasColumnType("timestamp without time zone");

                    b.Property<DateTime>("RegistrationTimeInUtc")
                        .HasColumnType("timestamp without time zone");

                    b.Property<string>("ThirdPartyId")
                        .HasColumnType("text");

                    b.HasKey("Id");

                    b.HasIndex("ThirdPartyId")
                        .IsUnique();

                    b.ToTable("Users");
                });

            modelBuilder.Entity("FeedReader.ServerCore.Models.UserFeed", b =>
                {
                    b.Property<string>("UserId")
                        .HasColumnType("character varying(32)");

                    b.Property<string>("FeedId")
                        .HasColumnType("character varying(64)");

                    b.Property<string>("Group")
                        .HasColumnType("text");

                    b.Property<DateTime>("LastReadedTimeInUtc")
                        .HasColumnType("timestamp without time zone");

                    b.HasKey("UserId", "FeedId");

                    b.HasIndex("FeedId");

                    b.HasIndex("UserId");

                    b.ToTable("UserFeeds");
                });

            modelBuilder.Entity("FeedReader.ServerCore.Models.UserFeedItem", b =>
                {
                    b.Property<string>("UserId")
                        .HasMaxLength(32)
                        .HasColumnType("character varying(32)");

                    b.Property<string>("FeedItemId")
                        .HasColumnType("character varying(64)");

                    b.Property<bool>("IsFavorite")
                        .HasColumnType("boolean");

                    b.HasKey("UserId", "FeedItemId");

                    b.HasIndex("FeedItemId");

                    b.HasIndex("UserId");

                    b.ToTable("UserFeedItems");
                });

            modelBuilder.Entity("FeedReader.ServerCore.Models.FeedItem", b =>
                {
                    b.HasOne("FeedReader.ServerCore.Models.Feed", "Feed")
                        .WithMany()
                        .HasForeignKey("FeedId");

                    b.Navigation("Feed");
                });

            modelBuilder.Entity("FeedReader.ServerCore.Models.UserFeed", b =>
                {
                    b.HasOne("FeedReader.ServerCore.Models.Feed", "Feed")
                        .WithMany()
                        .HasForeignKey("FeedId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("FeedReader.ServerCore.Models.User", "User")
                        .WithMany("Feeds")
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Feed");

                    b.Navigation("User");
                });

            modelBuilder.Entity("FeedReader.ServerCore.Models.UserFeedItem", b =>
                {
                    b.HasOne("FeedReader.ServerCore.Models.FeedItem", "FeedItem")
                        .WithMany()
                        .HasForeignKey("FeedItemId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("FeedItem");
                });

            modelBuilder.Entity("FeedReader.ServerCore.Models.User", b =>
                {
                    b.Navigation("Feeds");
                });
#pragma warning restore 612, 618
        }
    }
}
