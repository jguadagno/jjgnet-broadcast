using JosephGuadagno.Broadcasting.Data.Sql.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace JosephGuadagno.Broadcasting.Data.Sql;

// TODO: Look into fixing the nullability of this class

public partial class BroadcastingContext : DbContext
{
    private readonly IConfiguration _configuration;
        
    public BroadcastingContext(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public BroadcastingContext(DbContextOptions<BroadcastingContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Engagement> Engagements { get; set; }
    public virtual DbSet<ScheduledItem> ScheduledItems { get; set; }
    public virtual DbSet<Talk> Talks { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder) =>
        optionsBuilder.UseSqlServer(_configuration.GetConnectionString("JJGNetDatabaseSqlServer"));

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasAnnotation("Relational:Collation", "SQL_Latin1_General_CP1_CI_AS");

        modelBuilder.Entity<Engagement>(entity =>
        {
            entity.HasKey(e => e.Id)
                .HasName("Engagements_pk")
                .IsClustered(false);

            entity.Property(e => e.Name).IsRequired();
        });

        modelBuilder.Entity<ScheduledItem>(entity =>
        {
            entity.HasKey(e => e.Id)
                .HasName("ScheduledItems_pk")
                .IsClustered(false);

            entity.Property(e => e.ItemPrimaryKey)
                .IsRequired()
                .HasMaxLength(255)
                .IsUnicode(false);

            entity.Property(e => e.ItemTable)
                .IsRequired()
                .HasMaxLength(255)
                .IsUnicode(false);
        });

        modelBuilder.Entity<Talk>(entity =>
        {
            entity.HasKey(e => e.Id)
                .HasName("Talks_pk")
                .IsClustered(false);

            entity.Property(e => e.Name).IsRequired();

            entity.HasOne(d => d.Engagement)
                .WithMany(p => p.Talks)
                .HasForeignKey(d => d.EngagementId)
                .HasConstraintName("Talks_Engagements_Id");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}