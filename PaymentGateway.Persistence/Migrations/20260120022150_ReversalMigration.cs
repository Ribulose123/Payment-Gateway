using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PaymentGateway.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ReversalMigration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Reversals",
                columns: table => new
                {
                    ReversalId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TransactionReference = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Reversals", x => x.ReversalId);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Reversals");
        }
    }
}
