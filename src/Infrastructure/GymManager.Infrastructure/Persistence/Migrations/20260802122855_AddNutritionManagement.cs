using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GymManager.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddNutritionManagement : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "NutritionLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MemberId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    NutritionPlanId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    LoggedOn = table.Column<DateOnly>(type: "date", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NutritionLogs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "NutritionPlans",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MemberId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TrainerId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    DailyCalorieTarget = table.Column<int>(type: "int", nullable: true),
                    ProteinTargetG = table.Column<decimal>(type: "decimal(6,2)", nullable: true),
                    CarbsTargetG = table.Column<decimal>(type: "decimal(6,2)", nullable: true),
                    FatTargetG = table.Column<decimal>(type: "decimal(6,2)", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedOnUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    ModifiedOnUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    ModifiedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NutritionPlans", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "NutritionLogEntries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FoodName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Calories = table.Column<int>(type: "int", nullable: true),
                    ProteinG = table.Column<decimal>(type: "decimal(6,2)", nullable: true),
                    CarbsG = table.Column<decimal>(type: "decimal(6,2)", nullable: true),
                    FatG = table.Column<decimal>(type: "decimal(6,2)", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    NutritionLogId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NutritionLogEntries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_NutritionLogEntries_NutritionLogs_NutritionLogId",
                        column: x => x.NutritionLogId,
                        principalTable: "NutritionLogs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "NutritionPlanMeals",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Order = table.Column<int>(type: "int", nullable: false),
                    TimeOfDay = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Calories = table.Column<int>(type: "int", nullable: true),
                    ProteinG = table.Column<decimal>(type: "decimal(6,2)", nullable: true),
                    CarbsG = table.Column<decimal>(type: "decimal(6,2)", nullable: true),
                    FatG = table.Column<decimal>(type: "decimal(6,2)", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    NutritionPlanId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NutritionPlanMeals", x => x.Id);
                    table.ForeignKey(
                        name: "FK_NutritionPlanMeals_NutritionPlans_NutritionPlanId",
                        column: x => x.NutritionPlanId,
                        principalTable: "NutritionPlans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_NutritionLogEntries_NutritionLogId",
                table: "NutritionLogEntries",
                column: "NutritionLogId");

            migrationBuilder.CreateIndex(
                name: "IX_NutritionLogs_MemberId_LoggedOn",
                table: "NutritionLogs",
                columns: new[] { "MemberId", "LoggedOn" });

            migrationBuilder.CreateIndex(
                name: "IX_NutritionPlanMeals_NutritionPlanId",
                table: "NutritionPlanMeals",
                column: "NutritionPlanId");

            migrationBuilder.CreateIndex(
                name: "IX_NutritionPlans_MemberId_IsActive",
                table: "NutritionPlans",
                columns: new[] { "MemberId", "IsActive" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "NutritionLogEntries");

            migrationBuilder.DropTable(
                name: "NutritionPlanMeals");

            migrationBuilder.DropTable(
                name: "NutritionLogs");

            migrationBuilder.DropTable(
                name: "NutritionPlans");
        }
    }
}
