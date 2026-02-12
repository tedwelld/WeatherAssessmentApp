using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WeatherAssessmentApp.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "UserPreferences",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Units = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    RefreshIntervalMinutes = table.Column<int>(type: "int", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserPreferences", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Locations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    City = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    Country = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Latitude = table.Column<double>(type: "float", nullable: false),
                    Longitude = table.Column<double>(type: "float", nullable: false),
                    IsFavorite = table.Column<bool>(type: "bit", nullable: false),
                    LastSyncedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastWeatherFingerprint = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    UserPreferencesId = table.Column<int>(type: "int", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Locations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Locations_UserPreferences_UserPreferencesId",
                        column: x => x.UserPreferencesId,
                        principalTable: "UserPreferences",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "WeatherSnapshots",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    LocationId = table.Column<int>(type: "int", nullable: false),
                    ObservedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Temperature = table.Column<decimal>(type: "decimal(8,2)", nullable: false),
                    FeelsLike = table.Column<decimal>(type: "decimal(8,2)", nullable: false),
                    Humidity = table.Column<int>(type: "int", nullable: false),
                    Pressure = table.Column<int>(type: "int", nullable: false),
                    WindSpeed = table.Column<decimal>(type: "decimal(8,2)", nullable: false),
                    Summary = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    IconCode = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    SourcePayload = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WeatherSnapshots", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WeatherSnapshots_Locations_LocationId",
                        column: x => x.LocationId,
                        principalTable: "Locations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Locations_City_Country",
                table: "Locations",
                columns: new[] { "City", "Country" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Locations_UserPreferencesId",
                table: "Locations",
                column: "UserPreferencesId");

            migrationBuilder.CreateIndex(
                name: "IX_WeatherSnapshots_LocationId_ObservedAtUtc",
                table: "WeatherSnapshots",
                columns: new[] { "LocationId", "ObservedAtUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "WeatherSnapshots");

            migrationBuilder.DropTable(
                name: "Locations");

            migrationBuilder.DropTable(
                name: "UserPreferences");
        }
    }
}
