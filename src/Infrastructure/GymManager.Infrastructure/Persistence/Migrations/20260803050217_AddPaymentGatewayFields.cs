using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GymManager.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPaymentGatewayFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // defaultValue must be "None" (the enum's string-converted default), not "". EF's migration
            // scaffolding got this wrong for a new non-nullable enum-converted-to-string column on an
            // existing table — an empty string doesn't round-trip through the EnumToStringConverter when
            // reading a pre-existing Payment row back, and would throw at query time.
            migrationBuilder.AddColumn<string>(
                name: "GatewayProvider",
                table: "Payments",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "None");

            migrationBuilder.AddColumn<string>(
                name: "GatewayReferenceId",
                table: "Payments",
                type: "nvarchar(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Payments_GatewayReferenceId",
                table: "Payments",
                column: "GatewayReferenceId",
                unique: true,
                filter: "[GatewayReferenceId] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Payments_GatewayReferenceId",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "GatewayProvider",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "GatewayReferenceId",
                table: "Payments");
        }
    }
}
