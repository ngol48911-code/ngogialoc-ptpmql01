# MvcMovie - Quản lý dữ liệu sinh viên

Ứng dụng ASP.NET Core MVC dùng để minh họa và quản lý các đối tượng **Person**, **Movie** và **Student**. Dữ liệu được lưu bằng SQLite thông qua Entity Framework Core. Các màn hình CRUD hiện sử dụng AJAX/JSON và Bootstrap Modal để cập nhật dữ liệu mà không tải lại trang.

## 1. Công nghệ sử dụng

- ASP.NET Core MVC
- .NET 10
- C#
- Entity Framework Core 10
- SQLite
- Razor View
- Bootstrap và jQuery Validation

Thông tin package được khai báo trong [MvcMovie.csproj](./MvcMovie.csproj).

## 2. Cấu trúc dự án

```text
MvcMovie/
├── Controllers/              # Tiếp nhận HTTP request và điều phối nghiệp vụ
├── Data/                     # ApplicationDbContext
├── Migrations/               # Lịch sử thay đổi cấu trúc và dữ liệu database
├── Models/                   # Entity và ViewModel
├── Views/                    # Giao diện Razor
├── wwwroot/                  # CSS, JavaScript và thư viện tĩnh
│   └── js/                   # Script AJAX cho Student, Person và Movie
├── App.db                    # SQLite database
├── Program.cs                # Cấu hình ứng dụng và dependency injection
└── appsettings.json         # Connection string và cấu hình logging
```

## 3. Các chức năng chính

### 3.1. Person

Controller [PersonController.cs](./Controllers/PersonController.cs) hỗ trợ CRUD MVC cũ và CRUD AJAX:

- Xem danh sách Person
- Thêm Person
- Sửa Person
- Xóa Person

Endpoint AJAX:

```text
GET    /Person/GetPersons
GET    /Person/GetPersonById/{id}
POST   /Person/CreateAjax
PUT    /Person/EditAjax/{id}
DELETE /Person/DeleteAjax/{id}
```

Các trường chính:

- `PersonId`
- `FullName`
- `Address`

### 3.2. Movie

Model [Movie.cs](./Models/Movie.cs) mô tả dữ liệu phim:

- `Id`
- `Title`
- `ReleaseDate`
- `Genre`
- `Price`

CRUD AJAX của Movie được triển khai tại [MovieController.cs](./Controllers/MovieController.cs).

Endpoint AJAX:

```text
GET    /Movie/GetMovies
GET    /Movie/GetMovieById/{id}
POST   /Movie/CreateAjax
PUT    /Movie/EditAjax/{id}
DELETE /Movie/DeleteAjax/{id}
```

### 3.3. Student

Controller [StudentController.cs](./Controllers/StudentController.cs) hỗ trợ:

- Xem danh sách sinh viên
- Thêm sinh viên
- Sửa sinh viên
- Xóa sinh viên
- Tự động sinh mã sinh viên
- Chọn khoa từ bảng `Faculties`

Endpoint AJAX:

```text
GET    /Student/GetStudents
GET    /Student/GetStudentById/{id}
POST   /Student/CreateAjax
PUT    /Student/EditAjax/{id}
DELETE /Student/DeleteAjax/{id}
```

Các request thay đổi dữ liệu yêu cầu anti-forgery token trong header `RequestVerificationToken`. Giao diện Student nằm tại [Index.cshtml](./Views/Student/Index.cshtml) và logic `fetch` nằm tại [student-crud.js](./wwwroot/js/student-crud.js).

Các trường chính:

- `StudentId`
- `FullName`
- `Address`
- `University`
- `NamNhapHoc`
- `HeDaoTao`
- `FacultyId`

## 4. Thiết kế database

Database mặc định là file `App.db`, được cấu hình trong [appsettings.json](./appsettings.json):

```json
"ConnectionStrings": {
  "DefaultConnection": "Data Source=App.db; Cache=Shared"
}
```

Các bảng chính:

### Bảng `Students`

Lưu thông tin sinh viên. Cột `FacultyId` là khóa ngoại liên kết tới bảng `Faculties`.

### Bảng `Faculties`

Lưu danh sách khoa:

