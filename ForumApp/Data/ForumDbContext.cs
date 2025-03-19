using Microsoft.EntityFrameworkCore;
using ForumApp.Models;

namespace ForumApp.Data
{
    public class ForumDbContext : DbContext
    {
        public ForumDbContext(DbContextOptions<ForumDbContext> options) : base(options) { }

        public DbSet<User> Users { get; set; }
        public DbSet<Post> Posts { get; set; }
        public DbSet<Category> Categories { get; set; }
        public DbSet<Role> Roles { get; set; }
        public DbSet<Rating> Ratings { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<User>(entity =>
            {
                entity.HasKey(u => u.Id);
                entity.Property(u => u.Username).IsRequired().HasMaxLength(100);
                entity.Property(u => u.Email).IsRequired().HasMaxLength(150);
                entity.Property(u => u.Password).IsRequired();
            });

            modelBuilder.Entity<Category>(entity =>
            {
                entity.HasKey(c => c.Id);
                entity.Property(c => c.Name).IsRequired().HasMaxLength(100);
            });

            modelBuilder.Entity<Post>(entity =>
            {
                entity.HasKey(p => p.Id);
                entity.Property(p => p.Title).IsRequired().HasMaxLength(200);
                entity.Property(p => p.Content).IsRequired(false);
                entity.Property(p => p.FilePath).IsRequired(false);
                entity.Property(p => p.CreatedAt).HasDefaultValueSql("GETDATE()");
                entity.Property(p => p.Type).IsRequired();
                entity.Property(p => p.Status).HasDefaultValue("Chờ duyệt");
                entity.Property(p => p.TotalRating).HasDefaultValue(null);

                entity.HasOne(p => p.Category)
                      .WithMany(c => c.Posts)
                      .HasForeignKey(p => p.CategoryId)
                      .OnDelete(DeleteBehavior.Restrict); // Không xóa danh mục khi có bài viết

                entity.HasOne(p => p.User)
                      .WithMany()
                      .HasForeignKey(p => p.UserId)
                      .OnDelete(DeleteBehavior.Restrict); // Không xóa User khi xóa bài viết
            });

            modelBuilder.Entity<Rating>(entity =>
            {
                entity.HasKey(r => r.Id);
                entity.Property(r => r.Stars).IsRequired();

                entity.HasOne(r => r.Post)
                      .WithMany()
                      .HasForeignKey(r => r.PostId)
                      .OnDelete(DeleteBehavior.Cascade); // Xóa bài viết thì xóa luôn đánh giá

                entity.HasOne(r => r.User)
                      .WithMany()
                      .HasForeignKey(r => r.UserId)
                      .OnDelete(DeleteBehavior.Restrict); // Không xóa User khi xóa đánh giá
            });
        }
    }
}
