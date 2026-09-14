using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Beam.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddTemplatesAndElementLocking : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<List<string>>(
                name: "EditableProperties",
                table: "Elements",
                type: "text[]",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "SourceTemplateElementId",
                table: "Elements",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Templates",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    Category = table.Column<int>(type: "integer", nullable: false),
                    Scope = table.Column<int>(type: "integer", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: true),
                    DefinitionJson = table.Column<string>(type: "jsonb", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Templates", x => x.Id);
                });

            migrationBuilder.InsertData(
                table: "Templates",
                columns: new[] { "Id", "Category", "CreatedAtUtc", "DefinitionJson", "Name", "OrganizationId", "Scope", "UpdatedAtUtc" },
                values: new object[,]
                {
                    { new Guid("00000000-0000-0000-0001-000000000001"), 0, new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "{\"scenes\":[{\"name\":\"Starting Soon\",\"elements\":[{\"type\":0,\"name\":\"Title\",\"x\":260,\"y\":420,\"width\":1400,\"height\":120,\"rotation\":0,\"opacity\":1,\"config\":{\"content\":\"GAME NIGHT\",\"fontFamily\":\"Inter, sans-serif\",\"fontSize\":72,\"fontWeight\":\"800\",\"textAlign\":\"center\",\"color\":\"#a855f7\",\"backgroundColor\":null},\"editableProperties\":null},{\"type\":0,\"name\":\"Subtitle\",\"x\":260,\"y\":520,\"width\":1400,\"height\":60,\"rotation\":0,\"opacity\":1,\"config\":{\"content\":\"Starting soon...\",\"fontFamily\":\"Inter, sans-serif\",\"fontSize\":28,\"fontWeight\":\"500\",\"textAlign\":\"center\",\"color\":\"#e5e5e5\",\"backgroundColor\":null},\"editableProperties\":null}]},{\"name\":\"Main\",\"elements\":[{\"type\":0,\"name\":\"Lower Third\",\"x\":60,\"y\":900,\"width\":700,\"height\":90,\"rotation\":0,\"opacity\":1,\"config\":{\"content\":\"GAME NIGHT\",\"fontFamily\":\"Inter, sans-serif\",\"fontSize\":36,\"fontWeight\":\"700\",\"textAlign\":\"left\",\"color\":\"#ffffff\",\"backgroundColor\":\"#a855f7\"},\"editableProperties\":null}]},{\"name\":\"Break\",\"elements\":[{\"type\":0,\"name\":\"Title\",\"x\":260,\"y\":460,\"width\":1400,\"height\":120,\"rotation\":0,\"opacity\":1,\"config\":{\"content\":\"BE RIGHT BACK\",\"fontFamily\":\"Inter, sans-serif\",\"fontSize\":72,\"fontWeight\":\"800\",\"textAlign\":\"center\",\"color\":\"#a855f7\",\"backgroundColor\":null},\"editableProperties\":null},{\"type\":0,\"name\":\"Subtitle\",\"x\":260,\"y\":560,\"width\":1400,\"height\":60,\"rotation\":0,\"opacity\":1,\"config\":{\"content\":\"Live now\",\"fontFamily\":\"Inter, sans-serif\",\"fontSize\":28,\"fontWeight\":\"500\",\"textAlign\":\"center\",\"color\":\"#e5e5e5\",\"backgroundColor\":null},\"editableProperties\":null}]},{\"name\":\"Ending\",\"elements\":[{\"type\":0,\"name\":\"Title\",\"x\":260,\"y\":460,\"width\":1400,\"height\":120,\"rotation\":0,\"opacity\":1,\"config\":{\"content\":\"THANKS FOR WATCHING\",\"fontFamily\":\"Inter, sans-serif\",\"fontSize\":72,\"fontWeight\":\"800\",\"textAlign\":\"center\",\"color\":\"#a855f7\",\"backgroundColor\":null},\"editableProperties\":null},{\"type\":0,\"name\":\"Subtitle\",\"x\":260,\"y\":560,\"width\":1400,\"height\":60,\"rotation\":0,\"opacity\":1,\"config\":{\"content\":\"See you next time!\",\"fontFamily\":\"Inter, sans-serif\",\"fontSize\":28,\"fontWeight\":\"500\",\"textAlign\":\"center\",\"color\":\"#e5e5e5\",\"backgroundColor\":null},\"editableProperties\":null}]}]}", "Gaming", null, 0, new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("00000000-0000-0000-0001-000000000002"), 1, new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "{\"scenes\":[{\"name\":\"Starting Soon\",\"elements\":[{\"type\":0,\"name\":\"Title\",\"x\":260,\"y\":420,\"width\":1400,\"height\":120,\"rotation\":0,\"opacity\":1,\"config\":{\"content\":\"THE PODCAST\",\"fontFamily\":\"Inter, sans-serif\",\"fontSize\":72,\"fontWeight\":\"800\",\"textAlign\":\"center\",\"color\":\"#f59e0b\",\"backgroundColor\":null},\"editableProperties\":null},{\"type\":0,\"name\":\"Subtitle\",\"x\":260,\"y\":520,\"width\":1400,\"height\":60,\"rotation\":0,\"opacity\":1,\"config\":{\"content\":\"Starting soon...\",\"fontFamily\":\"Inter, sans-serif\",\"fontSize\":28,\"fontWeight\":\"500\",\"textAlign\":\"center\",\"color\":\"#e5e5e5\",\"backgroundColor\":null},\"editableProperties\":null}]},{\"name\":\"Main\",\"elements\":[{\"type\":0,\"name\":\"Lower Third\",\"x\":60,\"y\":900,\"width\":700,\"height\":90,\"rotation\":0,\"opacity\":1,\"config\":{\"content\":\"THE PODCAST\",\"fontFamily\":\"Inter, sans-serif\",\"fontSize\":36,\"fontWeight\":\"700\",\"textAlign\":\"left\",\"color\":\"#ffffff\",\"backgroundColor\":\"#f59e0b\"},\"editableProperties\":null}]},{\"name\":\"Break\",\"elements\":[{\"type\":0,\"name\":\"Title\",\"x\":260,\"y\":460,\"width\":1400,\"height\":120,\"rotation\":0,\"opacity\":1,\"config\":{\"content\":\"BE RIGHT BACK\",\"fontFamily\":\"Inter, sans-serif\",\"fontSize\":72,\"fontWeight\":\"800\",\"textAlign\":\"center\",\"color\":\"#f59e0b\",\"backgroundColor\":null},\"editableProperties\":null},{\"type\":0,\"name\":\"Subtitle\",\"x\":260,\"y\":560,\"width\":1400,\"height\":60,\"rotation\":0,\"opacity\":1,\"config\":{\"content\":\"Episode recording\",\"fontFamily\":\"Inter, sans-serif\",\"fontSize\":28,\"fontWeight\":\"500\",\"textAlign\":\"center\",\"color\":\"#e5e5e5\",\"backgroundColor\":null},\"editableProperties\":null}]},{\"name\":\"Ending\",\"elements\":[{\"type\":0,\"name\":\"Title\",\"x\":260,\"y\":460,\"width\":1400,\"height\":120,\"rotation\":0,\"opacity\":1,\"config\":{\"content\":\"THANKS FOR WATCHING\",\"fontFamily\":\"Inter, sans-serif\",\"fontSize\":72,\"fontWeight\":\"800\",\"textAlign\":\"center\",\"color\":\"#f59e0b\",\"backgroundColor\":null},\"editableProperties\":null},{\"type\":0,\"name\":\"Subtitle\",\"x\":260,\"y\":560,\"width\":1400,\"height\":60,\"rotation\":0,\"opacity\":1,\"config\":{\"content\":\"See you next time!\",\"fontFamily\":\"Inter, sans-serif\",\"fontSize\":28,\"fontWeight\":\"500\",\"textAlign\":\"center\",\"color\":\"#e5e5e5\",\"backgroundColor\":null},\"editableProperties\":null}]}]}", "Podcast", null, 0, new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("00000000-0000-0000-0001-000000000003"), 2, new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "{\"scenes\":[{\"name\":\"Starting Soon\",\"elements\":[{\"type\":0,\"name\":\"Title\",\"x\":260,\"y\":420,\"width\":1400,\"height\":120,\"rotation\":0,\"opacity\":1,\"config\":{\"content\":\"JUST CHATTING\",\"fontFamily\":\"Inter, sans-serif\",\"fontSize\":72,\"fontWeight\":\"800\",\"textAlign\":\"center\",\"color\":\"#22c55e\",\"backgroundColor\":null},\"editableProperties\":null},{\"type\":0,\"name\":\"Subtitle\",\"x\":260,\"y\":520,\"width\":1400,\"height\":60,\"rotation\":0,\"opacity\":1,\"config\":{\"content\":\"Starting soon...\",\"fontFamily\":\"Inter, sans-serif\",\"fontSize\":28,\"fontWeight\":\"500\",\"textAlign\":\"center\",\"color\":\"#e5e5e5\",\"backgroundColor\":null},\"editableProperties\":null}]},{\"name\":\"Main\",\"elements\":[{\"type\":0,\"name\":\"Lower Third\",\"x\":60,\"y\":900,\"width\":700,\"height\":90,\"rotation\":0,\"opacity\":1,\"config\":{\"content\":\"JUST CHATTING\",\"fontFamily\":\"Inter, sans-serif\",\"fontSize\":36,\"fontWeight\":\"700\",\"textAlign\":\"left\",\"color\":\"#ffffff\",\"backgroundColor\":\"#22c55e\"},\"editableProperties\":null}]},{\"name\":\"Break\",\"elements\":[{\"type\":0,\"name\":\"Title\",\"x\":260,\"y\":460,\"width\":1400,\"height\":120,\"rotation\":0,\"opacity\":1,\"config\":{\"content\":\"BE RIGHT BACK\",\"fontFamily\":\"Inter, sans-serif\",\"fontSize\":72,\"fontWeight\":\"800\",\"textAlign\":\"center\",\"color\":\"#22c55e\",\"backgroundColor\":null},\"editableProperties\":null},{\"type\":0,\"name\":\"Subtitle\",\"x\":260,\"y\":560,\"width\":1400,\"height\":60,\"rotation\":0,\"opacity\":1,\"config\":{\"content\":\"Hanging out\",\"fontFamily\":\"Inter, sans-serif\",\"fontSize\":28,\"fontWeight\":\"500\",\"textAlign\":\"center\",\"color\":\"#e5e5e5\",\"backgroundColor\":null},\"editableProperties\":null}]},{\"name\":\"Ending\",\"elements\":[{\"type\":0,\"name\":\"Title\",\"x\":260,\"y\":460,\"width\":1400,\"height\":120,\"rotation\":0,\"opacity\":1,\"config\":{\"content\":\"THANKS FOR WATCHING\",\"fontFamily\":\"Inter, sans-serif\",\"fontSize\":72,\"fontWeight\":\"800\",\"textAlign\":\"center\",\"color\":\"#22c55e\",\"backgroundColor\":null},\"editableProperties\":null},{\"type\":0,\"name\":\"Subtitle\",\"x\":260,\"y\":560,\"width\":1400,\"height\":60,\"rotation\":0,\"opacity\":1,\"config\":{\"content\":\"See you next time!\",\"fontFamily\":\"Inter, sans-serif\",\"fontSize\":28,\"fontWeight\":\"500\",\"textAlign\":\"center\",\"color\":\"#e5e5e5\",\"backgroundColor\":null},\"editableProperties\":null}]}]}", "Just Chatting", null, 0, new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Templates");

            migrationBuilder.DropColumn(
                name: "EditableProperties",
                table: "Elements");

            migrationBuilder.DropColumn(
                name: "SourceTemplateElementId",
                table: "Elements");
        }
    }
}