| FacultyId | FacultyName |
|---|---|
| 01 | Công nghệ thông tin |
| 02 | Cơ - Điện |
| 03 | Dầu khí và Năng lượng |
| 04 | Giáo dục Quốc phòng |
| 05 | Khoa học cơ bản |
| 06 | Khoa học và Kỹ thuật Địa chất |
| 07 | Kinh tế và Quản trị kinh doanh |
| 08 | Lý luận chính trị |
| 09 | Mỏ |
| 10 | Môi trường |

Danh sách khoa được seed trong [ApplicationDbcontext.cs](./Data/ApplicationDbcontext.cs) và migration [Seed_HUMG_Faculties.cs](./Migrations/20261112070813_Seed_HUMG_Faculties.cs).

## 5. Luồng hoạt động của ứng dụng

### 5.1. Luồng request MVC

```text
Người dùng
   |
   v
Razor View (form/link)
   |
   v
Controller
   |
   v
ApplicationDbContext
   |
   v
Entity Framework Core
   |
   v
SQLite (App.db)
```

### 5.2. Luồng tạo sinh viên

```text
GET /Student/Create
    |
    +-- Lấy danh sách khoa từ Faculties
    +-- Tạo dropdown năm nhập học và hệ đào tạo
    +-- Hiển thị form
    |
POST /Student/Create
    |
    +-- Model binding dữ liệu form vào Student
    +-- Kiểm tra dữ liệu bắt buộc
    +-- Sinh StudentId tự động
    +-- Lưu Student vào bảng Students
    +-- Chuyển về /Student/Index
```

### 5.3. Công thức sinh mã sinh viên

Mã có dạng:

```text
[2 số cuối năm nhập học][mã hệ 2 số][mã khoa 2 số][số thứ tự 4 số]
```

Ví dụ:

```text
2028 + 21 + 01 + sinh viên thứ 1
=> 2821010001
```

Controller tìm mã lớn nhất có cùng tiền tố, lấy 4 chữ số cuối và tăng lên 1.

## 6. Cài đặt và chạy dự án

### Yêu cầu

- .NET 10 SDK
- EF Core CLI (`dotnet-ef`)

Kiểm tra .NET:

```powershell
dotnet --version
```

Cài EF Core CLI nếu máy chưa có:

```powershell
dotnet tool install --global dotnet-ef
```

### Khởi chạy

Mở terminal tại thư mục project:

```powershell
cd C:\laragon\www\ngogialoc-ptpmql01\MvcMovie
dotnet restore
dotnet ef database update
dotnet run
```

Sau khi chạy, mở URL được hiển thị trong terminal, thường có dạng:

```text
https://localhost:<port>
```

## 7. Làm việc với migration

Tạo migration sau khi thay đổi model:

```powershell
dotnet ef migrations add TenMigration
```

Áp dụng migration vào SQLite:

```powershell
dotnet ef database update
```

Xem danh sách migration:

```powershell
dotnet ef migrations list
```

Không nên chỉnh sửa thủ công các file migration đã được áp dụng trên database dùng chung.

## 8. Tìm kiếm và phân trang AJAX

Ba trang quản lý đều hỗ trợ lọc và phân trang phía server:

- Student: tìm theo `StudentId`, `FullName`, `Address` và lọc `FacultyId`.
- Person: tìm theo `PersonId`, `FullName`, `Address`.
- Movie: tìm theo `Title` và lọc theo `Genre`.

Các controller lọc trên `IQueryable`, sau đó mới thực hiện `CountAsync`, `Skip`, `Take` và `ToListAsync`. Kết quả dùng cấu trúc [PagedResult.cs](./Models/PagedResult.cs):

```json
{
  "items": [],
  "totalItems": 45,
  "pageIndex": 1,
  "pageSize": 10,
  "totalPages": 5
}
```

Giao diện có debounce khoảng 350ms cho ô tìm kiếm, chọn 5/10/20 dòng mỗi trang, nút Trước/Sau và giữ bộ lọc cùng trang hiện tại sau CRUD. Nếu xóa bản ghi cuối cùng của trang, server tự chuẩn hóa `pageIndex` về trang cuối còn dữ liệu.

