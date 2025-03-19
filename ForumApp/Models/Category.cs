using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace ForumApp.Models
{
    public class Category
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string Name { get; set; }

        // Danh sách bài viết thuộc danh mục này
        public ICollection<Post> Posts { get; set; } = new List<Post>();
    }
}
