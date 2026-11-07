using Microsoft.EntityFrameworkCore;
using UTC_DATN.Entities;

namespace UTC_DATN.Data
{
    public static class DbInitializer
    {
        public static async Task InitializeAsync(UTC_DATNContext context)
        {
            await context.Database.EnsureCreatedAsync();
            await EnsureIndustrialAtsSchemaAsync(context);

            await SeedDepartmentsAsync(context);
            await SeedJobTypesAsync(context);
            await SeedSkillsAsync(context);
            await SeedPipelineStagesAsync(context);
            await SeedRolesAsync(context);
            await SeedUsersAsync(context);
        }

        private static async Task EnsureIndustrialAtsSchemaAsync(UTC_DATNContext context)
        {
            const string sql = @"
IF OBJECT_ID('dbo.CandidateBlacklists', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.CandidateBlacklists
    (
        Id              INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_CandidateBlacklists PRIMARY KEY,
        CitizenId       NVARCHAR(20)      NULL,
        PhoneNormalized NVARCHAR(20)      NULL,
        FullName        NVARCHAR(150)     NOT NULL,
        Reason          NVARCHAR(500)     NOT NULL,
        BanType         NVARCHAR(50)      NOT NULL CONSTRAINT DF_CandidateBlacklists_BanType DEFAULT ('PERMANENT'),
        BannedUntil     DATETIME2         NULL,
        CreatedBy       NVARCHAR(100)     NULL,
        CreatedAt       DATETIME2         NOT NULL CONSTRAINT DF_CandidateBlacklists_CreatedAt DEFAULT (SYSUTCDATETIME())
    );
    CREATE INDEX IX_CandidateBlacklists_CitizenId ON dbo.CandidateBlacklists(CitizenId) WHERE CitizenId IS NOT NULL;
    CREATE INDEX IX_CandidateBlacklists_PhoneNormalized ON dbo.CandidateBlacklists(PhoneNormalized) WHERE PhoneNormalized IS NOT NULL;
END;

IF COL_LENGTH('Jobs', 'RecruitmentTrack') IS NULL
    ALTER TABLE Jobs ADD RecruitmentTrack NVARCHAR(30) NOT NULL DEFAULT ('PROFESSIONAL');

IF COL_LENGTH('Jobs', 'TargetQuantity') IS NULL
    ALTER TABLE Jobs ADD TargetQuantity INT NOT NULL DEFAULT (50);

IF COL_LENGTH('Jobs', 'HiredCount') IS NULL
    ALTER TABLE Jobs ADD HiredCount INT NOT NULL DEFAULT (0);

IF COL_LENGTH('Jobs', 'WaitlistBufferPercent') IS NULL
    ALTER TABLE Jobs ADD WaitlistBufferPercent INT NOT NULL DEFAULT (15);

IF COL_LENGTH('Jobs', 'CurrentScoringWeightVersion') IS NULL
    ALTER TABLE Jobs ADD CurrentScoringWeightVersion INT NOT NULL DEFAULT (1);

IF COL_LENGTH('Jobs', 'GoogleFormUrl') IS NULL
    ALTER TABLE Jobs ADD GoogleFormUrl NVARCHAR(500) NULL;

IF COL_LENGTH('Applications', 'ApplicationSource') IS NULL
    ALTER TABLE Applications ADD ApplicationSource NVARCHAR(30) NOT NULL DEFAULT ('PDF_UPLOAD');

IF COL_LENGTH('Applications', 'ScoringMethod') IS NULL
    ALTER TABLE Applications ADD ScoringMethod NVARCHAR(30) NOT NULL DEFAULT ('GEMINI_AI');

IF COL_LENGTH('Applications', 'CvFileUrl') IS NULL
    ALTER TABLE Applications ADD CvFileUrl NVARCHAR(500) NULL;

IF COL_LENGTH('Applications', 'CitizenId') IS NULL
    ALTER TABLE Applications ADD CitizenId NVARCHAR(20) NULL;

IF COL_LENGTH('Applications', 'PhoneNormalized') IS NULL
    ALTER TABLE Applications ADD PhoneNormalized NVARCHAR(20) NULL;

IF COL_LENGTH('Applications', 'DuplicateCount') IS NULL
    ALTER TABLE Applications ADD DuplicateCount INT NOT NULL DEFAULT (1);

IF COL_LENGTH('Applications', 'PreviousApplicationId') IS NULL
    ALTER TABLE Applications ADD PreviousApplicationId INT NULL;

IF COL_LENGTH('Applications', 'IsBlacklisted') IS NULL
    ALTER TABLE Applications ADD IsBlacklisted BIT NOT NULL DEFAULT (0);

IF COL_LENGTH('Applications', 'BlacklistWarningNote') IS NULL
    ALTER TABLE Applications ADD BlacklistWarningNote NVARCHAR(500) NULL;

IF COL_LENGTH('Applications', 'IsScannedOrUnparseablePdf') IS NULL
    ALTER TABLE Applications ADD IsScannedOrUnparseablePdf BIT NOT NULL DEFAULT (0);

IF COL_LENGTH('Applications', 'RawResumeText') IS NULL
    ALTER TABLE Applications ADD RawResumeText NVARCHAR(MAX) NULL;

IF COL_LENGTH('Applications', 'AiAnalysisSummary') IS NULL
    ALTER TABLE Applications ADD AiAnalysisSummary NVARCHAR(MAX) NULL;

IF COL_LENGTH('Applications', 'AppliedScoringVersion') IS NULL
    ALTER TABLE Applications ADD AppliedScoringVersion INT NULL DEFAULT (1);

IF COL_LENGTH('Applications', 'AppliedScoringConfigJson') IS NULL
    ALTER TABLE Applications ADD AppliedScoringConfigJson NVARCHAR(MAX) NULL;

IF COL_LENGTH('Applications', 'OralInterviewChecklistJson') IS NULL
    ALTER TABLE Applications ADD OralInterviewChecklistJson NVARCHAR(MAX) NULL;

IF COL_LENGTH('Applications', 'MedicalCheckStatus') IS NULL
    ALTER TABLE Applications ADD MedicalCheckStatus NVARCHAR(30) NOT NULL DEFAULT ('Pending');

IF COL_LENGTH('Applications', 'BankAccountNumber') IS NULL
    ALTER TABLE Applications ADD BankAccountNumber NVARCHAR(50) NULL;

IF COL_LENGTH('Applications', 'BankName') IS NULL
    ALTER TABLE Applications ADD BankName NVARCHAR(100) NULL;

IF COL_LENGTH('Applications', 'OnboardingDocsCompleted') IS NULL
    ALTER TABLE Applications ADD OnboardingDocsCompleted BIT NOT NULL DEFAULT (0);

IF COL_LENGTH('Applications', 'ConvertedEmployeeCode') IS NULL
    ALTER TABLE Applications ADD ConvertedEmployeeCode NVARCHAR(50) NULL;

IF COL_LENGTH('Applications', 'SlaDeadlineAt') IS NULL
    ALTER TABLE Applications ADD SlaDeadlineAt DATETIME2 NULL;

IF COL_LENGTH('Applications', 'IsSlaEscalated') IS NULL
    ALTER TABLE Applications ADD IsSlaEscalated BIT NOT NULL DEFAULT (0);

IF COL_LENGTH('Applications', 'SlaOverrideReason') IS NULL
    ALTER TABLE Applications ADD SlaOverrideReason NVARCHAR(500) NULL;

IF COL_LENGTH('Applications', 'RowVersion') IS NULL
    ALTER TABLE Applications ADD RowVersion ROWVERSION;

IF NOT EXISTS (SELECT 1 FROM dbo.CandidateBlacklists WHERE CitizenId = '027098004512')
BEGIN
    INSERT INTO dbo.CandidateBlacklists (CitizenId, PhoneNormalized, FullName, Reason, BanType, BannedUntil, CreatedBy, CreatedAt)
    VALUES
    (N'027098004512', N'0984112990', N'Lê Văn Cường', N'Vi phạm kỷ luật an toàn lao động nghiêm trọng tại Xưởng SMT (Hút thuốc trong kho hóa chất)', N'PERMANENT', NULL, N'hr1@factory.com', SYSUTCDATETIME()),
    (N'001095008821', N'0913778221', N'Phạm Quốc Tuấn', N'Tự ý bỏ việc 5 ngày liên tiếp không bàn giao thiết bị đo kiểm tại Xưởng Bảo Trì', N'PERMANENT', NULL, N'truong.sx@factory.com', SYSUTCDATETIME()),
    (N'030201003319', N'0972665110', N'Hoàng Thị Mai', N'Nộp bằng cấp/chứng chỉ giả và nhờ người quẹt thẻ từ chấm công hộ', N'TEMPORARY_6M', DATEADD(MONTH, 6, SYSUTCDATETIME()), N'hr1@factory.com', SYSUTCDATETIME());
END;
";
            try
            {
                await context.Database.ExecuteSqlRawAsync(sql);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[DbInitializer] Schema check warning: {ex.Message}");
            }
        }

        private static async Task SeedJobTypesAsync(UTC_DATNContext context)
        {
            var defaults = new List<JobType>
            {
                new JobType
                {
                    JobTypeId = new Guid("40000000-0000-0000-0000-000000000001"),
                    Name = "Toàn thời gian",
                    Description = "Làm việc toàn thời gian theo ca hoặc hành chính"
                },
                new JobType
                {
                    JobTypeId = new Guid("40000000-0000-0000-0000-000000000002"),
                    Name = "Theo ca",
                    Description = "Làm việc theo ca sáng/chiều/đêm hoặc xoay ca"
                },
                new JobType
                {
                    JobTypeId = new Guid("40000000-0000-0000-0000-000000000003"),
                    Name = "Thời vụ",
                    Description = "Tuyển theo đợt cao điểm sản xuất"
                },
                new JobType
                {
                    JobTypeId = new Guid("40000000-0000-0000-0000-000000000004"),
                    Name = "Hợp đồng ngắn hạn",
                    Description = "Hợp đồng có thời hạn cho chiến dịch tuyển nhanh"
                }
            };

            var existingNames = await context.JobTypes
                .Select(x => x.Name.Trim().ToLower())
                .ToListAsync();

            var missing = defaults
                .Where(x => !existingNames.Contains(x.Name.Trim().ToLower()))
                .ToList();

            if (!missing.Any()) return;

            await context.JobTypes.AddRangeAsync(missing);
            await context.SaveChangesAsync();
            Console.WriteLine($"✓ Seeded {missing.Count} JobTypes mặc định");
        }

        private static async Task SeedSkillsAsync(UTC_DATNContext context)
        {
            var defaultSkillNames = new[]
            {
                "Lắp ráp điện tử",
                "Đứng chuyền sản xuất",
                "Vận hành máy",
                "Đóng gói thành phẩm",
                "Kiểm tra chất lượng",
                "Kho vận nội bộ",
                "An toàn lao động",
                "5S công nghiệp",
                "Sẵn sàng tăng ca",
                "Làm việc xoay ca",
                "Hàn cơ bản",
                "May công nghiệp"
            };

            var existing = await context.Skills
                .Select(x => x.NormalizedName)
                .ToListAsync();

            var now = DateTime.UtcNow;
            var missing = new List<Skill>();

            foreach (var name in defaultSkillNames)
            {
                var normalized = name.Trim().ToUpperInvariant();
                if (existing.Contains(normalized))
                {
                    continue;
                }

                missing.Add(new Skill
                {
                    SkillId = Guid.NewGuid(),
                    Name = name,
                    NormalizedName = normalized,
                    CreatedAt = now,
                    IsDeleted = false
                });
            }

            if (!missing.Any()) return;

            await context.Skills.AddRangeAsync(missing);
            await context.SaveChangesAsync();
            Console.WriteLine($"✓ Seeded {missing.Count} Skills mặc định");
        }

        private static async Task SeedPipelineStagesAsync(UTC_DATNContext context)
        {
            var defaults = new List<PipelineStage>
            {
                new PipelineStage
                {
                    StageId = new Guid("50000000-0000-0000-0000-000000000001"),
                    Code = "PENDING_REVIEW",
                    Name = "Tiếp nhận hồ sơ",
                    SortOrder = 1,
                    IsTerminal = false,
                    CreatedAt = DateTime.UtcNow,
                    IsSlaEnabled = true,
                    SlaMaxDays = 2,
                    SlaWarnBeforeDays = 1
                },
                new PipelineStage
                {
                    StageId = new Guid("50000000-0000-0000-0000-000000000002"),
                    Code = "SCREENING",
                    Name = "Sàng lọc",
                    SortOrder = 2,
                    IsTerminal = false,
                    CreatedAt = DateTime.UtcNow,
                    IsSlaEnabled = true,
                    SlaMaxDays = 3,
                    SlaWarnBeforeDays = 1
                },
                new PipelineStage
                {
                    StageId = new Guid("50000000-0000-0000-0000-000000000003"),
                    Code = "INTERVIEW",
                    Name = "Phỏng vấn",
                    SortOrder = 3,
                    IsTerminal = false,
                    CreatedAt = DateTime.UtcNow,
                    IsSlaEnabled = true,
                    SlaMaxDays = 3,
                    SlaWarnBeforeDays = 1
                },
                new PipelineStage
                {
                    StageId = new Guid("50000000-0000-0000-0000-000000000004"),
                    Code = "OFFER",
                    Name = "Đề nghị nhận việc",
                    SortOrder = 4,
                    IsTerminal = false,
                    CreatedAt = DateTime.UtcNow,
                    IsSlaEnabled = true,
                    SlaMaxDays = 2,
                    SlaWarnBeforeDays = 1
                },
                new PipelineStage
                {
                    StageId = new Guid("50000000-0000-0000-0000-000000000005"),
                    Code = "HIRED",
                    Name = "Đã nhận việc",
                    SortOrder = 5,
                    IsTerminal = true,
                    CreatedAt = DateTime.UtcNow,
                    IsSlaEnabled = false
                }
            };

            var existingCodes = await context.PipelineStages
                .Select(x => x.Code.Trim().ToUpper())
                .ToListAsync();

            var missing = defaults
                .Where(x => !existingCodes.Contains(x.Code.Trim().ToUpper()))
                .ToList();

            if (!missing.Any()) return;

            await context.PipelineStages.AddRangeAsync(missing);
            await context.SaveChangesAsync();
            Console.WriteLine($"✓ Seeded {missing.Count} PipelineStages mặc định");
        }

        // ════════════════════════════════════════════════════════════════════════
        // SEED: 7 Phòng ban / Khu vực chuẩn Nhà máy Công nghiệp
        // ════════════════════════════════════════════════════════════════════════
        private static async Task SeedDepartmentsAsync(UTC_DATNContext context)
        {
            var departments = new List<Department>
            {
                new Department
                {
                    DepartmentId   = new Guid("10000000-0000-0000-0000-000000000001"),
                    DepartmentCode = "ADMIN_SYS",
                    DepartmentName = "Ban Giám Đốc",
                    Description    = "Cấp quản lý cao nhất – Ban Giám đốc và Quản trị hệ thống ATS.",
                    IsActive       = true,
                    CreatedAt      = DateTime.UtcNow
                },
                new Department
                {
                    DepartmentId   = new Guid("10000000-0000-0000-0000-000000000002"),
                    DepartmentCode = "HR_TEAM",
                    DepartmentName = "Phòng Nhân Sự (HR)",
                    Description    = "Tuyển dụng, sàng lọc hồ sơ, quản lý pipeline ứng viên toàn nhà máy.",
                    IsActive       = true,
                    CreatedAt      = DateTime.UtcNow
                },
                new Department
                {
                    DepartmentId   = new Guid("10000000-0000-0000-0000-000000000003"),
                    DepartmentCode = "INT_TEAM",
                    DepartmentName = "Hội Đồng Phỏng Vấn",
                    Description    = "Thực hiện phỏng vấn chuyên môn, chấm điểm và phản hồi ứng viên.",
                    IsActive       = true,
                    CreatedAt      = DateTime.UtcNow
                },
                new Department
                {
                    DepartmentId   = new Guid("10000000-0000-0000-0000-000000000004"),
                    DepartmentCode = "PROD_LINE",
                    DepartmentName = "Xưởng Sản Xuất & Lắp Ráp",
                    Description    = "Trực tiếp sản xuất, lắp ráp sản phẩm theo dây chuyền công nghiệp.",
                    IsActive       = true,
                    CreatedAt      = DateTime.UtcNow
                },
                new Department
                {
                    DepartmentId   = new Guid("10000000-0000-0000-0000-000000000005"),
                    DepartmentCode = "QA_QC",
                    DepartmentName = "Phòng Quản Lý Chất Lượng (QA/QC)",
                    Description    = "Kiểm tra, kiểm soát chất lượng sản phẩm đầu vào và đầu ra.",
                    IsActive       = true,
                    CreatedAt      = DateTime.UtcNow
                },
                new Department
                {
                    DepartmentId   = new Guid("10000000-0000-0000-0000-000000000006"),
                    DepartmentCode = "WH_LOGS",
                    DepartmentName = "Khu Vực Kho & Logistics",
                    Description    = "Quản lý kho hàng, vận chuyển, xuất-nhập kho tại khu công nghiệp.",
                    IsActive       = true,
                    CreatedAt      = DateTime.UtcNow
                },
                new Department
                {
                    DepartmentId   = new Guid("10000000-0000-0000-0000-000000000007"),
                    DepartmentCode = "TECH_MNT",
                    DepartmentName = "Phòng Kỹ Thuật & Bảo Trì",
                    Description    = "Vận hành, bảo dưỡng máy móc, hệ thống điện-cơ trong khu sản xuất.",
                    IsActive       = true,
                    CreatedAt      = DateTime.UtcNow
                }
            };

            var existingCodes = await context.Departments
                .Select(x => x.DepartmentCode.Trim().ToUpper())
                .ToListAsync();

            var missing = departments
                .Where(x => !existingCodes.Contains(x.DepartmentCode.Trim().ToUpper()))
                .ToList();

            if (missing.Any())
            {
                await context.Departments.AddRangeAsync(missing);
                await context.SaveChangesAsync();
                Console.WriteLine($"✓ Seeded {missing.Count} Departments (nhà máy công nghiệp)");
            }
        }

        private static async Task SeedRolesAsync(UTC_DATNContext context)
        {
            if (await context.Roles.AnyAsync()) return;

            var roles = new List<Role>
            {
                new Role { RoleId = new Guid("20000000-0000-0000-0000-000000000001"), Code = "ADMIN",       Name = "Ban Giám Đốc / Administrator", CreatedAt = DateTime.UtcNow },
                new Role { RoleId = new Guid("20000000-0000-0000-0000-000000000002"), Code = "MANAGER",     Name = "Trưởng Phòng / Quản Lý",       CreatedAt = DateTime.UtcNow },
                new Role { RoleId = new Guid("20000000-0000-0000-0000-000000000003"), Code = "HR",          Name = "Chuyên Viên Nhân Sự",           CreatedAt = DateTime.UtcNow },
                new Role { RoleId = new Guid("20000000-0000-0000-0000-000000000004"), Code = "INTERVIEWER", Name = "Hội Đồng Phỏng Vấn",            CreatedAt = DateTime.UtcNow }
            };

            await context.Roles.AddRangeAsync(roles);
            await context.SaveChangesAsync();
            Console.WriteLine("✓ Seeded 4 Roles: ADMIN, MANAGER, HR, INTERVIEWER");
        }

        // ════════════════════════════════════════════════════════════════════════
        // SEED: Full matrix – Nhân sự đầy đủ cấu trúc phân cấp nhà máy
        // ════════════════════════════════════════════════════════════════════════
        private static async Task SeedUsersAsync(UTC_DATNContext context)
        {
            if (await context.Users.AnyAsync()) return;

            // ─── Định nghĩa các Guid cố định để tham chiếu ManagerId ───────────
            // Ban Giám Đốc
            var uGiamDoc          = new Guid("30000000-0000-0000-0000-000000000001");

            // Trưởng Phòng Nhân Sự
            var uTruongHR         = new Guid("30000000-0000-0000-0000-000000000010");
            // Trưởng Hội Đồng Phỏng Vấn
            var uTruongPV         = new Guid("30000000-0000-0000-0000-000000000020");
            // Trưởng Xưởng Sản Xuất
            var uTruongSX         = new Guid("30000000-0000-0000-0000-000000000030");
            // Trưởng Phòng QA/QC
            var uTruongQA         = new Guid("30000000-0000-0000-0000-000000000040");
            // Trưởng Kho & Logistics
            var uTruongKho        = new Guid("30000000-0000-0000-0000-000000000050");
            // Trưởng Phòng Kỹ Thuật
            var uTruongKT         = new Guid("30000000-0000-0000-0000-000000000060");

            var pw = BCrypt.Net.BCrypt.HashPassword("Password123!");

            // ─── DEPT IDs ─────────────────────────────────────────────────────
            var dAdmin  = new Guid("10000000-0000-0000-0000-000000000001");
            var dHR     = new Guid("10000000-0000-0000-0000-000000000002");
            var dPV     = new Guid("10000000-0000-0000-0000-000000000003");
            var dSX     = new Guid("10000000-0000-0000-0000-000000000004");
            var dQA     = new Guid("10000000-0000-0000-0000-000000000005");
            var dKho    = new Guid("10000000-0000-0000-0000-000000000006");
            var dKT     = new Guid("10000000-0000-0000-0000-000000000007");

            // ─── ROLE IDs ─────────────────────────────────────────────────────
            var rAdmin  = new Guid("20000000-0000-0000-0000-000000000001");
            var rMgr    = new Guid("20000000-0000-0000-0000-000000000002");
            var rHR     = new Guid("20000000-0000-0000-0000-000000000003");
            var rPV     = new Guid("20000000-0000-0000-0000-000000000004");

            // ─── Danh sách Users ──────────────────────────────────────────────
            var users = new List<(User user, Guid roleId)>
            {
                // ===== BAN GIÁM ĐỐC =====
                (new User
                {
                    UserId = uGiamDoc,
                    Email = "giamdoc@factory.com", NormalizedEmail = "GIAMDOC@FACTORY.COM",
                    PasswordHash = pw, FullName = "Nguyễn Văn Giám Đốc", Phone = "0901000001",
                    IsActive = true, CreatedAt = DateTime.UtcNow, DepartmentId = dAdmin, ManagerId = null
                }, rAdmin),

                (new User
                {
                    UserId = new Guid("30000000-0000-0000-0000-000000000002"),
                    Email = "pho.giamdoc@factory.com", NormalizedEmail = "PHO.GIAMDOC@FACTORY.COM",
                    PasswordHash = pw, FullName = "Trần Thị Phó Giám Đốc", Phone = "0901000002",
                    IsActive = true, CreatedAt = DateTime.UtcNow, DepartmentId = dAdmin, ManagerId = uGiamDoc
                }, rAdmin),

                // ===== PHÒNG NHÂN SỰ (HR) =====
                (new User
                {
                    UserId = uTruongHR,
                    Email = "truong.hr@factory.com", NormalizedEmail = "TRUONG.HR@FACTORY.COM",
                    PasswordHash = pw, FullName = "Lê Thị Trưởng HR", Phone = "0901000010",
                    IsActive = true, CreatedAt = DateTime.UtcNow, DepartmentId = dHR, ManagerId = uGiamDoc
                }, rMgr),

                (new User
                {
                    UserId = new Guid("30000000-0000-0000-0000-000000000011"),
                    Email = "hr1@factory.com", NormalizedEmail = "HR1@FACTORY.COM",
                    PasswordHash = pw, FullName = "Phạm Văn Nhân Sự 1", Phone = "0901000011",
                    IsActive = true, CreatedAt = DateTime.UtcNow, DepartmentId = dHR, ManagerId = uTruongHR
                }, rHR),

                (new User
                {
                    UserId = new Guid("30000000-0000-0000-0000-000000000012"),
                    Email = "hr2@factory.com", NormalizedEmail = "HR2@FACTORY.COM",
                    PasswordHash = pw, FullName = "Hoàng Thị Nhân Sự 2", Phone = "0901000012",
                    IsActive = true, CreatedAt = DateTime.UtcNow, DepartmentId = dHR, ManagerId = uTruongHR
                }, rHR),

                (new User
                {
                    UserId = new Guid("30000000-0000-0000-0000-000000000013"),
                    Email = "hr3@factory.com", NormalizedEmail = "HR3@FACTORY.COM",
                    PasswordHash = pw, FullName = "Đỗ Văn Nhân Sự 3", Phone = "0901000013",
                    IsActive = true, CreatedAt = DateTime.UtcNow, DepartmentId = dHR, ManagerId = uTruongHR
                }, rHR),

                // ===== HỘI ĐỒNG PHỎNG VẤN =====
                (new User
                {
                    UserId = uTruongPV,
                    Email = "truong.pv@factory.com", NormalizedEmail = "TRUONG.PV@FACTORY.COM",
                    PasswordHash = pw, FullName = "Ngô Văn Trưởng Phỏng Vấn", Phone = "0901000020",
                    IsActive = true, CreatedAt = DateTime.UtcNow, DepartmentId = dPV, ManagerId = uGiamDoc
                }, rMgr),

                (new User
                {
                    UserId = new Guid("30000000-0000-0000-0000-000000000021"),
                    Email = "pv1@factory.com", NormalizedEmail = "PV1@FACTORY.COM",
                    PasswordHash = pw, FullName = "Vũ Thị Phỏng Vấn Viên 1", Phone = "0901000021",
                    IsActive = true, CreatedAt = DateTime.UtcNow, DepartmentId = dPV, ManagerId = uTruongPV
                }, rPV),

                (new User
                {
                    UserId = new Guid("30000000-0000-0000-0000-000000000022"),
                    Email = "pv2@factory.com", NormalizedEmail = "PV2@FACTORY.COM",
                    PasswordHash = pw, FullName = "Bùi Văn Phỏng Vấn Viên 2", Phone = "0901000022",
                    IsActive = true, CreatedAt = DateTime.UtcNow, DepartmentId = dPV, ManagerId = uTruongPV
                }, rPV),

                (new User
                {
                    UserId = new Guid("30000000-0000-0000-0000-000000000023"),
                    Email = "pv3@factory.com", NormalizedEmail = "PV3@FACTORY.COM",
                    PasswordHash = pw, FullName = "Đinh Thị Phỏng Vấn Viên 3", Phone = "0901000023",
                    IsActive = true, CreatedAt = DateTime.UtcNow, DepartmentId = dPV, ManagerId = uTruongPV
                }, rPV),

                // ===== XƯỞNG SẢN XUẤT & LẮP RÁP =====
                (new User
                {
                    UserId = uTruongSX,
                    Email = "truong.sx@factory.com", NormalizedEmail = "TRUONG.SX@FACTORY.COM",
                    PasswordHash = pw, FullName = "Trịnh Văn Trưởng Xưởng", Phone = "0901000030",
                    IsActive = true, CreatedAt = DateTime.UtcNow, DepartmentId = dSX, ManagerId = uGiamDoc
                }, rMgr),

                (new User
                {
                    UserId = new Guid("30000000-0000-0000-0000-000000000031"),
                    Email = "sx.hr1@factory.com", NormalizedEmail = "SX.HR1@FACTORY.COM",
                    PasswordHash = pw, FullName = "Lý Thị HR Xưởng 1", Phone = "0901000031",
                    IsActive = true, CreatedAt = DateTime.UtcNow, DepartmentId = dSX, ManagerId = uTruongSX
                }, rHR),

                (new User
                {
                    UserId = new Guid("30000000-0000-0000-0000-000000000032"),
                    Email = "sx.pv1@factory.com", NormalizedEmail = "SX.PV1@FACTORY.COM",
                    PasswordHash = pw, FullName = "Mai Văn PV Xưởng 1", Phone = "0901000032",
                    IsActive = true, CreatedAt = DateTime.UtcNow, DepartmentId = dSX, ManagerId = uTruongSX
                }, rPV),

                // ===== PHÒNG QUẢN LÝ CHẤT LƯỢNG (QA/QC) =====
                (new User
                {
                    UserId = uTruongQA,
                    Email = "truong.qa@factory.com", NormalizedEmail = "TRUONG.QA@FACTORY.COM",
                    PasswordHash = pw, FullName = "Phan Thị Trưởng QA", Phone = "0901000040",
                    IsActive = true, CreatedAt = DateTime.UtcNow, DepartmentId = dQA, ManagerId = uGiamDoc
                }, rMgr),

                (new User
                {
                    UserId = new Guid("30000000-0000-0000-0000-000000000041"),
                    Email = "qa.hr1@factory.com", NormalizedEmail = "QA.HR1@FACTORY.COM",
                    PasswordHash = pw, FullName = "Tô Văn HR Chất Lượng 1", Phone = "0901000041",
                    IsActive = true, CreatedAt = DateTime.UtcNow, DepartmentId = dQA, ManagerId = uTruongQA
                }, rHR),

                (new User
                {
                    UserId = new Guid("30000000-0000-0000-0000-000000000042"),
                    Email = "qa.pv1@factory.com", NormalizedEmail = "QA.PV1@FACTORY.COM",
                    PasswordHash = pw, FullName = "Cao Thị PV Chất Lượng 1", Phone = "0901000042",
                    IsActive = true, CreatedAt = DateTime.UtcNow, DepartmentId = dQA, ManagerId = uTruongQA
                }, rPV),

                // ===== KHU VỰC KHO & LOGISTICS =====
                (new User
                {
                    UserId = uTruongKho,
                    Email = "truong.kho@factory.com", NormalizedEmail = "TRUONG.KHO@FACTORY.COM",
                    PasswordHash = pw, FullName = "Lương Văn Trưởng Kho", Phone = "0901000050",
                    IsActive = true, CreatedAt = DateTime.UtcNow, DepartmentId = dKho, ManagerId = uGiamDoc
                }, rMgr),

                (new User
                {
                    UserId = new Guid("30000000-0000-0000-0000-000000000051"),
                    Email = "kho.hr1@factory.com", NormalizedEmail = "KHO.HR1@FACTORY.COM",
                    PasswordHash = pw, FullName = "Đặng Thị HR Kho 1", Phone = "0901000051",
                    IsActive = true, CreatedAt = DateTime.UtcNow, DepartmentId = dKho, ManagerId = uTruongKho
                }, rHR),

                // ===== PHÒNG KỸ THUẬT & BẢO TRÌ =====
                (new User
                {
                    UserId = uTruongKT,
                    Email = "truong.kt@factory.com", NormalizedEmail = "TRUONG.KT@FACTORY.COM",
                    PasswordHash = pw, FullName = "Hứa Văn Trưởng Kỹ Thuật", Phone = "0901000060",
                    IsActive = true, CreatedAt = DateTime.UtcNow, DepartmentId = dKT, ManagerId = uGiamDoc
                }, rMgr),

                (new User
                {
                    UserId = new Guid("30000000-0000-0000-0000-000000000061"),
                    Email = "kt.pv1@factory.com", NormalizedEmail = "KT.PV1@FACTORY.COM",
                    PasswordHash = pw, FullName = "Dương Thị PV Kỹ Thuật 1", Phone = "0901000061",
                    IsActive = true, CreatedAt = DateTime.UtcNow, DepartmentId = dKT, ManagerId = uTruongKT
                }, rPV),

                (new User
                {
                    UserId = new Guid("30000000-0000-0000-0000-000000000062"),
                    Email = "kt.pv2@factory.com", NormalizedEmail = "KT.PV2@FACTORY.COM",
                    PasswordHash = pw, FullName = "Thái Văn PV Kỹ Thuật 2", Phone = "0901000062",
                    IsActive = true, CreatedAt = DateTime.UtcNow, DepartmentId = dKT, ManagerId = uTruongKT
                }, rPV),
            };

            // Thêm tất cả users vào context
            await context.Users.AddRangeAsync(users.Select(u => u.user));
            await context.SaveChangesAsync();

            // Gán roles
            var userRoles = users.Select(u => new UserRole
            {
                UserId    = u.user.UserId,
                RoleId    = u.roleId,
                CreatedAt = DateTime.UtcNow
            }).ToList();

            await context.UserRoles.AddRangeAsync(userRoles);
            await context.SaveChangesAsync();

            Console.WriteLine($"✓ Seeded {users.Count} Users với đầy đủ cấu trúc phân cấp nhà máy.");
            Console.WriteLine("─────────────────────────────────────────────────────────────────");
            Console.WriteLine("  📧 TÀI KHOẢN ĐĂNG NHẬP (mật khẩu: Password123!)");
            Console.WriteLine("─────────────────────────────────────────────────────────────────");
            Console.WriteLine("  [ADMIN]    giamdoc@factory.com        → Ban Giám Đốc");
            Console.WriteLine("  [ADMIN]    pho.giamdoc@factory.com    → Ban Giám Đốc");
            Console.WriteLine("  [MANAGER]  truong.hr@factory.com      → Phòng Nhân Sự (HR)");
            Console.WriteLine("  [MANAGER]  truong.pv@factory.com      → Hội Đồng Phỏng Vấn");
            Console.WriteLine("  [MANAGER]  truong.sx@factory.com      → Xưởng Sản Xuất");
            Console.WriteLine("  [MANAGER]  truong.qa@factory.com      → Phòng QA/QC");
            Console.WriteLine("  [MANAGER]  truong.kho@factory.com     → Khu Vực Kho");
            Console.WriteLine("  [MANAGER]  truong.kt@factory.com      → Phòng Kỹ Thuật");
            Console.WriteLine("  [HR]       hr1@factory.com            → Phòng Nhân Sự");
            Console.WriteLine("  [HR]       hr2@factory.com            → Phòng Nhân Sự");
            Console.WriteLine("  [PV]       pv1@factory.com            → Hội Đồng Phỏng Vấn");
            Console.WriteLine("  [PV]       pv2@factory.com            → Hội Đồng Phỏng Vấn");
            Console.WriteLine("─────────────────────────────────────────────────────────────────");
        }
    }
}
