using ArchitectureToolkit.Application.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using QuestPDF.Infrastructure;

namespace ArchitectureToolkit.Presentation.API.PdfExport;

/// <summary>
/// Wires IPdfRenderer to its QuestPDF-backed implementation and sets
/// QuestPDF's required license flag once, at startup — mirrors
/// IdentityAccountServiceRegistration's shape: its own small extension
/// rather than folded into AddPersistenceRegistrations()/
/// AddInfrastructureRegistration(), since neither Persistence nor
/// Infrastructure may reference the rendering library this needs (see
/// IPdfRenderer's own doc comment).
/// </summary>
public static class PdfExportServiceRegistration
{
    public static IHostApplicationBuilder AddPdfExportServices(this IHostApplicationBuilder builder)
    {
        // Community license — free under QuestPDF's revenue threshold
        // (questpdf.com/license has current terms). Revisit if
        // ArchitectureToolkit's usage ever falls outside it.
        QuestPDF.Settings.License = LicenseType.Community;

        builder.Services.AddSingleton<IPdfRenderer, QuestPdfRenderer>();

        return builder;
    }
}
