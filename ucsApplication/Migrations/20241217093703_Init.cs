using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ucsApplication.Migrations
{
    /// <inheritdoc />
    public partial class Init : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "MasterTable",
                columns: table => new
                {
                    MasterId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    Username = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    FingerPrintData = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LastTransactionDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MasterTable", x => x.MasterId);
                });

            migrationBuilder.CreateTable(
                name: "TransactionTable",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    CheckinDateTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CheckoutDateTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CheckInMethod = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TransactionTable", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TransactionTable_MasterTable_UserId",
                        column: x => x.UserId,
                        principalTable: "MasterTable",
                        principalColumn: "MasterId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TransactionTable_UserId",
                table: "TransactionTable",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TransactionTable");

            migrationBuilder.DropTable(
                name: "MasterTable");
        }
    }
}
