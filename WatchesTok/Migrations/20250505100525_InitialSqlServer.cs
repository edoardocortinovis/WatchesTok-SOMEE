using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WatchesTok.Migrations
{
    public partial class InitialSqlServer : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "EleUtenti",
                columns: table => new
                {
                    UtenteID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Nome = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Cognome = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    email = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    password = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EleUtenti", x => x.UtenteID);
                });

            migrationBuilder.CreateTable(
                name: "ElePost",
                columns: table => new
                {
                    PostID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UtenteID = table.Column<int>(type: "int", nullable: false),
                    Titolo = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Descrizione = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    Hashtags = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    MarcaModello = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    MediaPath = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    DataCreazione = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ElePost", x => x.PostID);
                    table.ForeignKey(
                        name: "FK_ElePost_EleUtenti_UtenteID",
                        column: x => x.UtenteID,
                        principalTable: "EleUtenti",
                        principalColumn: "UtenteID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "EleLikes",
                columns: table => new
                {
                    LikeID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PostID = table.Column<int>(type: "int", nullable: false),
                    UtenteID = table.Column<int>(type: "int", nullable: false),
                    DataCreazione = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EleLikes", x => x.LikeID);
                    table.ForeignKey(
                        name: "FK_EleLikes_ElePost_PostID",
                        column: x => x.PostID,
                        principalTable: "ElePost",
                        principalColumn: "PostID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_EleLikes_EleUtenti_UtenteID",
                        column: x => x.UtenteID,
                        principalTable: "EleUtenti",
                        principalColumn: "UtenteID");
                });

            migrationBuilder.CreateIndex(
                name: "IX_EleLikes_PostID",
                table: "EleLikes",
                column: "PostID");

            migrationBuilder.CreateIndex(
                name: "IX_EleLikes_UtenteID",
                table: "EleLikes",
                column: "UtenteID");

            migrationBuilder.CreateIndex(
                name: "IX_ElePost_UtenteID",
                table: "ElePost",
                column: "UtenteID");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EleLikes");

            migrationBuilder.DropTable(
                name: "ElePost");

            migrationBuilder.DropTable(
                name: "EleUtenti");
        }
    }
}
