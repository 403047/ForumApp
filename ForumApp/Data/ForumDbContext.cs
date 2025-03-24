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
        public DbSet<PostPrice> PostPrices { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Cấu hình bảng User
            modelBuilder.Entity<User>(entity =>
            {
                entity.HasKey(u => u.Id);
                entity.Property(u => u.Username).IsRequired().HasMaxLength(100);
                entity.Property(u => u.Email).IsRequired().HasMaxLength(150);
                entity.Property(u => u.Password).IsRequired();
            });

            // Cấu hình bảng Category
            modelBuilder.Entity<Category>(entity =>
            {
                entity.HasKey(c => c.Id);
                entity.Property(c => c.Name).IsRequired().HasMaxLength(100);
            });

            // Cấu hình bảng Post
            modelBuilder.Entity<Post>(entity =>
            {
                entity.HasKey(p => p.Id);
                entity.Property(p => p.Title).IsRequired().HasMaxLength(200);
                // Đã xóa thuộc tính Content trong model nên xóa cấu hình này:
                // entity.Property(p => p.Content).IsRequired(false);
                entity.Property(p => p.FilePath).IsRequired(false);
                entity.Property(p => p.CreatedAt).HasDefaultValueSql("GETDATE()");
                entity.Property(p => p.Type).IsRequired();
                entity.Property(p => p.Status).HasDefaultValue("Chờ duyệt");

                // Không cần cấu hình TotalRating vì nó được đánh dấu [NotMapped] trong model

                entity.HasOne(p => p.Category)
                      .WithMany(c => c.Posts)
                      .HasForeignKey(p => p.CategoryId)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(p => p.User)
                      .WithMany()
                      .HasForeignKey(p => p.UserId)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            // Cấu hình bảng Rating
            modelBuilder.Entity<Rating>(entity =>
            {
                entity.HasKey(r => r.Id);
                entity.Property(r => r.Stars).IsRequired();

                entity.HasOne(r => r.Post)
                      .WithMany(p => p.Ratings)
                      .HasForeignKey(r => r.PostId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(r => r.User)
                      .WithMany()
                      .HasForeignKey(r => r.UserId)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            // Cấu hình bảng PostPrice
            modelBuilder.Entity<PostPrice>(entity =>
            {
                entity.HasKey(pp => pp.Id);
                entity.Property(pp => pp.Price).HasColumnType("decimal(18,2)").IsRequired();
                entity.Property(pp => pp.UpdatedAt).HasDefaultValueSql("GETDATE()");

                entity.HasOne(pp => pp.Post)
                      .WithMany(p => p.PostPrices)
                      .HasForeignKey(pp => pp.PostId)
                      .OnDelete(DeleteBehavior.Cascade);
            });
        }
    }
}
