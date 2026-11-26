# Hệ thống quản lý dữ liệu ASP.NET Core MVC

Repository này là bài thực hành phát triển ứng dụng web bằng **ASP.NET Core MVC (.NET 10)**. Project chính được đặt tại thư mục `MvcMovie/` và cung cấp các nhóm chức năng quản lý:

- Quản lý Person
- Quản lý phim
- Quản lý sinh viên, có liên kết với danh mục khoa
- Quản lý nhóm quyền Identity (chỉ Admin)

Ứng dụng sử dụng **SQLite** để lưu dữ liệu, **Entity Framework Core** để truy cập và cập nhật cơ sở dữ liệu, **Razor View** để xây dựng giao diện, cùng **Bootstrap 5** cho layout và component.

---

## Mục lục

1. [Tổng quan](#1-tổng-quan)
2. [Công nghệ và thư viện](#2-công-nghệ-và-thư-viện)
3. [Cấu trúc repository](#3-cấu-trúc-repository)
4. [Chức năng chính](#4-chức-năng-chính)
5. [Thiết kế cơ sở dữ liệu](#5-thiết-kế-cơ-sở-dữ-liệu)
6. [Luồng hoạt động AJAX/CRUD](#6-luồng-hoạt-động-ajaxcrud)
7. [Giao diện và Bootstrap 5](#7-giao-diện-và-bootstrap-5)
8. [Yêu cầu môi trường](#8-yêu-cầu-môi-trường)
9. [Cài đặt và chạy project](#9-cài-đặt-và-chạy-project)
10. [Migration và database](#10-migration-và-database)
11. [Kiểm thử](#11-kiểm-thử)
12. [Các API chính](#12-các-api-chính)
13. [Troubleshooting](#13-troubleshooting)
14. [Điểm phát triển tiếp](#14-điểm-phát-triển-tiếp)

---

## 1. Tổng quan

Project mô phỏng một hệ thống quản trị dữ liệu đơn giản, trong đó người dùng có thể:

- Xem danh sách bằng bảng Bootstrap.
- Tìm kiếm và lọc dữ liệu.
- Phân trang dữ liệu phía server.
- Thêm mới và cập nhật bằng modal Bootstrap.
- Xóa bản ghi bằng yêu cầu AJAX.
- Nhận thông báo thành công hoặc lỗi ngay trên giao diện.
- Quản lý quan hệ khóa ngoại giữa sinh viên và khoa.

Các thao tác CRUD đều ưu tiên gọi API AJAX thay vì tải lại toàn bộ trang.

---

## 2. Công nghệ và thư viện

| Thành phần | Công nghệ |
|---|---|
| Runtime | .NET 10 |
| Web framework | ASP.NET Core MVC |
| Ngôn ngữ | C# |
| Template engine | Razor `.cshtml` |
| ORM | Entity Framework Core 10 |
| Database | SQLite |
| Giao diện | Bootstrap 5.3.3 |
| JavaScript | Vanilla JavaScript, `fetch`, DOM API |
| Kiểm thử | xUnit hoặc MSTest tùy project test, EF InMemory |

Project đã tách tài nguyên giao diện:

- Bootstrap CSS: `MvcMovie/wwwroot/lib/bootstrap/dist/css/bootstrap.min.css`
- Bootstrap JavaScript: `MvcMovie/wwwroot/lib/bootstrap/dist/js/bootstrap.bundle.min.js`
- CSS chung: `MvcMovie/wwwroot/css/site.css`
- CSS tùy chỉnh: `MvcMovie/wwwroot/css/app.css`
- JavaScript chung: `MvcMovie/wwwroot/js/site.js`

Chi tiết package được khai báo trong `MvcMovie/MvcMovie.csproj`.

---

## 3. Cấu trúc repository

```text
ngogialoc-ptpmql01/
├── README.md
└── MvcMovie/
    ├── App.db                         # SQLite database phát triển
    ├── MvcMovie.csproj                # Project và package
    ├── Program.cs                     # Cấu hình DI, middleware và routing
    ├── appsettings.json               # Connection string và logging
    ├── Controllers/
    │   ├── HomeController.cs
    │   ├── HelloWorldController.cs
    │   ├── PersonController.cs
    │   ├── MovieController.cs
    │   ├── StudentController.cs
    │   ├── RoleController.cs
    │   └── UserController.cs
    ├── Data/
    │   └── ApplicationDbcontext.cs    # DbContext và seed Faculties
    ├── Models/
    │   ├── Person.cs
    │   ├── Movie.cs
    │   ├── Student.cs
    │   ├── Faculty.cs
    │   ├── PagedResult.cs
    │   ├── PersonResponseDto.cs
    │   ├── MovieResponseDto.cs
    │   ├── StudentResponseDto.cs
    │   ├── RoleDto.cs
    │   ├── RoleResponseDto.cs
    │   └── UserRoleDtos.cs
    ├── Migrations/                    # Lịch sử migration EF Core
    ├── Views/
    │   ├── Shared/
    │   │   ├── _Layout.cshtml         # Layout Bootstrap chung
    │   │   └── _ValidationScriptsPartial.cshtml
    │   ├── Person/
    │   ├── Movie/
    │   ├── Student/
    │   ├── Home/
    │   ├── Role/
    │   └── User/
    ├── wwwroot/
    │   ├── css/
    │   ├── js/
    │   └── lib/bootstrap/
    └── MvcMovie.Tests/                # Unit/integration test
```

---

## 4. Chức năng chính

### 4.1. Quản lý Person

Person là module CRUD cơ bản.

Dữ liệu:

| Thuộc tính | Kiểu | Mô tả |
|---|---|---|
| `PersonId` | `string` | Mã duy nhất do người dùng nhập |
| `FullName` | `string` | Họ và tên |
| `Address` | `string` | Địa chỉ |

Chức năng:

- Tìm kiếm theo mã, họ tên hoặc địa chỉ.
- Thêm Person vào modal.
- Cập nhật họ tên và địa chỉ.
- Không cho trùng `PersonId`.
- Xóa Person bằng endpoint AJAX.
- Phân trang với kích thước trang 5, 10 hoặc 20.

### 4.2. Quản lý Movie

Movie quản lý thông tin phim.

| Thuộc tính | Kiểu | Mô tả |
|---|---|---|
| `Id` | `int` | Khóa chính tự tăng |
| `Title` | `string` | Tên phim |
| `ReleaseDate` | `DateTime` | Ngày phát hành |
| `Genre` | `string` | Thể loại |
| `Price` | `decimal` | Giá |

Chức năng:

- Tìm theo tên phim.
- Lọc theo thể loại.
- Thêm, sửa và xóa phim trong modal.
- Phân trang phía server.
- Cập nhật bảng mà không tải lại trang.

### 4.3. Quản lý Student

Student là module phức tạp nhất vì có khóa ngoại tới bảng khoa.

| Thuộc tính | Kiểu | Mô tả |
|---|---|---|
| `StudentId` | `string` | Mã sinh viên được sinh tự động |
| `FullName` | `string` | Họ tên, bắt buộc |
| `Address` | `string` | Địa chỉ |
| `University` | `string` | Trường đại học |
| `NamNhapHoc` | `string` | Năm nhập học |
| `HeDaoTao` | `string` | Mã hệ đào tạo |
| `FacultyId` | `string` | Mã khoa, khóa ngoại |

Hệ đào tạo được hỗ trợ:

| Mã | Tên |
|---|---|
| `21` | Đại học chính quy |
| `22` | Cao đẳng |
| `23` | Liên thông |
| `24` | Vừa làm vừa học |

Chức năng:

- Tìm sinh viên theo mã, họ tên hoặc địa chỉ.
- Lọc theo khoa.
- Sinh mã sinh viên tự động.
- Hiển thị tên khoa thay vì chỉ hiển thị `FacultyId`.
- Validation bắt buộc cho họ tên, năm, hệ và khoa.
- Cập nhật sinh viên mà không thay đổi `StudentId`.

### 4.4. Quản lý nhóm quyền

Trang `/Role` cho phép Admin xem, tìm kiếm, phân trang, thêm, sửa và xóa nhóm
quyền dùng trực tiếp các bảng Identity `AspNetRoles` và `AspNetUserRoles`.
Tìm kiếm tên không phân biệt hoa thường; kích thước trang hỗ trợ 5, 10 hoặc 20.
Hai nhóm quyền hệ thống `Admin` và `User` không thể sửa hoặc xóa. Nhóm quyền
đang được gán cho người dùng cũng không thể xóa. Mọi yêu cầu thay đổi dữ liệu
đều gửi anti-forgery token trong header `RequestVerificationToken`.

### 4.5. Quản lý người dùng và gán role

Trang `/User` dành cho Admin, hỗ trợ tìm người dùng theo email, lọc theo role,
phân trang 5/10/20 dòng và xem trạng thái xác thực email. Modal phân quyền hiển
thị toàn bộ role hiện có và cho phép cập nhật lựa chọn. Các thay đổi dùng
`UserManager<IdentityUser>`/`RoleManager<IdentityRole>` trên bảng Identity sẵn có.
Admin đang đăng nhập không thể tự gỡ role `Admin`; request cập nhật gửi
anti-forgery token qua header `RequestVerificationToken`.

---

## 5. Thiết kế cơ sở dữ liệu

Database mặc định là file SQLite `MvcMovie/App.db`.

Connection string trong `appsettings.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=App.db; Cache=Shared"
  }
}
```

Đường dẫn database được resolved theo thư mục làm việc khi chạy ứng dụng.

### Bảng chính

```text
Persons
Movies
Students
Faculties
AspNetRoles
AspNetUsers
AspNetUserRoles
```

Các bảng Identity đã có trong migration hiện tại; chức năng quản lý nhóm quyền
tái sử dụng `RoleManager<IdentityRole>` và `UserManager<IdentityUser>`, không
thêm bảng hoặc migration.

### Quan hệ

```text
Faculty 1 ───────< N Student
```

Mỗi `Student.FacultyId` tham chiếu tới `Faculty.FacultyId`. Khi đọc danh sách sinh viên, ứng dụng dùng `Include(s => s.Faculty)` để lấy tên khoa.

### Danh mục khoa mặc định

Ứng dụng seed 10 khoa:

| Mã | Tên khoa |
|---|---|
| `01` | Công nghệ thông tin |
| `02` | Cơ - Điện |
| `03` | Dầu khí và Năng lượng |
| `04` | Giáo dục Quốc phòng |
| `05` | Khoa học cơ bản |
| `06` | Khoa học và Kỹ thuật Địa chất |
| `07` | Kinh tế và Quản trị kinh doanh |
| `08` | Lý luận chính trị |
| `09` | Mỏ |
| `10` | Môi trường |

---

## 6. Luồng hoạt động AJAX/CRUD

### Luồng request MVC thông thường

```text
Người dùng
   ↓
Razor View hoặc JavaScript
   ↓
MVC Controller
   ↓
ApplicationDbContext
   ↓
Entity Framework Core
   ↓
SQLite
```

### Luồng AJAX

```text
Modal Bootstrap
   ↓
JavaScript fetch()
   ↓
HTTP JSON request
   ↓
Controller Action
   ↓
Validation và EF Core
   ↓
JSON response
   ↓
Cập nhật bảng và alert Bootstrap
```

Request tạo, sửa và xóa có anti-forgery token. JavaScript gửi token qua header:

```text
RequestVerificationToken
```

### Luồng sinh mã sinh viên

Mã sinh viên gồm 10 chữ số:

```text
[2 số năm][2 số hệ][2 số khoa][4 số thứ tự]
```

Ví dụ, sinh viên năm `2028`, hệ `21`, khoa `05`, thứ tự `199`:

```text
2821050199
```

Công thức tiền tố:

```text
2 số cuối năm + mã hệ + mã khoa
282105
```

Controller tìm sinh viên có `StudentId` cùng tiền tố, đọc 4 ký tự cuối và tăng số thứ tự.

### Phân trang

Tất cả danh sách dùng `PagedResult<T>`:

```json
{
  "items": [],
  "totalItems": 45,
  "pageIndex": 1,
  "pageSize": 10,
  "totalPages": 5
}
```

Controller thực hiện:

1. Tạo `IQueryable`.
2. Áp dụng tìm kiếm và bộ lọc.
3. Đếm tổng bản ghi bằng `CountAsync`.
4. Dùng `Skip` và `Take`.
5. Trả dữ liệu và metadata phân trang.

---

## 7. Giao diện và Bootstrap 5

Bootstrap được tích hợp trực tiếp trong `_Layout.cshtml`.

Các component đang sử dụng:

- Navbar responsive
- Container và grid
- Table responsive
- Form control và form select
- Modal
- Pagination
- Alert
- Button và badge

CSS tùy chỉnh nằm trong `wwwroot/css/app.css`. `app.css` được nạp sau Bootstrap để có thể override style mặc định mà không sửa file Bootstrap gốc.

Layout chung chứa:

- Thanh điều hướng
- Menu dẫn tới Person, Student, Movie, Quản lý Role và Quản lý Người dùng (hai mục quản trị chỉ hiển thị với Admin)
- Container render `@RenderBody()`
- Footer
- Bootstrap Bundle JavaScript
- JavaScript component riêng của từng view

---

## 8. Yêu cầu môi trường

Trên máy phát triển cần có:

- Windows hoặc môi trường hỗ trợ .NET
- .NET 10 SDK
- Laragon, Visual Studio hoặc VS Code
- EF Core CLI
- SQLite CLI là tùy chọn

Kiểm tra SDK:

```powershell
dotnet --version
```

Cài EF Core CLI:

```powershell
dotnet tool install --global dotnet-ef
```

Cập nhật EF Core CLI:

```powershell
dotnet tool update --global dotnet-ef
```

---

## 9. Cài đặt và chạy project

### Cách 1: Chạy bằng terminal

Từ thư mục repository:

```powershell
cd C:\laragon\www\ngogialoc-ptpmql01\MvcMovie
dotnet restore
dotnet ef database update
dotnet run
```

Terminal sẽ hiển thị URL ứng dụng, ví dụ:

```text
https://localhost:7001
http://localhost:5001
```

### Cách 2: Chạy bằng Visual Studio

1. Mở `MvcMovie/MvcMovie.csproj`.
2. Chọn cấu hình `Debug`.
3. Chọn profile HTTPS hoặc IIS Express.
4. Nhấn `F5`.

### Cách 3: Chạy bằng VS Code

1. Mở thư mục `MvcMovie`.
2. Đảm bảo đã cài C# Dev Kit hoặc C# extension.
3. Chạy `dotnet run` trong terminal.
4. Mở URL được terminal cung cấp.

### Các route chính

| URL | Chức năng |
|---|---|
| `/` | Trang chủ |
| `/Person` | Danh sách Person |
| `/Movie` | Danh sách phim |
| `/Student` | Danh sách sinh viên |
| `/Role` | Quản lý nhóm quyền (Admin) |
| `/User` | Quản lý người dùng và role (Admin) |

---

## 10. Migration và database

Sau khi thay đổi model Entity Framework, tạo migration:

```powershell
cd C:\laragon\www\ngogialoc-ptpmql01\MvcMovie
dotnet ef migrations add TenMigration
```

Áp dụng migration:

```powershell
dotnet ef database update
```

Xem danh sách migration:

```powershell
dotnet ef migrations list
```

Hoàn tác migration đã áp dụng tới target cụ thể:

```powershell
dotnet ef database update TenMigrationTruocDo
```

### Lưu ý

- Không chỉnh thủ công các file migration đã được triển khai.
- Sao lưu `App.db` trước khi chạy migration trên dữ liệu thật.
- Thư mục `Migrations/` hiện chứa migration seed danh sách khoa.

### Kiểm tra SQLite bằng CLI

```powershell
sqlite3 App.db
```

Câu lệnh SQL mẫu:

```sql
.tables

SELECT * FROM Faculties;
SELECT * FROM Students;

SELECT
    s.StudentId,
    s.FullName,
    f.FacultyName
FROM Students AS s
LEFT JOIN Faculties AS f
    ON s.FacultyId = f.FacultyId;

.quit
```

---

## 11. Kiểm thử

Project test nằm trong `MvcMovie/MvcMovie.Tests`.

Chạy toàn bộ test:

```powershell
cd C:\laragon\www\ngogialoc-ptpmql01\MvcMovie
dotnet test MvcMovie.Tests\MvcMovie.Tests.csproj
```

Chạy test bằng filter:

```powershell
dotnet test MvcMovie.Tests\MvcMovie.Tests.csproj --filter FullyQualifiedName~Student
```

Các nhóm test có thể bao gồm:

- Sinh mã sinh viên đầu tiên.
- Tăng số thứ tự của mã sinh viên.
- Validation sinh viên.
- CRUD Person.
- CRUD Movie.
- CRUD Student.
- Tìm kiếm/phân trang và CRUD nhóm quyền, bao gồm bảo vệ role hệ thống và role có người dùng.
- Tìm kiếm/phân trang người dùng, lọc role, cập nhật role và chống Admin tự gỡ quyền.
- Phân trang và chuẩn hóa trang.

Test database dùng provider InMemory thay vì ghi trực tiếp vào `App.db`.

---

## 12. Các API chính

### Person

| Method | Route | Chức năng |
|---|---|---|
| `GET` | `/Person/GetPersons` | Lấy danh sách, tìm kiếm, phân trang |
| `GET` | `/Person/GetPersonById/{id}` | Lấy Person theo mã |
| `POST` | `/Person/CreateAjax` | Tạo Person |
| `PUT` | `/Person/EditAjax/{id}` | Cập nhật Person |
| `DELETE` | `/Person/DeleteAjax/{id}` | Xóa Person |

### Movie

| Method | Route | Chức năng |
|---|---|---|
| `GET` | `/Movie/GetMovies` | Lấy danh sách, tìm kiếm, lọc, phân trang |
| `GET` | `/Movie/GetMovieById/{id}` | Lấy phim theo ID |
| `POST` | `/Movie/CreateAjax` | Tạo phim |
| `PUT` | `/Movie/EditAjax/{id}` | Cập nhật phim |
| `DELETE` | `/Movie/DeleteAjax/{id}` | Xóa phim |

### Student

| Method | Route | Chức năng |
|---|---|---|
| `GET` | `/Student/GetStudents` | Lấy danh sách, tìm kiếm, lọc, phân trang |
| `GET` | `/Student/GetStudentById/{id}` | Lấy sinh viên theo mã |
| `POST` | `/Student/CreateAjax` | Tạo sinh viên |
| `PUT` | `/Student/EditAjax/{id}` | Cập nhật sinh viên |
| `DELETE` | `/Student/DeleteAjax/{id}` | Xóa sinh viên |

### Role

| Method | Route | Chức năng |
|---|---|---|
| `GET` | `/Role/GetRoles?searchString=&pageIndex=1&pageSize=10` | Tìm kiếm và phân trang nhóm quyền |
| `GET` | `/Role/GetRoleById/{id}` | Lấy nhóm quyền và số người dùng |
| `POST` | `/Role/CreateAjax` | Tạo nhóm quyền (JSON + anti-forgery token) |
| `PUT` | `/Role/EditAjax/{id}` | Đổi tên nhóm quyền tùy chỉnh |
| `DELETE` | `/Role/DeleteAjax/{id}` | Xóa nhóm quyền trống |

Toàn bộ `RoleController` yêu cầu role `Admin`. Validation trả về `400` với
thông báo tiếng Việt; role không tồn tại trả về `404`. Không thể đổi tên/xóa
`Admin` hoặc `User`, và không thể xóa role đang được gán cho người dùng. Giao
diện dùng chung modal Bootstrap và các hàm `renderPagination`/`renderPageInfo`
của `site.js`.

### User

| Method | Route | Chức năng |
|---|---|---|
| `GET` | `/User/GetUsers?searchString=&roleFilter=&pageIndex=1&pageSize=10` | Tìm, lọc và phân trang người dùng cùng danh sách role |
| `GET` | `/User/GetUserRoles?userId={id}` | Lấy toàn bộ role và trạng thái được gán |
| `PUT` | `/User/UpdateUserRoles` | Cập nhật role bằng JSON và anti-forgery token |

`UserController` yêu cầu role `Admin`. API không cho Admin đang đăng nhập tự
loại bỏ role `Admin` của chính mình và trả về `400` với thông báo tiếng Việt.
Các lỗi chưa đăng nhập/không đủ quyền trả về `401`/`403` cho AJAX.

Tham số phân trang mẫu:

```text
/Student/GetStudents?searchString=nguyen&facultyId=01&pageIndex=1&pageSize=10
```

---

## 13. Troubleshooting

### Không kết nối được SQLite

Kiểm tra:

```powershell
dotnet ef database update
```

Sau đó kiểm tra file `MvcMovie/App.db` và connection string trong `appsettings.json`.

### Thiếu danh sách khoa

Chạy lại migration hoặc seed:

```powershell
dotnet ef database update
```

Sau đó truy vấn:

```sql
SELECT * FROM Faculties;
```

### Không tạo được sinh viên

Kiểm tra đồng thời:

- Họ tên không được để trống.
- Năm nhập học đã được chọn.
- Hệ đào tạo đã được chọn.
- Khoa đã được chọn.
- Khoa tồn tại trong bảng `Faculties`.

### AJAX trả về 400

Nguyên nhân phổ biến:

- Thiếu `RequestVerificationToken`.
- Dữ liệu JSON không đúng field.
- Thiếu field bắt buộc.
- `FacultyId` không tồn tại.
- Tên nhóm quyền để trống, vượt 50 ký tự hoặc đã tồn tại.
- Đang sửa/xóa role hệ thống hoặc xóa role đang có người dùng được gán.

### AJAX trả về 404

Bản ghi đã bị xóa hoặc ID trên URL không khớp dữ liệu trong database.

### Cập nhật model không áp dụng vào database

Chạy:

```powershell
dotnet ef migrations list
dotnet ef migrations add TenMigration
dotnet ef database update
```

### Port đã được sử dụng

Chạy bằng port chỉ định:

```powershell
dotnet run --urls "http://localhost:5080"
```

Hoặc dừng tiến trình đang giữ port rồi chạy lại.

---

## 14. Điểm phát triển tiếp

Các hướng phát triển phù hợp:

- Mở rộng chính sách phân quyền và quản lý quyền truy cập theo chức năng.
- Tách service/repository khỏi controller.
- Thêm logging bằng Serilog.
- Thêm validation phía server và client đầy đủ.
- Đóng gói API response bằng response model thống nhất.
- Thêm Dockerfile và cấu hình môi trường.
- Chuyển từ SQLite sang SQL Server hoặc PostgreSQL trong môi trường production.
- Thêm CI/CD bằng GitHub Actions.
- Thêm test phân trang, test anti-forgery và test giao diện.
- Thêm loading state và thông báo lỗi chung cho toàn bộ ứng dụng.
- Giới hạn số request đồng thời khi sinh `StudentId`.
