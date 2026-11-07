-- Seed master data for Post Job screen (idempotent)
-- Run once on the target database.

SET NOCOUNT ON;

BEGIN TRY
    BEGIN TRAN;

    -- 1) Departments
    IF NOT EXISTS (SELECT 1 FROM Departments WHERE DepartmentCode = 'ADMIN_SYS')
    BEGIN
        INSERT INTO Departments (DepartmentId, DepartmentCode, DepartmentName, Description, IsActive, CreatedAt)
        VALUES
        ('10000000-0000-0000-0000-000000000001', 'ADMIN_SYS', 'Ban Giám Đốc', 'Cấp quản lý cao nhất – Ban Giám đốc và Quản trị hệ thống ATS.', 1, SYSUTCDATETIME()),
        ('10000000-0000-0000-0000-000000000002', 'HR_TEAM', 'Phòng Nhân Sự (HR)', 'Tuyển dụng, sàng lọc hồ sơ, quản lý pipeline ứng viên toàn nhà máy.', 1, SYSUTCDATETIME()),
        ('10000000-0000-0000-0000-000000000003', 'INT_TEAM', 'Hội Đồng Phỏng Vấn', 'Thực hiện phỏng vấn chuyên môn, chấm điểm và phản hồi ứng viên.', 1, SYSUTCDATETIME()),
        ('10000000-0000-0000-0000-000000000004', 'PROD_LINE', 'Xưởng Sản Xuất & Lắp Ráp', 'Trực tiếp sản xuất, lắp ráp theo dây chuyền.', 1, SYSUTCDATETIME()),
        ('10000000-0000-0000-0000-000000000005', 'QA_QC', 'Phòng Quản Lý Chất Lượng (QA/QC)', 'Kiểm tra, kiểm soát chất lượng sản phẩm đầu vào và đầu ra.', 1, SYSUTCDATETIME()),
        ('10000000-0000-0000-0000-000000000006', 'WH_LOGS', 'Khu Vực Kho & Logistics', 'Quản lý kho hàng, vận chuyển, xuất-nhập kho.', 1, SYSUTCDATETIME()),
        ('10000000-0000-0000-0000-000000000007', 'TECH_MNT', 'Phòng Kỹ Thuật & Bảo Trì', 'Vận hành, bảo dưỡng máy móc, hệ thống điện-cơ.', 1, SYSUTCDATETIME());
    END

    -- 2) JobTypes
    IF NOT EXISTS (SELECT 1 FROM JobTypes WHERE Name = N'Toàn thời gian')
    BEGIN
        INSERT INTO JobTypes (JobTypeId, Name, Description)
        VALUES
        ('40000000-0000-0000-0000-000000000001', N'Toàn thời gian', N'Làm việc toàn thời gian theo ca hoặc hành chính'),
        ('40000000-0000-0000-0000-000000000002', N'Theo ca', N'Làm việc theo ca sáng/chiều/đêm hoặc xoay ca'),
        ('40000000-0000-0000-0000-000000000003', N'Thời vụ', N'Tuyển theo đợt cao điểm sản xuất'),
        ('40000000-0000-0000-0000-000000000004', N'Hợp đồng ngắn hạn', N'Hợp đồng có thời hạn cho chiến dịch tuyển nhanh');
    END

    -- 3) Skills
    IF NOT EXISTS (SELECT 1 FROM Skills WHERE NormalizedName = 'LẮP RÁP ĐIỆN TỬ')
    BEGIN
        INSERT INTO Skills (SkillId, Name, NormalizedName, CreatedAt, IsDeleted)
        VALUES
        (NEWID(), N'Lắp ráp điện tử', N'LẮP RÁP ĐIỆN TỬ', SYSUTCDATETIME(), 0),
        (NEWID(), N'Đứng chuyền sản xuất', N'ĐỨNG CHUYỀN SẢN XUẤT', SYSUTCDATETIME(), 0),
        (NEWID(), N'Vận hành máy', N'VẬN HÀNH MÁY', SYSUTCDATETIME(), 0),
        (NEWID(), N'Đóng gói thành phẩm', N'ĐÓNG GÓI THÀNH PHẨM', SYSUTCDATETIME(), 0),
        (NEWID(), N'Kiểm tra chất lượng', N'KIỂM TRA CHẤT LƯỢNG', SYSUTCDATETIME(), 0),
        (NEWID(), N'Kho vận nội bộ', N'KHO VẬN NỘI BỘ', SYSUTCDATETIME(), 0),
        (NEWID(), N'An toàn lao động', N'AN TOÀN LAO ĐỘNG', SYSUTCDATETIME(), 0),
        (NEWID(), N'5S công nghiệp', N'5S CÔNG NGHIỆP', SYSUTCDATETIME(), 0),
        (NEWID(), N'Sẵn sàng tăng ca', N'SẴN SÀNG TĂNG CA', SYSUTCDATETIME(), 0),
        (NEWID(), N'Làm việc xoay ca', N'LÀM VIỆC XOAY CA', SYSUTCDATETIME(), 0),
        (NEWID(), N'Hàn cơ bản', N'HÀN CƠ BẢN', SYSUTCDATETIME(), 0),
        (NEWID(), N'May công nghiệp', N'MAY CÔNG NGHIỆP', SYSUTCDATETIME(), 0);
    END

    -- 4) PipelineStages (for QR import/application workflow)
    IF NOT EXISTS (SELECT 1 FROM PipelineStages WHERE Code = 'PENDING_REVIEW')
    BEGIN
        INSERT INTO PipelineStages (StageId, Code, Name, SortOrder, IsTerminal, CreatedAt, IsSlaEnabled, SlaMaxDays, SlaWarnBeforeDays)
        VALUES
        ('50000000-0000-0000-0000-000000000001', 'PENDING_REVIEW', N'Tiếp nhận hồ sơ', 1, 0, SYSUTCDATETIME(), 1, 2, 1),
        ('50000000-0000-0000-0000-000000000002', 'SCREENING', N'Sàng lọc', 2, 0, SYSUTCDATETIME(), 1, 3, 1),
        ('50000000-0000-0000-0000-000000000003', 'INTERVIEW', N'Phỏng vấn', 3, 0, SYSUTCDATETIME(), 1, 3, 1),
        ('50000000-0000-0000-0000-000000000004', 'OFFER', N'Đề nghị nhận việc', 4, 0, SYSUTCDATETIME(), 1, 2, 1),
        ('50000000-0000-0000-0000-000000000005', 'HIRED', N'Đã nhận việc', 5, 1, SYSUTCDATETIME(), 0, NULL, NULL);
    END

    COMMIT TRAN;
    PRINT N'Seed master data completed successfully.';
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRAN;

    DECLARE @ErrMsg NVARCHAR(4000) = ERROR_MESSAGE();
    DECLARE @ErrLine INT = ERROR_LINE();
    PRINT N'Seed failed at line ' + CAST(@ErrLine AS NVARCHAR(20)) + N': ' + @ErrMsg;
    THROW;
END CATCH;
