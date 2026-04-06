using Microsoft.EntityFrameworkCore;
using SmartContentRecommender.Domain.Entities;

namespace SmartContentRecommender.Infrastructure.Persistence;

/// <summary>
/// Контекст базы данных EF Core. Здесь описываем таблицы и связи (Fluent API).
/// </summary>
public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Tag> Tags => Set<Tag>();
    public DbSet<Domain.Entities.Content> Contents => Set<Domain.Entities.Content>();
    public DbSet<ContentTag> ContentTags => Set<ContentTag>();
    public DbSet<UserAction> UserActions => Set<UserAction>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // --- users ---
        modelBuilder.Entity<User>(entity =>
        {
            entity.ToTable("users");
            entity.HasKey(u => u.Id);

            entity.Property(u => u.Email).HasMaxLength(256).IsRequired();
            entity.Property(u => u.PasswordHash).HasMaxLength(512).IsRequired();

            // Enum храним как число (0 = User, 1 = Admin)
            entity.Property(u => u.Role).HasConversion<int>();

            entity.HasIndex(u => u.Email).IsUnique();
        });

        // --- categories ---
        modelBuilder.Entity<Category>(entity =>
        {
            entity.ToTable("categories");
            entity.HasKey(c => c.Id);

            entity.Property(c => c.Name).HasMaxLength(200).IsRequired();
            entity.HasIndex(c => c.Name).IsUnique();
        });

        // --- tags ---
        modelBuilder.Entity<Tag>(entity =>
        {
            entity.ToTable("tags");
            entity.HasKey(t => t.Id);

            entity.Property(t => t.Name).HasMaxLength(100).IsRequired();
            entity.HasIndex(t => t.Name).IsUnique();
        });

        // --- contents ---
        modelBuilder.Entity<Domain.Entities.Content>(entity =>
        {
            entity.ToTable("contents");
            entity.HasKey(c => c.Id);

            entity.Property(c => c.Title).HasMaxLength(500).IsRequired();
            entity.Property(c => c.Description).HasMaxLength(4000);
            entity.Property(c => c.Url).HasMaxLength(2000).IsRequired();

            entity.HasOne(c => c.Category)
                .WithMany(cat => cat.Contents)
                .HasForeignKey(c => c.CategoryId)
                // Нельзя удалить категорию, пока есть контент (проще и безопаснее для данных)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // --- content_tags (связка многие-ко-многим) ---
        modelBuilder.Entity<ContentTag>(entity =>
        {
            entity.ToTable("content_tags");
            entity.HasKey(ct => new { ct.ContentId, ct.TagId });

            entity.HasOne(ct => ct.Content)
                .WithMany(c => c.ContentTags)
                .HasForeignKey(ct => ct.ContentId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(ct => ct.Tag)
                .WithMany(t => t.ContentTags)
                .HasForeignKey(ct => ct.TagId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // --- user_actions (история поведения: просмотры, лайки, клики) ---
        modelBuilder.Entity<UserAction>(entity =>
        {
            entity.ToTable("user_actions");
            entity.HasKey(a => a.Id);

            entity.Property(a => a.Type).HasConversion<int>();

            entity.HasOne(a => a.User)
                .WithMany(u => u.Actions)
                .HasForeignKey(a => a.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(a => a.Content)
                .WithMany(c => c.Actions)
                .HasForeignKey(a => a.ContentId)
                .OnDelete(DeleteBehavior.Cascade);

            // Индексы ускорят отчёты и рекомендации (по пользователю/контенту/типу)
            entity.HasIndex(a => new { a.UserId, a.ContentId, a.Type });
            entity.HasIndex(a => a.CreatedAtUtc);
        });
    }
}
