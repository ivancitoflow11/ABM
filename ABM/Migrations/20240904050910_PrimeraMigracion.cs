using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ABM.Migrations
{
    public partial class PrimeraMigracion : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Usuario",
                columns: table => new
                {
                    IdUsuario = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    nombre = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    correo = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    password_c = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    TokenRecuperacion = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ExpiracionToken = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Usuario", x => x.IdUsuario);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Usuario");
        }
    }
}
