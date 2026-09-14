using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Beam.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddOrganizationRolesBrandKitsAndTemplateLocking : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BrandKits",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    Colors = table.Column<List<string>>(type: "text[]", nullable: false),
                    Fonts = table.Column<List<string>>(type: "text[]", nullable: false),
                    LogoAssetId = table.Column<Guid>(type: "uuid", nullable: true),
                    WatermarkAssetId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BrandKits", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BrandKits_OrganizationId",
                table: "BrandKits",
                column: "OrganizationId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BrandKits");
        }
    }
}
