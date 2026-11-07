/* =========================================================================================
   FILE: database_update_v3_enterprise_compliance.sql
   HỆ THỐNG: Quản lý tuyển dụng nhân sự khu công nghiệp tích hợp AI (ATS Industrial)
   MỤC ĐÍCH:
     1. Bổ sung Phê duyệt định biên (Job Requisition) & Ràng buộc ca đêm cho bảng Jobs / JobPostings
     2. Bổ sung PII Masking, Điểm PV thực tế, Human Override, Đàm phán Offer & Điều chuyển xưởng cho bảng Applications
     3. Tạo Index tối ưu tra cứu
     4. Chèn/Cập nhật dữ liệu mẫu (Seed Data) kiểm thử ngay trên giao diện
   ========================================================================================= */

SET NOCOUNT ON;
GO

PRINT N'🚀 BẮT ĐẦU CẬP NHẬT DATABASE V3 - ENTERPRISE COMPLIANCE & 6 LOGIC NGHIỆP VỤ NÂNG CAO...';
GO

-- =========================================================================================
-- 1. ALTER TABLE Jobs (và JobPostings) - PHÊ DUYỆT ĐỊNH BIÊN & RÀNG BUỘC CA ĐÊM
-- =========================================================================================
IF OBJECT_ID('dbo.Jobs', 'U') IS NOT NULL
BEGIN
    -- 1.1 Trạng thái đề xuất / định biên: 'Draft' | 'Pending_Approval' | 'Approved' | 'Rejected_Budget'
    IF COL_LENGTH('dbo.Jobs', 'RequisitionStatus') IS NULL
        ALTER TABLE dbo.Jobs ADD RequisitionStatus NVARCHAR(30) NOT NULL CONSTRAINT DF_Jobs_RequisitionStatus DEFAULT ('Approved');

    -- 1.2 Người đề xuất tuyển dụng (Quản đốc xưởng)
    IF COL_LENGTH('dbo.Jobs', 'RequestedByUserId') IS NULL
    BEGIN
        IF EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Jobs' AND COLUMN_NAME = 'JobId' AND DATA_TYPE = 'uniqueidentifier')
            ALTER TABLE dbo.Jobs ADD RequestedByUserId UNIQUEIDENTIFIER NULL;
        ELSE
            ALTER TABLE dbo.Jobs ADD RequestedByUserId INT NULL;
    END

    -- 1.3 Người phê duyệt định biên ngân sách (Giám đốc / Trưởng phòng HR)
    IF COL_LENGTH('dbo.Jobs', 'ApprovedByUserId') IS NULL
    BEGIN
        IF EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Jobs' AND COLUMN_NAME = 'JobId' AND DATA_TYPE = 'uniqueidentifier')
            ALTER TABLE dbo.Jobs ADD ApprovedByUserId UNIQUEIDENTIFIER NULL;
        ELSE
            ALTER TABLE dbo.Jobs ADD ApprovedByUserId INT NULL;
    END

    -- 1.4 Thời điểm phê duyệt định biên
    IF COL_LENGTH('dbo.Jobs', 'ApprovedAt') IS NULL
        ALTER TABLE dbo.Jobs ADD ApprovedAt DATETIME2 NULL;

    -- 1.5 Lý do đề xuất tuyển dụng: Mở chuyền mới, bù nhân sự nghỉ việc...
    IF COL_LENGTH('dbo.Jobs', 'RequisitionNote') IS NULL
        ALTER TABLE dbo.Jobs ADD RequisitionNote NVARCHAR(500) NULL;

    -- 1.6 Bắt buộc làm ca đêm (22h-06h) hoặc dây chuyền nặng nhọc -> Chặn tuyệt đối lao động dưới 18 tuổi
    IF COL_LENGTH('dbo.Jobs', 'RequireNightShift') IS NULL
        ALTER TABLE dbo.Jobs ADD RequireNightShift BIT NOT NULL CONSTRAINT DF_Jobs_RequireNightShift DEFAULT (0);

    PRINT N'✅ Đã bổ sung các cột Phê duyệt định biên & Ca đêm vào dbo.Jobs.';
END
GO

