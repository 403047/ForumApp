using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ForumApp.Models
{
    public class User
    {
        [Key]
        public int Id { get; set; }

        [Required, StringLength(100)]
        public string Username { get; set; }

        [Required, StringLength(150)]
        public string Email { get; set; }

        [Required]
        public string Password { get; set; }

        // Thêm khóa ngoại đến bảng Role
        [ForeignKey("Role")]
        public int IdRole { get; set; }
        public Role Role { get; set; }
    }
}
