using Microsoft.EntityFrameworkCore;
using QLBGX.Models;

namespace QLBGX.Devices;

/// <summary>Một mục gợi ý trong ô quẹt thẻ.</summary>
public record GoiYThe(string MaThe, string MoTa)
{
    public string HienThi => string.IsNullOrEmpty(MoTa) ? MaThe : $"{MaThe}  –  {MoTa}";
    public override string ToString() => MaThe;
}

public enum NguCanhQuetThe
{
    XeVao,        // thẻ lượt Rảnh + thẻ tháng đang gắn vé tháng
    XeRa,         // thẻ đang gắn với lượt gửi trong bãi
    TheThangRanh, // đăng ký / cấp lại thẻ tháng
}

/// <summary>
/// Đầu đọc thẻ RFID. Đầu đọc thật hoạt động như bàn phím (gõ mã + Enter) nên giao diện dùng ComboBox cho phép gõ;
/// interface này chuẩn hóa mã thẻ đọc được và cung cấp danh sách gợi ý để demo nhanh.
/// </summary>
public interface ICardReader
{
    string ChuanHoaMaThe(string? raw);
    Task<IReadOnlyList<GoiYThe>> LayGoiYAsync(NguCanhQuetThe nguCanh);
}

public class SimulatedCardReader(IDbContextFactory<QuanLyBaiGiuXeKTXContext> dbFactory) : ICardReader
{
    private static readonly string[] TrongBai = ["Đang gửi", "Chờ xử lý"];

    public string ChuanHoaMaThe(string? raw) => (raw ?? string.Empty).Trim().ToUpperInvariant();

    public async Task<IReadOnlyList<GoiYThe>> LayGoiYAsync(NguCanhQuetThe nguCanh)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        switch (nguCanh)
        {
            case NguCanhQuetThe.XeVao:
            {
                var theLuot = await db.Thes.AsNoTracking()
                    .Where(t => t.LoaiThe == "Thẻ lượt" && t.TrangThai == "Rảnh")
                    .OrderBy(t => t.MaThe)
                    .Select(t => new { t.MaThe })
                    .ToListAsync();
                var theThang = await db.ThanhViens.AsNoTracking()
                    .OrderBy(tv => tv.MaThe)
                    .Select(tv => new { tv.MaThe, tv.MaXeNavigation.BienSo, tv.TrangThai, tv.NgayHetHan })
                    .ToListAsync();
                return
                [
                    .. theLuot.Select(t => new GoiYThe(t.MaThe, "Thẻ lượt – Rảnh")),
                    .. theThang.Select(t => new GoiYThe(t.MaThe,
                        $"Thẻ tháng – xe {t.BienSo} – {t.TrangThai} – hạn {t.NgayHetHan:dd/MM/yyyy}")),
                ];
            }
            case NguCanhQuetThe.XeRa:
            {
                var ds = await db.VeGuiXes.AsNoTracking()
                    .Where(v => TrongBai.Contains(v.TrangThai))
                    .OrderBy(v => v.MaThe)
                    .Select(v => new { v.MaThe, v.MaTheNavigation.LoaiThe, v.MaXeNavigation.BienSo, v.TrangThai, v.MaXe })
                    .ToListAsync();
                // Lượt vào bằng thẻ tháng: quẹt thẻ tháng hiện tại của thành viên (có thể đã cấp lại)
                var theThangHienTai = await db.ThanhViens.AsNoTracking()
                    .Where(tv => ds.Select(d => d.MaXe).Contains(tv.MaXe))
                    .ToDictionaryAsync(tv => tv.MaXe, tv => tv.MaThe);
                return ds.Select(d =>
                {
                    var ma = d.LoaiThe == "Thẻ tháng" && theThangHienTai.TryGetValue(d.MaXe, out var m) ? m : d.MaThe;
                    return new GoiYThe(ma, $"{d.LoaiThe} – xe {d.BienSo}" + (d.TrangThai == "Chờ xử lý" ? " – CHỜ XỬ LÝ" : ""));
                }).ToList();
            }
            default:
                return await db.Thes.AsNoTracking()
                    .Where(t => t.LoaiThe == "Thẻ tháng" && t.TrangThai == "Rảnh")
                    .OrderBy(t => t.MaThe)
                    .Select(t => new GoiYThe(t.MaThe, "Thẻ tháng – Rảnh"))
                    .ToListAsync();
        }
    }
}
