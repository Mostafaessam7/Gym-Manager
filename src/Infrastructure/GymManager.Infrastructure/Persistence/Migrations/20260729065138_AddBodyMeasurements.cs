using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GymManager.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddBodyMeasurements : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BodyMeasurements",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MemberId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RecordedOnUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    HeightCm = table.Column<decimal>(type: "decimal(6,2)", nullable: true),
                    WeightKg = table.Column<decimal>(type: "decimal(6,2)", nullable: true),
                    BodyFatPercentage = table.Column<decimal>(type: "decimal(5,2)", nullable: true),
                    ChestCm = table.Column<decimal>(type: "decimal(6,2)", nullable: true),
                    WaistCm = table.Column<decimal>(type: "decimal(6,2)", nullable: true),
                    HipsCm = table.Column<decimal>(type: "decimal(6,2)", nullable: true),
                    ArmCm = table.Column<decimal>(type: "decimal(6,2)", nullable: true),
                    ThighCm = table.Column<decimal>(type: "decimal(6,2)", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    PhotoUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BodyMeasurements", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BodyMeasurements_MemberId_RecordedOnUtc",
                table: "BodyMeasurements",
                columns: new[] { "MemberId", "RecordedOnUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BodyMeasurements");
        }
    }
}
