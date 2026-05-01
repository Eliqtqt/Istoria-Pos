using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace CafeWebsite.Data.Migrations
{
    [Migration("20260427050249_AddEmailVerificationFieldsToUser")]
    public partial class AddEmailVerificationFieldsToUser : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "EmailConfirmed",
                table: "Users",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "EmailConfirmationToken",
                table: "Users",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "TokenExpiration",
                table: "Users",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EmailConfirmed",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "EmailConfirmationToken",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "TokenExpiration",
                table: "Users");
        }
    }
}
