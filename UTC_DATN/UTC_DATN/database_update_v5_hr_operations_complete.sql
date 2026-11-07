/* =========================================================================================
   FILE: database_update_v5_hr_operations_complete.sql
   HỆ THỐNG: Quản lý tuyển dụng nhân sự khu công nghiệp tích hợp AI (ATS Industrial)
   MỤC ĐÍCH:
     1. Bổ sung các cột nghiệp vụ Vận hành Nhà máy (Factory & HR Operations) vào bảng Applications
     2. Tạo bảng Quản lý Ca phỏng vấn tập trung (InterviewBatches)
     3. Tạo bảng lưu trữ Nhân sự cũ đã nghỉ việc (ExEmployees) để phục vụ nhận diện Rehire
     4. Chèn dữ liệu mẫu (Seed Data) kiểm thử ngay trên giao diện
   ========================================================================================= */

SET NOCOUNT ON;
GO

PRINT N'🚀 BẮT ĐẦU CẬP NHẬT DATABASE V5 - HR OPERATIONS COMPLETE...';
GO

-- =========================================================================================
-- 1. ALTER TABLE Applications - BỔ SUNG CÁC CỘT VẬN HÀNH NHÀ MÁY
-- =========================================================================================
IF OBJECT_ID('dbo.Applications', 'U') IS NOT NULL
BEGIN
    -- 1.1 Cờ nhân viên cũ tái ứng tuyển
    IF COL_LENGTH('dbo.Applications', 'IsExEmployeeRehire') IS NULL
        ALTER TABLE dbo.Applications ADD IsExEmployeeRehire BIT NOT NULL CONSTRAINT DF_Applications_IsExEmployeeRehire DEFAULT (0);

    -- 1.2 Mã nhân viên cũ từng làm việc tại nhà máy
    IF COL_LENGTH('dbo.Applications', 'PreviousEmployeeCode') IS NULL
        ALTER TABLE dbo.Applications ADD PreviousEmployeeCode NVARCHAR(50) NULL;

    -- 1.3 Số lần xin dời lịch phỏng vấn (Tối đa 2 lần)
    IF COL_LENGTH('dbo.Applications', 'InterviewRescheduleCount') IS NULL
        ALTER TABLE dbo.Applications ADD InterviewRescheduleCount INT NOT NULL CONSTRAINT DF_Applications_InterviewRescheduleCount DEFAULT (0);

    -- 1.4 Thời hạn hiệu lực Offer Letter (72h)
    IF COL_LENGTH('dbo.Applications', 'OfferExpiresAt') IS NULL
        ALTER TABLE dbo.Applications ADD OfferExpiresAt DATETIME2 NULL;

    -- 1.5 Cờ bảo lưu vào kho ứng viên tiềm năng khi chiến dịch đóng
    IF COL_LENGTH('dbo.Applications', 'IsInTalentPool') IS NULL
        ALTER TABLE dbo.Applications ADD IsInTalentPool BIT NOT NULL CONSTRAINT DF_Applications_IsInTalentPool DEFAULT (0);

    -- 1.6 Đánh giá thử việc sau 30 ngày: 'Not_Started' | 'In_Probation' | 'Passed_30Days' | 'Early_Resigned'
    IF COL_LENGTH('dbo.Applications', 'Probation30DayStatus') IS NULL
        ALTER TABLE dbo.Applications ADD Probation30DayStatus NVARCHAR(30) NULL CONSTRAINT DF_Applications_Probation30DayStatus DEFAULT ('Not_Started');

    -- 1.7 Khóa ngoại liên kết tới Ca phỏng vấn tập trung (InterviewBatchId)
    IF COL_LENGTH('dbo.Applications', 'InterviewBatchId') IS NULL
        ALTER TABLE dbo.Applications ADD InterviewBatchId INT NULL;

    PRINT N'✅ Đã bổ sung các cột Vận hành Nhà máy vào dbo.Applications.';
END
GO