-- Tương thích nếu DB sử dụng bảng tên JobPostings
IF OBJECT_ID('dbo.JobPostings', 'U') IS NOT NULL
BEGIN
    IF COL_LENGTH('dbo.JobPostings', 'RequisitionStatus') IS NULL
        ALTER TABLE dbo.JobPostings ADD RequisitionStatus NVARCHAR(30) NOT NULL CONSTRAINT DF_JobPostings_RequisitionStatus DEFAULT ('Approved');

    IF COL_LENGTH('dbo.JobPostings', 'RequestedByUserId') IS NULL
        ALTER TABLE dbo.JobPostings ADD RequestedByUserId INT NULL;

    IF COL_LENGTH('dbo.JobPostings', 'ApprovedByUserId') IS NULL
        ALTER TABLE dbo.JobPostings ADD ApprovedByUserId INT NULL;

    IF COL_LENGTH('dbo.JobPostings', 'ApprovedAt') IS NULL
        ALTER TABLE dbo.JobPostings ADD ApprovedAt DATETIME2 NULL;

    IF COL_LENGTH('dbo.JobPostings', 'RequisitionNote') IS NULL
        ALTER TABLE dbo.JobPostings ADD RequisitionNote NVARCHAR(500) NULL;

    IF COL_LENGTH('dbo.JobPostings', 'RequireNightShift') IS NULL
        ALTER TABLE dbo.JobPostings ADD RequireNightShift BIT NOT NULL CONSTRAINT DF_JobPostings_RequireNightShift DEFAULT (0);

    PRINT N'✅ Đã bổ sung các cột Phê duyệt định biên & Ca đêm vào dbo.JobPostings.';
END
GO

-- =========================================================================================
-- 2. ALTER TABLE Applications - NGÀY SINH, PII MASKING, INTERVIEW SCORE, OVERRIDE, OFFER & TRANSFER
-- =========================================================================================
IF OBJECT_ID('dbo.Applications', 'U') IS NOT NULL
BEGIN
    -- 2.1 Ngày/Tháng/Năm sinh chính xác để tính tuổi lao động đến từng ngày theo Bộ luật Lao động
    IF COL_LENGTH('dbo.Applications', 'DateOfBirth') IS NULL
        ALTER TABLE dbo.Applications ADD DateOfBirth DATE NULL;

    -- 2.2 Cờ xác nhận đã ẩn SĐT/CCCD/Email trước khi gửi lên Gemini API (Blind Screening / NĐ 13/2023/NĐ-CP)
    IF COL_LENGTH('dbo.Applications', 'IsPiiMaskedBeforeAi') IS NULL
        ALTER TABLE dbo.Applications ADD IsPiiMaskedBeforeAi BIT NOT NULL CONSTRAINT DF_Applications_IsPiiMaskedBeforeAi DEFAULT (1);

    -- 2.3 Điểm đánh giá chuyên môn thực tế khi phỏng vấn (tách biệt với AiMatchingScore để đối chiếu)
    IF COL_LENGTH('dbo.Applications', 'InterviewScore') IS NULL
        ALTER TABLE dbo.Applications ADD InterviewScore DECIMAL(5,2) NULL;

    -- 2.4 Cờ đánh dấu HR / Quản đốc bác bỏ gợi ý AI (Human Override để vớt hoặc loại hồ sơ)
    IF COL_LENGTH('dbo.Applications', 'IsAiOverriddenByHuman') IS NULL
        ALTER TABLE dbo.Applications ADD IsAiOverriddenByHuman BIT NOT NULL CONSTRAINT DF_Applications_IsAiOverriddenByHuman DEFAULT (0);

    -- 2.5 Lý do ghi đè kết quả AI (VD: "CV viết sơ sài nhưng có chứng chỉ nghề bậc 3 thực tế")
    IF COL_LENGTH('dbo.Applications', 'AiOverrideReason') IS NULL
        ALTER TABLE dbo.Applications ADD AiOverrideReason NVARCHAR(500) NULL;

    -- 2.6 Trạng thái Offer Letter: 'Not_Required' | 'Offer_Pending' | 'Offer_Sent' | 'Offer_Accepted' | 'Offer_Declined'
    IF COL_LENGTH('dbo.Applications', 'OfferStatus') IS NULL
        ALTER TABLE dbo.Applications ADD OfferStatus NVARCHAR(30) NOT NULL CONSTRAINT DF_Applications_OfferStatus DEFAULT ('Not_Required');

    -- 2.7 Mức lương thỏa thuận trong Offer Letter cho khối PROFESSIONAL (Kỹ sư / Văn phòng)
    IF COL_LENGTH('dbo.Applications', 'OfferedSalary') IS NULL
        ALTER TABLE dbo.Applications ADD OfferedSalary DECIMAL(18,2) NULL;

    -- 2.8 Ghi chú / Lý do ứng viên phản hồi Offer (Đồng ý hoặc lý do từ chối)
    IF COL_LENGTH('dbo.Applications', 'OfferResponseNote') IS NULL
        ALTER TABLE dbo.Applications ADD OfferResponseNote NVARCHAR(500) NULL;

    -- 2.9 Lưu vết JobId cũ nếu ứng viên được điều chuyển từ phân xưởng khác sang
    IF COL_LENGTH('dbo.Applications', 'TransferredFromJobId') IS NULL
    BEGIN
        IF EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Applications' AND COLUMN_NAME = 'JobId' AND DATA_TYPE = 'uniqueidentifier')
            ALTER TABLE dbo.Applications ADD TransferredFromJobId UNIQUEIDENTIFIER NULL;
        ELSE
            ALTER TABLE dbo.Applications ADD TransferredFromJobId INT NULL;
    END

    -- 2.10 Lý do điều chuyển phân xưởng
    IF COL_LENGTH('dbo.Applications', 'TransferReason') IS NULL
        ALTER TABLE dbo.Applications ADD TransferReason NVARCHAR(500) NULL;

    PRINT N'✅ Đã bổ sung các cột PII, InterviewScore, Human Override, Offer & Transfer vào dbo.Applications.';
