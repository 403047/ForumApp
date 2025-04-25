using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace ForumApp.Models
{
    public class HoaDon
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string Status { get; set; }

        public int AuthenticationTimes { get; set; } = 0;

        // Ngày gửi thanh toán
        [Required]
        public DateTime DateSend { get; set; } = DateTime.Now;

        // Navigation 1-1
        public ChiTietHoaDon ChiTietHoaDon { get; set; }
    }
}
