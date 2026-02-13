using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WeatherAssessmentApp.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddSyncOperations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SyncOperations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Type = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    LocationId = table.Column<int>(type: "int", nullable: true),
                    LocationDisplayName = table.Column<string>(type: "nvarchar(192)", maxLength: 192, nullable: false),
                    RefreshedLocations = table.Column<int>(type: "int", nullable: false),
                    SnapshotsCreated = table.Column<int>(type: "int", nullable: false),
                    OccurredAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SyncOperations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SyncOperations_Locations_LocationId",
                        column: x => x.LocationId,
                        principalTable: "Locations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SyncOperations_LocationId",
                table: "SyncOperations",
                column: "LocationId");

            migrationBuilder.CreateIndex(
                name: "IX_SyncOperations_OccurredAtUtc",
                table: "SyncOperations",
                column: "OccurredAtUtc");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SyncOperations");
        }
    }
}
