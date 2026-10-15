namespace QLBGX.Models.Dtos;

/// <summary>Một dòng kết quả của sp_DangNhap.</summary>
public record DangNhapDto(string MaNV, string TenNV, string ChucVu, string? MaCa, string? TenCa);
