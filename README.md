# Quản lý bãi giữ xe Ký túc xá – chương trình demo (WPF)

Ứng dụng desktop demo cho đồ án môn Phân tích thiết kế yêu cầu. Toàn bộ quy tắc nghiệp vụ (QT1–QT19) nằm trong CSDL SQL Server (`QuanLyBaiGiuXeKTX.sql`). Ứng dụng làm giao diện và gọi đúng thủ tục/View, **không tự tính cước hay viết lại nghiệp vụ**.

## 1. Yêu cầu

- Windows 10/11, .NET SDK 10
- SQL Server 2016 trở lên (Express cũng được) và SSMS
- Visual Studio 2022/2026 (hoặc chạy bằng `dotnet run`)

## 2. Cài đặt và chạy

1. **Tạo CSDL:** mở `QuanLyBaiGiuXeKTX.sql` trong SSMS rồi nhấn F5. Script tự xóa CSDL cũ cùng tên rồi tạo lại kèm dữ liệu mẫu.
   > Mỗi lần demo có ghi dữ liệu thật. Muốn đưa dữ liệu về trạng thái ban đầu thì chạy lại script này.
2. **Chuỗi kết nối:** sửa `appsettings.json`, khóa `ConnectionStrings:BaiGiuXe`:
   ```json
   "BaiGiuXe": "Server=TEN_MAY\\SQLEXPRESS;Database=QuanLyBaiGiuXeKTX;Trusted_Connection=True;TrustServerCertificate=True;"
   ```
3. **Chạy:** mở `QLBGX.slnx` rồi nhấn F5, hoặc chạy lệnh `dotnet run` trong thư mục `QLBGX`.

## 3. Bật/tắt camera ANPR

Mặc định camera đọc biển số **từ tên tệp ảnh** (ví dụ `59X399988.jpg` cho ra `59X399988`). Thư mục `AnhMau/` có sẵn ảnh mẫu, hộp chọn ảnh mở sẵn thư mục này.

Để dùng chương trình nhận dạng biển số bên ngoài:

```json
"Anpr": { "Enabled": true, "ExePath": "Tools/ANPR/anpr.exe", "TimeoutMs": 8000 }
```

Chương trình nhận đường dẫn ảnh làm tham số và in biển số ra stdout (UTF-8). Nếu lỗi, quá thời gian hoặc không đọc được, ô Biển số để trống, lý do ghi vào `logs/app.log`, app vẫn chạy bình thường. Nhân viên luôn sửa được ô Biển số.

Ảnh chụp được lưu vào `AnhBaiXe/yyyyMMdd/V_yyyyMMdd_HHmmss_vao|ra.jpg` cạnh file chạy. CSDL lưu đường dẫn tương đối.

## 4. Tài khoản mẫu (mật khẩu đều là `123456`)

| Tài khoản | Mã | Chức vụ | Ca |
|---|---|---|---|
| `quanlm` | QL01 | Quản lý | Ca Sáng |
| `nhunglth` | TN01 | Thu ngân | Ca Sáng |
| `hungpv` | BV01 | Bảo vệ | Ca Sáng |
| `trangntt` | TN02 | Thu ngân | Ca Chiều |
| `datvt` | BV02 | Bảo vệ | Ca Chiều |
| `maidt` | TN03 | Thu ngân | Ca Đêm |
| `taihv` | BV03 | Bảo vệ | Ca Đêm |
| `locvt` | BV04 | Bảo vệ (đã nghỉ việc, không đăng nhập được) | – |

Phân quyền theo Bảng 3.1:
- Menu **Danh mục** chỉ Quản lý thấy.
- **Bảo vệ** không thu tiền được: nút Thanh toán bị khóa và hiện "chuyển Thu ngân xử lý". Bảo vệ chỉ xem khách hàng và vé tháng.
- **Thu ngân** chỉ xem doanh thu theo ca của mình. Tab Doanh thu tổng chỉ dành cho Quản lý.
- Mở khóa vé tháng, giải quyết lượt "Chờ xử lý", xác nhận chốt ca, khai báo, khóa và mở khóa thẻ: chỉ Quản lý.

CSDL vẫn kiểm tra lại các quyền này.

## 5. Phím tắt (màn hình Xe vào / Xe ra)

| Phím | Chức năng |
|---|---|
| F2 | Chụp ảnh (chọn tệp) |
| F5 | Tạo lượt gửi / Thanh toán và mở barrier |
| Esc | Làm mới form |
| Enter trong ô thẻ | Quẹt thẻ |

## 6. Kịch bản demo

Dữ liệu mẫu nằm trong khoảng 01–12/10/2026. Ở Tổng quan, chọn **Ngày xem** trong khoảng đó để thấy số liệu.

