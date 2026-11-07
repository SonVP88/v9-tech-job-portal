# HƯỚNG DẪN QUY TRÌNH THU HÚT & ĐỒNG BỘ ỨNG VIÊN (QR CODE, GOOGLE FORMS, GOOGLE SHEETS & NGROK WEBHOOK)

Tài liệu này hướng dẫn chi tiết quy trình vận hành thu thập và tiếp nhận hồ sơ công nhân số lượng lớn (Mass Recruitment) cho hệ thống ATS, từ cách tiện lợi nhất thao tác hoàn toàn trên giao diện web đến cách chạy tự động hóa thời gian thực (Real-time).

---

## 📌 BẢNG SO SÁNH 3 PHƯƠNG THỨC TIẾP NHẬN

| Tiêu chí | Cách 1: Đồng bộ 1-Click từ Google Sheet | Cách 2: Form Web nội bộ ATS + Mã QR | Cách 3: Webhook Real-time (Google Forms + ngrok) |
| :--- | :--- | :--- | :--- |
| **Độ phức tạp** | ⭐ Rất dễ (Khuyên dùng khi demo/báo cáo) | ⭐ Rất dễ | ⭐⭐⭐ Nâng cao |
| **Cần ngrok?** | ❌ **Không** | ❌ **Không** (nếu chạy nội bộ) / Có (nếu quét từ 4G) | ✅ **Bắt buộc** |
| **Cần Google Script?** | ❌ **Không** | ❌ **Không** | ✅ **Có** (Trigger onFormSubmit) |
| **Nơi thao tác** | **100% trên Web ATS** | **100% trên Web ATS** | Web ATS + Google Apps Script |
| **Thời gian cập nhật** | Thủ công (bấm nút "Đồng bộ ngay") | Tức thì khi ứng viên bấm Gửi | Tức thì khi ứng viên bấm Gửi Form |

---

## 🚀 CÁCH 1: ĐỒNG BỘ 1-CLICK TỪ GOOGLE SHEET (TIỆN LỢI NHẤT)

> **Mục tiêu**: Công nhân quét QR điền Google Form, dữ liệu đổ về Google Sheet. HR chỉ cần bấm 1 nút trên Web ATS là hồ sơ được kéo về hệ thống mà không cần cài đặt ngrok hay lập trình script.

### Quy trình thực hiện:

#### Bước 1: Chuẩn bị Google Form & Google Sheet
1. Tạo một biểu mẫu trên [Google Forms](https://forms.google.com) với các câu hỏi:
   * Họ và tên *(Bắt buộc)*
   * Số điện thoại *(Bắt buộc)*
   * Năm sinh
   * Quê quán / Thường trú
   * Kinh nghiệm làm việc
   * Sẵn sàng làm việc xoay 3 ca / tăng ca *(Có / Không)*
   * Email *(nếu có)*
2. Chuyển sang tab **Câu trả lời (Responses)** &rarr; Bấm **Liên kết với Trang tính (Link to Sheets)** để tạo file Google Sheet nhận dữ liệu.

#### Bước 2: Bật quyền chia sẻ Google Sheet
1. Mở file Google Sheet vừa tạo &rarr; Bấm nút **Chia sẻ (Share)** ở góc trên bên phải.
2. Tại mục **Quyền truy cập chung (General access)**, đổi thành:
   * **Bất kỳ ai có đường liên kết (Anyone with the link)**
   * Quyền: **Người xem (Viewer)**
3. Bấm **Sao chép đường liên kết (Copy link)**.

#### Bước 3: Tạo mã QR Google Form trên Web ATS
1. Đăng nhập vào hệ thống ATS với tài khoản HR/Admin &rarr; Vào menu **Tin tuyển dụng**.
2. Tìm vị trí cần tuyển &rarr; Bấm icon **Mã QR chiến dịch**.
3. Tại tab **1. Form & Mã QR**:
   * Dán link biểu mẫu Google Form vào ô *Tùy chỉnh form tuyển dụng*.
   * Bấm **Lưu & Sinh QR**.
   * Bấm **Tải QR** để in ra poster, standee dán tại cổng nhà máy hoặc gửi cho cộng tác viên tuyển dụng.

#### Bước 4: Đồng bộ dữ liệu về ATS (Thao tác trên Web)
Khi có công nhân quét QR và điền Google Form:
1. Mở Web ATS &rarr; Vào **Quản lý hồ sơ ứng viên** của Job đó (hoặc bấm vào icon QR của Job).
2. Bấm nút **"Đồng bộ Google Sheet"** (hoặc tab *2. Đồng bộ Google Sheet*).
3. Dán link Google Sheet đã sao chép ở Bước 2 &rarr; Bấm **"Đồng bộ ngay"**.
4. Hệ thống sẽ:
   * Tự động đọc dữ liệu mới.
   * Chuẩn hóa và đối soát số điện thoại (tự động bỏ qua số bị trùng lặp).
   * Lưu hồ sơ ứng viên vào cơ sở dữ liệu với trạng thái **Sơ tuyển (PENDING / Sàng lọc)**.
   * Hiển thị bảng tổng kết: *Số hồ sơ nhập mới, Số trùng lặp, Danh sách ứng viên*.

---

## 📱 CÁCH 2: DÙNG FORM ỨNG TUYỂN WEB NỘI BỘ ATS (KHÔNG CẦN GOOGLE)

> **Mục tiêu**: Hệ thống ATS đã có sẵn trang ứng tuyển trực tuyến trên điện thoại tại `/apply/{jobId}`. Không phụ thuộc vào Google.

### Quy trình thực hiện:
1. **Lấy mã QR nội bộ**:
   * Vào **Quản lý tin tuyển dụng** &rarr; Bấm icon **Mã QR chiến dịch**.
   * Hệ thống mặc định đã tạo sẵn mã QR trỏ thẳng tới link: `http://localhost:4200/apply/{jobId}`.
   * Bấm **Tải QR**.
2. **Ứng viên nộp hồ sơ**:
   * Ứng viên quét mã QR bằng Zalo, Camera điện thoại &rarr; Mở giao diện ứng tuyển mobile-friendly của ATS.
   * Nhập Họ tên, SĐT, chọn làm xoay 3 ca &rarr; Bấm **Gửi đăng ký**.
3. **Kết quả**:
   * Hồ sơ ghi nhận thẳng vào Database ATS và thông báo đến HR tức thì.

---

## ⚡ CÁCH 3: TÍCH HỢP WEBHOOK REAL-TIME QUA NGROK

> **Mục tiêu**: Mỗi khi ứng viên bấm nút **Gửi (Submit)** trên Google Form, Google Apps Script sẽ lập tức gọi webhook chuyển tiếp dữ liệu về máy bạn qua ngrok.

### Bước 1: Khởi động ngrok trên máy tính
Mở Command Prompt / PowerShell và chạy lệnh mở cổng Backend (port 5222):
```bash
ngrok http 5222
```
*Giao diện ngrok sẽ xuất hiện đường dẫn công khai, ví dụ: `https://abcd-1234.ngrok-free.app`.*

### Bước 2: Cập nhật URL ngrok trên Web ATS
1. Đăng nhập ATS &rarr; Vào **Tin tuyển dụng** &rarr; Mở modal **QR chiến dịch**.
2. Chọn tab **3. Webhook Real-time (ngrok)**.
3. Dán link ngrok (ví dụ: `https://abcd-1234.ngrok-free.app`) vào ô **Public API URL**.
4. Bấm **Áp Dụng ngrok**. Hệ thống sẽ tự động cập nhật:
   * Webhook Endpoint: `https://abcd-1234.ngrok-free.app/api/applications/webhook-google-form`
   * Toàn bộ mã Google Apps Script bên dưới.

### Bước 3: Kiểm tra kết nối Webhook (Test Ping)
* Bấm nút **"Bắn Thử 1 Ứng Viên Mẫu Qua Webhook"** ngay trên giao diện ATS.
* Nếu thông báo xanh **"✅ Tiếp nhận ứng viên qua Google Forms thành công"** nghĩa là Webhook đã sẵn sàng nhận dữ liệu thật từ Google!

### Bước 4: Cài đặt Trigger trên Google Form / Sheet
1. Mở Google Form hoặc file Google Sheet liên kết.
2. Trên thanh menu, chọn: **Tiện ích mở rộng (Extensions)** &rarr; **Apps Script**.
3. Bấm nút **"Copy Mã"** trên Web ATS và dán toàn bộ vào file `Code.gs`:

```javascript
function onFormSubmit(e) {
  const named = e.namedValues || {};
  const first = (key) => (named[key] && named[key][0]) ? named[key][0] : '';

  const payload = {
    jobId: '{{JOB_ID_CỦA_BẠN}}',
    fullName: first('Họ và tên'),
    phone: first('Số điện thoại'),
    yearOfBirth: first('Năm sinh'),
    hometown: first('Quê quán / Thường trú'),
    experienceNotes: first('Kinh nghiệm làm việc'),
    shiftReady: first('Sẵn sàng làm việc xoay 3 ca / tăng ca'),
    email: first('Email (nếu có)')
  };

  const webhookUrl = 'https://YOUR-NGROK-DOMAIN.ngrok-free.app/api/applications/webhook-google-form';

  const options = {
    method: 'post',
    contentType: 'application/json',
    headers: {
      'X-Webhook-Secret': 'ats_google_webhook_secret_2026',
      'ngrok-skip-browser-warning': 'true' // Bắt buộc để vượt qua trang cảnh báo của ngrok
    },
    payload: JSON.stringify(payload),
    muteHttpExceptions: true
  };

  try {
    const response = UrlFetchApp.fetch(webhookUrl, options);
    Logger.log('ATS Webhook status: ' + response.getResponseCode());
    Logger.log('ATS Webhook response: ' + response.getContentText());
  } catch (err) {
    Logger.log('Lỗi gọi ATS Webhook: ' + err.toString());
  }
}
```
4. Bấm biểu tượng đĩa mềm **Lưu dự án (Ctrl + S)**.
5. Ở menu bên trái, bấm vào icon đồng hồ **Kích hoạt (Triggers)** &rarr; Bấm **Thêm trình kích hoạt (Add Trigger)** ở góc dưới bên phải:
   * **Chọn hàm để chạy**: `onFormSubmit`
   * **Nguồn sự kiện**: `Từ biểu mẫu (From form)` hoặc `Từ bảng tính (From spreadsheet)`
   * **Loại sự kiện**: `Khi gửi biểu mẫu (On form submit)`
6. Bấm **Lưu (Save)** và chấp nhận quyền truy cập tài khoản khi Google yêu cầu.

---

## 🛡️ CÁC HEADERS BẢO MẬT & ĐẶC THÙ

| Header | Giá trị | Mục đích |
| :--- | :--- | :--- |
| `Content-Type` | `application/json` | Định dạng dữ liệu JSON gửi lên API |
| `X-Webhook-Secret` | `ats_google_webhook_secret_2026` | Token bí mật xác thực nguồn gửi từ Google, chống giả mạo request |
| `ngrok-skip-browser-warning` | `true` | **Rất quan trọng cho ngrok free**: Chặn ngrok trả về trang cảnh báo HTML, đảm bảo request đi thẳng vào ASP.NET Core |

---

## 🔍 CƠ CHẾ LỌC TRÙNG LẶP (DE-DUPLICATION)

Hệ thống tích hợp sẵn cơ chế chống trùng ứng viên trong cùng một chiến dịch tuyển dụng:
1. **Chuẩn hóa số điện thoại**: Tự động loại bỏ dấu cách, dấu chấm, dấu gạch nối và mã quốc gia `+84` &rarr; Chuẩn hóa thành dạng `09xxxxxxxx`.
2. **Kiểm tra trùng**: Nếu số điện thoại đã tồn tại trong danh sách ứng tuyển của Job đó, hệ thống sẽ:
   * Từ chối ghi đè hồ sơ cũ.
   * Ghi nhận vào danh sách `DuplicateCount` và thông báo rõ trên giao diện.

---

## 🛠️ XỬ LÝ LỖI THƯỜNG GẶP (TROUBLESHOOTING)

1. **Lỗi: *"Không thể đọc dữ liệu từ Google Sheet"***:
   * *Nguyên nhân*: File Google Sheet đang để chế độ "Hạn chế" (Restricted).
   * *Khắc phục*: Vào nút **Chia sẻ (Share)** trên Google Sheet &rarr; Đổi thành **"Bất kỳ ai có đường liên kết đều có thể xem (Viewer)"**.

2. **Lỗi: Google Apps Script báo `401 Unauthorized`**:
   * *Nguyên nhân*: Header `X-Webhook-Secret` không khớp với giá trị cấu hình trong `appsettings.json`.
   * *Khắc phục*: Kiểm tra token trong `appsettings.json` (`GoogleIntegration:WebhookSecret`) và cập nhật lại trong Apps Script.

3. **Lỗi: Webhook trả về trang HTML cảnh báo ngrok**:
   * *Nguyên nhân*: Thiếu header `ngrok-skip-browser-warning: 'true'`.
   * *Khắc phục*: Mã Apps Script sinh ra trên Web ATS đã có sẵn dòng này, bạn chỉ cần copy lại đúng mẫu.

