using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sygnia.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddMovementConflictAudits : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "MovementConflictAudits",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AccountId = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    ExternalRef = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    AttemptedAmount = table.Column<decimal>(type: "DECIMAL(19,4)", nullable: false),
                    AttemptedCurrency = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: false),
                    AttemptedOccurredAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    StoredAmount = table.Column<decimal>(type: "DECIMAL(19,4)", nullable: false),
                    StoredCurrency = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: false),
                    StoredOccurredAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ConflictingFields = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    DetectedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MovementConflictAudits", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MovementConflictAudits_AccountId_ExternalRef",
                table: "MovementConflictAudits",
                columns: new[] { "AccountId", "ExternalRef" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MovementConflictAudits");
        }
    }
}
