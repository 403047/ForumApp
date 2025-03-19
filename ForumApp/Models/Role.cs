using System.ComponentModel.DataAnnotations;

namespace ForumApp.Models
{
    public class Role
    {
        [Key]
        public int IdRole { get; set; }

        [Required, StringLength(50)]
        public string RoleName { get; set; }
    }
}
