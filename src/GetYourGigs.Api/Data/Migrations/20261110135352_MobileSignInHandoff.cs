using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GetYourGigs.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class MobileSignInHandoff : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "MobileGrant",
                columns: table => new
                {
                    Hash = table.Column<string>(type: "text", nullable: false),
                    AccountId = table.Column<Guid>(type: "uuid", nullable: false),
                    Challenge = table.Column<string>(type: "text", nullable: false),
                    ExpiresAt = table.Column<DateTimeOffset>(
                        type: "timestamp with time zone",
                        nullable: false
                    ),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MobileGrant", x => x.Hash);
                }
            );

            migrationBuilder.CreateIndex(
                name: "IX_MobileGrant_ExpiresAt",
                table: "MobileGrant",
                column: "ExpiresAt"
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "MobileGrant");
        }
    }
}
