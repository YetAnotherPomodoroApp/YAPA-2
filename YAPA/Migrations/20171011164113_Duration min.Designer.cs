using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using YAPA.WPF;

namespace YAPA.Migrations
{
    [DbContext(typeof(DatabaseContext))]
    [Migration("20171011164113_Duration min")]
    partial class Durationmin
    {
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
            modelBuilder
                .HasAnnotation("ProductVersion", "1.1.0-rtm-22752");

            modelBuilder.Entity("YAPA.Shared.Contracts.PomodoroEntity", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd();

                    b.Property<int>("Count");

                    b.Property<DateTime>("DateTime");

                    b.Property<int>("DurationMin");

                    b.HasKey("Id");

                    b.ToTable("Pomodoros");
                });
        }
    }
}
