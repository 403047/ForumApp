// Models/ChiTietHoaDon.cs
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ForumApp.Models
{
    public class ChiTietHoaDon
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [ForeignKey("HoaDon")]
        public int IdHoaDon { get; set; }

        [Required]
        [ForeignKey("Post")]
        public int IdPost { get; set; }

        [Required]
        [ForeignKey("User")]
        public int IdUser { get; set; }

        [Required]
        [Display(Name = "Giá")]
        public decimal Gia { get; set; }

        [Display(Name = "Minh chứng")]
        public string MinhChung { get; set; } // Đường dẫn ảnh minh chứng

        [Display(Name = "QR chủ bài")]
        public string QRImagePath { get; set; } // Đường dẫn ảnh QR của chủ bài đăng

        // Navigation
        public HoaDon HoaDon { get; set; }
        public Post Post { get; set; }
        public User User { get; set; }
    }
}
