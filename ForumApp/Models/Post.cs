using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ForumApp.Models
{
    public class Post
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(200)]
        public string Title { get; set; }

        public string Content { get; set; }

        public string FilePath { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // Khóa ngoại tham chiếu đến Category
        public int CategoryId { get; set; }

        [ForeignKey("CategoryId")]
        public Category Category { get; set; }
    }
}
