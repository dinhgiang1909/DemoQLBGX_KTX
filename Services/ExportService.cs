using System.Collections;
using System.IO;
using System.Reflection;
using System.Text;
using ClosedXML.Excel;
using Microsoft.Win32;

namespace QLBGX.Services;

/// <summary>Một bảng cần xuất: tên sheet + các dòng (xuất mọi thuộc tính public đơn giản).</summary>
public record BangXuat(string TenSheet, IEnumerable Dong);

/// <summary>Xuất báo cáo ra Excel (ClosedXML) hoặc CSV.</summary>
public class ExportService
{
    /// <returns>Đường dẫn tệp đã lưu, null nếu người dùng hủy.</returns>
    public string? Xuat(string tenGoiY, params BangXuat[] bang)
    {
        var dlg = new SaveFileDialog
        {
            FileName = $"{tenGoiY}_{DateTime.Now:yyyyMMdd_HHmm}",
            Filter = "Excel (*.xlsx)|*.xlsx|CSV UTF-8 (*.csv)|*.csv",
            DefaultExt = ".xlsx",
        };
        if (dlg.ShowDialog() != true) return null;

        if (Path.GetExtension(dlg.FileName).Equals(".csv", StringComparison.OrdinalIgnoreCase))
            XuatCsv(dlg.FileName, bang);
        else
            XuatExcel(dlg.FileName, bang);
        return dlg.FileName;
    }

    private static void XuatExcel(string file, BangXuat[] bang)
    {
        using var wb = new XLWorkbook();
        foreach (var b in bang)
        {
            var ten = b.TenSheet.Length > 31 ? b.TenSheet[..31] : b.TenSheet;
            var ws = wb.Worksheets.Add(ten);
            var (cot, dong) = DocBang(b.Dong);
            for (var c = 0; c < cot.Count; c++)
            {
                ws.Cell(1, c + 1).Value = cot[c].Name;
                ws.Cell(1, c + 1).Style.Font.Bold = true;
                ws.Cell(1, c + 1).Style.Fill.BackgroundColor = XLColor.LightGray;
            }
            for (var r = 0; r < dong.Count; r++)
                for (var c = 0; c < cot.Count; c++)
                {
                    var cell = ws.Cell(r + 2, c + 1);
                    switch (cot[c].GetValue(dong[r]))
                    {
                        case null: break;
                        case decimal d: cell.Value = d; cell.Style.NumberFormat.Format = "#,##0"; break;
                        case int i: cell.Value = i; break;
                        case long l: cell.Value = l; break;
                        case DateTime dt: cell.Value = dt; cell.Style.DateFormat.Format = "dd/MM/yyyy HH:mm"; break;
                        case DateOnly d0: cell.Value = d0.ToDateTime(TimeOnly.MinValue); cell.Style.DateFormat.Format = "dd/MM/yyyy"; break;
                        case var v: cell.Value = v.ToString(); break;
                    }
                }
            ws.Columns().AdjustToContents();
        }
        wb.SaveAs(file);
    }

    private static void XuatCsv(string file, BangXuat[] bang)
    {
        var sb = new StringBuilder();
        foreach (var b in bang)
        {
            if (bang.Length > 1) sb.AppendLine("# " + b.TenSheet);
            var (cot, dong) = DocBang(b.Dong);
            sb.AppendLine(string.Join(",", cot.Select(c => Csv(c.Name))));
            foreach (var d in dong)
                sb.AppendLine(string.Join(",", cot.Select(c => Csv(Chuoi(c.GetValue(d))))));
            sb.AppendLine();
        }
        File.WriteAllText(file, sb.ToString(), new UTF8Encoding(true));
    }

    private static string Chuoi(object? v) => v switch
    {
        null => "",
        DateTime dt => dt.ToString("dd/MM/yyyy HH:mm"),
        DateOnly d => d.ToString("dd/MM/yyyy"),
        decimal d => d.ToString("0.##"),
        _ => v.ToString() ?? "",
    };

    private static string Csv(string s) => s.Contains(',') || s.Contains('"') || s.Contains('\n') ? $"\"{s.Replace("\"", "\"\"")}\"" : s;

    private static (List<PropertyInfo> Cot, List<object> Dong) DocBang(IEnumerable dong)
    {
        var ds = dong.Cast<object>().ToList();
        if (ds.Count == 0) return ([], ds);
        var cot = ds[0].GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.GetIndexParameters().Length == 0 && LaKieuDon(p.PropertyType))
            .ToList();
        return (cot, ds);
    }

    private static bool LaKieuDon(Type t)
    {
        t = Nullable.GetUnderlyingType(t) ?? t;
        return t.IsPrimitive || t.IsEnum || t == typeof(string) || t == typeof(decimal) || t == typeof(DateTime)
               || t == typeof(DateOnly) || t == typeof(TimeOnly);
    }
}
