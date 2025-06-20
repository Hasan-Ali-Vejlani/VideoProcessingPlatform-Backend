using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VideoProcessingPlatform.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddUploadMetadataTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "UploadMetadata",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OriginalFileName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    OriginalFileSize = table.Column<long>(type: "bigint", nullable: false),
                    MimeType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    TotalChunks = table.Column<int>(type: "int", nullable: false),
                    CompletedChunks = table.Column<string>(type: "NVARCHAR(MAX)", nullable: false),
                    OriginalStoragePath = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: true),
                    UploadStatus = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    UploadedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UploadMetadata", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UploadMetadata_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_UploadMetadata_UserId",
                table: "UploadMetadata",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UploadMetadata");
        }
    }
}