### Xe vào (đăng nhập `hungpv` hoặc `nhunglth`)
| # | Thao tác | Kết quả mong đợi |
|---|---|---|
| 1 | F2 chọn `AnhMau/59X399988.jpg`, loại *Xe máy thường*, quẹt `TL005`, F5 | Lượt mới ở KV_A, vé lượt, barrier xanh 3 giây |
| 2 | Biển `59X245678`, quẹt `TT002` | Khung vé tháng viền xanh "Khớp xe đăng ký", F5 thành công, vé tháng |
| 3 | Biển `59X300001`, quẹt `TT002` | Viền đỏ "Thẻ này thuộc xe 59X2-456.78…", F5 bị chặn **50011** |
| 4 | F2 chọn `59X100234.jpg`, quẹt `TT014` | Cảnh báo cam "Vé tháng không hợp lệ", F5 bị chặn **50011**. Quẹt thẻ lượt `TL006` thì vào được |
| 5 | Xe mới, quẹt `TL001` (thẻ lượt đang dùng) | **50012** |
| 6 | Biển `59X112345` (đang trong bãi) | Cảnh báo đỏ ngay khi nhập; F5 bị chặn **50010** |
| 7 | Không chọn ảnh, không ghi chú | **50005**. Ghi "Camera lỗi" thì vào được |
| 8 | Danh mục › Khu vực đỗ: giảm sức chứa KV_A bằng đúng số xe đang gửi, rồi cho xe máy vào | **50014** (nhớ trả lại sức chứa 1000 sau khi demo) |

### Xe ra (đăng nhập `nhunglth`, trừ khi ghi khác)
| # | Thao tác | Kết quả mong đợi |
|---|---|---|
| 1 | Quẹt `TL005` (lượt vừa vào ở kịch bản Xe vào 1), F2 chọn lại `59X399988.jpg` | Vé lượt khung Ngày, chọn KM02 giảm 20%, thanh toán, mở cửa sổ in hóa đơn |
| 2 | Quẹt `TL002` (xe `61C134455` vào 12/10) | Khung **Đêm**, N đêm × giá đêm |
| 3 | Tick **Khách mất thẻ**, nhập `59G155335` | Hiện phí mất thẻ; bắt buộc tick "Đã kiểm tra giấy tờ" (không tick thì **50027**) |
| 4 | Xe vào bằng ảnh `59X300001.jpg`; khi ra chọn ảnh `59X399989.jpg` | Biển số ra tô đỏ, phải tick giấy tờ |
| 5 | Quẹt một thẻ lượt bất kỳ, nhập/chụp biển số ra, bấm **Giữ xe**, nhập lý do | Lượt chuyển "Chờ xử lý" (lưu biển số + ảnh ra), barrier không mở. Chưa có biển số ra thì app yêu cầu nhập trước |
| 6 | `quanlm`: xe `62C145789` đang "Chờ xử lý". **Cách a (giống Bảng 4.21 dòng 9):** tick **Khách mất thẻ**, nhập `62C145789`, biển số ra `62C145789`. **Cách b:** khách còn thẻ, quẹt `TL003` | Hiện nút **Giải quyết & cho ra** (gọi `sp_XuLyXeRa` với @XacNhanGiayTo = 1). Cách a: hóa đơn **có phí mất thẻ**, TL003 bị khóa. Cách b: chỉ tính cước, TL003 về Rảnh. Thu ngân/Bảo vệ chỉ thấy "đang bị giữ" |
| 7 | Quẹt `TT016` (vé tháng hợp lệ) | 0 đồng, mở barrier |
| 8 | `hungpv` quẹt `TL002` | Báo trước "chuyển Thu ngân xử lý", nút Thanh toán bị khóa (CSDL: **50029**) |
| 9 | Tick mất thẻ, nhập `59X112345` (thẻ tháng TT001) | **50024**. Sang Khách hàng & Vé tháng › khách *Nguyễn Văn Thành* › vé `59X112345` › **Cấp lại thẻ** với `TT018`, rồi quay lại quẹt `TT018`: ra 0 đồng |

### Khách hàng & Vé tháng (`nhunglth` / `quanlm`)
- Thêm khách, sửa, **Ngừng** (chỉ đổi trạng thái, không xóa).
- **Đăng ký vé tháng:** chọn khách, nhập biển số, loại xe, số tháng, quẹt thẻ tháng Rảnh (TT018…TT030). Màn hình hiện trước số tiền và ngày hết hạn, rồi in hóa đơn.
- **Gia hạn:** gõ **mã thẻ tháng** (vd `TT001`) hoặc **biển số** (vd `59X1-123.45`) vào ô tìm, app tự chọn đúng khách và vé tháng. Màn hình hiện trước ngày hết hạn mới và ghi rõ "nối tiếp" hay "tính từ hôm nay" (QT11).
- **Khóa** (nhập lý do). **Mở khóa** chỉ Quản lý (vé số 9, thẻ TT009 đang bị khóa).
- **Lịch sử:** lượt gửi và hóa đơn của vé theo khoảng ngày; in lại hóa đơn.