END
GO

-- =========================================================================================
-- 3. TẠO INDEX TỐI ƯU TRA CỨU
-- =========================================================================================
IF OBJECT_ID('dbo.Jobs', 'U') IS NOT NULL
BEGIN
    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Jobs_RequisitionStatus' AND object_id = OBJECT_ID('dbo.Jobs'))
    BEGIN
        CREATE INDEX IX_Jobs_RequisitionStatus ON dbo.Jobs(RequisitionStatus);
        PRINT N'✅ Đã tạo Index IX_Jobs_RequisitionStatus.';
    END
END
GO

IF OBJECT_ID('dbo.Applications', 'U') IS NOT NULL
BEGIN
    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Applications_OfferStatus' AND object_id = OBJECT_ID('dbo.Applications'))
    BEGIN
        CREATE INDEX IX_Applications_OfferStatus ON dbo.Applications(OfferStatus);
        PRINT N'✅ Đã tạo Index IX_Applications_OfferStatus.';
    END

    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Applications_TransferredFromJobId' AND object_id = OBJECT_ID('dbo.Applications'))
    BEGIN
        CREATE INDEX IX_Applications_TransferredFromJobId ON dbo.Applications(TransferredFromJobId) WHERE TransferredFromJobId IS NOT NULL;
        PRINT N'✅ Đã tạo Index IX_Applications_TransferredFromJobId.';
    END
END
GO

-- =========================================================================================
-- 4. SEED DATA / DỮ LIỆU MẪU KIỂM THỬ TRÊN GIAO DIỆN
-- =========================================================================================
PRINT N'🌱 Đang thiết lập dữ liệu mẫu kiểm thử V3...';

