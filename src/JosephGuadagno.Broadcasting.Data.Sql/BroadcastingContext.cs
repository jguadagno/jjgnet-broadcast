using JosephGuadagno.Broadcasting.Data.Sql.Models;
using Microsoft.EntityFrameworkCore;

namespace JosephGuadagno.Broadcasting.Data.Sql;

public partial class BroadcastingContext : DbContext
{

    public BroadcastingContext(DbContextOptions<BroadcastingContext> options) : base(options)
    {
    }

    public virtual DbSet<Engagement> Engagements { get; set; } = null!;
    public virtual DbSet<ScheduledItem> ScheduledItems { get; set; } = null!;
    public virtual DbSet<Talk> Talks { get; set; } = null!;
    public virtual DbSet<FeedCheck> FeedChecks { get; set; } = null!;
    public virtual DbSet<TokenRefresh> TokenRefreshes { get; set; } = null!;
    public virtual DbSet<SyndicationFeedSource> SyndicationFeedSources { get; set; } = null!;
    public virtual DbSet<YouTubeSource> YouTubeSources { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Engagement>(entity =>
        {
            entity.HasKey(e => e.Id)
                .HasName("Engagements_pk")
                .IsClustered(false);

            entity.Property(e => e.CreatedOn)
                .HasDefaultValueSql("(getdate())");

            entity.Property(e => e.LastUpdatedOn)
                .HasDefaultValueSql("(getdate())");
        });

        modelBuilder.Entity<ScheduledItem>(entity =>
        {
            entity.HasKey(e => e.Id)
                .HasName("ScheduledItems_pk")
                .IsClustered(false);

            entity.HasIndex(e => e.MessageSentOn, "ScheduledItems_MessageSentOn_index");

            entity.Property(e => e.ItemPrimaryKey);

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

        modelBuilder.Entity<FeedCheck>(entity =>
        {
            entity.HasKey(e => e.Id)
                .HasName("FeedCheck_pk_Id");

            entity.HasIndex(e => e.Name, "FeedCheck_Unique_Name")
                .IsUnique();

            entity.Property(e => e.Name)
                .HasMaxLength(255)
                .IsRequired();

            entity.Property(e => e.LastCheckedFeed)
                .HasDefaultValueSql("(getutcdate())");

            entity.Property(e => e.LastItemAddedOrUpdated)
                .HasDefaultValueSql("(getutcdate())");

            entity.Property(e => e.LastUpdatedOn)
                .HasDefaultValueSql("(getutcdate())");
        });

        modelBuilder.Entity<TokenRefresh>(entity =>
        {
            entity.HasKey(e => e.Id)
                .HasName("TokenRefresh_pk_Id");

            entity.HasIndex(e => e.Name, "ToeknRefresh_Unique_Name")
                .IsUnique();

            entity.Property(e => e.Name)
                .HasMaxLength(255)
                .IsRequired();

            entity.Property(e => e.Expires)
                .HasDefaultValueSql("(getutcdate())");

            entity.Property(e => e.LastChecked)
                .HasDefaultValueSql("(getutcdate())");

            entity.Property(e => e.LastRefreshed)
                .HasDefaultValueSql("(getutcdate())");

            entity.Property(e => e.LastUpdatedOn)
                .HasDefaultValueSql("(getutcdate())");
        });

        modelBuilder.Entity<SyndicationFeedSource>(entity =>
        {
            entity.HasKey(e => e.Id)
                .HasName("SyndicationFeedSource_pk_Id");

            entity.Property(e => e.FeedIdentifier)
                .IsRequired();

            entity.Property(e => e.Author)
                .HasMaxLength(255)
                .IsRequired();

            entity.Property(e => e.Title)
                .HasMaxLength(512)
                .IsRequired();

            entity.Property(e => e.ShortenedUrl)
                .HasMaxLength(255);

            entity.Property(e => e.Url)
                .IsRequired();

            entity.Property(e => e.PublicationDate)
                .HasDefaultValueSql("(getutcdate())");

            entity.Property(e => e.AddedOn)
                .HasDefaultValueSql("(getutcdate())");

            entity.Property(e => e.ItemLastUpdatedOn)
                .HasDefaultValueSql("(getutcdate())");

            entity.Property(e => e.LastUpdatedOn)
                .HasDefaultValueSql("(getutcdate())");
        });

        modelBuilder.Entity<YouTubeSource>(entity =>
        {
            entity.HasKey(e => e.Id)
                .HasName("YouTubeSource_pk_Id");

            entity.Property(e => e.VideoId)
                .HasMaxLength(20)
                .IsRequired();

            entity.Property(e => e.Author)
                .HasMaxLength(255)
                .IsRequired();

            entity.Property(e => e.Title)
                .HasMaxLength(512)
                .IsRequired();

            entity.Property(e => e.ShortenedUrl)
                .HasMaxLength(255);

            entity.Property(e => e.Url)
                .IsRequired();

            entity.Property(e => e.PublicationDate)
                .HasDefaultValueSql("(getutcdate())");

            entity.Property(e => e.AddedOn)
                .HasDefaultValueSql("(getutcdate())");

            entity.Property(e => e.ItemLastUpdatedOn)
                .HasDefaultValueSql("(getutcdate())");

            entity.Property(e => e.LastUpdatedOn)
                .HasDefaultValueSql("(getutcdate())");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}