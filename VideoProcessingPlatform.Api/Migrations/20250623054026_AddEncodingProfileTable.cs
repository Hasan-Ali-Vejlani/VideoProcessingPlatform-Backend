using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VideoProcessingPlatform.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddEncodingProfileTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "EncodingProfiles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProfileName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    Resolution = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    BitrateKbps = table.Column<int>(type: "int", nullable: false),
                    Format = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    FFmpegArgsTemplate = table.Column<string>(type: "NVARCHAR(MAX)", maxLength: 4000, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastModifiedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EncodingProfiles", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_EncodingProfiles_ProfileName",
                table: "EncodingProfiles",
                column: "ProfileName",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EncodingProfiles");
        }
    }
}
