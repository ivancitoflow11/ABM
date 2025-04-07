using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ABM.Migrations
{
    public partial class AddCODOTCColumn : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "TokenRecuperacion",
                table: "Usuario",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AddColumn<int>(
                name: "COD_OTC",
                table: "Usuario",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "FechaCambioPassword",
                table: "Usuario",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ID_Subgerencia",
                table: "Usuario",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ID_gerencia",
                table: "Usuario",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "ResponsableFirma",
                table: "Usuario",
                type: "bit",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "estado",
                table: "Usuario",
                type: "char(1)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "firma",
                table: "Usuario",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "idRol",
                table: "Usuario",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "password",
                table: "Usuario",
                type: "varchar(max)",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "im_gerencia",
                columns: table => new
                {
                    ID_gerencia = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Nom_Gerencia = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_im_gerencia", x => x.ID_gerencia);
                });

            migrationBuilder.CreateTable(
                name: "Rol",
                columns: table => new
                {
                    idRol = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    nombre = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Rol", x => x.idRol);
                });

            migrationBuilder.CreateTable(
                name: "im_Subgerencias",
                columns: table => new
                {
                    ID_Subgerencia = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Nom_Subgerencia = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    COD_Gerencia = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_im_Subgerencias", x => x.ID_Subgerencia);
                    table.ForeignKey(
                        name: "FK_im_Subgerencias_im_gerencia_COD_Gerencia",
                        column: x => x.COD_Gerencia,
                        principalTable: "im_gerencia",
                        principalColumn: "ID_gerencia",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Usuario_ID_gerencia",
                table: "Usuario",
                column: "ID_gerencia");

            migrationBuilder.CreateIndex(
                name: "IX_Usuario_ID_Subgerencia",
                table: "Usuario",
                column: "ID_Subgerencia");

            migrationBuilder.CreateIndex(
                name: "IX_Usuario_idRol",
                table: "Usuario",
                column: "idRol");

            migrationBuilder.CreateIndex(
                name: "IX_im_Subgerencias_COD_Gerencia",
                table: "im_Subgerencias",
                column: "COD_Gerencia");

            migrationBuilder.AddForeignKey(
                name: "FK_Usuario_im_gerencia_ID_gerencia",
                table: "Usuario",
                column: "ID_gerencia",
                principalTable: "im_gerencia",
                principalColumn: "ID_gerencia");

            migrationBuilder.AddForeignKey(
                name: "FK_Usuario_im_Subgerencias_ID_Subgerencia",
                table: "Usuario",
                column: "ID_Subgerencia",
                principalTable: "im_Subgerencias",
                principalColumn: "ID_Subgerencia");

            migrationBuilder.AddForeignKey(
                name: "FK_Usuario_Rol_idRol",
                table: "Usuario",
                column: "idRol",
                principalTable: "Rol",
                principalColumn: "idRol");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Usuario_im_gerencia_ID_gerencia",
                table: "Usuario");

            migrationBuilder.DropForeignKey(
                name: "FK_Usuario_im_Subgerencias_ID_Subgerencia",
                table: "Usuario");

            migrationBuilder.DropForeignKey(
                name: "FK_Usuario_Rol_idRol",
                table: "Usuario");

            migrationBuilder.DropTable(
                name: "im_Subgerencias");

            migrationBuilder.DropTable(
                name: "Rol");

            migrationBuilder.DropTable(
                name: "im_gerencia");

            migrationBuilder.DropIndex(
                name: "IX_Usuario_ID_gerencia",
                table: "Usuario");

            migrationBuilder.DropIndex(
                name: "IX_Usuario_ID_Subgerencia",
                table: "Usuario");

            migrationBuilder.DropIndex(
                name: "IX_Usuario_idRol",
                table: "Usuario");

            migrationBuilder.DropColumn(
                name: "COD_OTC",
                table: "Usuario");

            migrationBuilder.DropColumn(
                name: "FechaCambioPassword",
                table: "Usuario");

            migrationBuilder.DropColumn(
                name: "ID_Subgerencia",
                table: "Usuario");

            migrationBuilder.DropColumn(
                name: "ID_gerencia",
                table: "Usuario");

            migrationBuilder.DropColumn(
                name: "ResponsableFirma",
                table: "Usuario");

            migrationBuilder.DropColumn(
                name: "estado",
                table: "Usuario");

            migrationBuilder.DropColumn(
                name: "firma",
                table: "Usuario");

            migrationBuilder.DropColumn(
                name: "idRol",
                table: "Usuario");

            migrationBuilder.DropColumn(
                name: "password",
                table: "Usuario");

            migrationBuilder.AlterColumn<string>(
                name: "TokenRecuperacion",
                table: "Usuario",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(200)",
                oldMaxLength: 200,
                oldNullable: true);
        }
    }
}
