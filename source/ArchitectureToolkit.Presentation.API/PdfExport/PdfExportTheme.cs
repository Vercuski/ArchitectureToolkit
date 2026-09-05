using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace ArchitectureToolkit.Presentation.API.PdfExport;

/// <summary>
/// Shared visual constants for every page QuestPdfRenderer produces —
/// kept in one place so the master document's cover/TOC/contributors
/// pages and every per-document PDF read as one consistent export rather
/// than two unrelated tools stitched together.
/// </summary>
internal static class PdfExportTheme
{
    public const string HeadingFont = "Helvetica";
    public const string BodyFont = "Helvetica";
    public const string MonospaceFont = "Courier New";

    public static readonly Color MutedTextColor = Colors.Grey.Darken1;
    public static readonly Color RuleColor = Colors.Grey.Lighten2;
    public static readonly Color CodeBackground = Colors.Grey.Lighten4;

    public static void ApplyPageDefaults(PageDescriptor page)
    {
        page.Size(PageSizes.A4);
        page.Margin(2, Unit.Centimetre);
        page.PageColor(Colors.White);
        page.DefaultTextStyle(x => x.FontFamily(BodyFont).FontSize(11));
    }
}
