using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ForumApp.Models
{
    public class Post
    {
        [Key]
        public int Id { get; set; }

        [Required, StringLength(200)]
        public string Title { get; set; }

        public string? Content { get; set; }

        public string? FilePath { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public int CategoryId { get; set; }
        public Category Category { get; set; }

        [Required]
        public string Type { get; set; } // "Miễn phí" hoặc "Trả phí"

        [Column(TypeName = "decimal(18,2)")]
        public decimal? Price { get; set; }

        public int UserId { get; set; }
        public User User { get; set; }

        public string Status { get; set; } = "Chờ duyệt";

        // Navigation property cho các đánh giá của bài đăng
        public ICollection<Rating> Ratings { get; set; } = new List<Rating>();
        [NotMapped]
        public decimal? TotalRating { get; set; }
    }
}
