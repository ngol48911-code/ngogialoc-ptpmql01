using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MvcMovie.Models
{
    [Table("Faculties")]
    public class Faculty
    {
        [Key]
        [Display(Name = "Mã Khoa")]
        public string FacultyId { get; set; } = null!; // Ví dụ: "01", "02", "05"...

        [Required(ErrorMessage = "Tên khoa không được để trống")]
        [Display(Name = "Tên Khoa")]
        public string FacultyName { get; set; } = null!; // Ví dụ: "Công nghệ thông tin"

        // Quan hệ 1 - N: 1 Khoa có danh sách nhiều Sinh viên
        public ICollection<Student>? Students { get; set; }
    }
}