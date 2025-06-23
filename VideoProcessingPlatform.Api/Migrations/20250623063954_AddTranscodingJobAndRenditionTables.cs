using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VideoProcessingPlatform.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddTranscodingJobAndRenditionTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TranscodingJobs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UploadMetadataId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EncodingProfileId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SourceStoragePath = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Progress = table.Column<int>(type: "int", nullable: false),
                    StatusMessage = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastUpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TranscodingJobs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TranscodingJobs_EncodingProfiles_EncodingProfileId",
                        column: x => x.EncodingProfileId,
                        principalTable: "EncodingProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TranscodingJobs_UploadMetadata_UploadMetadataId",
                        column: x => x.UploadMetadataId,
                        principalTable: "UploadMetadata",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TranscodingJobs_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "VideoRenditions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TranscodingJobId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RenditionType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    StoragePath = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: false),
                    IsEncrypted = table.Column<bool>(type: "bit", nullable: false),
                    PlaybackUrl = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: true),
                    GeneratedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VideoRenditions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VideoRenditions_TranscodingJobs_TranscodingJobId",
                        column: x => x.TranscodingJobId,
                        principalTable: "TranscodingJobs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TranscodingJobs_EncodingProfileId",
                table: "TranscodingJobs",
                column: "EncodingProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_TranscodingJobs_UploadMetadataId",
                table: "TranscodingJobs",
                column: "UploadMetadataId");

            migrationBuilder.CreateIndex(
                name: "IX_TranscodingJobs_UserId",
                table: "TranscodingJobs",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_VideoRenditions_TranscodingJobId",
                table: "VideoRenditions",
                column: "TranscodingJobId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "VideoRenditions");

            migrationBuilder.DropTable(
                name: "TranscodingJobs");
        }
    }
}
