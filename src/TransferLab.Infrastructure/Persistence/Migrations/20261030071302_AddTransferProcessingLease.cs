using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TransferLab.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddTransferProcessingLease : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Transfers_Status",
                table: "Transfers");

            migrationBuilder.AddColumn<int>(
                name: "AttemptCount",
                table: "Transfers",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "ProcessingStartedUtc",
                table: "Transfers",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.Sql(
                """
                UPDATE [Transfers]
                SET
                    [AttemptCount] = 1,
                    [ProcessingStartedUtc] = CASE
                        WHEN [Status] = N'Processing' THEN [CreatedUtc]
                        ELSE [ProcessingStartedUtc]
                    END
                WHERE [Status] IN (N'Processing', N'Completed', N'Failed')
                  AND [AttemptCount] = 0;
                """);

            migrationBuilder.CreateIndex(
                name: "IX_Transfers_Status_CreatedUtc",
                table: "Transfers",
                columns: new[] { "Status", "CreatedUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_Transfers_Status_ProcessingStartedUtc",
                table: "Transfers",
                columns: new[] { "Status", "ProcessingStartedUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Transfers_Status_CreatedUtc",
                table: "Transfers");

            migrationBuilder.DropIndex(
                name: "IX_Transfers_Status_ProcessingStartedUtc",
                table: "Transfers");

            migrationBuilder.DropColumn(
                name: "AttemptCount",
                table: "Transfers");

            migrationBuilder.DropColumn(
                name: "ProcessingStartedUtc",
                table: "Transfers");

            migrationBuilder.CreateIndex(
                name: "IX_Transfers_Status",
                table: "Transfers",
                column: "Status");
        }
    }
}
