using GymManager.Infrastructure.Reports;
using Xunit;

namespace GymManager.UnitTests.Reports;

public sealed class ReportExporterTests
{
    private sealed record SampleRow(string Name, decimal Amount, DateOnly Date);

    private readonly ReportExporter _exporter = new();

    [Fact]
    public void ExportToExcel_Should_Produce_A_NonEmpty_Workbook()
    {
        var rows = new[]
        {
            new SampleRow("Alice", 49.99m, new DateOnly(2026, 1, 1)),
            new SampleRow("Bob", 19.99m, new DateOnly(2026, 1, 2)),
        };

        var bytes = _exporter.ExportToExcel("Sample Report", rows);

        Assert.NotEmpty(bytes);
    }

    [Fact]
    public void ExportToPdf_Should_Produce_A_NonEmpty_Document()
    {
        var rows = new[] { new SampleRow("Alice", 49.99m, new DateOnly(2026, 1, 1)) };

        var bytes = _exporter.ExportToPdf("Sample Report", rows);

        Assert.NotEmpty(bytes);
        Assert.Equal((byte)'%', bytes[0]);
    }

    [Fact]
    public void ExportToExcel_Should_Handle_Empty_Row_Set()
    {
        var bytes = _exporter.ExportToExcel("Empty Report", Array.Empty<SampleRow>());

        Assert.NotEmpty(bytes);
    }
}
