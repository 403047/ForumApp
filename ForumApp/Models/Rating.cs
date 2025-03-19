using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace ForumApp.Models
{
    public class Rating
    {
        public int Id { get; set; }

        [ForeignKey("Post")]
        public int PostId { get; set; }
        public Post Post { get; set; }

        [ForeignKey("User")]
        public int UserId { get; set; }
        public User User { get; set; }

        [Range(1, 5)]
        public int Stars { get; set; } // Đánh giá từ 1-5 sao
    }
}
