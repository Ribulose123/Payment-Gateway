using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PaymentGateway.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class IdempotencyMigration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Idempotencies",
                columns: table => new
                {
                    IdempotencyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Key = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ClientId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RequsetHash = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    OperationType = table.Column<int>(type: "int", nullable: false),
                    OperationRefernceId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    ResponseSnapshot = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    FirstSeenAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastSeenAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ExpirationAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Idempotencies", x => x.IdempotencyId);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Idempotencies");
        }
    }
}
