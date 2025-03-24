using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;

namespace ForumApp.Models
{
    public class Post
    {
        [Key]
        public int Id { get; set; }

        [Required, StringLength(200)]
        public string Title { get; set; }

        // Loại bỏ thuộc tính Content vì ta sẽ lưu nội dung trong file Word và chỉ lưu đường dẫn (FilePath)
        // public string? Content { get; set; }

        // FilePath lưu đường dẫn đến file Word chứa nội dung bài viết
        public string? FilePath { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public int CategoryId { get; set; }
        public Category Category { get; set; }

        [Required]
        public string Type { get; set; } // "Miễn phí" hoặc "Trả phí"

        public int UserId { get; set; }
        public User User { get; set; }

        public string Status { get; set; } = "Chờ duyệt";

        // Danh sách các đánh giá của bài đăng
        public ICollection<Rating> Ratings { get; set; } = new List<Rating>();

        // Danh sách giá bài viết
        public ICollection<PostPrice> PostPrices { get; set; } = new List<PostPrice>();

        // Tính trung bình rating (read-only, không lưu trong DB)
        [NotMapped]
        public decimal TotalRating => Ratings.Any() ? (decimal)Ratings.Average(r => r.Stars) : 0;

        // Lấy giá mới nhất (read-only, không lưu trong DB)
        [NotMapped]
        public decimal? CurrentPrice => PostPrices.OrderByDescending(p => p.UpdatedAt).FirstOrDefault()?.Price;
    }
}
