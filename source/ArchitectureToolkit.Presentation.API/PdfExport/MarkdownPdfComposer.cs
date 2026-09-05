using ArchitectureToolkit.Application.Abstractions;
using Markdig;
using Markdig.Extensions.Tables;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;

namespace ArchitectureToolkit.Presentation.API.PdfExport;

/// <summary>
/// Walks a Markdig-parsed AST and emits the equivalent QuestPDF content
/// into an existing ColumnDescriptor. Not a general-purpose Markdown-to-PDF
/// converter — it covers exactly the block/inline types ToastUI's editor
/// actually produces (headings, paragraphs, emphasis, inline code, links,
/// images, lists, block quotes, fenced code blocks, tables, thematic
/// breaks) and falls back to plain text for anything else, rather than
/// silently dropping unrecognized content from an architecture document.
/// </summary>
internal static class MarkdownPdfComposer
{
    private static readonly MarkdownPipeline Pipeline = new MarkdownPipelineBuilder().UseAdvancedExtensions().Build();

    public static void Compose(
        ColumnDescriptor column, string markdownContent, IReadOnlyDictionary<string, ExportedImage> inlineImages)
    {
        var document = Markdown.Parse(markdownContent, Pipeline);

        foreach (var block in document)
        {
            ComposeBlock(column, block, inlineImages);
        }
    }

    private static void ComposeBlock(ColumnDescriptor column, Block block, IReadOnlyDictionary<string, ExportedImage> images)
    {
        switch (block)
        {
            case HeadingBlock heading:
                ComposeHeading(column, heading);
                break;

            case ParagraphBlock paragraph:
                ComposeParagraph(column, paragraph, images);
                break;

            case ListBlock list:
                ComposeList(column, list, images, depth: 0);
                break;

            case QuoteBlock quote:
                ComposeQuote(column, quote, images);
                break;

            case FencedCodeBlock or CodeBlock:
                ComposeCodeBlock(column, (LeafBlock)block);
                break;

            case Table table:
                ComposeTable(column, table);
                break;

            case ThematicBreakBlock:
                column.Item().PaddingVertical(6).LineHorizontal(1).LineColor(PdfExportTheme.RuleColor);
                break;

            case ContainerBlock container:
                foreach (var child in container)
                {
                    ComposeBlock(column, child, images);
                }
                break;

            default:
                if (block is LeafBlock { Inline: not null } leaf)
                {
                    column.Item().Text(ExtractPlainText(leaf.Inline));
                }
                break;
        }
    }

    private static void ComposeHeading(ColumnDescriptor column, HeadingBlock heading)
    {
        var fontSize = heading.Level switch
        {
            1 => 20,
            2 => 17,
            3 => 15,
            4 => 13,
            _ => 12,
        };

        column.Item().PaddingTop(10).PaddingBottom(4)
            .Text(text => ComposeInlines(text, heading.Inline, fontSize, bold: true));
    }

    private static void ComposeParagraph(
        ColumnDescriptor column, ParagraphBlock paragraph, IReadOnlyDictionary<string, ExportedImage> images)
    {
        // An image alone on its own line renders as a block-level figure,
        // not inline text — matches how ToastUI's viewer presents it.
        if (paragraph.Inline is { FirstChild: LinkInline { IsImage: true } soleImage } && soleImage.NextSibling is null)
        {
            ComposeImage(column, soleImage, images);
            return;
        }

        column.Item().PaddingBottom(6).Text(text => ComposeInlines(text, paragraph.Inline));
    }

    private static void ComposeList(
        ColumnDescriptor column, ListBlock list, IReadOnlyDictionary<string, ExportedImage> images, int depth)
    {
        var index = list.OrderedStart is not null && int.TryParse(list.OrderedStart, out var start) ? start : 1;

        foreach (var item in list)
        {
            if (item is not ListItemBlock listItem)
            {
                continue;
            }

            var marker = list.IsOrdered ? $"{index}." : "•";
            index++;

            column.Item().PaddingLeft(depth * 16).Row(row =>
            {
                row.ConstantItem(20).Text(marker);
                row.RelativeItem().Column(itemColumn =>
                {
                    foreach (var child in listItem)
                    {
                        if (child is ListBlock nestedList)
                        {
                            ComposeList(itemColumn, nestedList, images, depth + 1);
                        }
                        else
                        {
                            ComposeBlock(itemColumn, child, images);
                        }
                    }
                });
            });
        }
    }

