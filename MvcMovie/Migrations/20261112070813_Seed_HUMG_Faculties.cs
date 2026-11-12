using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace MvcMovie.Migrations
{
    /// <inheritdoc />
    public partial class Seed_HUMG_Faculties : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Students_Faculties_FacultyId",
                table: "Students");

            migrationBuilder.AlterColumn<string>(
                name: "NamNhapHoc",
                table: "Students",
                type: "TEXT",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "HeDaoTao",
                table: "Students",
                type: "TEXT",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "FacultyId",
                table: "Students",
                type: "TEXT",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldNullable: true);

            migrationBuilder.InsertData(
                table: "Faculties",
                columns: new[] { "FacultyId", "FacultyName" },
                values: new object[,]
                {
                    { "01", "Công nghệ thông tin" },
                    { "02", "Cơ - Điện" },
                    { "03", "Dầu khí và Năng lượng" },
                    { "04", "Giáo dục Quốc phòng" },
                    { "05", "Khoa học cơ bản" },
                    { "06", "Khoa học và Kỹ thuật Địa chất" },
                    { "07", "Kinh tế và Quản trị kinh doanh" },
                    { "08", "Lý luận chính trị" },
                    { "09", "Mỏ" },
                    { "10", "Môi trường" }
                });

            migrationBuilder.AddForeignKey(
                name: "FK_Students_Faculties_FacultyId",
                table: "Students",
                column: "FacultyId",
                principalTable: "Faculties",
                principalColumn: "FacultyId",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Students_Faculties_FacultyId",
                table: "Students");

            migrationBuilder.DeleteData(
                table: "Faculties",
                keyColumn: "FacultyId",
                keyValue: "01");

            migrationBuilder.DeleteData(
                table: "Faculties",
                keyColumn: "FacultyId",
                keyValue: "02");

            migrationBuilder.DeleteData(
                table: "Faculties",
                keyColumn: "FacultyId",
                keyValue: "03");

            migrationBuilder.DeleteData(
                table: "Faculties",
                keyColumn: "FacultyId",
                keyValue: "04");

            migrationBuilder.DeleteData(
                table: "Faculties",
                keyColumn: "FacultyId",
                keyValue: "05");

            migrationBuilder.DeleteData(
                table: "Faculties",
                keyColumn: "FacultyId",
                keyValue: "06");

            migrationBuilder.DeleteData(
                table: "Faculties",
                keyColumn: "FacultyId",
                keyValue: "07");

            migrationBuilder.DeleteData(
                table: "Faculties",
                keyColumn: "FacultyId",
                keyValue: "08");

            migrationBuilder.DeleteData(
                table: "Faculties",
                keyColumn: "FacultyId",
                keyValue: "09");

            migrationBuilder.DeleteData(
                table: "Faculties",
                keyColumn: "FacultyId",
                keyValue: "10");

            migrationBuilder.AlterColumn<string>(
                name: "NamNhapHoc",
                table: "Students",
                type: "TEXT",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "TEXT");

            migrationBuilder.AlterColumn<string>(
                name: "HeDaoTao",
                table: "Students",
                type: "TEXT",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "TEXT");

            migrationBuilder.AlterColumn<string>(
                name: "FacultyId",
                table: "Students",
                type: "TEXT",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "TEXT");

            migrationBuilder.AddForeignKey(
                name: "FK_Students_Faculties_FacultyId",
                table: "Students",
                column: "FacultyId",
                principalTable: "Faculties",
                principalColumn: "FacultyId");
        }
    }
}