### Chốt ca
- Nhân viên xem phiên hiện tại, nhập tiền mặt thực nộp. Có chênh lệch thì số hiện màu đỏ và bắt buộc ghi chú. Bấm Chốt ca rồi in biên bản.
- `quanlm`: danh sách phiên "Chờ xác nhận", bấm **Xác nhận**.
- Danh mục › Cập nhật ca trực: đổi ca cho nhân viên còn hóa đơn chưa chốt thì bị chặn **50084**.

### Danh mục, kho thẻ, báo cáo (`quanlm`)
- **Loại xe, Khu vực đỗ** (không giảm sức chứa dưới số xe đang gửi), **Khuyến mãi**, **Ca làm việc**.
- **Bảng giá:** chỉ thêm mới. Nhấp đúp một dòng cũ để lấy làm mẫu.
- **Nhân viên:** thêm nhân viên có mật khẩu (băm SHA-256 giống CSDL nên đăng nhập được ngay). **Cho nghỉ việc** sẽ bỏ ca qua `sp_CapNhatCaTruc` rồi đổi trạng thái.
- **Kho thẻ:** lọc thẻ, xem thẻ đang gắn với lượt gửi hay vé tháng nào, khai báo thẻ mới, **khóa thẻ** (hỏng/thất lạc, nhập lý do), mở khóa `TL013`.
  - Không khóa được thẻ gắn vé tháng (dùng Khóa vé tháng/Cấp lại thẻ) hoặc thẻ đang có xe trong bãi (dùng Khách mất thẻ ở Xe ra).
  - Bộ lọc **"Đang sử dụng – lệch dữ liệu"**: thẻ "Đang sử dụng" mà không có lượt gửi hay vé tháng. Xử lý: khóa rồi mở khóa để đưa về Rảnh.
  - Băng đỏ cảnh báo khi **hết thẻ lượt Rảnh** hoặc có thẻ lệch dữ liệu.
- **Báo cáo:**
  - Doanh thu: báo cáo từ `sp_BaoCaoDoanhThu` (3 bảng) kèm biểu đồ cột theo ngày.
  - Các tab còn lại: theo ca, tỷ lệ lấp đầy, xe tồn đọng (`59G299900`), lượt chờ xử lý, lệch biển số (có xem ảnh), vé tháng sắp hết hạn.
  - Nút **Xuất Excel/CSV** ở mỗi tab.
- **Tổng quan:** thẻ số liệu, thanh lấp đầy (khu Bảo trì màu xám), khối Cảnh báo bấm vào để mở màn hình tương ứng (gồm cả *Thẻ lượt Rảnh còn lại*: đỏ khi hết, cam khi < 5; và *thẻ lệch dữ liệu*). Tự làm mới mỗi 30 giây.
- **Xe vào:** báo ngay khi hết/sắp hết thẻ lượt Rảnh; quẹt thẻ "Đang sử dụng" mà không có lượt trong bãi thì báo "dữ liệu lệch – báo Quản lý".

## 7. Cấu trúc mã nguồn

```
Data/        Partial của DbContext (đã kiểm tra đủ HasTrigger)
Models/      Entity + DbContext do EF Core Power Tools sinh (không sửa); Dtos/ cho kết quả thủ tục
Services/    Gọi CSDL: thủ tục bằng ADO.NET (DbProc) cho nghiệp vụ ghi nhiều bảng, LINQ cho danh mục
Devices/     Mô phỏng thiết bị: IBienSoReader (Manual/Exe), ICardReader, IBarrier, ImageStore
ViewModels/  Một ViewModel cho mỗi màn hình (CommunityToolkit.Mvvm)
Views/       Cửa sổ và trang XAML (WPF-UI); code-behind chỉ xử lý con trỏ/phím
Helpers/     Session, Permissions, SqlErrorHelper, BienSoHelper, MoneyFormatter, converters
AnhMau/      Ảnh mẫu cho camera mô phỏng
```

Quy ước truy cập CSDL:
- Lỗi nghiệp vụ (mã 50001–50107) hiện **nguyên văn** thông điệp tiếng Việt của CSDL. Lỗi khác hiện "Lỗi hệ thống: …" và được ghi vào `logs/app.log`.
- Không gọi `Remove()` với các bảng xóa mềm. HoaDon chỉ đọc.
- Mỗi thao tác tạo một DbContext ngắn hạn qua `IDbContextFactory`.
