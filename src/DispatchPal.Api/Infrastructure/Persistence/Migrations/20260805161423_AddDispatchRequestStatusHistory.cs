using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DispatchPal.Api.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddDispatchRequestStatusHistory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DispatchRequestStatusHistories",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DispatchRequestId = table.Column<Guid>(type: "uuid", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    ChangedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DispatchRequestStatusHistories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DispatchRequestStatusHistories_DispatchRequests_DispatchReq~",
                        column: x => x.DispatchRequestId,
                        principalTable: "DispatchRequests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DispatchRequestStatusHistories_DispatchRequestId_ChangedAtU~",
                table: "DispatchRequestStatusHistories",
                columns: new[] { "DispatchRequestId", "ChangedAtUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DispatchRequestStatusHistories");
        }
    }
}
