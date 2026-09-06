using System;
using Amtarc.Web.Domain;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Amtarc.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class Initial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:Enum:NewsCategory", "CONCOURS,EVENEMENT,TRAVAUX");

            migrationBuilder.CreateTable(
                name: "Admin",
                columns: table => new
                {
                    id = table.Column<string>(type: "text", nullable: false),
                    email = table.Column<string>(type: "text", nullable: false),
                    passwordHash = table.Column<string>(type: "text", nullable: false),
                    name = table.Column<string>(type: "text", nullable: true),
                    createdAt = table.Column<DateTime>(type: "timestamp(3) without time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("Admin_pkey", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "News",
                columns: table => new
                {
                    id = table.Column<string>(type: "text", nullable: false),
                    slug = table.Column<string>(type: "text", nullable: false),
                    title = table.Column<string>(type: "text", nullable: false),
                    category = table.Column<NewsCategory>(type: "\"NewsCategory\"", nullable: false),
                    imageUrl = table.Column<string>(type: "text", nullable: true),
                    excerpt = table.Column<string>(type: "text", nullable: true),
                    body = table.Column<string>(type: "text", nullable: false),
                    published = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    publishedAt = table.Column<DateTime>(type: "timestamp(3) without time zone", nullable: true, defaultValueSql: "CURRENT_TIMESTAMP"),
                    createdAt = table.Column<DateTime>(type: "timestamp(3) without time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updatedAt = table.Column<DateTime>(type: "timestamp(3) without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("News_pkey", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "SiteContent",
                columns: table => new
                {
                    key = table.Column<string>(type: "text", nullable: false),
                    data = table.Column<string>(type: "jsonb", nullable: false),
                    updatedAt = table.Column<DateTime>(type: "timestamp(3) without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("SiteContent_pkey", x => x.key);
                });

            migrationBuilder.CreateIndex(
                name: "Admin_email_key",
                table: "Admin",
                column: "email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "News_published_publishedAt_idx",
                table: "News",
                columns: new[] { "published", "publishedAt" });

            migrationBuilder.CreateIndex(
                name: "News_slug_key",
                table: "News",
                column: "slug",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Admin");

            migrationBuilder.DropTable(
                name: "News");

            migrationBuilder.DropTable(
                name: "SiteContent");
        }
    }
}
