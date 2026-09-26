using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FieldOps.WorkOrders.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddWorkOrderAuditHistory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "workOrderAuditEntries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    WorkOrderId = table.Column<Guid>(type: "uuid", nullable: false),
                    Action = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    PreviousStatus = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    NewStatus = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    PreviousTechnicianId = table.Column<Guid>(type: "uuid", nullable: true),
                    NewTechnicianId = table.Column<Guid>(type: "uuid", nullable: true),
                    ChangedByUserId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    ChangedByEmail = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: true),
                    OccurredAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_workOrderAuditEntries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_workOrderAuditEntries_WorkOrders_WorkOrderId",
                        column: x => x.WorkOrderId,
                        principalTable: "WorkOrders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_workOrderAuditEntries_WorkOrderId_OccurredAtUtc",
                table: "workOrderAuditEntries",
                columns: new[] { "WorkOrderId", "OccurredAtUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "workOrderAuditEntries");
        }
    }
}
