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

        // public string? Content { get; set; } // Đã bỏ

        public string? FilePath { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public int CategoryId { get; set; }
        public Category Category { get; set; }

        [Required]
        public string Type { get; set; } // "Miễn phí" hoặc "Trả phí"

        public int UserId { get; set; }
        public User User { get; set; }

        public string Status { get; set; } = "Chờ duyệt";

        public ICollection<Rating> Ratings { get; set; } = new List<Rating>();
        public ICollection<PostPrice> PostPrices { get; set; } = new List<PostPrice>();

        [NotMapped]
        public decimal TotalRating => Ratings.Any() ? (decimal)Ratings.Average(r => r.Stars) : 0;

        [NotMapped]
        public decimal? CurrentPrice => PostPrices
            .OrderByDescending(p => p.UpdatedAt)
            .FirstOrDefault()?.Price;

        // ============== THUỘC TÍNH THÊM ==============
        // Dùng để hiển thị số trang trong file Word (nếu có).
        [NotMapped]
        public int? TotalPages { get; set; }
    }
}