-- 4.1 Cập nhật 1 Job mẫu thành ca đêm bắt buộc (RequireNightShift = 1) và 1 Job chờ duyệt định biên
IF OBJECT_ID('dbo.Jobs', 'U') IS NOT NULL
BEGIN
    -- Cập nhật Job đầu tiên có chữ "công nhân" hoặc "vận hành" thành RequireNightShift = 1
    UPDATE TOP (1) dbo.Jobs
    SET RequireNightShift = 1,
        RequisitionStatus = 'Approved',
        RequisitionNote = N'Tăng công suất xưởng chuyền 3, vận hành 3 ca 4 kíp bắt buộc ca đêm'
    WHERE IsDeleted = 0;

    -- Thêm 1 Job mẫu ở trạng thái 'Pending_Approval' nếu chưa có
    IF NOT EXISTS (SELECT 1 FROM dbo.Jobs WHERE RequisitionStatus = 'Pending_Approval')
    BEGIN
        INSERT INTO dbo.Jobs
        (
            JobId, Code, Title, Department, Description, Requirements, Benefits,
            NumberOfPositions, Status, CreatedAt, IsDeleted,
            RecruitmentTrack, TargetQuantity, HiredCount, WaitlistBufferPercent, CurrentScoringWeightVersion,
            RequisitionStatus, RequisitionNote, RequireNightShift
        )
        VALUES
        (
            NEWID(), 'REQ-2026-CHUYEN4', N'Công Nhân Đóng Gói Chuyền 4 (Đề Xuất Mới)', N'Xưởng Đóng Gói (PKG)',
            N'Đề xuất bổ sung 30 công nhân phục vụ kế hoạch xuất khẩu quý 4.',
            N'Tốt nghiệp THCS trở lên, sức khỏe loại 1 hoặc 2.',
            N'Lương 7.500.000 - 9.000.000 VNĐ, phụ cấp ca đêm, ăn trưa miễn phí.',
            30, 'PENDING_APPROVAL', SYSUTCDATETIME(), 0,
            'MASS_LABOR', 30, 0, 15, 1,
            'Pending_Approval', N'Đề xuất mở chuyền 4 bù đơn hàng gấp cho đối tác Samsung', 1
        );
        PRINT N'✅ Đã tạo Job mẫu chờ duyệt định biên: REQ-2026-CHUYEN4.';
    END
END
GO

-- 4.2 Cập nhật một số hồ sơ mẫu kiểm thử các trường hợp nghiệp vụ mới
IF OBJECT_ID('dbo.Applications', 'U') IS NOT NULL
BEGIN
    -- Cập nhật cờ PII Masking và InterviewScore cho các hồ sơ hiện có
    UPDATE dbo.Applications
    SET IsPiiMaskedBeforeAi = 1
    WHERE IsPiiMaskedBeforeAi IS NULL OR IsPiiMaskedBeforeAi = 0;

    -- Mẫu 1: Hồ sơ có Ghi đè AI (Human Override)
    UPDATE TOP (1) dbo.Applications
    SET IsAiOverriddenByHuman = 1,
        AiOverrideReason = N'Điểm AI thấp (42%) do CV scan sơ sài nhưng ứng viên có chứng chỉ nghề Cơ điện bậc 3 thực tế và kinh nghiệm 4 năm.',
        InterviewScore = 88.50
    WHERE Status IN ('ScheduledInterview', 'InterviewPassed', 'Submitted')
      AND IsBlacklisted = 0;

    -- Mẫu 2: Hồ sơ có Offer lương khối PROFESSIONAL (Đang chờ ứng viên chấp nhận)
    UPDATE TOP (1) dbo.Applications
    SET OfferStatus = 'Offer_Sent',
        OfferedSalary = 18500000.00,
        OfferResponseNote = N'Đã gửi Offer Letter qua email với mức lương 18.5M Net + thưởng KPI.'
    WHERE ApplicationSource = 'PDF_UPLOAD'
      AND Status = 'InterviewPassed';

    -- Mẫu 3: Hồ sơ điều chuyển từ phân xưởng khác sang
    UPDATE TOP (1) dbo.Applications
    SET TransferReason = N'Thị lực 8/10 không đạt chuẩn xưởng linh kiện điện tử SMT, điều chuyển sang Xưởng Kho Vận (WH_LOGS)',
        Status = 'Transferred_Pending'
    WHERE ApplicationSource = 'GOOGLE_FORM_QR'
      AND IsBlacklisted = 0;

    PRINT N'✅ Đã chèn dữ liệu mẫu test PII, Human Override, Offer Letter và Điều chuyển xưởng.';
END
GO

PRINT N'🎉 HOÀN TẤT CẬP NHẬT DATABASE V3 - ENTERPRISE COMPLIANCE THÀNH CÔNG!';
GO

