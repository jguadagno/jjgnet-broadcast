using JosephGuadagno.Broadcasting.Data.Sql.Models;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace JosephGuadagno.Broadcasting.Data.Sql;

public partial class BroadcastingContext : DbContext
{

    public BroadcastingContext(DbContextOptions<BroadcastingContext> options) : base(options)
    {
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return await base.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (ex.InnerException is SqlException sqlEx)
        {
            if (sqlEx.Number == 1105)
            {
                throw new InvalidOperationException(
                    "Database capacity exceeded. The database has reached its maximum size limit. " +
                    "Contact the administrator to increase the database capacity or archive old data.",
                    ex);
            }
            throw;
        }
    }

    public virtual DbSet<Engagement> Engagements { get; set; } = null!;
    public virtual DbSet<ScheduledItem> ScheduledItems { get; set; } = null!;
    public virtual DbSet<Talk> Talks { get; set; } = null!;
    public virtual DbSet<FeedCheck> FeedChecks { get; set; } = null!;
    public virtual DbSet<TokenRefresh> TokenRefreshes { get; set; } = null!;
    public virtual DbSet<SyndicationFeedSource> SyndicationFeedSources { get; set; } = null!;
    public virtual DbSet<YouTubeSource> YouTubeSources { get; set; } = null!;
    public virtual DbSet<MessageTemplate> MessageTemplates { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Engagement>(entity =>
        {
            entity.HasKey(e => e.Id)
                .HasName("Engagements_pk")
                .IsClustered(false);

            entity.Property(e => e.Name)
                .IsRequired();
            entity.Property(e => e.Url);

            entity.Property(e => e.StartDateTime)
                .IsRequired()
                .HasColumnType("datetimeoffset");

            entity.Property(e => e.EndDateTime)
                .IsRequired()
                .HasColumnType("datetimeoffset");

            entity.Property(e => e.Comments);

            entity.Property(e => e.TimeZoneId)
                .IsRequired()
                .HasMaxLength(50)
                .HasDefaultValueSql("America/Phoenix");

            entity.Property(e => e.CreatedOn)
                .IsRequired()
                .HasDefaultValueSql("(getutcdate())");

            entity.Property(e => e.LastUpdatedOn)
                .IsRequired()
                .HasDefaultValueSql("(getutcdate())");

            entity.Property(e => e.BlueSkyHandle)
                .HasMaxLength(255);

            entity.Property(e => e.ConferenceHashtag)
                .HasMaxLength(255);

            entity.Property(e => e.ConferenceTwitterHandle)
                .HasMaxLength(255);
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

            entity.Property(e => e.SendOnDateTime)
                .HasColumnType("datetimeoffset")
                .IsRequired();

            entity.Property(e => e.Message);

            entity.Property(e => e.MessageSent)
                .HasDefaultValueSql("0");

            entity.Property(e => e.ImageUrl)
                .HasMaxLength(2048);
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
                .HasMaxLength(450)
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

        modelBuilder.Entity<MessageTemplate>(entity =>
        {
            entity.HasKey(e => new { e.Platform, e.MessageType })
                .HasName("PK_MessageTemplates");

            entity.Property(e => e.Platform)
                .HasMaxLength(50)
                .IsRequired();

            entity.Property(e => e.MessageType)
                .HasMaxLength(50)
                .IsRequired();

            entity.Property(e => e.Template)
                .IsRequired();

            entity.Property(e => e.Description)
                .HasMaxLength(500);
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}