using Microsoft.EntityFrameworkCore;
using Pm.Api.Domain.Entities;

namespace Pm.Api.Data;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();

    public DbSet<Board> Boards => Set<Board>();

    public DbSet<BoardColumn> Columns => Set<BoardColumn>();

    public DbSet<CardItem> Cards => Set<CardItem>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>(entity =>
        {
            entity.ToTable("users");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Username).HasMaxLength(64).IsRequired();
            entity.HasIndex(x => x.Username).IsUnique();

            entity.HasOne(x => x.Board)
                .WithOne(x => x.User)
                .HasForeignKey<Board>(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Board>(entity =>
        {
            entity.ToTable("boards");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Name).HasMaxLength(128).IsRequired();
            entity.HasIndex(x => x.UserId).IsUnique();
        });

        modelBuilder.Entity<BoardColumn>(entity =>
        {
            entity.ToTable("columns");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Name).HasMaxLength(64).IsRequired();
            entity.HasIndex(x => new { x.BoardId, x.Position }).IsUnique();

            entity.HasOne(x => x.Board)
                .WithMany(x => x.Columns)
                .HasForeignKey(x => x.BoardId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<CardItem>(entity =>
        {
            entity.ToTable("cards");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Title).HasMaxLength(256).IsRequired();
            entity.Property(x => x.Description).HasMaxLength(4000).IsRequired();
            entity.HasIndex(x => new { x.ColumnId, x.Position }).IsUnique();

            entity.HasOne(x => x.Board)
                .WithMany(x => x.Cards)
                .HasForeignKey(x => x.BoardId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(x => x.Column)
                .WithMany(x => x.Cards)
                .HasForeignKey(x => x.ColumnId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
