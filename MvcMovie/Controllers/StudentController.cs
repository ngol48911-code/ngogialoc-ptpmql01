using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using MvcMovie.Data;
using MvcMovie.Models;

namespace MvcMovie.Controllers
{
    public class StudentController : Controller
    {
        private readonly ApplicationDbContext _context;

        public StudentController(ApplicationDbContext context)
        {
            _context = context;
        }

        // ==========================================
        // HÀM SINH MÃ SINH VIÊN TỰ ĐỘNG
        // Mã: [Năm: 2 số][Hệ: 2 số][Ngành: 2 số][STT: 4 số]
        // Ví dụ: 28 + 21 + 05 + 0199 -> 2821050199
        // ==========================================
        private string GenerateStudentId(string namNhapHoc, string heDaoTao, string facultyId)
        {
            // 1. Lấy 2 số cuối của năm: "2028" -> "28"
            string nam = namNhapHoc.Length >= 4 
                ? namNhapHoc.Substring(namNhapHoc.Length - 2, 2) 
                : namNhapHoc.PadLeft(2, '0');

            // 2. Hệ đào tạo 2 chữ số: "21"
            string he = heDaoTao.PadLeft(2, '0');

            // 3. Mã ngành/khoa 2 chữ số: "05"
            string nganh = facultyId.PadLeft(2, '0');

            // 4. Ghép tiền tố prefix: "282105"
            string prefix = $"{nam}{he}{nganh}";

            // 5. Tìm mã SV lớn nhất có cùng tiền tố trong CSDL
            var lastStudent = _context.Student
                .Where(s => s.StudentId != null && s.StudentId.StartsWith(prefix))
                .OrderByDescending(s => s.StudentId)
                .FirstOrDefault();

            // 6. Tính số thứ tự kế tiếp
            int nextNumber = 1; // Mặc định từ 0001
            if (lastStudent != null && lastStudent.StudentId!.Length >= 10)
            {
                // Cắt 4 số cuối: "2821050199" -> "0199"
                string last4Digits = lastStudent.StudentId.Substring(6, 4);
                if (int.TryParse(last4Digits, out int currentNumber))
                {
                    nextNumber = currentNumber + 1; // 199 + 1 = 200
                }
            }

            // 7. Trả về mã đầy đủ: "282105" + "0200" = "2821050200"
            return $"{prefix}{nextNumber:D4}";
        }

        // ==========================================
        // HÀM CHUẨN BỊ DROPDOWN CHO GIAO DIỆN
        // ==========================================
        private void PrepareDropdowns(Student? student = null)
        {
            // Dropdown Năm nhập học
            var years = Enumerable.Range(2020, 11)
                .Select(y => new SelectListItem
                {
                    Value = y.ToString(),
                    Text = y.ToString(),
                    Selected = student?.NamNhapHoc == y.ToString()
                });
            ViewBag.NamNhapHoc = years;

            // Dropdown Hệ đào tạo
            var heDaoTaoList = new List<SelectListItem>
            {
                new SelectListItem { Value = "21", Text = "Đại học chính quy" },
                new SelectListItem { Value = "22", Text = "Cao đẳng" },
                new SelectListItem { Value = "23", Text = "Liên thông" },
                new SelectListItem { Value = "24", Text = "Vừa làm vừa học" }
            };
            if (student?.HeDaoTao != null)
            {
                foreach (var item in heDaoTaoList)
                    item.Selected = item.Value == student.HeDaoTao;
            }
            ViewBag.HeDaoTao = heDaoTaoList;

            // Dropdown Khoa (lấy từ bảng Faculties trong CSDL)
            ViewBag.FacultyId = new SelectList(
                _context.Faculty.OrderBy(f => f.FacultyId),
                "FacultyId",
                "FacultyName",
                student?.FacultyId
            );
        }

