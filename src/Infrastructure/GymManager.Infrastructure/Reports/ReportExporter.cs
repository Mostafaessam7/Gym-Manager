using System.Reflection;
using ClosedXML.Excel;
using GymManager.Application.Abstractions;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace GymManager.Infrastructure.Reports;

/// <inheritdoc cref="IReportExporter"/>
public sealed class ReportExporter : IReportExporter
{
    static ReportExporter()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public byte[] ExportToPdf<TRow>(string title, IReadOnlyCollection<TRow> rows)
    {
        var properties = GetReportProperties<TRow>();

        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4.Landscape());
                page.Margin(30);
                page.DefaultTextStyle(style => style.FontSize(9));

                page.Header().Text(title).SemiBold().FontSize(16);

                page.Content().Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        foreach (var _ in properties)
                            columns.RelativeColumn();
                    });

                    table.Header(header =>
                    {
                        foreach (var property in properties)
                            header.Cell().Border(1).Background(Colors.Grey.Lighten3).Padding(4).Text(property.Name).SemiBold();
                    });

                    foreach (var row in rows)
                    {
                        foreach (var property in properties)
                            table.Cell().Border(1).Padding(4).Text(FormatValue(property.GetValue(row)));
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

    public byte[] ExportToExcel<TRow>(string title, IReadOnlyCollection<TRow> rows)
    {
        var properties = GetReportProperties<TRow>();

        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add(title.Length > 31 ? title[..31] : title);

        for (var column = 0; column < properties.Length; column++)
        {
            worksheet.Cell(1, column + 1).Value = properties[column].Name;
            worksheet.Cell(1, column + 1).Style.Font.Bold = true;
        }

        var rowIndex = 2;
        foreach (var row in rows)
        {
            for (var column = 0; column < properties.Length; column++)
            {
                var value = properties[column].GetValue(row);
                worksheet.Cell(rowIndex, column + 1).Value = value is null ? Blank.Value : XLCellValue.FromObject(value);
            }

            rowIndex++;
        }

        worksheet.Columns().AdjustToContents();

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);

        return stream.ToArray();
    }

    private static PropertyInfo[] GetReportProperties<TRow>() =>
        typeof(TRow).GetProperties(BindingFlags.Public | BindingFlags.Instance);

    private static string FormatValue(object? value) => value switch
    {
        null => string.Empty,
        DateTimeOffset dto => dto.ToString("g"),
        DateOnly d => d.ToString("d"),
        decimal dec => dec.ToString("0.00"),
        double dbl => dbl.ToString("0.00"),
        _ => value.ToString() ?? string.Empty,
    };
}
