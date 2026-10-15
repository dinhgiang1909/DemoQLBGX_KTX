namespace QLBGX.Helpers;

/// <summary>Lỗi nghiệp vụ do ứng dụng kiểm tra (hiện nguyên văn, không ghi log như lỗi hệ thống).</summary>
public class LoiNghiepVuException(string message) : Exception(message);
