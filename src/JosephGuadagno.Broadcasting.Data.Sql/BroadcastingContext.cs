using JosephGuadagno.Broadcasting.Data.Sql.Models;
using JosephGuadagno.Broadcasting.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace JosephGuadagno.Broadcasting.Data.Sql;

public partial class BroadcastingContext : DbContext
{
    private readonly ISettings _settings;
    public BroadcastingContext(ISettings settings)
    {
        _settings = settings;
    }

    public virtual DbSet<Engagement> Engagements { get; set; } = null!;
    public virtual DbSet<ScheduledItem> ScheduledItems { get; set; } = null!;
    public virtual DbSet<Talk> Talks { get; set; } = null!;

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder) =>
        optionsBuilder.UseSqlServer(_settings.JJGNetDatabaseSqlServer);

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Engagement>(entity =>
        {
            entity.HasKey(e => e.Id)
                .HasName("Engagements_pk")
                .IsClustered(false);
        });

        modelBuilder.Entity<ScheduledItem>(entity =>
        {
            entity.HasKey(e => e.Id)
                .HasName("ScheduledItems_pk")
                .IsClustered(false);

            entity.HasIndex(e => e.MessageSentOn, "ScheduledItems_MessageSentOn_index");

            entity.Property(e => e.ItemPrimaryKey)
                .HasMaxLength(255)
                .IsUnicode(false);

            entity.Property(e => e.ItemSecondaryKey)
                .HasMaxLength(255)
                .IsUnicode(false);

            entity.Property(e => e.ItemTableName)
                .HasMaxLength(255)
                .IsUnicode(false);
        });

        modelBuilder.Entity<Talk>(entity =>
        {
            entity.HasKey(e => e.Id)
                .HasName("Talks_pk")
                .IsClustered(false);

            entity.HasOne(d => d.Engagement)
                .WithMany(p => p.Talks)
                .HasForeignKey(d => d.EngagementId)
                .HasConstraintName("Talks_Engagements_Id");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}