-- =========================================================================================
-- 2. TẠO BẢNG QUẢN LÝ CA PHỎNG VẤN TẬP TRUNG (InterviewBatches)
-- =========================================================================================
IF OBJECT_ID('dbo.InterviewBatches', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.InterviewBatches
    (
        Id                  INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_InterviewBatches PRIMARY KEY,
        JobId               UNIQUEIDENTIFIER  NULL, -- Tương thích với JobId trong hệ thống
        BatchCode           NVARCHAR(50)      NOT NULL, -- VD: BATCH-SMT-2026-01
        ScheduledDate       DATE              NOT NULL,
        ShiftSlot           NVARCHAR(30)      NOT NULL, -- 'MORNING_08_11' | 'AFTERNOON_13_17'
        MaxCapacity         INT               NOT NULL CONSTRAINT DF_InterviewBatches_MaxCapacity DEFAULT (50),
        CurrentCount        INT               NOT NULL CONSTRAINT DF_InterviewBatches_CurrentCount DEFAULT (0),
        InterviewerUserId   UNIQUEIDENTIFIER  NULL,     -- ID Người phỏng vấn (Quản đốc / HR)
        CreatedAt           DATETIME2         NOT NULL CONSTRAINT DF_InterviewBatches_CreatedAt DEFAULT (SYSUTCDATETIME())
    );

    CREATE INDEX IX_InterviewBatches_Interviewer_Slot
        ON dbo.InterviewBatches(InterviewerUserId, ScheduledDate, ShiftSlot)
        WHERE InterviewerUserId IS NOT NULL;

    CREATE INDEX IX_InterviewBatches_JobId
        ON dbo.InterviewBatches(JobId)
        WHERE JobId IS NOT NULL;

    PRINT N'✅ Đã tạo bảng dbo.InterviewBatches và các Index chống trùng lịch.';
END
ELSE
BEGIN
    PRINT N'ℹ️ Bảng dbo.InterviewBatches đã tồn tại.';
END
GO

-- =========================================================================================
-- 3. TẠO BẢNG NHÂN SỰ CŨ ĐÃ NGHỈ VIỆC (ExEmployees) ĐỂ TRA CỨU REHIRE
-- =========================================================================================
IF OBJECT_ID('dbo.ExEmployees', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.ExEmployees
    (
        Id                  INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_ExEmployees PRIMARY KEY,
        EmployeeCode        NVARCHAR(50)      NOT NULL, -- VD: NV-2024-0892
        CitizenId           NVARCHAR(20)      NULL,
        PhoneNormalized     NVARCHAR(20)      NULL,
        FullName            NVARCHAR(150)     NOT NULL,
        DepartmentName      NVARCHAR(100)     NULL,     -- Xưởng SMT, Kho vận...
        ResignedDate        DATE              NULL,
        ResignationReason   NVARCHAR(255)     NULL,     -- Nghỉ việc đúng quy trình, gia đình...
        IsEligibleForRehire BIT               NOT NULL CONSTRAINT DF_ExEmployees_IsEligibleForRehire DEFAULT (1),
        CreatedAt           DATETIME2         NOT NULL CONSTRAINT DF_ExEmployees_CreatedAt DEFAULT (SYSUTCDATETIME())
    );

    CREATE INDEX IX_ExEmployees_CitizenId ON dbo.ExEmployees(CitizenId) WHERE CitizenId IS NOT NULL;
    CREATE INDEX IX_ExEmployees_PhoneNormalized ON dbo.ExEmployees(PhoneNormalized) WHERE PhoneNormalized IS NOT NULL;

    PRINT N'✅ Đã tạo bảng dbo.ExEmployees tra cứu nhân sự cũ.';
END
ELSE
BEGIN
    PRINT N'ℹ️ Bảng dbo.ExEmployees đã tồn tại.';
END
GO

-- =========================================================================================
-- 4. TẠO CÁC INDEX HỖ TRỢ HIỆU NĂNG CHO TALENT POOL & PROBATION
-- =========================================================================================
IF OBJECT_ID('dbo.Applications', 'U') IS NOT NULL
BEGIN
    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Applications_IsInTalentPool' AND object_id = OBJECT_ID('dbo.Applications'))
    BEGIN
        CREATE INDEX IX_Applications_IsInTalentPool ON dbo.Applications(IsInTalentPool) WHERE IsInTalentPool = 1;
        PRINT N'✅ Đã tạo Index IX_Applications_IsInTalentPool.';
    END

    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Applications_Probation30DayStatus' AND object_id = OBJECT_ID('dbo.Applications'))
    BEGIN
        CREATE INDEX IX_Applications_Probation30DayStatus ON dbo.Applications(Probation30DayStatus);
        PRINT N'✅ Đã tạo Index IX_Applications_Probation30DayStatus.';
    END
END
GO

-- =========================================================================================
-- 5. SEED DATA / DỮ LIỆU MẪU KIỂM THỬ TRÊN GIAO DIỆN
-- =========================================================================================
PRINT N'🌱 Đang chèn dữ liệu mẫu V5 (ExEmployees, InterviewBatches, Talent Pool, Probation)...';

-- 5.1 Thêm nhân sự cũ mẫu vào bảng ExEmployees
IF OBJECT_ID('dbo.ExEmployees', 'U') IS NOT NULL
BEGIN
    IF NOT EXISTS (SELECT 1 FROM dbo.ExEmployees WHERE EmployeeCode = 'NV-2024-0412')
    BEGIN
        INSERT INTO dbo.ExEmployees (EmployeeCode, CitizenId, PhoneNormalized, FullName, DepartmentName, ResignedDate, ResignationReason, IsEligibleForRehire)
        VALUES
        ('NV-2024-0412', '027200001234', '0912345678', N'Nguyễn Văn Toàn', N'Xưởng Lắp Ráp SMT', '2024-11-15', N'Xin nghỉ việc đúng quy trình về giải quyết việc gia đình', 1),
        ('NV-2023-0188', '034200005678', '0987654321', N'Trần Thị Mai', N'Xưởng Đóng Gói (PKG)', '2023-12-30', N'Hết hạn hợp đồng, có nguyện vọng học thêm tiếng Hàn', 1),
        ('NV-2024-0995', '019200009999', '0901234567', N'Lê Hoàng Long', N'Kho Vận & Vận Chuyển', '2024-08-20', N'Nghỉ việc chuyển chỗ ở sang KCN Quang Châu', 1);

        PRINT N'✅ Đã thêm 3 bản ghi mẫu vào dbo.ExEmployees.';
    END
END
GO

-- 5.2 Thêm 2 Ca phỏng vấn tập trung mẫu vào InterviewBatches
IF OBJECT_ID('dbo.InterviewBatches', 'U') IS NOT NULL
BEGIN
    IF NOT EXISTS (SELECT 1 FROM dbo.InterviewBatches WHERE BatchCode = 'BATCH-SMT-2026-01')
    BEGIN
        DECLARE @FirstJobId UNIQUEIDENTIFIER = (SELECT TOP 1 JobId FROM dbo.Jobs WHERE IsDeleted = 0);
        DECLARE @FirstUserId UNIQUEIDENTIFIER = (SELECT TOP 1 UserId FROM dbo.Users WHERE IsActive = 1);

        INSERT INTO dbo.InterviewBatches (JobId, BatchCode, ScheduledDate, ShiftSlot, MaxCapacity, CurrentCount, InterviewerUserId)
        VALUES
        (@FirstJobId, 'BATCH-SMT-2026-01', CAST(GETDATE() + 2 AS DATE), 'MORNING_08_11', 50, 48, @FirstUserId),
        (@FirstJobId, 'BATCH-SMT-2026-02', CAST(GETDATE() + 2 AS DATE), 'AFTERNOON_13_17', 50, 15, @FirstUserId);

        PRINT N'✅ Đã tạo 2 Ca phỏng vấn tập trung mẫu: BATCH-SMT-2026-01 (48/50) và BATCH-SMT-2026-02 (15/50).';
    END
END
GO

-- 5.3 Cập nhật mẫu hồ sơ trong Applications để hiển thị ngay trên UI
IF OBJECT_ID('dbo.Applications', 'U') IS NOT NULL
BEGIN
    -- Mẫu 1: Hồ sơ nhân viên cũ tái ứng tuyển (Ex-Employee Rehire)
    UPDATE TOP (1) dbo.Applications
    SET IsExEmployeeRehire = 1,
        PreviousEmployeeCode = 'NV-2024-0412'
    WHERE IsBlacklisted = 0;

    -- Mẫu 2: Hồ sơ lưu trong Talent Pool (Kho bảo lưu)
    UPDATE TOP (1) dbo.Applications
    SET IsInTalentPool = 1,
        Status = 'Saved_In_Talent_Pool'
    WHERE ApplicationId NOT IN (SELECT TOP 1 ApplicationId FROM dbo.Applications WHERE IsExEmployeeRehire = 1);

    -- Mẫu 3: Hồ sơ đã cấp mã NV và đạt thử việc 30 ngày
    UPDATE TOP (1) dbo.Applications
    SET Probation30DayStatus = 'Passed_30Days',
        ConvertedEmployeeCode = 'NV-2026-0001'
    WHERE Status = 'Hired' OR ConvertedEmployeeCode IS NOT NULL;

    PRINT N'✅ Đã cập nhật dữ liệu mẫu ứng viên: Rehire, Talent Pool và Probation Passed.';
END
GO

PRINT N'🎉 HOÀN TẤT CẬP NHẬT DATABASE V5 - HR OPERATIONS COMPLETE THÀNH CÔNG!';
GO

