using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HotelAgentIA.Infrastructure.Persistencia.Migraciones
{
    /// <inheritdoc />
    public partial class ComidasEnPlatos : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string[]>(
                name: "Comidas",
                table: "platos_menu",
                type: "text[]",
                nullable: false,
                defaultValue: new string[0]);

            // La semilla es idempotente y no toca bases ya sembradas: se ajustan los platos de ejemplo
            migrationBuilder.Sql("UPDATE platos_menu SET \"Comidas\" = ARRAY['desayuno'] WHERE \"Id\" = 'principal-desayuno';");
            migrationBuilder.Sql(
                "UPDATE platos_menu SET \"Comidas\" = ARRAY['almuerzo','cena'] " +
                "WHERE \"Id\" IN ('principal-risotto','principal-salmon','principal-curry','bar-ensalada','bar-club');");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Comidas",
                table: "platos_menu");
        }
    }
}
