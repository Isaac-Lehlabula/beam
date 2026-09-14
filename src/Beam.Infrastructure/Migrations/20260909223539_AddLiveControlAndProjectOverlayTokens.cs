using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Beam.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddLiveControlAndProjectOverlayTokens : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<Guid>(
                name: "SceneId",
                table: "OverlayTokens",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AddColumn<Guid>(
                name: "ProjectId",
                table: "OverlayTokens",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "LiveSessions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ProjectId = table.Column<Guid>(type: "uuid", nullable: false),
                    StartedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    EndedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LiveSessions", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_OverlayTokens_ProjectId",
                table: "OverlayTokens",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_LiveSessions_ProjectId_EndedAtUtc",
                table: "LiveSessions",
                columns: new[] { "ProjectId", "EndedAtUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LiveSessions");

            migrationBuilder.DropIndex(
                name: "IX_OverlayTokens_ProjectId",
                table: "OverlayTokens");

            migrationBuilder.DropColumn(
                name: "ProjectId",
                table: "OverlayTokens");

            migrationBuilder.AlterColumn<Guid>(
                name: "SceneId",
                table: "OverlayTokens",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);
        }
    }
}
