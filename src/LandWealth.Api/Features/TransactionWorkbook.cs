using ClosedXML.Excel;
using LandWealth.Application.Features.Transactions;
using LandWealth.Domain.Enums;

namespace LandWealth.Api.Features;

public static class TransactionWorkbook
{
    public static byte[] Build(IReadOnlyList<TransactionDto> transactions)
    {
        using var workbook = new XLWorkbook();
        var sheet = workbook.AddWorksheet("Transactions");
        var headers = new[] { "Date", "Type", "Description", "Status", "Amount", "Notes" };
        for (var column = 0; column < headers.Length; column++)
            sheet.Cell(1, column + 1).Value = headers[column];
        sheet.Row(1).Style.Font.Bold = true;

        for (var index = 0; index < transactions.Count; index++)
        {
            var transaction = transactions[index];
            var row = index + 2;
            sheet.Cell(row, 1).Value = transaction.TransactionDate.ToDateTime(TimeOnly.MinValue);
            sheet.Cell(row, 1).Style.DateFormat.Format = "yyyy-mm-dd";
            sheet.Cell(row, 2).Value = TypeLabel(transaction.TransactionType);
            sheet.Cell(row, 3).Value = transaction.Description;
            sheet.Cell(row, 4).Value = transaction.Status.ToString();
            sheet.Cell(row, 5).Value = transaction.Amount;
            sheet.Cell(row, 5).Style.NumberFormat.Format = "#,##0.00";
            sheet.Cell(row, 6).Value = transaction.Notes ?? string.Empty;
        }

        sheet.Columns().AdjustToContents();
        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    private static string TypeLabel(TransactionType type) => type switch
    {
        TransactionType.PropertyPurchase => "Property purchase",
        TransactionType.PropertyExpense => "Property expense",
        TransactionType.PropertyIncome => "Property income",
        TransactionType.PropertySale => "Property sale",
        TransactionType.LiabilityPayment => "Liability payment",
        _ => type.ToString()
    };
}
