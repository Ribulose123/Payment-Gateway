using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PaymentGateway.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class FraudCheckMigration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "FraudChecks",
                columns: table => new
                {
                    FraudCheckId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OperationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OperationType = table.Column<int>(type: "int", nullable: false),
                    RiskScore = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    Decision = table.Column<int>(type: "int", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    EvaluatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    EvaluatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FraudChecks", x => x.FraudCheckId);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FraudChecks");
        }
    }
}
