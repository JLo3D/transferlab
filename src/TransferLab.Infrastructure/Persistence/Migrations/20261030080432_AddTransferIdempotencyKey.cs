using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TransferLab.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddTransferIdempotencyKey : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "IdempotencyKey",
                table: "Transfers",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Transfers_IdempotencyKey",
                table: "Transfers",
                column: "IdempotencyKey",
                unique: true,
                filter: "[IdempotencyKey] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Transfers_IdempotencyKey",
                table: "Transfers");

            migrationBuilder.DropColumn(
                name: "IdempotencyKey",
                table: "Transfers");
        }
    }
}
