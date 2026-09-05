using ArchitectureToolkit.Application.Abstractions;
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;

namespace ArchitectureToolkit.Presentation.API.PdfExport;

/// <summary>
/// IPdfRenderer implemented via QuestPDF (ADR-0019) — chosen over a
/// headless-Chromium approach to keep this self-hosted deployment's
/// Docker image free of a bundled browser binary, consistent with the
/// zero-external-dependency default PersistedCertificateProvisioner's
/// own doc comment describes. Lives in Presentation.API, not
/// Infrastructure or Persistence — see IPdfRenderer's own doc comment
/// for why.
///
/// Markdown-to-PDF is hand-rolled (MarkdownPdfComposer) rather than via
/// an existing markdown-to-QuestPDF package, so it can special-case the
/// one thing a generic converter can't know about: resolving
/// attachment-download URLs into actually-embedded images (see
/// ExportedDocumentContent.InlineImages).
/// </summary>
public sealed class QuestPdfRenderer : IPdfRenderer
{
    public byte[] RenderCoverSection(ProjectExportManifest manifest)
    {
        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                PdfExportTheme.ApplyPageDefaults(page);
                page.Content().Column(column =>
                {
                    column.Item().AlignCenter().PaddingTop(8, QuestPDF.Infrastructure.Unit.Centimetre)
                        .Text(manifest.ProjectName).FontFamily(PdfExportTheme.HeadingFont).FontSize(28).Bold();
                    column.Item().AlignCenter().PaddingTop(0.5f, QuestPDF.Infrastructure.Unit.Centimetre)
                        .Text("Architecture Documentation Export").FontSize(14).FontColor(PdfExportTheme.MutedTextColor);
                    column.Item().AlignCenter().PaddingTop(2, QuestPDF.Infrastructure.Unit.Centimetre)
                        .Text($"Exported {manifest.ExportedAtUtc:yyyy-MM-dd HH:mm} UTC")
                        .FontSize(11).FontColor(PdfExportTheme.MutedTextColor);
                });
            });

            container.Page(page =>
            {
                PdfExportTheme.ApplyPageDefaults(page);
                page.Header().Text("Table of Contents").FontSize(18).Bold();
                page.Content().PaddingTop(0.5f, QuestPDF.Infrastructure.Unit.Centimetre).Column(column =>
                {
                    column.Spacing(8);
                    foreach (var category in manifest.Categories)
                    {
                        column.Item().Text(category.CategoryName).FontSize(13).Bold();
                        foreach (var doc in category.Documents)
                        {
                            column.Item().PaddingLeft(1, QuestPDF.Infrastructure.Unit.Centimetre).Text(doc.Title).FontSize(11);
                        }
                    }
                });
            });

            container.Page(page =>
            {
                PdfExportTheme.ApplyPageDefaults(page);
                page.Header().Text("Contributors").FontSize(18).Bold();
                page.Content().PaddingTop(0.5f, QuestPDF.Infrastructure.Unit.Centimetre).Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.RelativeColumn(2);
                        columns.RelativeColumn(3);
                    });

                    table.Cell().Element(HeaderCell).Text("Name");
                    table.Cell().Element(HeaderCell).Text("Email");

                    foreach (var contributor in manifest.Contributors)
                    {
                        table.Cell().Element(BodyCell).Text(contributor.Name);
                        table.Cell().Element(BodyCell).Text(contributor.Email);
                    }
                });
            });

            container.Page(page =>
            {
                PdfExportTheme.ApplyPageDefaults(page);
                page.Header().Text("Included Documents").FontSize(18).Bold();
                page.Content().PaddingTop(0.5f, QuestPDF.Infrastructure.Unit.Centimetre).Column(column =>
                {
                    column.Spacing(6);
                    foreach (var category in manifest.Categories)
                    {
                        column.Item().Text(category.CategoryName).FontSize(13).Bold();
                        foreach (var doc in category.Documents)
                        {
                            column.Item().PaddingLeft(1, QuestPDF.Infrastructure.Unit.Centimetre).Row(row =>
                            {
                                row.RelativeItem().Text(doc.Title);
                                row.RelativeItem().AlignRight().Text(doc.RelativePath)
                                    .FontFamily(PdfExportTheme.MonospaceFont).FontSize(9)
                                    .FontColor(PdfExportTheme.MutedTextColor);
                            });
                        }
                    }
                });
                page.Footer().AlignCenter().Text(text =>
                {
                    text.CurrentPageNumber();
                    text.Span(" / ");
                    text.TotalPages();
                });
            });
        });

        return document.GeneratePdf();
    }

    public byte[] RenderMarkdownDocument(ExportedDocumentContent content)
    {
        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                PdfExportTheme.ApplyPageDefaults(page);

                page.Header().Column(column =>
                {
                    column.Item().Text(content.Title).FontSize(18).Bold();
                    column.Item()
                        .Text($"{content.CategoryName} · v{content.Version} · Updated {content.LastUpdatedAtUtc:yyyy-MM-dd}")
                        .FontSize(9).FontColor(PdfExportTheme.MutedTextColor);
                    column.Item().PaddingTop(4).LineHorizontal(1).LineColor(PdfExportTheme.RuleColor);
                });

                page.Content().PaddingTop(0.5f, QuestPDF.Infrastructure.Unit.Centimetre).Column(column =>
                {
                    MarkdownPdfComposer.Compose(column, content.MarkdownContent, content.InlineImages);
                });

                page.Footer().AlignCenter().Text(text =>
                {
                    text.CurrentPageNumber();
                    text.Span(" / ");
                    text.TotalPages();
                });
            });
        });

        return document.GeneratePdf();
    }

    private static IContainer HeaderCell(IContainer container) =>
        container.Background(PdfExportTheme.CodeBackground).Padding(6)
            .BorderBottom(1).BorderColor(PdfExportTheme.RuleColor);

    private static IContainer BodyCell(IContainer container) =>
        container.Padding(6).BorderBottom(0.5f).BorderColor(PdfExportTheme.RuleColor);
}
