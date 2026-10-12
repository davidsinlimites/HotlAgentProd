using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HotelAgentIA.Infrastructure.Persistencia.Migraciones
{
    /// <inheritdoc />
    public partial class MemoriaClaveValor : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "memoria_clave_valor",
                columns: table => new
                {
                    Clave = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    Valor = table.Column<string>(type: "text", nullable: false),
                    ActualizadoEn = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_memoria_clave_valor", x => x.Clave);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "memoria_clave_valor");
        }
    }
}
