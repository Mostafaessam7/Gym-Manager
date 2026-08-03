namespace GymManager.Application.Abstractions;

/// <summary>
/// Renders any flat, record-shaped report dataset as a downloadable file. Column headers and cell values
/// are derived by reflection from the row type's public properties, so individual reports never need their
/// own bespoke export code.
/// </summary>
public interface IReportExporter
{
    byte[] ExportToPdf<TRow>(string title, IReadOnlyCollection<TRow> rows);

    byte[] ExportToExcel<TRow>(string title, IReadOnlyCollection<TRow> rows);
}
