using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Beam.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddOverlayTokens : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "OverlayTokens",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SceneId = table.Column<Guid>(type: "uuid", nullable: false),
                    Token = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    RevokedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    LastAccessedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OverlayTokens", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_OverlayTokens_SceneId",
                table: "OverlayTokens",
                column: "SceneId");

            migrationBuilder.CreateIndex(
                name: "IX_OverlayTokens_Token",
                table: "OverlayTokens",
                column: "Token",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "OverlayTokens");
        }
    }
}