    private static void ComposeQuote(
        ColumnDescriptor column, QuoteBlock quote, IReadOnlyDictionary<string, ExportedImage> images)
    {
        column.Item().PaddingBottom(6).BorderLeft(2).BorderColor(PdfExportTheme.RuleColor).PaddingLeft(10)
            .Column(inner =>
            {
                foreach (var child in quote)
                {
                    ComposeBlock(inner, child, images);
                }
            });
    }

    private static void ComposeCodeBlock(ColumnDescriptor column, LeafBlock codeBlock)
    {
        var code = codeBlock.Lines.ToString();

        column.Item().PaddingBottom(6).Background(PdfExportTheme.CodeBackground).Padding(8)
            .Text(code).FontFamily(PdfExportTheme.MonospaceFont).FontSize(9.5f);
    }

    private static void ComposeTable(ColumnDescriptor column, Table table)
    {
        column.Item().PaddingBottom(6).Table(questTable =>
        {
            var columnCount = table.OfType<TableRow>().FirstOrDefault()?.Count ?? 1;

            questTable.ColumnsDefinition(columns =>
            {
                for (var i = 0; i < columnCount; i++)
                {
                    columns.RelativeColumn();
                }
            });

            foreach (var rowItem in table)
            {
                if (rowItem is not TableRow row)
                {
                    continue;
                }

                foreach (var cellItem in row)
                {
                    if (cellItem is not TableCell cell)
                    {
                        continue;
                    }

                    var cellText = ExtractCellText(cell);
                    var isHeader = row.IsHeader;

                    questTable.Cell()
                        .Element(c => isHeader
                            ? c.Background(PdfExportTheme.CodeBackground).Padding(5).BorderBottom(1).BorderColor(PdfExportTheme.RuleColor)
                            : c.Padding(5).BorderBottom(0.5f).BorderColor(PdfExportTheme.RuleColor))
                        .Text(cellText).FontSize(9.5f).Bold();
                }
            }
        });
    }

    private static string ExtractCellText(TableCell cell) =>
        string.Join(" ", cell.OfType<ParagraphBlock>().Select(p => ExtractPlainText(p.Inline)));

    private static void ComposeImage(
        ColumnDescriptor column, LinkInline image, IReadOnlyDictionary<string, ExportedImage> images)
    {
        if (image.Url is not null && images.TryGetValue(image.Url, out var resolved))
        {
            column.Item().PaddingBottom(8).MaxWidth(400).Image(resolved.Content).FitWidth();
        }
        else
        {
            column.Item().PaddingBottom(8).Text($"[image unavailable: {ExtractPlainText(image)}]")
                .FontSize(9).Italic().FontColor(PdfExportTheme.MutedTextColor);
        }
    }

    private static void ComposeInlines(TextDescriptor text, ContainerInline? inline, float fontSize = 11, bool bold = false)
    {
        if (inline is null)
        {
            return;
        }

        foreach (var child in inline)
        {
            switch (child)
            {
                case LiteralInline literal:
                    ApplySpan(text.Span(literal.Content.ToString()), fontSize, bold);
                    break;

                case CodeInline code:
                    text.Span(code.Content).FontFamily(PdfExportTheme.MonospaceFont).FontSize(fontSize - 1);
                    break;

                case EmphasisInline emphasis:
                    ComposeInlines(text, emphasis, fontSize, bold || emphasis.DelimiterCount >= 2);
                    break;

                case LinkInline { IsImage: false } link:
                    // Hyperlink is a TextDescriptor factory method — a
                    // sibling of Span, not a TextSpanDescriptor modifier
                    // chained onto Span's result (that overload doesn't
                    // exist; see text.Hyperlink(displayText, url) in
                    // QuestPDF's own text-formatting samples).
                    var linkText = ExtractPlainText(link);
                    var linkSpan = !string.IsNullOrWhiteSpace(link.Url)
                        ? text.Hyperlink(linkText, link.Url)
                        : text.Span(linkText);
                    ApplySpan(linkSpan, fontSize, bold);
                    break;

                case LineBreakInline:
                    text.Line(string.Empty);
                    break;

                case ContainerInline container:
                    ComposeInlines(text, container, fontSize, bold);
                    break;

                default:
                    ApplySpan(text.Span(ExtractPlainText(child)), fontSize, bold);
                    break;
            }
        }
    }

    private static void ApplySpan(TextSpanDescriptor span, float fontSize, bool bold)
    {
        span.FontSize(fontSize);
        if (bold)
        {
            span.Bold();
        }
    }

    private static string ExtractPlainText(Inline? inline) => inline switch
    {
        null => string.Empty,
        LiteralInline literal => literal.Content.ToString(),
        ContainerInline container => string.Concat(container.Select(ExtractPlainText)),
        _ => string.Empty,
    };
}