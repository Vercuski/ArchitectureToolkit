using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ArchitectureToolkit.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CATEGORY",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CATEGORY", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PROJECT",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PROJECT", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "USER",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    Email = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: false),
                    SystemRole = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_USER", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PROJECT_MEMBER",
                columns: table => new
                {
                    ProjectId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Role = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PROJECT_MEMBER", x => new { x.ProjectId, x.UserId });
                    table.ForeignKey(
                        name: "FK_PROJECT_MEMBER_PROJECT_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "PROJECT",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PROJECT_MEMBER_USER_UserId",
                        column: x => x.UserId,
                        principalTable: "USER",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "USER_IDENTITY",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Issuer = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    ExternalSubjectId = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    ProviderLabel = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    LinkedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_USER_IDENTITY", x => x.Id);
                    table.ForeignKey(
                        name: "FK_USER_IDENTITY_USER_UserId",
                        column: x => x.UserId,
                        principalTable: "USER",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DOCUMENT_REVISION",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DocumentId = table.Column<Guid>(type: "uuid", nullable: false),
                    Version = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    BumpType = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: true),
                    Content = table.Column<string>(type: "text", nullable: false),
                    AuthorId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DOCUMENT_REVISION", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DOCUMENT_REVISION_USER_AuthorId",
                        column: x => x.AuthorId,
                        principalTable: "USER",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PROJECT_DOCUMENT",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ProjectId = table.Column<Guid>(type: "uuid", nullable: false),
                    CategoryId = table.Column<Guid>(type: "uuid", nullable: false),
                    SourceTemplateRevisionId = table.Column<Guid>(type: "uuid", nullable: true),
                    Title = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    CurrentRevisionId = table.Column<Guid>(type: "uuid", nullable: true),
                    CurrentVersion = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PROJECT_DOCUMENT", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PROJECT_DOCUMENT_CATEGORY_CategoryId",
                        column: x => x.CategoryId,
                        principalTable: "CATEGORY",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PROJECT_DOCUMENT_DOCUMENT_REVISION_CurrentRevisionId",
                        column: x => x.CurrentRevisionId,
                        principalTable: "DOCUMENT_REVISION",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PROJECT_DOCUMENT_PROJECT_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "PROJECT",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TEMPLATE",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CategoryId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    CurrentRevisionId = table.Column<Guid>(type: "uuid", nullable: true),
                    CurrentVersion = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TEMPLATE", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TEMPLATE_CATEGORY_CategoryId",
                        column: x => x.CategoryId,
                        principalTable: "CATEGORY",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TEMPLATE_REVISION",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TemplateId = table.Column<Guid>(type: "uuid", nullable: false),
                    Version = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    BumpType = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: true),
                    Content = table.Column<string>(type: "text", nullable: false),
                    AuthorId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TEMPLATE_REVISION", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TEMPLATE_REVISION_TEMPLATE_TemplateId",
                        column: x => x.TemplateId,
                        principalTable: "TEMPLATE",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TEMPLATE_REVISION_USER_AuthorId",
                        column: x => x.AuthorId,
                        principalTable: "USER",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CATEGORY_Code",
                table: "CATEGORY",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DOCUMENT_REVISION_AuthorId",
                table: "DOCUMENT_REVISION",
                column: "AuthorId");

            migrationBuilder.CreateIndex(
                name: "IX_DOCUMENT_REVISION_DocumentId",
                table: "DOCUMENT_REVISION",
                column: "DocumentId");

            migrationBuilder.CreateIndex(
                name: "IX_PROJECT_DOCUMENT_CategoryId",
                table: "PROJECT_DOCUMENT",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_PROJECT_DOCUMENT_CurrentRevisionId",
                table: "PROJECT_DOCUMENT",
                column: "CurrentRevisionId");

            migrationBuilder.CreateIndex(
                name: "IX_PROJECT_DOCUMENT_ProjectId",
                table: "PROJECT_DOCUMENT",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_PROJECT_DOCUMENT_SourceTemplateRevisionId",
                table: "PROJECT_DOCUMENT",
                column: "SourceTemplateRevisionId");

            migrationBuilder.CreateIndex(
                name: "IX_PROJECT_MEMBER_UserId",
                table: "PROJECT_MEMBER",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_TEMPLATE_CategoryId",
                table: "TEMPLATE",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_TEMPLATE_CurrentRevisionId",
                table: "TEMPLATE",
                column: "CurrentRevisionId");

            migrationBuilder.CreateIndex(
                name: "IX_TEMPLATE_REVISION_AuthorId",
                table: "TEMPLATE_REVISION",
                column: "AuthorId");

            migrationBuilder.CreateIndex(
                name: "IX_TEMPLATE_REVISION_TemplateId",
                table: "TEMPLATE_REVISION",
                column: "TemplateId");

            migrationBuilder.CreateIndex(
                name: "IX_USER_Email",
                table: "USER",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_USER_IDENTITY_Issuer_ExternalSubjectId",
                table: "USER_IDENTITY",
                columns: new[] { "Issuer", "ExternalSubjectId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_USER_IDENTITY_UserId",
                table: "USER_IDENTITY",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_DOCUMENT_REVISION_PROJECT_DOCUMENT_DocumentId",
                table: "DOCUMENT_REVISION",
                column: "DocumentId",
                principalTable: "PROJECT_DOCUMENT",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_PROJECT_DOCUMENT_TEMPLATE_REVISION_SourceTemplateRevisionId",
                table: "PROJECT_DOCUMENT",
                column: "SourceTemplateRevisionId",
                principalTable: "TEMPLATE_REVISION",
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

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DOCUMENT_REVISION_PROJECT_DOCUMENT_DocumentId",
                table: "DOCUMENT_REVISION");

            migrationBuilder.DropForeignKey(
                name: "FK_TEMPLATE_REVISION_USER_AuthorId",
                table: "TEMPLATE_REVISION");

            migrationBuilder.DropForeignKey(
                name: "FK_TEMPLATE_CATEGORY_CategoryId",
                table: "TEMPLATE");

            migrationBuilder.DropForeignKey(
                name: "FK_TEMPLATE_TEMPLATE_REVISION_CurrentRevisionId",
                table: "TEMPLATE");

            migrationBuilder.DropTable(
                name: "PROJECT_MEMBER");

            migrationBuilder.DropTable(
                name: "USER_IDENTITY");

            migrationBuilder.DropTable(
                name: "PROJECT_DOCUMENT");

            migrationBuilder.DropTable(
                name: "DOCUMENT_REVISION");

            migrationBuilder.DropTable(
                name: "PROJECT");

            migrationBuilder.DropTable(
                name: "USER");

            migrationBuilder.DropTable(
                name: "CATEGORY");

            migrationBuilder.DropTable(
                name: "TEMPLATE_REVISION");

            migrationBuilder.DropTable(
                name: "TEMPLATE");
        }
    }
}
