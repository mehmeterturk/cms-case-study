using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ContentService.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddSlugAndStatus : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "PublishedAt",
                table: "contents",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Slug",
                table: "contents",
                type: "character varying(250)",
                maxLength: 250,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Status",
                table: "contents",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_contents_Slug",
                table: "contents",
                column: "Slug",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_contents_Slug",
                table: "contents");

            migrationBuilder.DropColumn(
                name: "PublishedAt",
                table: "contents");

            migrationBuilder.DropColumn(
                name: "Slug",
                table: "contents");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "contents");
        }
    }
}