        // ==========================================
        // INDEX - Danh sách sinh viên
        // ==========================================
        public IActionResult Index()
        {
            PrepareDropdowns();
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> GetStudents(
            string? searchString = null,
            string? facultyId = null,
            int pageIndex = 1,
            int pageSize = 10)
        {
            var query = _context.Student
                .AsNoTracking()
                .Include(s => s.Faculty)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(searchString))
            {
                var pattern = $"%{searchString.Trim()}%";
                query = query.Where(s =>
                    EF.Functions.Like(s.StudentId!, pattern) ||
                    EF.Functions.Like(s.FullName!, pattern) ||
                    EF.Functions.Like(s.Address!, pattern));
            }

            if (!string.IsNullOrWhiteSpace(facultyId))
            {
                query = query.Where(s => s.FacultyId == facultyId);
            }

            var students = query
                .OrderBy(s => s.StudentId)
                .Select(s => new StudentResponseDto(
                    s.StudentId,
                    s.FullName,
                    s.Address,
                    s.University,
                    s.NamNhapHoc,
                    s.HeDaoTao,
                    s.FacultyId,
                    s.Faculty!.FacultyName));

            return Ok(await PagedResult<StudentResponseDto>.CreateAsync(
                students, pageIndex, pageSize));
        }

        [HttpGet]
        public async Task<IActionResult> GetStudentById(string id)
        {
            var student = await _context.Student
                .Include(s => s.Faculty)
                .FirstOrDefaultAsync(s => s.StudentId == id);

            return student == null
                ? NotFound(new { message = "Không tìm thấy sinh viên." })
                : Ok(new StudentResponseDto(
                    student.StudentId,
                    student.FullName,
                    student.Address,
                    student.University,
                    student.NamNhapHoc,
                    student.HeDaoTao,
                    student.FacultyId,
                    student.Faculty?.FacultyName));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> CreateAjax([FromBody] Student? student)
        {
            if (student == null)
            {
                return BadRequest(new { message = "Dữ liệu sinh viên không hợp lệ." });
            }

            ValidateStudent(student);
            if (!await _context.Faculty.AnyAsync(f => f.FacultyId == student.FacultyId))
            {
                ModelState.AddModelError(nameof(Student.FacultyId), "Khoa được chọn không tồn tại.");
            }

            if (!ModelState.IsValid)
            {
                return BadRequest(new
                {
                    message = "Vui lòng kiểm tra lại dữ liệu.",
                    errors = GetValidationErrors()
                });
            }

            student.StudentId = GenerateStudentId(
                student.NamNhapHoc!,
                student.HeDaoTao!,
                student.FacultyId!);

            _context.Student.Add(student);
            await _context.SaveChangesAsync();

            var response = await _context.Student
                .Include(s => s.Faculty)
                .Where(s => s.StudentId == student.StudentId)
                .Select(s => new StudentResponseDto(
                    s.StudentId,
                    s.FullName,
                    s.Address,
                    s.University,
                    s.NamNhapHoc,
                    s.HeDaoTao,
                    s.FacultyId,
                    s.Faculty!.FacultyName))
                .SingleAsync();

            return Ok(response);
        }

        [HttpPut]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> EditAjax(string id, [FromBody] Student? student)
        {
            if (student == null)
            {
                return BadRequest(new { message = "Dữ liệu sinh viên không hợp lệ." });
            }

            var existingStudent = await _context.Student.FindAsync(id);
            if (existingStudent == null)
            {
                return NotFound(new { message = "Không tìm thấy sinh viên." });
            }

            student.StudentId = id;
            ValidateStudent(student);
            if (!await _context.Faculty.AnyAsync(f => f.FacultyId == student.FacultyId))
            {
                ModelState.AddModelError(nameof(Student.FacultyId), "Khoa được chọn không tồn tại.");
            }

            if (!ModelState.IsValid)
            {
                return BadRequest(new
                {
                    message = "Vui lòng kiểm tra lại dữ liệu.",
                    errors = GetValidationErrors()
                });
            }

            existingStudent.FullName = student.FullName;
            existingStudent.Address = student.Address;
            existingStudent.University = student.University;
            existingStudent.NamNhapHoc = student.NamNhapHoc;
            existingStudent.HeDaoTao = student.HeDaoTao;
            existingStudent.FacultyId = student.FacultyId;
            await _context.SaveChangesAsync();

            var response = await _context.Student
                .Include(s => s.Faculty)
                .Where(s => s.StudentId == id)
                .Select(s => new StudentResponseDto(
                    s.StudentId,
                    s.FullName,
                    s.Address,
                    s.University,
                    s.NamNhapHoc,
                    s.HeDaoTao,
                    s.FacultyId,
                    s.Faculty!.FacultyName))
                .SingleAsync();

            return Ok(response);
        }

        [HttpDelete]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteAjax(string id)
        {
            var student = await _context.Student.FindAsync(id);
            if (student == null)
            {
                return NotFound(new { message = "Không tìm thấy sinh viên." });
            }

            _context.Student.Remove(student);
            await _context.SaveChangesAsync();
            return Ok(new { message = "Đã xóa sinh viên." });
        }

        private void ValidateStudent(Student student)
        {
            var validationResults = new List<ValidationResult>();
            var validationContext = new ValidationContext(student);
            Validator.TryValidateObject(student, validationContext, validationResults, validateAllProperties: true);

            foreach (var validationResult in validationResults)
            {
                var memberName = validationResult.MemberNames.FirstOrDefault();
                ModelState.AddModelError(memberName ?? string.Empty, validationResult.ErrorMessage ?? "Dữ liệu không hợp lệ.");
            }
        }

        private Dictionary<string, string[]> GetValidationErrors()
        {
            return ModelState
                .Where(entry => entry.Value?.Errors.Count > 0)
                .ToDictionary(
                    entry => entry.Key,
                    entry => entry.Value!.Errors
                        .Select(error => error.ErrorMessage)
                        .Where(message => !string.IsNullOrWhiteSpace(message))
                        .ToArray());
        }

        // ==========================================
        // CREATE - Thêm mới sinh viên
        // ==========================================
        [Authorize(Roles = "Admin")]
        public IActionResult Create()
        {
            PrepareDropdowns();
            if (!_context.Faculty.Any())
            {
                ModelState.AddModelError("FacultyId", "Chưa có dữ liệu khoa. Hãy thêm ít nhất một khoa trước khi tạo sinh viên.");
            }
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Create([Bind("FullName,Address,University,NamNhapHoc,HeDaoTao,FacultyId")] Student student)
        {
            if (!await _context.Faculty.AnyAsync())
            {
                ModelState.AddModelError(nameof(Student.FacultyId),
                    "Chưa có dữ liệu khoa. Hãy thêm ít nhất một khoa trước khi tạo sinh viên.");
            }

            if (ModelState.IsValid)
            {
                // Tự động sinh mã sinh viên
                student.StudentId = GenerateStudentId(
                    student.NamNhapHoc!,
                    student.HeDaoTao!,
                    student.FacultyId!
                );

                _context.Add(student);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            PrepareDropdowns(student);
            return View(student);
        }

        // ==========================================
        // EDIT - Sửa sinh viên
        // ==========================================
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(string id)
        {
            if (id == null || _context.Student == null)
            {
                return NotFound();
            }

            var student = await _context.Student.FindAsync(id);
            if (student == null)
            {
                return NotFound();
            }
            PrepareDropdowns(student);
            return View(student);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(string id, [Bind("StudentId,FullName,Address,University,NamNhapHoc,HeDaoTao,FacultyId")] Student student)
        {
            if (id != student.StudentId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(student);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!StudentExists(student.StudentId))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            PrepareDropdowns(student);
            return View("Update", student);
        }

        // ==========================================
        // DELETE - Xóa sinh viên
        // ==========================================
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(string id)
        {
            if (id == null || _context.Student == null)
            {
                return NotFound();
            }

            var student = await _context.Student
                .Include(s => s.Faculty)
                .FirstOrDefaultAsync(m => m.StudentId == id);
            if (student == null)
            {
                return NotFound();
            }
            return View(student);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteConfirmed(string id)
        {
            if (_context.Student == null)
            {
                return Problem("Entity set 'ApplicationDbContext.Student' is null.");
            }

            var student = await _context.Student.FindAsync(id);
            if (student != null)
            {
                _context.Student.Remove(student);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool StudentExists(string? id)
        {
            return (_context.Student?.Any(e => e.StudentId == id)).GetValueOrDefault();
        }
    }
}