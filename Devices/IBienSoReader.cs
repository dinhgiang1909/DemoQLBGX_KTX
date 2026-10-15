namespace QLBGX.Devices;

/// <summary>Camera ANPR: đọc biển số từ ảnh. Không đọc được thì trả chuỗi rỗng (không ném lỗi).</summary>
public interface IBienSoReader
{
    Task<string> DocBienSoAsync(string duongDanAnh);
}

/// <summary>Mặc định: lấy biển số từ tên tệp ảnh (ví dụ 59X112345.jpg → "59X112345").</summary>
public class ManualBienSoReader : IBienSoReader
{
    public Task<string> DocBienSoAsync(string duongDanAnh)
    {
        var ten = System.IO.Path.GetFileNameWithoutExtension(duongDanAnh);
        return Task.FromResult(Helpers.BienSoHelper.CoDangBienSo(ten) ? Helpers.BienSoHelper.ChuanHoa(ten) : string.Empty);
    }
}
