using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ArchitectureToolkit.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RemoveCurrentRevisionForeignKeys : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PROJECT_DOCUMENT_DOCUMENT_REVISION_CurrentRevisionId",
                table: "PROJECT_DOCUMENT");

            migrationBuilder.DropForeignKey(
                name: "FK_TEMPLATE_TEMPLATE_REVISION_CurrentRevisionId",
                table: "TEMPLATE");

            migrationBuilder.DropIndex(
                name: "IX_TEMPLATE_CurrentRevisionId",
                table: "TEMPLATE");

            migrationBuilder.DropIndex(
                name: "IX_PROJECT_DOCUMENT_CurrentRevisionId",
                table: "PROJECT_DOCUMENT");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_TEMPLATE_CurrentRevisionId",
                table: "TEMPLATE",
                column: "CurrentRevisionId");

            migrationBuilder.CreateIndex(
                name: "IX_PROJECT_DOCUMENT_CurrentRevisionId",
                table: "PROJECT_DOCUMENT",
                column: "CurrentRevisionId");

            migrationBuilder.AddForeignKey(
                name: "FK_PROJECT_DOCUMENT_DOCUMENT_REVISION_CurrentRevisionId",
                table: "PROJECT_DOCUMENT",
                column: "CurrentRevisionId",
                principalTable: "DOCUMENT_REVISION",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_TEMPLATE_TEMPLATE_REVISION_CurrentRevisionId",
                table: "TEMPLATE",
                column: "CurrentRevisionId",
                principalTable: "TEMPLATE_REVISION",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
