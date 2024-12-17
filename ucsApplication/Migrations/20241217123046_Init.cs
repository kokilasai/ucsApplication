using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

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
                    MasterId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<int>(type: "integer", nullable: false),
                    Username = table.Column<string>(type: "text", nullable: false),
                    FingerPrintData = table.Column<string>(type: "text", nullable: true),
                    LastTransactionDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MasterTable", x => x.MasterId);
                });

            migrationBuilder.CreateTable(
                name: "TransactionTable",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<int>(type: "integer", nullable: false),
                    CheckinDateTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CheckoutDateTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CheckInMethod = table.Column<string>(type: "text", nullable: false)
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