Hàm dùng chung `renderPagination` và `renderPageInfo` nằm trong [site.js](./wwwroot/js/site.js). Script riêng của từng trang chỉ quản lý state và gọi API.

## 9. Kiểm thử tự động

Project kiểm thử độc lập nằm trong [MvcMovie.Tests](./MvcMovie.Tests). Project này dùng `Microsoft.EntityFrameworkCore.InMemory`, vì vậy test không đọc hoặc ghi file `App.db` thật.

Chạy toàn bộ test:

```powershell
dotnet test MvcMovie.Tests\MvcMovie.Tests.csproj
```

Các test hiện bao phủ:

- Sinh mã sinh viên đầu tiên với hậu tố `0001`.
- Tăng mã sinh viên tiếp theo thành `0002`.
- Validation khi thiếu `FullName` hoặc `FacultyId`.
- Trả về `FacultyName` mà không serialize vòng lặp navigation.
- Cập nhật Student nhưng giữ nguyên `StudentId`.
- Xóa Student và xử lý ID không tồn tại.
- CRUD AJAX của Person và Movie.

## 10. Checklist kiểm thử thủ công AJAX

1. Chạy ứng dụng bằng `dotnet run`.
2. Mở `/Student`, mở DevTools bằng `F12`, chọn tab **Network**.
3. Bấm **Thêm sinh viên**, nhập đầy đủ dữ liệu và lưu.
   - Kiểm tra request `POST /Student/CreateAjax`.
   - Kiểm tra request có header `RequestVerificationToken`.
   - Kiểm tra response `200` và `StudentId` có dạng 10 chữ số.
   - Kiểm tra bảng cập nhật mà không reload trang.
4. Bấm **Sửa** một sinh viên.
   - Kiểm tra `GET /Student/GetStudentById/{id}`.
   - Lưu thay đổi và kiểm tra `PUT /Student/EditAjax/{id}` trả về `200`.
   - Xác nhận `StudentId` không thay đổi.
5. Bỏ trống họ tên hoặc khoa rồi bấm lưu.
   - Kiểm tra request trả về `400`.
   - Kiểm tra lỗi hiển thị trong Modal.
6. Bấm **Xóa**, xác nhận hộp thoại.
   - Kiểm tra `DELETE /Student/DeleteAjax/{id}` trả về `200`.
   - Kiểm tra dòng bị xóa khỏi bảng mà không reload trang.
7. Lặp lại các bước tương tự tại `/Person` và `/Movie`.
8. Trong tab **Network**, thử mở một ID không tồn tại và xác nhận API trả về `404`.

## 11. Kiểm tra dữ liệu SQLite

Nếu đã cài SQLite CLI:

```powershell
sqlite3 App.db
```

Một số lệnh hữu ích:

```sql
.tables
SELECT * FROM Faculties;
SELECT * FROM Students;

SELECT
    Students.StudentId,
    Students.FullName,
    Faculties.FacultyName
FROM Students
LEFT JOIN Faculties
    ON Students.FacultyId = Faculties.FacultyId;

.quit
```

## 12. Xử lý lỗi thường gặp

### Dropdown Khoa không có dữ liệu

Chạy:

```powershell
dotnet ef database update
```

Sau đó kiểm tra:

```sql
SELECT * FROM Faculties;
```

### Không tạo được sinh viên

Kiểm tra:

- Đã nhập họ tên chưa.
- Đã chọn năm nhập học chưa.
- Đã chọn hệ đào tạo chưa.
- Đã chọn khoa chưa.
- Bảng `Faculties` đã có dữ liệu chưa.

### Database không cập nhật theo model

Kiểm tra migration:

```powershell
dotnet ef migrations list
dotnet ef database update
```

## 13. Điểm cần lưu ý

- Ứng dụng hiện chưa có chức năng đăng nhập và phân quyền.
- `StudentId` được sinh tại controller, nên cần tránh nhiều request tạo cùng lúc nếu triển khai nhiều người dùng.
- Không xóa hoặc sửa mã khoa đã được sinh viên tham chiếu nếu chưa xử lý khóa ngoại.
- File `App.db` là database phát triển cục bộ; nên sao lưu trước khi thao tác dữ liệu trực tiếp.
