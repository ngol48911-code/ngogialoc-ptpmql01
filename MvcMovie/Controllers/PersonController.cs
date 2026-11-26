using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MvcMovie.Data;
using MvcMovie.Models;

namespace MvcMovie.Controllers
{
    [Authorize]
    public class PersonController : Controller
    {
        // public IActionResult Index()
        // {
        //     return View();
        // }
        // [HttpPost]
        // public IActionResult Index(Person ps)
        // {
        //     string strOutput="xin chao" +ps.PersonId + "-" + ps.FullName +"-" + ps.Address;
        //     string ten=" toi ten la "+ ps.FullName;
        //     ViewBag.infoPerson= strOutput;
        //     ViewData["name"]=ten;
        //     return View();
        // }

        private readonly ApplicationDbContext _context;

        public PersonController(ApplicationDbContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> GetPersons(
            string? searchString = null,
            int pageIndex = 1,
            int pageSize = 10)
        {
            var query = _context.Person
                .AsNoTracking()
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(searchString))
            {
                var pattern = $"%{searchString.Trim()}%";
                query = query.Where(p =>
                    EF.Functions.Like(p.PersonId!, pattern) ||
                    EF.Functions.Like(p.FullName!, pattern) ||
                    EF.Functions.Like(p.Address!, pattern));
            }

            var persons = query
                .OrderBy(p => p.PersonId)
                .Select(p => new PersonResponseDto(p.PersonId, p.FullName, p.Address))
                ;

            return Ok(await PagedResult<PersonResponseDto>.CreateAsync(
                persons, pageIndex, pageSize));
        }

        [HttpGet]
        public async Task<IActionResult> GetPersonById(string id)
        {
            var person = await _context.Person.FindAsync(id);
            return person == null
                ? NotFound(new { message = "Không tìm thấy người dùng." })
                : Ok(new PersonResponseDto(person.PersonId, person.FullName, person.Address));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateAjax([FromBody] Person? person)
        {
            if (person == null || string.IsNullOrWhiteSpace(person.PersonId))
            {
                return BadRequest(new { message = "Mã và dữ liệu Person là bắt buộc." });
            }

            if (await _context.Person.AnyAsync(p => p.PersonId == person.PersonId))
            {
                return BadRequest(new { message = "Mã Person đã tồn tại." });
            }

            _context.Person.Add(person);
            await _context.SaveChangesAsync();
            return Ok(new PersonResponseDto(person.PersonId, person.FullName, person.Address));
        }

        [HttpPut]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditAjax(string id, [FromBody] Person? person)
        {
            if (person == null)
            {
                return BadRequest(new { message = "Dữ liệu Person không hợp lệ." });
            }

            var existingPerson = await _context.Person.FindAsync(id);
            if (existingPerson == null)
            {
                return NotFound(new { message = "Không tìm thấy người dùng." });
            }

            existingPerson.FullName = person.FullName;
            existingPerson.Address = person.Address;
            await _context.SaveChangesAsync();
            return Ok(new PersonResponseDto(existingPerson.PersonId, existingPerson.FullName, existingPerson.Address));
        }

        [HttpDelete]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteAjax(string id)
        {
            var person = await _context.Person.FindAsync(id);
            if (person == null)
            {
                return NotFound(new { message = "Không tìm thấy người dùng." });
            }

            _context.Person.Remove(person);
            await _context.SaveChangesAsync();
            return Ok(new { message = "Đã xóa Person." });
        }

        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("PersonId,FullName,Address")] Person person)
        {
            if (ModelState.IsValid)
            {
                _context.Add(person);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(person);
        }

        public async Task<IActionResult> Edit(string id)
        {
            if (id == null || _context.Person == null)
            {
                return NotFound();
            }

            var person = await _context.Person.FindAsync(id);
            if (person == null)
            {
                return NotFound();
            }
            return View( person);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(string id, [Bind("PersonId,FullName,Address")] Person person)
        {
            if (id != person.PersonId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(person);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!PersonExists(person.PersonId))
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
            return View("Update", person);
        }

        public async Task<IActionResult> Delete(string id)
        {
            if (id == null || _context.Person == null)
            {
                return NotFound();
            }

            var person = await _context.Person
                .FirstOrDefaultAsync(m => m.PersonId == id);
            if (person == null)
            {
                return NotFound();
            }
            return View(person);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(string id)
        {
            if (_context.Person == null)
            {
                return Problem("Entity set 'ApplicationDbContext.Person' is null.");
            }

            var person = await _context.Person.FindAsync(id);
            if (person != null)
            {
                _context.Person.Remove(person);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool PersonExists(string id)
        {
            return (_context.Person?.Any(e => e.PersonId == id)).GetValueOrDefault();
        }
    }
}