using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace HotelAgentIA.Infrastructure.Persistencia.Migraciones
{
    /// <inheritdoc />
    public partial class Inicial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "actividades",
                columns: table => new
                {
                    Id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Nombre = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Descripcion = table.Column<string>(type: "text", nullable: false),
                    Ubicacion = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Dia = table.Column<int>(type: "integer", nullable: false),
                    Inicio = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                    Fin = table.Column<TimeOnly>(type: "time without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_actividades", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "huespedes",
                columns: table => new
                {
                    Id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Nombre = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_huespedes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "restaurantes",
                columns: table => new
                {
                    Id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Nombre = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Descripcion = table.Column<string>(type: "text", nullable: false),
                    Ubicacion = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_restaurantes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "estancias",
                columns: table => new
                {
                    Id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    HuespedId = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    NumeroHabitacion = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Entrada = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Salida = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_estancias", x => x.Id);
                    table.ForeignKey(
                        name: "FK_estancias_huespedes_HuespedId",
                        column: x => x.HuespedId,
                        principalTable: "huespedes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "horarios_apertura",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    RestauranteId = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Dia = table.Column<int>(type: "integer", nullable: false),
                    Abre = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                    Cierra = table.Column<TimeOnly>(type: "time without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_horarios_apertura", x => x.Id);
                    table.ForeignKey(
                        name: "FK_horarios_apertura_restaurantes_RestauranteId",
                        column: x => x.RestauranteId,
                        principalTable: "restaurantes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "platos_menu",
                columns: table => new
                {
                    Id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    RestauranteId = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Nombre = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Descripcion = table.Column<string>(type: "text", nullable: false),
                    Precio = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    EtiquetasDieteticas = table.Column<string[]>(type: "text[]", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_platos_menu", x => x.Id);
                    table.ForeignKey(
                        name: "FK_platos_menu_restaurantes_RestauranteId",
                        column: x => x.RestauranteId,
                        principalTable: "restaurantes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_actividades_Dia",
                table: "actividades",
                column: "Dia");

            migrationBuilder.CreateIndex(
                name: "IX_estancias_HuespedId",
                table: "estancias",
                column: "HuespedId");

            migrationBuilder.CreateIndex(
                name: "IX_horarios_apertura_RestauranteId",
                table: "horarios_apertura",
                column: "RestauranteId");

            migrationBuilder.CreateIndex(
                name: "IX_platos_menu_RestauranteId",
                table: "platos_menu",
                column: "RestauranteId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "actividades");

            migrationBuilder.DropTable(
                name: "estancias");

            migrationBuilder.DropTable(
                name: "horarios_apertura");

            migrationBuilder.DropTable(
                name: "platos_menu");

            migrationBuilder.DropTable(
                name: "huespedes");

            migrationBuilder.DropTable(
                name: "restaurantes");
        }
    }
}
