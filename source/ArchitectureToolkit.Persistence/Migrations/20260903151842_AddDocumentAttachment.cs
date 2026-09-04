using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ArchitectureToolkit.Persistence.Migrations;

/// <inheritdoc />
public partial class AddDocumentAttachment : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "DOCUMENT_ATTACHMENT",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                ProjectId = table.Column<Guid>(type: "uuid", nullable: false),
                FileName = table.Column<string>(type: "character varying(260)", maxLength: 260, nullable: false),
                ContentType = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                SizeBytes = table.Column<long>(type: "bigint", nullable: false),
                StorageKey = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                UploadedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                UploadedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_DOCUMENT_ATTACHMENT", x => x.Id);
                table.ForeignKey(
                    name: "FK_DOCUMENT_ATTACHMENT_PROJECT_ProjectId",
                    column: x => x.ProjectId,
                    principalTable: "PROJECT",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_DOCUMENT_ATTACHMENT_USER_UploadedByUserId",
                    column: x => x.UploadedByUserId,
                    principalTable: "USER",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateIndex(
            name: "IX_DOCUMENT_ATTACHMENT_ProjectId",
            table: "DOCUMENT_ATTACHMENT",
            column: "ProjectId");

        migrationBuilder.CreateIndex(
            name: "IX_DOCUMENT_ATTACHMENT_UploadedByUserId",
            table: "DOCUMENT_ATTACHMENT",
            column: "UploadedByUserId");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "DOCUMENT_ATTACHMENT");
    }
}
