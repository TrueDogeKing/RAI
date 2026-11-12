namespace ProjectDefense.ConsoleClient;

public static class ConsoleTable
{
    public static void Write(IReadOnlyList<string> headers, IEnumerable<IReadOnlyList<string>> rows)
    {
        var data = rows.ToList();
        var widths = headers
            .Select((header, column) => Math.Max(header.Length, data.Count == 0 ? 0 : data.Max(row => row[column].Length)))
            .ToArray();

        var separator = "+" + string.Join("+", widths.Select(w => new string('-', w + 2))) + "+";

        Console.WriteLine(separator);
        WriteRow(headers, widths);
        Console.WriteLine(separator);
        data.ForEach(row => WriteRow(row, widths));
        Console.WriteLine(separator);
    }

    private static void WriteRow(IReadOnlyList<string> cells, int[] widths)
        => Console.WriteLine("| " + string.Join(" | ", cells.Select((cell, column) => cell.PadRight(widths[column]))) + " |");
}
