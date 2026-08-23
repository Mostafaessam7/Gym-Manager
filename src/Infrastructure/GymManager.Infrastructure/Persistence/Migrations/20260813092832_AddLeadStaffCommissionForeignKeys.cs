using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GymManager.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddLeadStaffCommissionForeignKeys : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddForeignKey(
                name: "FK_Commissions_Users_UserId",
                table: "Commissions",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Leads_Branches_BranchId",
                table: "Leads",
                column: "BranchId",
                principalTable: "Branches",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Leads_Users_AssignedToUserId",
                table: "Leads",
                column: "AssignedToUserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_StaffShifts_Branches_BranchId",
                table: "StaffShifts",
                column: "BranchId",
                principalTable: "Branches",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_StaffShifts_Users_UserId",
                table: "StaffShifts",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Commissions_Users_UserId",
                table: "Commissions");

            migrationBuilder.DropForeignKey(
                name: "FK_Leads_Branches_BranchId",
                table: "Leads");

            migrationBuilder.DropForeignKey(
                name: "FK_Leads_Users_AssignedToUserId",
                table: "Leads");

            migrationBuilder.DropForeignKey(
                name: "FK_StaffShifts_Branches_BranchId",
                table: "StaffShifts");

            migrationBuilder.DropForeignKey(
                name: "FK_StaffShifts_Users_UserId",
                table: "StaffShifts");
        }
    }
}
