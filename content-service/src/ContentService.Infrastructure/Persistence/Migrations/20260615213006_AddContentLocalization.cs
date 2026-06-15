using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ContentService.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddContentLocalization : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Language",
                table: "contents",
                type: "character varying(5)",
                maxLength: 5,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<Guid>(
                name: "TranslationGroupId",
                table: "contents",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateIndex(
                name: "IX_contents_TranslationGroupId_Language",
                table: "contents",
                columns: new[] { "TranslationGroupId", "Language" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_contents_TranslationGroupId_Language",
                table: "contents");

            migrationBuilder.DropColumn(
                name: "Language",
                table: "contents");

            migrationBuilder.DropColumn(
                name: "TranslationGroupId",
                table: "contents");
        }
    }
}
