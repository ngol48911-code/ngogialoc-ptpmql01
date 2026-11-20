using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using MvcMovie.Models;

namespace MvcMovie.Data
{
    public class ApplicationDbContext : IdentityDbContext<IdentityUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }

        public DbSet<Person> Person { get; set; }
        public DbSet<Movie> Movie { get; set; }
        public DbSet<Student> Student { get; set; }
        public DbSet<Faculty> Faculty { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Faculty>().HasData(
                new Faculty { FacultyId = "01", FacultyName = "Công nghệ thông tin" },
                new Faculty { FacultyId = "02", FacultyName = "Cơ - Điện" },
                new Faculty { FacultyId = "03", FacultyName = "Dầu khí và Năng lượng" },
                new Faculty { FacultyId = "04", FacultyName = "Giáo dục Quốc phòng" },
                new Faculty { FacultyId = "05", FacultyName = "Khoa học cơ bản" },
                new Faculty { FacultyId = "06", FacultyName = "Khoa học và Kỹ thuật Địa chất" },
                new Faculty { FacultyId = "07", FacultyName = "Kinh tế và Quản trị kinh doanh" },
                new Faculty { FacultyId = "08", FacultyName = "Lý luận chính trị" },
                new Faculty { FacultyId = "09", FacultyName = "Mỏ" },
                new Faculty { FacultyId = "10", FacultyName = "Môi trường" }
            );
        }
    }
}