namespace QLBGX.Helpers;

/// <summary>Phân quyền theo Bảng 3.1 của báo cáo. CSDL vẫn kiểm tra lại các quyền quan trọng.</summary>
public static class Permissions
{
    public static bool DanhMuc(Session s) => s.LaQuanLy;
    public static bool CapNhatCaTruc(Session s) => s.LaQuanLy;
    public static bool QuanLyThe(Session s) => s.LaQuanLy;          // khai báo, mở khóa thẻ; người khác chỉ xem
    public static bool XeVao(Session s) => s.DaDangNhap;
    public static bool XeRaThuTien(Session s) => s.LaQuanLy || s.LaThuNgan;
    public static bool GiaiQuyetChoXuLy(Session s) => s.LaQuanLy;
    public static bool SuaKhachHang(Session s) => s.LaQuanLy || s.LaThuNgan;  // Bảo vệ chỉ xem
    public static bool NghiepVuVeThang(Session s) => s.LaQuanLy || s.LaThuNgan;
    public static bool MoKhoaVeThang(Session s) => s.LaQuanLy;
    public static bool XemDoanhThu(Session s) => s.LaQuanLy || s.LaThuNgan;   // Thu ngân chỉ xem ca mình
    public static bool XacNhanChotCa(Session s) => s.LaQuanLy;
}
