using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FieldOps.WorkOrders.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class CompleteWorkOrderAuditHistory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_workOrderAuditEntries_WorkOrders_WorkOrderId",
                table: "workOrderAuditEntries");

            migrationBuilder.DropPrimaryKey(
                name: "PK_workOrderAuditEntries",
                table: "workOrderAuditEntries");

            migrationBuilder.RenameTable(
                name: "workOrderAuditEntries",
                newName: "WorkOrderAuditEntries");

            migrationBuilder.RenameIndex(
                name: "IX_workOrderAuditEntries_WorkOrderId_OccurredAtUtc",
                table: "WorkOrderAuditEntries",
                newName: "IX_WorkOrderAuditEntries_WorkOrderId_OccurredAtUtc");

            migrationBuilder.AddPrimaryKey(
                name: "PK_WorkOrderAuditEntries",
                table: "WorkOrderAuditEntries",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_WorkOrderAuditEntries_WorkOrders_WorkOrderId",
                table: "WorkOrderAuditEntries",
                column: "WorkOrderId",
                principalTable: "WorkOrders",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_WorkOrderAuditEntries_WorkOrders_WorkOrderId",
                table: "WorkOrderAuditEntries");

            migrationBuilder.DropPrimaryKey(
                name: "PK_WorkOrderAuditEntries",
                table: "WorkOrderAuditEntries");

            migrationBuilder.RenameTable(
                name: "WorkOrderAuditEntries",
                newName: "workOrderAuditEntries");

            migrationBuilder.RenameIndex(
                name: "IX_WorkOrderAuditEntries_WorkOrderId_OccurredAtUtc",
                table: "workOrderAuditEntries",
                newName: "IX_workOrderAuditEntries_WorkOrderId_OccurredAtUtc");

            migrationBuilder.AddPrimaryKey(
                name: "PK_workOrderAuditEntries",
                table: "workOrderAuditEntries",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_workOrderAuditEntries_WorkOrders_WorkOrderId",
                table: "workOrderAuditEntries",
                column: "WorkOrderId",
                principalTable: "WorkOrders",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
