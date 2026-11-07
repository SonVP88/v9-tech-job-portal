/* =========================================================================================
   FILE: database_update_v2_business_rules.sql
   HỆ THỐNG: Quản lý tuyển dụng nhân sự khu công nghiệp tích hợp AI (ATS Industrial)
   MỤC ĐÍCH:
     1. Tạo bảng Danh sách đen ứng viên (CandidateBlacklists)
     2. Bổ sung cột kiểm soát chỉ tiêu & luồng cho bảng Jobs / JobPostings
     3. Bổ sung cột kiểm soát 7 Logic Nghiệp Vụ & Onboarding cho bảng Applications
     4. Chèn dữ liệu mẫu (Seed Data) phục vụ kiểm thử ngay trên giao diện
   ========================================================================================= */

SET NOCOUNT ON;
GO

-- =========================================================================================
-- 1. TẠO BẢNG DANH SÁCH ĐEN ỨNG VIÊN (CandidateBlacklists)
-- =========================================================================================
IF OBJECT_ID('dbo.CandidateBlacklists', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.CandidateBlacklists
    (
        Id              INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_CandidateBlacklists PRIMARY KEY,
        CitizenId       NVARCHAR(20)      NULL,
        PhoneNormalized NVARCHAR(20)      NULL,
        FullName        NVARCHAR(150)     NOT NULL,
        Reason          NVARCHAR(500)     NOT NULL,
        BanType         NVARCHAR(50)      NOT NULL CONSTRAINT DF_CandidateBlacklists_BanType DEFAULT ('PERMANENT'), -- 'PERMANENT' | 'TEMPORARY_6M'
        BannedUntil     DATETIME2         NULL,
        CreatedBy       NVARCHAR(100)     NULL,
        CreatedAt       DATETIME2         NOT NULL CONSTRAINT DF_CandidateBlacklists_CreatedAt DEFAULT (SYSUTCDATETIME())
    );

    CREATE INDEX IX_CandidateBlacklists_CitizenId
        ON dbo.CandidateBlacklists(CitizenId)
        WHERE CitizenId IS NOT NULL;

    CREATE INDEX IX_CandidateBlacklists_PhoneNormalized
        ON dbo.CandidateBlacklists(PhoneNormalized)
        WHERE PhoneNormalized IS NOT NULL;

    PRINT N'✅ Đã tạo bảng dbo.CandidateBlacklists và các Index tra cứu.';
END
ELSE
BEGIN
    PRINT N'ℹ️ Bảng dbo.CandidateBlacklists đã tồn tại.';
END
GO

-- =========================================================================================
-- 2. ALTER TABLE Jobs (và JobPostings nếu tồn tại) - BỔ SUNG CỘT CHỈ TIÊU & LUỒNG TUYỂN DỤNG
-- =========================================================================================
IF OBJECT_ID('dbo.Jobs', 'U') IS NOT NULL
BEGIN
    IF COL_LENGTH('dbo.Jobs', 'RecruitmentTrack') IS NULL
        ALTER TABLE dbo.Jobs ADD RecruitmentTrack NVARCHAR(30) NOT NULL CONSTRAINT DF_Jobs_RecruitmentTrack DEFAULT ('PROFESSIONAL');

    IF COL_LENGTH('dbo.Jobs', 'TargetQuantity') IS NULL
        ALTER TABLE dbo.Jobs ADD TargetQuantity INT NOT NULL CONSTRAINT DF_Jobs_TargetQuantity DEFAULT (50);

    IF COL_LENGTH('dbo.Jobs', 'HiredCount') IS NULL
        ALTER TABLE dbo.Jobs ADD HiredCount INT NOT NULL CONSTRAINT DF_Jobs_HiredCount DEFAULT (0);

    IF COL_LENGTH('dbo.Jobs', 'WaitlistBufferPercent') IS NULL
        ALTER TABLE dbo.Jobs ADD WaitlistBufferPercent INT NOT NULL CONSTRAINT DF_Jobs_WaitlistBufferPercent DEFAULT (15);

    IF COL_LENGTH('dbo.Jobs', 'CurrentScoringWeightVersion') IS NULL
        ALTER TABLE dbo.Jobs ADD CurrentScoringWeightVersion INT NOT NULL CONSTRAINT DF_Jobs_CurrentScoringWeightVersion DEFAULT (1);

    IF COL_LENGTH('dbo.Jobs', 'GoogleFormUrl') IS NULL
        ALTER TABLE dbo.Jobs ADD GoogleFormUrl NVARCHAR(500) NULL;

    PRINT N'✅ Đã cập nhật các cột kiểm soát chỉ tiêu & luồng cho bảng dbo.Jobs.';
END
GO

-- Hỗ trợ tương thích nếu DB có bảng tên JobPostings
IF OBJECT_ID('dbo.JobPostings', 'U') IS NOT NULL
BEGIN
    IF COL_LENGTH('dbo.JobPostings', 'RecruitmentTrack') IS NULL
        ALTER TABLE dbo.JobPostings ADD RecruitmentTrack NVARCHAR(30) NOT NULL CONSTRAINT DF_JobPostings_RecruitmentTrack DEFAULT ('PROFESSIONAL');

    IF COL_LENGTH('dbo.JobPostings', 'TargetQuantity') IS NULL
        ALTER TABLE dbo.JobPostings ADD TargetQuantity INT NOT NULL CONSTRAINT DF_JobPostings_TargetQuantity DEFAULT (50);

    IF COL_LENGTH('dbo.JobPostings', 'HiredCount') IS NULL
        ALTER TABLE dbo.JobPostings ADD HiredCount INT NOT NULL CONSTRAINT DF_JobPostings_HiredCount DEFAULT (0);

    IF COL_LENGTH('dbo.JobPostings', 'WaitlistBufferPercent') IS NULL
        ALTER TABLE dbo.JobPostings ADD WaitlistBufferPercent INT NOT NULL CONSTRAINT DF_JobPostings_WaitlistBufferPercent DEFAULT (15);

    IF COL_LENGTH('dbo.JobPostings', 'CurrentScoringWeightVersion') IS NULL
        ALTER TABLE dbo.JobPostings ADD CurrentScoringWeightVersion INT NOT NULL CONSTRAINT DF_JobPostings_CurrentScoringWeightVersion DEFAULT (1);

    IF COL_LENGTH('dbo.JobPostings', 'GoogleFormUrl') IS NULL
        ALTER TABLE dbo.JobPostings ADD GoogleFormUrl NVARCHAR(500) NULL;
END
GO

-- =========================================================================================
-- 3. ALTER TABLE Applications - BỔ SUNG CỘT KIỂM SOÁT NGHIỆP VỤ & ONBOARDING
-- =========================================================================================
IF OBJECT_ID('dbo.Applications', 'U') IS NOT NULL
BEGIN
    IF COL_LENGTH('dbo.Applications', 'ApplicationSource') IS NULL
        ALTER TABLE dbo.Applications ADD ApplicationSource NVARCHAR(30) NOT NULL CONSTRAINT DF_Applications_ApplicationSource DEFAULT ('PDF_UPLOAD');

    IF COL_LENGTH('dbo.Applications', 'ScoringMethod') IS NULL
        ALTER TABLE dbo.Applications ADD ScoringMethod NVARCHAR(30) NOT NULL CONSTRAINT DF_Applications_ScoringMethod DEFAULT ('GEMINI_AI');

    IF COL_LENGTH('dbo.Applications', 'CitizenId') IS NULL
        ALTER TABLE dbo.Applications ADD CitizenId NVARCHAR(20) NULL;

    IF COL_LENGTH('dbo.Applications', 'PhoneNormalized') IS NULL
        ALTER TABLE dbo.Applications ADD PhoneNormalized NVARCHAR(20) NULL;

    IF COL_LENGTH('dbo.Applications', 'DuplicateCount') IS NULL
        ALTER TABLE dbo.Applications ADD DuplicateCount INT NOT NULL CONSTRAINT DF_Applications_DuplicateCount DEFAULT (1);

    IF COL_LENGTH('dbo.Applications', 'PreviousApplicationId') IS NULL
        ALTER TABLE dbo.Applications ADD PreviousApplicationId INT NULL;

    IF COL_LENGTH('dbo.Applications', 'IsBlacklisted') IS NULL
        ALTER TABLE dbo.Applications ADD IsBlacklisted BIT NOT NULL CONSTRAINT DF_Applications_IsBlacklisted DEFAULT (0);

    IF COL_LENGTH('dbo.Applications', 'BlacklistWarningNote') IS NULL
        ALTER TABLE dbo.Applications ADD BlacklistWarningNote NVARCHAR(500) NULL;

    IF COL_LENGTH('dbo.Applications', 'IsScannedOrUnparseablePdf') IS NULL
        ALTER TABLE dbo.Applications ADD IsScannedOrUnparseablePdf BIT NOT NULL CONSTRAINT DF_Applications_IsScannedOrUnparseablePdf DEFAULT (0);

    IF COL_LENGTH('dbo.Applications', 'RawResumeText') IS NULL
        ALTER TABLE dbo.Applications ADD RawResumeText NVARCHAR(MAX) NULL;

    IF COL_LENGTH('dbo.Applications', 'AiAnalysisSummary') IS NULL
        ALTER TABLE dbo.Applications ADD AiAnalysisSummary NVARCHAR(MAX) NULL;

    IF COL_LENGTH('dbo.Applications', 'AppliedScoringVersion') IS NULL
        ALTER TABLE dbo.Applications ADD AppliedScoringVersion INT NULL CONSTRAINT DF_Applications_AppliedScoringVersion DEFAULT (1);

    IF COL_LENGTH('dbo.Applications', 'AppliedScoringConfigJson') IS NULL
        ALTER TABLE dbo.Applications ADD AppliedScoringConfigJson NVARCHAR(MAX) NULL;

    IF COL_LENGTH('dbo.Applications', 'OralInterviewChecklistJson') IS NULL
        ALTER TABLE dbo.Applications ADD OralInterviewChecklistJson NVARCHAR(MAX) NULL;

    IF COL_LENGTH('dbo.Applications', 'MedicalCheckStatus') IS NULL
        ALTER TABLE dbo.Applications ADD MedicalCheckStatus NVARCHAR(30) NOT NULL CONSTRAINT DF_Applications_MedicalCheckStatus DEFAULT ('Pending');

    IF COL_LENGTH('dbo.Applications', 'BankAccountNumber') IS NULL
        ALTER TABLE dbo.Applications ADD BankAccountNumber NVARCHAR(50) NULL;

    IF COL_LENGTH('dbo.Applications', 'BankName') IS NULL
        ALTER TABLE dbo.Applications ADD BankName NVARCHAR(100) NULL;

    IF COL_LENGTH('dbo.Applications', 'OnboardingDocsCompleted') IS NULL
        ALTER TABLE dbo.Applications ADD OnboardingDocsCompleted BIT NOT NULL CONSTRAINT DF_Applications_OnboardingDocsCompleted DEFAULT (0);

    IF COL_LENGTH('dbo.Applications', 'ConvertedEmployeeCode') IS NULL
        ALTER TABLE dbo.Applications ADD ConvertedEmployeeCode NVARCHAR(50) NULL;

    IF COL_LENGTH('dbo.Applications', 'SlaDeadlineAt') IS NULL
        ALTER TABLE dbo.Applications ADD SlaDeadlineAt DATETIME2 NULL;

    IF COL_LENGTH('dbo.Applications', 'IsSlaEscalated') IS NULL
        ALTER TABLE dbo.Applications ADD IsSlaEscalated BIT NOT NULL CONSTRAINT DF_Applications_IsSlaEscalated DEFAULT (0);

    IF COL_LENGTH('dbo.Applications', 'SlaOverrideReason') IS NULL
        ALTER TABLE dbo.Applications ADD SlaOverrideReason NVARCHAR(500) NULL;

    IF COL_LENGTH('dbo.Applications', 'RowVersion') IS NULL
        ALTER TABLE dbo.Applications ADD RowVersion ROWVERSION;

    PRINT N'✅ Đã cập nhật đầy đủ các cột kiểm soát nghiệp vụ & Onboarding cho bảng dbo.Applications.';
END
GO

-- Tạo các Index trên bảng Applications
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Applications_CitizenId' AND object_id = OBJECT_ID('dbo.Applications'))
BEGIN
    CREATE INDEX IX_Applications_CitizenId
        ON dbo.Applications(CitizenId)
        WHERE CitizenId IS NOT NULL;
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Applications_PhoneNormalized' AND object_id = OBJECT_ID('dbo.Applications'))
BEGIN
    CREATE INDEX IX_Applications_PhoneNormalized
        ON dbo.Applications(PhoneNormalized)
        WHERE PhoneNormalized IS NOT NULL;
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UX_Applications_ConvertedEmployeeCode' AND object_id = OBJECT_ID('dbo.Applications'))
BEGIN
    CREATE UNIQUE INDEX UX_Applications_ConvertedEmployeeCode
        ON dbo.Applications(ConvertedEmployeeCode)
        WHERE ConvertedEmployeeCode IS NOT NULL;
END
GO

-- =========================================================================================
-- 4. SEED DATA MẪU PHỤC VỤ KIỂM THỬ 7 LOGIC NGHIỆP VỤ & ONBOARDING
-- =========================================================================================

-- 4.1. Thêm 3 bản ghi mẫu vào CandidateBlacklists
IF NOT EXISTS (SELECT 1 FROM dbo.CandidateBlacklists WHERE CitizenId = '027098004512')
BEGIN
    INSERT INTO dbo.CandidateBlacklists (CitizenId, PhoneNormalized, FullName, Reason, BanType, BannedUntil, CreatedBy, CreatedAt)
    VALUES
    (N'027098004512', N'0984112990', N'Lê Văn Cường', N'Vi phạm kỷ luật an toàn lao động nghiêm trọng tại Xưởng SMT (Hút thuốc trong kho hóa chất)', N'PERMANENT', NULL, N'hr1@factory.com', SYSUTCDATETIME()),
    (N'001095008821', N'0913778221', N'Phạm Quốc Tuấn', N'Tự ý bỏ việc 5 ngày liên tiếp không bàn giao thiết bị đo kiểm tại Xưởng Bảo Trì', N'PERMANENT', NULL, N'truong.sx@factory.com', SYSUTCDATETIME()),
    (N'030201003319', N'0972665110', N'Hoàng Thị Mai', N'Nộp bằng cấp/chứng chỉ giả và nhờ người quẹt thẻ từ chấm công hộ', N'TEMPORARY_6M', DATEADD(MONTH, 6, SYSUTCDATETIME()), N'hr1@factory.com', SYSUTCDATETIME());

    PRINT N'✅ Đã seed 3 bản ghi mẫu vào bảng dbo.CandidateBlacklists.';
END
GO

-- 4.2. Seed chiến dịch mẫu & các hồ sơ ứng viên ở các trạng thái đặc biệt để test ngay
DECLARE @StageId UNIQUEIDENTIFIER = (SELECT TOP 1 StageId FROM dbo.PipelineStages ORDER BY SortOrder);
IF @StageId IS NULL
BEGIN
    SET @StageId = NEWID();
    INSERT INTO dbo.PipelineStages (StageId, Name, Code, SortOrder, IsTerminal)
    VALUES (@StageId, N'Tiếp nhận hồ sơ', N'APPLIED', 1, 0);
END

DECLARE @MassJobId UNIQUEIDENTIFIER = (SELECT TOP 1 JobId FROM dbo.Jobs WHERE Code = 'JOB-SMT-100');
IF @MassJobId IS NULL
BEGIN
    SET @MassJobId = '60000000-0000-0000-0000-000000000101';
    IF NOT EXISTS (SELECT 1 FROM dbo.Jobs WHERE JobId = @MassJobId)
    BEGIN
        INSERT INTO dbo.Jobs (JobId, Code, Title, Department, Location, EmploymentType, Status, CreatedAt, IsDeleted, NumberOfPositions, RecruitmentTrack, TargetQuantity, HiredCount, WaitlistBufferPercent, CurrentScoringWeightVersion, GoogleFormUrl)
        VALUES (@MassJobId, N'JOB-SMT-100', N'Công nhân vận hành dây chuyền SMT (Tuyển gấp 100 người)', N'Xưởng Sản Xuất', N'KCN Yên Phong - Bắc Ninh', N'Full-time', N'OPEN', SYSUTCDATETIME(), 0, 100, N'MASS_LABOR', 100, 98, 15, 2, N'https://docs.google.com/forms/d/e/1FAIpQLSd-SMT100/viewform');
    END
END
ELSE
BEGIN
    UPDATE dbo.Jobs
    SET RecruitmentTrack = N'MASS_LABOR',
        TargetQuantity = 100,
        HiredCount = 98,
        WaitlistBufferPercent = 15,
        CurrentScoringWeightVersion = 2
    WHERE JobId = @MassJobId;
END

DECLARE @ProJobId UNIQUEIDENTIFIER = (SELECT TOP 1 JobId FROM dbo.Jobs WHERE Code = 'JOB-ENG-012');
IF @ProJobId IS NULL
BEGIN
    SET @ProJobId = '60000000-0000-0000-0000-000000000102';
    IF NOT EXISTS (SELECT 1 FROM dbo.Jobs WHERE JobId = @ProJobId)
    BEGIN
        INSERT INTO dbo.Jobs (JobId, Code, Title, Department, Location, EmploymentType, Status, CreatedAt, IsDeleted, NumberOfPositions, RecruitmentTrack, TargetQuantity, HiredCount, WaitlistBufferPercent, CurrentScoringWeightVersion)
        VALUES (@ProJobId, N'JOB-ENG-012', N'Kỹ sư Tự động hóa PLC & Bảo trì Dây chuyền (Mitsubishi/Siemens)', N'Bộ Phận Kỹ Thuật & Bảo Trì', N'KCN Yên Phong - Bắc Ninh', N'Full-time', N'OPEN', SYSUTCDATETIME(), 0, 10, N'PROFESSIONAL', 10, 8, 15, 3);
    END
END
ELSE
BEGIN
    UPDATE dbo.Jobs
    SET RecruitmentTrack = N'PROFESSIONAL',
        TargetQuantity = 10,
        HiredCount = 8,
        WaitlistBufferPercent = 15,
        CurrentScoringWeightVersion = 3
    WHERE JobId = @ProJobId;
END

-- (A) Hồ sơ 1: Dính Blacklist (Rejected_Blacklisted)
DECLARE @CandBlacklist UNIQUEIDENTIFIER = '70000000-0000-0000-0000-000000000201';
IF NOT EXISTS (SELECT 1 FROM dbo.Candidates WHERE CandidateId = @CandBlacklist)
BEGIN
    INSERT INTO dbo.Candidates (CandidateId, FullName, Email, Phone, Location, Headline, Summary, Source, CreatedAt)
    VALUES (@CandBlacklist, N'Lê Văn Cường', N'cuong.le.blacklist@gmail.com', N'0984112990', N'Bắc Ninh', N'Công nhân vận hành máy', N'Đã từng làm việc tại xưởng SMT năm 2025', N'GOOGLE_FORM_QR', SYSUTCDATETIME());
END

IF NOT EXISTS (SELECT 1 FROM dbo.Applications WHERE JobId = @MassJobId AND CandidateId = @CandBlacklist)
BEGIN
    INSERT INTO dbo.Applications (
        ApplicationId, JobId, CandidateId, CurrentStageId, AppliedAt, LastStageChangedAt,
        Source, Status, ContactName, ContactEmail, ContactPhone,
        ApplicationSource, ScoringMethod, CitizenId, PhoneNormalized,
        DuplicateCount, IsBlacklisted, BlacklistWarningNote,
        RawResumeText, MedicalCheckStatus, OnboardingDocsCompleted, IsSlaEscalated
    )
    VALUES (
        '80000000-0000-0000-0000-000000000301', @MassJobId, @CandBlacklist, @StageId, SYSUTCDATETIME(), SYSUTCDATETIME(),
        N'GOOGLE_FORM_QR', N'Rejected_Blacklisted', N'Lê Văn Cường', N'cuong.le.blacklist@gmail.com', N'0984112990',
        N'GOOGLE_FORM_QR', N'RULE_ENGINE', N'027098004512', N'0984112990',
        2, 1, N'CẤM TUYỂN VĨNH VIỄN: Vi phạm kỷ luật an toàn lao động nghiêm trọng tại Xưởng SMT (Hút thuốc trong kho hóa chất)',
        N'Họ tên: Lê Văn Cường | CCCD: 027098004512 | SĐT: 0984112990', N'Pending', 0, 0
    );
END

-- (B) Hồ sơ 2: File PDF Scan dạng ảnh lỗi + Quá hạn SLA + Điểm trọng số cũ (Need_Manual_Review)
DECLARE @CandScanPdf UNIQUEIDENTIFIER = '70000000-0000-0000-0000-000000000202';
IF NOT EXISTS (SELECT 1 FROM dbo.Candidates WHERE CandidateId = @CandScanPdf)
BEGIN
    INSERT INTO dbo.Candidates (CandidateId, FullName, Email, Phone, Location, Headline, Summary, Source, CreatedAt)
    VALUES (@CandScanPdf, N'Trịnh Đình Quang', N'quang.trinh.plc@gmail.com', N'0904556778', N'Hà Nội', N'Kỹ sư Điện - Tự động hóa', N'File CV scan từ bản cứng', N'PDF_UPLOAD', SYSUTCDATETIME());
END

IF NOT EXISTS (SELECT 1 FROM dbo.Applications WHERE JobId = @ProJobId AND CandidateId = @CandScanPdf)
BEGIN
    INSERT INTO dbo.Applications (
        ApplicationId, JobId, CandidateId, CurrentStageId, AppliedAt, LastStageChangedAt,
        Source, Status, ContactName, ContactEmail, ContactPhone,
        ApplicationSource, ScoringMethod, CitizenId, PhoneNormalized,
        DuplicateCount, IsBlacklisted, IsScannedOrUnparseablePdf,
        RawResumeText, AiAnalysisSummary, AppliedScoringVersion, AppliedScoringConfigJson,
        MedicalCheckStatus, OnboardingDocsCompleted, SlaDeadlineAt, IsSlaEscalated
    )
    VALUES (
        '80000000-0000-0000-0000-000000000302', @ProJobId, @CandScanPdf, @StageId, DATEADD(DAY, -4, SYSUTCDATETIME()), SYSUTCDATETIME(),
        N'PDF_UPLOAD', N'Need_Manual_Review', N'Trịnh Đình Quang', N'quang.trinh.plc@gmail.com', N'0904556778',
        N'PDF_UPLOAD', N'GEMINI_AI', N'001097005543', N'0904556778',
        3, 0, 1,
        N'[SCANNED_IMAGE_PDF_18_CHARS]',
        N'File PDF dạng ảnh scan không có lớp văn bản, cần kiểm tra trực tiếp hoặc OCR.',
        1, N'{"skillsWeight":40,"experienceWeight":40,"educationWeight":20,"version":1}',
        N'Pending', 0, DATEADD(HOUR, -18, SYSUTCDATETIME()), 0
    );
END

-- (C) Hồ sơ 3: Đang chờ Khám sức khỏe Onboarding (InterviewPassed + MedicalCheckStatus = 'Pending')
DECLARE @CandOnboardingPending UNIQUEIDENTIFIER = '70000000-0000-0000-0000-000000000203';
IF NOT EXISTS (SELECT 1 FROM dbo.Candidates WHERE CandidateId = @CandOnboardingPending)
BEGIN
    INSERT INTO dbo.Candidates (CandidateId, FullName, Email, Phone, Location, Headline, Summary, Source, CreatedAt)
    VALUES (@CandOnboardingPending, N'Nguyễn Thị Thu Hà', N'thuha.smt@gmail.com', N'0968223445', N'Bắc Giang', N'Công nhân vận hành SMT', N'2 năm kinh nghiệm đứng máy gắn chip Yamaha', N'GOOGLE_FORM_QR', SYSUTCDATETIME());
END

IF NOT EXISTS (SELECT 1 FROM dbo.Applications WHERE JobId = @MassJobId AND CandidateId = @CandOnboardingPending)
BEGIN
    INSERT INTO dbo.Applications (
        ApplicationId, JobId, CandidateId, CurrentStageId, AppliedAt, LastStageChangedAt,
        Source, Status, ContactName, ContactEmail, ContactPhone,
        ApplicationSource, ScoringMethod, CitizenId, PhoneNormalized,
        DuplicateCount, IsBlacklisted, IsScannedOrUnparseablePdf,
        RawResumeText, OralInterviewChecklistJson,
        MedicalCheckStatus, BankAccountNumber, BankName, OnboardingDocsCompleted
    )
    VALUES (
        '80000000-0000-0000-0000-000000000303', @MassJobId, @CandOnboardingPending, @StageId, DATEADD(DAY, -2, SYSUTCDATETIME()), SYSUTCDATETIME(),
        N'GOOGLE_FORM_QR', N'InterviewPassed', N'Nguyễn Thị Thu Hà', N'thuha.smt@gmail.com', N'0968223445',
        N'GOOGLE_FORM_QR', N'RULE_ENGINE', N'024199006789', N'0968223445',
        1, 0, 0,
        N'Họ tên: Nguyễn Thị Thu Hà | Năm sinh: 1999 | Quê quán: Việt Yên, Bắc Giang | Sẵn sàng ca đêm 12h',
        N'{"HandEyePass":true,"VisionPass":true,"Shift12hConfirmed":true,"AttitudePass":true,"QuickNote":"Thao tác nhíp nhanh, mắt 10/10"}',
        N'Pending', N'19036677889901', N'Techcombank', 0
    );
END

-- (D) Hồ sơ 4: Đang nằm trong danh sách Dự bị (Waitlisted) - đã Đạt KSK & đủ CCCD, điểm cao sẵn sàng đôn lên
DECLARE @CandWaitlist UNIQUEIDENTIFIER = '70000000-0000-0000-0000-000000000204';
IF NOT EXISTS (SELECT 1 FROM dbo.Candidates WHERE CandidateId = @CandWaitlist)
BEGIN
    INSERT INTO dbo.Candidates (CandidateId, FullName, Email, Phone, Location, Headline, Summary, Source, CreatedAt)
    VALUES (@CandWaitlist, N'Đỗ Minh Tuấn', N'tuan.do.waitlist@gmail.com', N'0936998112', N'Thái Nguyên', N'Công nhân kỹ thuật chuyền SMT', N'3 năm vận hành lò hàn Reflow & AOI', N'WALK_IN', SYSUTCDATETIME());
END

IF NOT EXISTS (SELECT 1 FROM dbo.Applications WHERE JobId = @MassJobId AND CandidateId = @CandWaitlist)
BEGIN
    INSERT INTO dbo.Applications (
        ApplicationId, JobId, CandidateId, CurrentStageId, AppliedAt, LastStageChangedAt,
        Source, Status, ContactName, ContactEmail, ContactPhone,
        ApplicationSource, ScoringMethod, CitizenId, PhoneNormalized,
        DuplicateCount, IsBlacklisted, IsScannedOrUnparseablePdf,
        RawResumeText, OralInterviewChecklistJson,
        MedicalCheckStatus, BankAccountNumber, BankName, OnboardingDocsCompleted
    )
    VALUES (
        '80000000-0000-0000-0000-000000000304', @MassJobId, @CandWaitlist, @StageId, DATEADD(DAY, -1, SYSUTCDATETIME()), SYSUTCDATETIME(),
        N'WALK_IN', N'Waitlisted', N'Đỗ Minh Tuấn', N'tuan.do.waitlist@gmail.com', N'0936998112',
        N'WALK_IN', N'RULE_ENGINE', N'019098003456', N'0936998112',
        1, 0, 0,
        N'Họ tên: Đỗ Minh Tuấn | Kinh nghiệm: 3 năm vận hành lò hàn Reflow & máy kiểm tra ngoại quan AOI',
        N'{"HandEyePass":true,"VisionPass":true,"Shift12hConfirmed":true,"AttitudePass":true,"QuickNote":"Ứng viên xuất sắc, đưa vào danh sách dự bị ưu tiên số 1 do xưởng đang chạm trần định biên"}',
        N'Passed', N'1028839405', N'Vietcombank', 1
    );
END

PRINT N'🎉 Hoàn tất chạy script database_update_v2_business_rules.sql thành công!';
GO

