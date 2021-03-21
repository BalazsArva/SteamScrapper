﻿// <auto-generated />
using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using SteamScrapper.Infrastructure.Database.Context;

namespace SteamScrapper.Infrastructure.Database.Migrations
{
    [DbContext(typeof(SteamContext))]
    partial class SteamContextModelSnapshot : ModelSnapshot
    {
        protected override void BuildModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("Relational:MaxIdentifierLength", 128)
                .HasAnnotation("ProductVersion", "5.0.4")
                .HasAnnotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn);

            modelBuilder.Entity("SteamScrapper.Infrastructure.Database.Entities.App", b =>
                {
                    b.Property<long>("Id")
                        .HasColumnType("bigint");

                    b.Property<string>("BannerUrl")
                        .HasMaxLength(2048)
                        .HasColumnType("nvarchar(2048)");

                    b.Property<bool>("IsActive")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("bit")
                        .HasDefaultValueSql("0");

                    b.Property<string>("Title")
                        .IsRequired()
                        .ValueGeneratedOnAdd()
                        .HasMaxLength(2048)
                        .HasColumnType("nvarchar(2048)")
                        .HasDefaultValueSql("N'Unknown App'");

                    b.Property<DateTime>("UtcDateTimeLastModified")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("datetime2")
                        .HasDefaultValueSql("CONVERT(datetime2(7), N'2000-01-01T00:00:00+00:00')");

                    b.Property<DateTime>("UtcDateTimeRecorded")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("datetime2")
                        .HasDefaultValueSql("SYSUTCDATETIME()");

                    b.HasKey("Id");

                    b.HasIndex("IsActive");

                    b.HasIndex("UtcDateTimeLastModified");

                    b.ToTable("Apps");
                });

            modelBuilder.Entity("SteamScrapper.Infrastructure.Database.Entities.Bundle", b =>
                {
                    b.Property<long>("Id")
                        .HasColumnType("bigint");

                    b.Property<string>("BannerUrl")
                        .HasMaxLength(2048)
                        .HasColumnType("nvarchar(2048)");

                    b.Property<bool>("IsActive")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("bit")
                        .HasDefaultValueSql("0");

                    b.Property<string>("Title")
                        .IsRequired()
                        .ValueGeneratedOnAdd()
                        .HasMaxLength(2048)
                        .HasColumnType("nvarchar(2048)")
                        .HasDefaultValueSql("N'Unknown Bundle'");

                    b.Property<DateTime>("UtcDateTimeLastModified")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("datetime2")
                        .HasDefaultValueSql("CONVERT(datetime2(7), N'2000-01-01T00:00:00+00:00')");

                    b.Property<DateTime>("UtcDateTimeRecorded")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("datetime2")
                        .HasDefaultValueSql("SYSUTCDATETIME()");

                    b.HasKey("Id");

                    b.HasIndex("IsActive");

                    b.HasIndex("UtcDateTimeLastModified");

                    b.ToTable("Bundles");
                });

            modelBuilder.Entity("SteamScrapper.Infrastructure.Database.Entities.Sub", b =>
                {
                    b.Property<long>("Id")
                        .HasColumnType("bigint");

                    b.Property<bool>("IsActive")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("bit")
                        .HasDefaultValueSql("0");

                    b.Property<string>("Title")
                        .IsRequired()
                        .ValueGeneratedOnAdd()
                        .HasMaxLength(2048)
                        .HasColumnType("nvarchar(2048)")
                        .HasDefaultValueSql("N'Unknown Sub'");

                    b.Property<DateTime>("UtcDateTimeLastModified")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("datetime2")
                        .HasDefaultValueSql("CONVERT(datetime2(7), N'2000-01-01T00:00:00+00:00')");

                    b.Property<DateTime>("UtcDateTimeRecorded")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("datetime2")
                        .HasDefaultValueSql("SYSUTCDATETIME()");

                    b.HasKey("Id");

                    b.HasIndex("IsActive");

                    b.HasIndex("UtcDateTimeLastModified");

                    b.ToTable("Subs");
                });
#pragma warning restore 612, 618
        }
    }
}
