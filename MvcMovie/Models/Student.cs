using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MvcMovie.Models
{
    [Table("Students")]
    public class Student
    {
        [Key]
        [Display(Name = "Mã Sinh Viên")]
        public string? StudentId { get; set; }

        [Required(ErrorMessage = "Không để trống họ tên")]
        [Display(Name = "Họ và Tên")]
        public string? FullName { get; set; }

        [Display(Name = "Địa chỉ")]
        public string? Address { get; set; }

        // --- Thuộc tính trường học của bạn vẫn giữ nguyên ở đây ---
        [Display(Name = "Trường Đại học")]
        public string? University { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn năm nhập học")]
        [Display(Name = "Năm nhập học")]
        public string? NamNhapHoc { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn hệ đào tạo")]
        [Display(Name = "Hệ đào tạo")]
        public string? HeDaoTao { get; set; }

        // ==========================================
        // KHÓA NGOẠI LIÊN KẾT TỚI BẢNG KHOA (FACULTY)
        // ==========================================
        [Required(ErrorMessage = "Vui lòng chọn khoa")]
        [Display(Name = "Khoa")]
        public string? FacultyId { get; set; } // Lưu mã khoa (ví dụ: "05")

        [ForeignKey("FacultyId")]
        public Faculty? Faculty { get; set; } // Đối tượng liên kết tới bảng Faculty
    }
}