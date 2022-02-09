using JosephGuadagno.Broadcasting.Data.Sql.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace JosephGuadagno.Broadcasting.Data.Sql;

public partial class BroadcastingContext : DbContext
{
    private readonly IConfiguration _configuration;
    public BroadcastingContext(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public virtual DbSet<Engagement> Engagements { get; set; } = null!;
    public virtual DbSet<ScheduledItem> ScheduledItems { get; set; } = null!;
    public virtual DbSet<Talk> Talks { get; set; } = null!;

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder) =>
        optionsBuilder.UseSqlServer(_configuration.GetConnectionString("JJGNetDatabaseSqlServer"));

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

            entity.Property(e => e.ItemTable)
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