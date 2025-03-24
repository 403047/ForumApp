using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ForumApp.Models
{
    public class PostPrice
    {
        [Key]
        public int Id { get; set; }

        [ForeignKey("Post")]
        public int PostId { get; set; }
        public Post Post { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal Price { get; set; }

        public DateTime UpdatedAt { get; set; } = DateTime.Now;
    }
}
