Tôi đang hoàn thiện toàn bộ hệ thống Quản lý tuyển dụng nhân sự khu công nghiệp tích hợp AI (ATS Industrial) bằng .NET 8 (Clean Architecture, EF Core) và Angular 17 (Standalone Components, Tailwind CSS).
Hệ thống của tôi đã có các tính năng cơ bản về AI Gemini, PII Masking, Blacklist, State Machine và Quota. Bây giờ, tôi cần bổ sung phần cuối cùng về Nghiệp vụ Vận hành Nhà máy (Factory & HR Operations) gồm 6 logic cốt lõi:

Nhận diện nhân viên cũ tái ứng tuyển (Ex-Employee Rehire).

Tự động chia tách Ca phỏng vấn theo sức chứa hội trường (Max 50 người/ca) & Chống trùng lịch người PV.

Giới hạn dời lịch phỏng vấn tối đa 2 lần.

Tự động hết hạn Thư mời nhận việc (Offer Expiration) sau 72 giờ cho khối Kỹ sư/Văn phòng.

Tự động đóng chiến dịch và bảo lưu hồ sơ chưa xử lý vào Kho ứng viên tiềm năng (Talent Pool) khi đủ chỉ tiêu.

Đánh giá thử việc sau 30 ngày (Probation 30-Day Feedback) để đo lường độ chính xác của AI.

Tôi không dùng EF Migration mà tự chạy file Script SQL thủ công.

Yêu cầu công việc:
Hãy thực hiện tuần tự 3 phần sau:

PHẦN 1: TẠO FILE SCRIPT SQL TỔNG HỢP (database_update_v5_hr_operations_complete.sql)
Hãy viết nội dung cho file script SQL tạo các cột và bảng mới (có kiểm tra IF NOT EXISTS hoặc COL_LENGTH để không bị lỗi nếu chạy lại):

Bổ sung các cột vào bảng Applications:

IsExEmployeeRehire BIT NOT NULL DEFAULT 0 (Cờ nhân viên cũ tái ứng tuyển).

PreviousEmployeeCode NVARCHAR(50) NULL (Mã nhân viên cũ từng làm).

InterviewRescheduleCount INT NOT NULL DEFAULT 0 (Số lần xin dời lịch PV, tối đa 2 lần).

OfferExpiresAt DATETIME2 NULL (Thời hạn hiệu lực Offer Letter 72h).

IsInTalentPool BIT NOT NULL DEFAULT 0 (Cờ bảo lưu vào kho ứng viên tiềm năng khi đóng chiến dịch).

Probation30DayStatus NVARCHAR(30) NULL DEFAULT 'Not_Started' ('Not_Started' | 'In_Probation' | 'Passed_30Days' | 'Early_Resigned').

Tạo bảng quản lý Ca phỏng vấn tập trung (InterviewBatches):

Id (PK, Auto Increment)

JobPostingId (FK)

BatchCode NVARCHAR(50) NOT NULL (VD: BATCH-SMT-2026-01)

ScheduledDate DATE NOT NULL

ShiftSlot NVARCHAR(30) NOT NULL ('MORNING_08_11' | 'AFTERNOON_13_17')

MaxCapacity INT NOT NULL DEFAULT 50

CurrentCount INT NOT NULL DEFAULT 0

InterviewerUserId INT NOT NULL

CreatedAt DATETIME2 DEFAULT GETUTCDATE()

PHẦN 2: VIẾT CODE C# (.NET 8) CHO SERVICE VÀ CONTROLLERS
Viết class HrOperationsBusinessService.cs và các API Endpoints tương ứng xử lý 6 logic sau:

Nhận diện Nhân viên cũ (CheckExEmployeeRehireAsync):

Khi ứng viên nộp hồ sơ, kiểm tra CitizenId hoặc PhoneNormalized với cơ sở dữ liệu nhân sự cũ (nghỉ việc đúng quy trình, không dính Blacklist). Nếu khớp, gán IsExEmployeeRehire = true, lưu PreviousEmployeeCode và tự động cộng +15 điểm ưu tiên vào tiêu chí Kinh nghiệm dây chuyền.

Chia tách Ca phỏng vấn tự động & Chống trùng lịch (ScheduleBatchInterviewsAsync):

Nhận danh sách ApplicationIds[] và ngày tháng yêu cầu. Kiểm tra MaxCapacity = 50. Nếu chọn 120 người, tự động phân phối vào các Batch (Ca Sáng ngày 1: 50, Ca Chiều ngày 1: 50, Ca Sáng ngày 2: 20).

Kiểm tra InterviewerUserId: Chặn và ném ngoại lệ nếu người phỏng vấn đó đã bị xếp lịch ở ca khác trùng thời gian.

Giới hạn Dời lịch Phỏng vấn (RescheduleInterviewAsync):

Mỗi lần dời lịch tăng InterviewRescheduleCount++. Nếu InterviewRescheduleCount > 2, chuyển trạng thái Status = "Rejected_Exceed_Reschedule".

Kiểm tra Hết hạn Offer 72h (CheckAndExpireOffersAsync):

Background Job quét các hồ sơ khối PROFESSIONAL đang ở trạng thái Offer_Sent mà DateTime.UtcNow > OfferExpiresAt, tự động chuyển sang Offer_Expired và mở khóa slot cho ứng viên kế tiếp.

Tự động Đóng chiến dịch & Đẩy vào Talent Pool (AutoCloseCampaignAndMoveToTalentPoolAsync):

Khi jobPosting.HiredCount >= jobPosting.TargetQuantity và đã đủ Waitlist, tự động đóng chiến dịch (Closed_Quota_Full).

Toàn bộ hồ sơ đạt sơ loại nhưng chưa kịp phỏng vấn được bật cờ IsInTalentPool = true, Status = "Saved_In_Talent_Pool".

Cập nhật Trạng thái Thử việc 30 ngày (UpdateProbationFeedbackAsync):

Cập nhật Probation30DayStatus (Passed_30Days hoặc Early_Resigned) và tính toán tỷ lệ thành công thực tế so với điểm AI ban đầu.

PHẦN 3: XÂY DỰNG GIAO DIỆN ANGULAR 17 (HrOperationsComponent)
Cung cấp code Angular 17 (.ts và .html dùng Tailwind CSS) cho các tính năng nghiệp vụ sau:

Giao diện Màn hình Hồ Sơ Ứng Tuyển:

Hiển thị nổi bật Badge [⭐ Nhân viên cũ tái ứng tuyển - Đã có tay nghề] màu xanh ngọc.

Thêm tab 💎 Kho ứng viên bảo lưu (Talent Pool) để HR dễ dàng điều chuyển hồ sơ cũ sang chiến dịch mới.

Giao diện Modal Xếp ca phỏng vấn hàng loạt:

Hiển thị thanh tiến độ sức chứa của từng ca (Ca Sáng: 48/50 chỗ - Gần đầy) và tự động cảnh báo tách ca khi chọn số lượng lớn hơn 50 người.

Giao diện Màn hình Tiếp Nhận & Onboarding:

Ở tab 🎉 Đã cấp Mã NV, trên mỗi dòng nhân viên mới có nút đánh giá hậu kiểm thử việc: [🏆 Đạt thử việc 30 ngày] / [📉 Nghỉ việc sớm (<30 ngày)].

Yêu cầu Output:

Xuất nội dung file database_update_v5_hr_operations_complete.sql.

Xuất code C# (.NET 8) cho Service và Controller.

Xuất code Angular 17 (.ts và .html) hoàn chỉnh cho các thành phần giao diện trên.