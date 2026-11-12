using System.Globalization;
using System.Text;
using ClosedXML.Excel;
using Microsoft.Extensions.Localization;
using ProjectDefense.Core.Dtos;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace ProjectDefense.Web.Services;

public enum ExportFormat
{
    Txt,
    Xlsx,
    Pdf
}

public sealed record ExportFile(byte[] Content, string ContentType, string FileName);

public interface IExportService
{
    Task<ExportFile> ExportAsync(ReservationFilter filter, ExportFormat format, CancellationToken cancellationToken = default);
}

public sealed class ExportService : IExportService
{
    private readonly IReservationService _reservationService;
    private readonly IStringLocalizer<SharedResource> _localizer;
    private readonly TimeProvider _timeProvider;

    public ExportService(IReservationService reservationService, IStringLocalizer<SharedResource> localizer, TimeProvider timeProvider)
    {
        _reservationService = reservationService;
        _localizer = localizer;
        _timeProvider = timeProvider;
    }

    public async Task<ExportFile> ExportAsync(ReservationFilter filter, ExportFormat format, CancellationToken cancellationToken = default)
    {
        var rows = await _reservationService.GetReservationsAsync(filter, cancellationToken);
        var stamp = _timeProvider.GetLocalNow().ToString("yyyyMMdd-HHmm", CultureInfo.InvariantCulture);
        var baseName = $"rezerwacje-{stamp}";

        return format switch
        {
            ExportFormat.Txt => new ExportFile(BuildText(rows), "text/plain; charset=utf-8", $"{baseName}.txt"),
            ExportFormat.Xlsx => new ExportFile(BuildWorkbook(rows), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"{baseName}.xlsx"),
            ExportFormat.Pdf => new ExportFile(BuildPdf(rows, filter), "application/pdf", $"{baseName}.pdf"),
            _ => throw new ArgumentOutOfRangeException(nameof(format))
        };
    }

    private string[] Headers() =>
    [
        _localizer["Room"],
        _localizer["Date"],
        _localizer["Time"],
        _localizer["Status"],
        _localizer["Student"],
        _localizer["StudentIndexNumber"],
        _localizer["Email"]
    ];

    private string[] Cells(ReservationDto row) =>
    [
        $"{row.RoomNumber} ({row.RoomName})",
        row.StartTime.ToString("d", CultureInfo.CurrentCulture),
        $"{row.StartTime:HH:mm}-{row.EndTime:HH:mm}",
        _localizer[$"Status{row.Status}"],
        row.StudentName ?? "-",
        row.StudentIndexNumber ?? "-",
        row.StudentEmail ?? "-"
    ];

    private byte[] BuildText(IReadOnlyList<ReservationDto> rows)
    {
        var table = new[] { Headers() }.Concat(rows.Select(Cells)).ToList();
        var widths = Enumerable.Range(0, table[0].Length)
            .Select(column => table.Max(line => line[column].Length))
            .ToArray();

        var builder = new StringBuilder();
        builder.AppendLine(_localizer["ExportTitle"]);
        builder.AppendLine(new string('=', widths.Sum() + 3 * (widths.Length - 1)));
        table.ForEach(line => builder.AppendLine(string.Join(" | ", line.Select((cell, column) => cell.PadRight(widths[column])))));
        builder.AppendLine();
        builder.AppendLine(_localizer["ExportCount", rows.Count]);

        return new UTF8Encoding(encoderShouldEmitUTF8Identifier: true).GetPreamble()
            .Concat(Encoding.UTF8.GetBytes(builder.ToString()))
            .ToArray();
    }

    private byte[] BuildWorkbook(IReadOnlyList<ReservationDto> rows)
    {
        using var workbook = new XLWorkbook();
        var sheet = workbook.Worksheets.Add(_localizer["Reservations"].Value);
        var headers = Headers();

        for (var column = 0; column < headers.Length; column++)
        {
            sheet.Cell(1, column + 1).Value = headers[column];
        }

        for (var row = 0; row < rows.Count; row++)
        {
            var cells = Cells(rows[row]);
            for (var column = 0; column < cells.Length; column++)
            {
                sheet.Cell(row + 2, column + 1).Value = cells[column];
            }
        }

        var header = sheet.Range(1, 1, 1, headers.Length);
        header.Style.Font.Bold = true;
        header.Style.Fill.BackgroundColor = XLColor.LightGray;
        sheet.SheetView.FreezeRows(1);
        if (rows.Count > 0)
        {
            sheet.Range(1, 1, rows.Count + 1, headers.Length).SetAutoFilter();
        }

        sheet.Columns().AdjustToContents();

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    private byte[] BuildPdf(IReadOnlyList<ReservationDto> rows, ReservationFilter filter)
    {
        var headers = Headers();
        var title = _localizer["ExportTitle"].Value;
        var generated = _localizer["ExportGenerated", _timeProvider.GetLocalNow().DateTime.ToString("g", CultureInfo.CurrentCulture)].Value;
        var countLine = _localizer["ExportCount", rows.Count].Value;
        var fromText = filter.From?.ToString("d", CultureInfo.CurrentCulture) ?? "...";
        var toText = filter.To?.ToString("d", CultureInfo.CurrentCulture) ?? "...";
        var range = filter.From is null && filter.To is null ? string.Empty : fromText + " - " + toText;

        return Document.Create(document =>
        {
            document.Page(page =>
            {
                page.Size(PageSizes.A4.Landscape());
                page.Margin(1.5f, Unit.Centimetre);
                page.DefaultTextStyle(style => style.FontSize(9));

                page.Header().Column(column =>
                {
                    column.Item().Text(title).FontSize(16).SemiBold();
                    column.Item().Text(generated).FontColor(Colors.Grey.Darken1);
                    if (range.Length > 0)
                    {
                        column.Item().Text(range).FontColor(Colors.Grey.Darken1);
                    }

                    column.Item().PaddingBottom(8);
                });

                page.Content().Column(column =>
                {
                    column.Item().Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.RelativeColumn(3);
                            columns.RelativeColumn(2);
                            columns.RelativeColumn(2);
                            columns.RelativeColumn(2);
                            columns.RelativeColumn(3);
                            columns.RelativeColumn(2);
                            columns.RelativeColumn(4);
                        });

                        table.Header(header =>
                        {
                            foreach (var text in headers)
                            {
                                header.Cell()
                                    .Background(Colors.Grey.Lighten2)
                                    .BorderBottom(1)
                                    .Padding(4)
                                    .Text(text)
                                    .SemiBold();
                            }
                        });

                        foreach (var row in rows)
                        {
                            foreach (var text in Cells(row))
                            {
                                table.Cell()
                                    .BorderBottom(0.5f)
                                    .BorderColor(Colors.Grey.Lighten1)
                                    .Padding(4)
                                    .Text(text);
                            }
                        }
                    });

                    column.Item().PaddingTop(8).Text(countLine);
                });

                page.Footer().AlignCenter().Text(text =>
                {
                    text.CurrentPageNumber();
                    text.Span(" / ");
                    text.TotalPages();
                });
            });
        }).GeneratePdf();
    }
}
