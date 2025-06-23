using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VideoProcessingPlatform.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddResolutionAndBitrateToVideoRendition : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "BitrateKbps",
                table: "VideoRenditions",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "Resolution",
                table: "VideoRenditions",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BitrateKbps",
                table: "VideoRenditions");

            migrationBuilder.DropColumn(
                name: "Resolution",
                table: "VideoRenditions");
        }
    }
}
