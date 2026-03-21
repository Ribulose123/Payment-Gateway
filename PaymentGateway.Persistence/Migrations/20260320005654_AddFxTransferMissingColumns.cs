using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PaymentGateway.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddFxTransferMissingColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CreditTransactionReference",
                table: "FxTransfers",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DebitTransactionReference",
                table: "FxTransfers",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "FxTransfers",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CreditTransactionReference",
                table: "FxTransfers");

            migrationBuilder.DropColumn(
                name: "DebitTransactionReference",
                table: "FxTransfers");

            migrationBuilder.DropColumn(
                name: "Description",
                table: "FxTransfers");
        }
    }
}
