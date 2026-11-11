-- ====================================================================================================
-- HỆ THỐNG QUẢN TRỊ TUYỂN DỤNG VÀ SÀNG LỌC ỨNG VIÊN ATS (SMART RECRUITMENT & SCREENING ATS)
-- FILE CƠ SỞ DỮ LIỆU ĐẦY ĐỦ VÀ TINH GỌN (CLEAN & COMPLETE SQL SERVER SCRIPT)
-- ĐỒ ÁN TỐT NGHIỆP - ĐẠI HỌC GIAO THÔNG VẬN TẢI (UTC)
-- KIẾN TRÚC MA TRẬN NHÀ MÁY / KHU CÔNG NGHIỆP & TỰ ĐỘNG HÓA AI SCREENING (GEMINI 2.5 FLASH)
-- ====================================================================================================
-- CẤU TRÚC ĐƯỢC CHIA TÁCH BẠCH RÕ RÀNG:
--   PHẦN I  : TẠO TOÀN BỘ CẤU TRÚC BẢNG, KHÓA CHÍNH, KHÓA NGOẠI & CHỈ MỤC (DDL - CREATE TABLES & INDEXES)
--   PHẦN II : NẠP TOÀN BỘ DỮ LIỆU MẪU & KHỞI TẠO HỆ THỐNG (DML - INSERT SEED DATA CHUẨN TIẾNG VIỆT UNIKEY)
--   PHẦN III: BÁO CÁO THỐNG KÊ SỐ LƯỢNG BẢN GHI SAU KHI KHỞI TẠO
-- ====================================================================================================

USE [master];
GO

IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = N'UTC_DATN')
BEGIN
    CREATE DATABASE [UTC_DATN] COLLATE SQL_Latin1_General_CP1_CI_AS;
    PRINT N'✓ Đã tạo mới cơ sở dữ liệu UTC_DATN';
END
GO

USE [UTC_DATN];
GO
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

-- ====================================================================================================
-- LƯU Ý KHI MUỐN XÓA SẠCH VÀ CHẠY LẠI TỪ ĐẦU (CLEAN RESET):
-- Mở khối comment dưới đây để xóa các bảng theo đúng thứ tự ràng buộc khóa ngoại:
/*
IF OBJECT_ID('dbo.AiCodeReviewReport', 'U') IS NOT NULL DROP TABLE dbo.AiCodeReviewReport;
IF OBJECT_ID('dbo.CandidateCodeSubmission', 'U') IS NOT NULL DROP TABLE dbo.CandidateCodeSubmission;
IF OBJECT_ID('dbo.CodeChallenge', 'U') IS NOT NULL DROP TABLE dbo.CodeChallenge;
IF OBJECT_ID('dbo.EmailSendLog', 'U') IS NOT NULL DROP TABLE dbo.EmailSendLog;
IF OBJECT_ID('dbo.EmailQueue', 'U') IS NOT NULL DROP TABLE dbo.EmailQueue;
IF OBJECT_ID('dbo.AutomationRules', 'U') IS NOT NULL DROP TABLE dbo.AutomationRules;
IF OBJECT_ID('dbo.EmailTemplates', 'U') IS NOT NULL DROP TABLE dbo.EmailTemplates;
IF OBJECT_ID('dbo.PreScreenAnswers', 'U') IS NOT NULL DROP TABLE dbo.PreScreenAnswers;
IF OBJECT_ID('dbo.PreScreenQuestions', 'U') IS NOT NULL DROP TABLE dbo.PreScreenQuestions;
IF OBJECT_ID('dbo.ChatFeedbacks', 'U') IS NOT NULL DROP TABLE dbo.ChatFeedbacks;
IF OBJECT_ID('dbo.ChatAnalytics', 'U') IS NOT NULL DROP TABLE dbo.ChatAnalytics;
IF OBJECT_ID('dbo.ChatMessages', 'U') IS NOT NULL DROP TABLE dbo.ChatMessages;
IF OBJECT_ID('dbo.ChatSessions', 'U') IS NOT NULL DROP TABLE dbo.ChatSessions;
IF OBJECT_ID('dbo.ChatbotFaqs', 'U') IS NOT NULL DROP TABLE dbo.ChatbotFaqs;
IF OBJECT_ID('dbo.InterviewEvaluations', 'U') IS NOT NULL DROP TABLE dbo.InterviewEvaluations;
IF OBJECT_ID('dbo.InterviewFeedback', 'U') IS NOT NULL DROP TABLE dbo.InterviewFeedback;
IF OBJECT_ID('dbo.InterviewScorecards', 'U') IS NOT NULL DROP TABLE dbo.InterviewScorecards;
IF OBJECT_ID('dbo.Interviewers', 'U') IS NOT NULL DROP TABLE dbo.Interviewers;
IF OBJECT_ID('dbo.Interviews', 'U') IS NOT NULL DROP TABLE dbo.Interviews;
IF OBJECT_ID('dbo.AntiCheatEvents', 'U') IS NOT NULL DROP TABLE dbo.AntiCheatEvents;
IF OBJECT_ID('dbo.AssessmentAnswers', 'U') IS NOT NULL DROP TABLE dbo.AssessmentAnswers;
IF OBJECT_ID('dbo.AssessmentAttempts', 'U') IS NOT NULL DROP TABLE dbo.AssessmentAttempts;
IF OBJECT_ID('dbo.AssessmentInvites', 'U') IS NOT NULL DROP TABLE dbo.AssessmentInvites;
IF OBJECT_ID('dbo.AssessmentQuestions', 'U') IS NOT NULL DROP TABLE dbo.AssessmentQuestions;
IF OBJECT_ID('dbo.Assessments', 'U') IS NOT NULL DROP TABLE dbo.Assessments;
IF OBJECT_ID('dbo.QuestionOptions', 'U') IS NOT NULL DROP TABLE dbo.QuestionOptions;
IF OBJECT_ID('dbo.QuestionBank', 'U') IS NOT NULL DROP TABLE dbo.QuestionBank;
IF OBJECT_ID('dbo.ApplicationViews', 'U') IS NOT NULL DROP TABLE dbo.ApplicationViews;
IF OBJECT_ID('dbo.ApplicationStageHistory', 'U') IS NOT NULL DROP TABLE dbo.ApplicationStageHistory;
IF OBJECT_ID('dbo.ApplicationNotes', 'U') IS NOT NULL DROP TABLE dbo.ApplicationNotes;
IF OBJECT_ID('dbo.ApplicationAiScores', 'U') IS NOT NULL DROP TABLE dbo.ApplicationAiScores;
IF OBJECT_ID('dbo.Applications', 'U') IS NOT NULL DROP TABLE dbo.Applications;
IF OBJECT_ID('dbo.InterviewBatches', 'U') IS NOT NULL DROP TABLE dbo.InterviewBatches;
IF OBJECT_ID('dbo.CoverLetters', 'U') IS NOT NULL DROP TABLE dbo.CoverLetters;
IF OBJECT_ID('dbo.CandidateSkills', 'U') IS NOT NULL DROP TABLE dbo.CandidateSkills;
IF OBJECT_ID('dbo.CandidateExperiences', 'U') IS NOT NULL DROP TABLE dbo.CandidateExperiences;
IF OBJECT_ID('dbo.CandidateEducations', 'U') IS NOT NULL DROP TABLE dbo.CandidateEducations;
IF OBJECT_ID('dbo.CandidateDocuments', 'U') IS NOT NULL DROP TABLE dbo.CandidateDocuments;
IF OBJECT_ID('dbo.SavedJobs', 'U') IS NOT NULL DROP TABLE dbo.SavedJobs;
IF OBJECT_ID('dbo.JobTagMap', 'U') IS NOT NULL DROP TABLE dbo.JobTagMap;
IF OBJECT_ID('dbo.JobSkillMap', 'U') IS NOT NULL DROP TABLE dbo.JobSkillMap;
IF OBJECT_ID('dbo.Candidates', 'U') IS NOT NULL DROP TABLE dbo.Candidates;
IF OBJECT_ID('dbo.Jobs', 'U') IS NOT NULL DROP TABLE dbo.Jobs;
IF OBJECT_ID('dbo.NotificationSettings', 'U') IS NOT NULL DROP TABLE dbo.NotificationSettings;
IF OBJECT_ID('dbo.Notifications', 'U') IS NOT NULL DROP TABLE dbo.Notifications;
IF OBJECT_ID('dbo.RefreshTokens', 'U') IS NOT NULL DROP TABLE dbo.RefreshTokens;
IF OBJECT_ID('dbo.UserRoles', 'U') IS NOT NULL DROP TABLE dbo.UserRoles;
IF OBJECT_ID('dbo.Users', 'U') IS NOT NULL DROP TABLE dbo.Users;
IF OBJECT_ID('dbo.ExEmployees', 'U') IS NOT NULL DROP TABLE dbo.ExEmployees;
IF OBJECT_ID('dbo.CandidateBlacklists', 'U') IS NOT NULL DROP TABLE dbo.CandidateBlacklists;
IF OBJECT_ID('dbo.Files', 'U') IS NOT NULL DROP TABLE dbo.Files;
IF OBJECT_ID('dbo.Skills', 'U') IS NOT NULL DROP TABLE dbo.Skills;
IF OBJECT_ID('dbo.PipelineStages', 'U') IS NOT NULL DROP TABLE dbo.PipelineStages;
IF OBJECT_ID('dbo.JobTags', 'U') IS NOT NULL DROP TABLE dbo.JobTags;
IF OBJECT_ID('dbo.JobTypes', 'U') IS NOT NULL DROP TABLE dbo.JobTypes;
IF OBJECT_ID('dbo.Roles', 'U') IS NOT NULL DROP TABLE dbo.Roles;
IF OBJECT_ID('dbo.Departments', 'U') IS NOT NULL DROP TABLE dbo.Departments;
*/
GO

-- ====================================================================================================
-- PHẦN I: TẠO TOÀN BỘ CẤU TRÚC BẢNG & RÀNG BUỘC (DDL - CREATE TABLES)
-- ====================================================================================================

-- 1. BẢNG PHÒNG BAN / PHÂN XƯỞNG NHÀ MÁY (Departments)
IF OBJECT_ID('dbo.Departments', 'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[Departments] (
        [DepartmentId]   UNIQUEIDENTIFIER NOT NULL DEFAULT (NEWID()),
        [DepartmentCode] NVARCHAR(30)     NOT NULL,
        [DepartmentName] NVARCHAR(200)    NOT NULL,
        [Description]    NVARCHAR(500)    NULL,
        [IsActive]       BIT              NOT NULL DEFAULT (1),
        [CreatedAt]      DATETIME2(7)     NOT NULL DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT [PK_Departments] PRIMARY KEY CLUSTERED ([DepartmentId] ASC),
        CONSTRAINT [UQ_Departments_Code] UNIQUE NONCLUSTERED ([DepartmentCode] ASC)
    );
END
GO

-- 2. BẢNG VAI TRÒ HỆ THỐNG (Roles)
IF OBJECT_ID('dbo.Roles', 'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[Roles] (
        [RoleId]    UNIQUEIDENTIFIER NOT NULL DEFAULT (NEWID()),
        [Code]      NVARCHAR(50)     NOT NULL,
        [Name]      NVARCHAR(100)    NOT NULL,
        [CreatedAt] DATETIME2(7)     NOT NULL DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT [PK_Roles] PRIMARY KEY CLUSTERED ([RoleId] ASC),
        CONSTRAINT [UQ_Roles_Code] UNIQUE NONCLUSTERED ([Code] ASC)
    );
END
GO

-- 3. BẢNG HÌNH THỨC LÀM VIỆC (JobTypes)
IF OBJECT_ID('dbo.JobTypes', 'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[JobTypes] (
        [JobTypeId]   UNIQUEIDENTIFIER NOT NULL DEFAULT (NEWID()),
        [Name]        NVARCHAR(100)    NOT NULL,
        [Description] NVARCHAR(500)    NULL,
        CONSTRAINT [PK_JobTypes] PRIMARY KEY CLUSTERED ([JobTypeId] ASC)
    );
END
GO

-- 4. BẢNG THẺ NHÃN CÔNG VIỆC (JobTags)
IF OBJECT_ID('dbo.JobTags', 'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[JobTags] (
        [TagId]     UNIQUEIDENTIFIER NOT NULL DEFAULT (NEWID()),
        [Name]      NVARCHAR(100)    NOT NULL,
        [CreatedAt] DATETIME2(7)     NOT NULL DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT [PK_JobTags] PRIMARY KEY CLUSTERED ([TagId] ASC)
    );
END
GO

-- 5. BẢNG VÒNG TUYỂN DỤNG & SLA (PipelineStages)
IF OBJECT_ID('dbo.PipelineStages', 'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[PipelineStages] (
        [StageId]           UNIQUEIDENTIFIER NOT NULL DEFAULT (NEWID()),
        [Code]              NVARCHAR(50)     NOT NULL,
        [Name]              NVARCHAR(100)    NOT NULL,
        [SortOrder]         INT              NOT NULL DEFAULT (0),
        [IsTerminal]        BIT              NOT NULL DEFAULT (0),
        [CreatedAt]         DATETIME2(7)     NOT NULL DEFAULT (SYSUTCDATETIME()),
        [SlaMaxDays]        INT              NULL,
        [SlaWarnBeforeDays] INT              NULL,
        [IsSlaEnabled]      BIT              NOT NULL DEFAULT (0),
        CONSTRAINT [PK_PipelineStages] PRIMARY KEY CLUSTERED ([StageId] ASC),
        CONSTRAINT [UQ_PipelineStages_Code] UNIQUE NONCLUSTERED ([Code] ASC)
    );
END
GO

-- 6. BẢNG DANH MỤC KỸ NĂNG (Skills)
IF OBJECT_ID('dbo.Skills', 'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[Skills] (
        [SkillId]        UNIQUEIDENTIFIER NOT NULL DEFAULT (NEWID()),
        [Name]           NVARCHAR(100)    NOT NULL,
        [NormalizedName] NVARCHAR(100)    NULL,
        [CreatedAt]      DATETIME2(7)     NOT NULL DEFAULT (SYSUTCDATETIME()),
        [IsDeleted]      BIT              NOT NULL DEFAULT (0),
        CONSTRAINT [PK_Skills] PRIMARY KEY CLUSTERED ([SkillId] ASC)
    );
END
GO

-- 7. BẢNG TỆP TIN ĐÍNH KÈM / LƯU TRỮ VẬT LÝ (Files)
IF OBJECT_ID('dbo.Files', 'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[Files] (
        [FileId]          UNIQUEIDENTIFIER NOT NULL DEFAULT (NEWID()),
        [FileName]        NVARCHAR(255)    NOT NULL,
        [LocalPath]       NVARCHAR(500)    NOT NULL,
        [MimeType]        NVARCHAR(100)    NULL,
        [FileSize]        BIGINT           NULL,
        [StorageProvider] NVARCHAR(50)     NOT NULL DEFAULT ('Local'),
        [CreatedAt]       DATETIME2(7)     NOT NULL DEFAULT (SYSUTCDATETIME()),
        [CreatedBy]       UNIQUEIDENTIFIER NULL,
        CONSTRAINT [PK_Files] PRIMARY KEY CLUSTERED ([FileId] ASC)
    );
END
GO

-- 8. BẢNG DANH SÁCH ĐEN CẤM TUYỂN DỤNG (CandidateBlacklists - V2 Compliance)
IF OBJECT_ID('dbo.CandidateBlacklists', 'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[CandidateBlacklists] (
        [BlacklistId]   UNIQUEIDENTIFIER NOT NULL DEFAULT (NEWID()),
        [CitizenId]     NVARCHAR(20)     NULL,
        [Phone]         NVARCHAR(20)     NULL,
        [FullName]      NVARCHAR(200)    NULL,
        [Reason]        NVARCHAR(500)    NOT NULL,
        [BlacklistedBy] UNIQUEIDENTIFIER NULL,
        [BlacklistedAt] DATETIME2(7)     NOT NULL DEFAULT (SYSUTCDATETIME()),
        [IsActive]      BIT              NOT NULL DEFAULT (1),
        CONSTRAINT [PK_CandidateBlacklists] PRIMARY KEY CLUSTERED ([BlacklistId] ASC)
    );
    CREATE NONCLUSTERED INDEX [IX_CandidateBlacklists_CitizenId] ON [dbo].[CandidateBlacklists]([CitizenId]);
    CREATE NONCLUSTERED INDEX [IX_CandidateBlacklists_Phone] ON [dbo].[CandidateBlacklists]([Phone]);
END
GO

-- 9. BẢNG NHÂN VIÊN CŨ / LỊCH SỬ NGHỈ VIỆC (ExEmployees - V5 Factory Operations)
IF OBJECT_ID('dbo.ExEmployees', 'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[ExEmployees] (
        [ExEmployeeId]       UNIQUEIDENTIFIER NOT NULL DEFAULT (NEWID()),
        [EmployeeCode]       NVARCHAR(50)     NOT NULL,
        [FullName]           NVARCHAR(200)    NOT NULL,
        [CitizenId]          NVARCHAR(20)     NULL,
        [Phone]              NVARCHAR(20)     NULL,
        [DepartmentCode]     NVARCHAR(50)     NULL,
        [LastPosition]       NVARCHAR(100)    NULL,
        [ResignedAt]         DATETIME2(7)     NULL,
        [ReasonForLeaving]   NVARCHAR(500)    NULL,
        [RehireEligibility]  NVARCHAR(30)     NOT NULL DEFAULT ('Eligible'),
        [CreatedAt]          DATETIME2(7)     NOT NULL DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT [PK_ExEmployees] PRIMARY KEY CLUSTERED ([ExEmployeeId] ASC),
        CONSTRAINT [UQ_ExEmployees_Code] UNIQUE NONCLUSTERED ([EmployeeCode] ASC)
    );
    CREATE NONCLUSTERED INDEX [IX_ExEmployees_CitizenId] ON [dbo].[ExEmployees]([CitizenId]);
END
GO

-- 10. BẢNG NGƯỜI DÙNG & TÀI KHOẢN NỘI BỘ (Users)
IF OBJECT_ID('dbo.Users', 'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[Users] (
        [UserId]       UNIQUEIDENTIFIER NOT NULL DEFAULT (NEWID()),
        [Email]        NVARCHAR(256)    NOT NULL,
        [PasswordHash] NVARCHAR(MAX)    NULL,
        [FullName]     NVARCHAR(200)    NOT NULL,
        [Phone]        NVARCHAR(50)     NULL,
        [IsActive]     BIT              NOT NULL DEFAULT (1),
        [DepartmentId] UNIQUEIDENTIFIER NULL,
        [ManagerId]    UNIQUEIDENTIFIER NULL,
        [AuthProvider] NVARCHAR(50)     NOT NULL DEFAULT ('Local'),
        [CreatedAt]    DATETIME2(7)     NOT NULL DEFAULT (SYSUTCDATETIME()),
        [UpdatedAt]    DATETIME2(7)     NULL,
        CONSTRAINT [PK_Users] PRIMARY KEY CLUSTERED ([UserId] ASC),
        CONSTRAINT [UQ_Users_Email] UNIQUE NONCLUSTERED ([Email] ASC),
        CONSTRAINT [FK_Users_Departments] FOREIGN KEY ([DepartmentId]) REFERENCES [dbo].[Departments] ([DepartmentId]),
        CONSTRAINT [FK_Users_Manager] FOREIGN KEY ([ManagerId]) REFERENCES [dbo].[Users] ([UserId])
    );
END
GO

-- 11. BẢNG ÁNH XẠ VAI TRÒ NGƯỜI DÙNG (UserRoles)
IF OBJECT_ID('dbo.UserRoles', 'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[UserRoles] (
        [UserId]    UNIQUEIDENTIFIER NOT NULL,
        [RoleId]    UNIQUEIDENTIFIER NOT NULL,
        [CreatedAt] DATETIME2(7)     NOT NULL DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT [PK_UserRoles] PRIMARY KEY CLUSTERED ([UserId] ASC, [RoleId] ASC),
        CONSTRAINT [FK_UserRoles_Users] FOREIGN KEY ([UserId]) REFERENCES [dbo].[Users] ([UserId]) ON DELETE CASCADE,
        CONSTRAINT [FK_UserRoles_Roles] FOREIGN KEY ([RoleId]) REFERENCES [dbo].[Roles] ([RoleId]) ON DELETE CASCADE
    );
END
GO

-- 12. BẢNG PHIÊN ĐĂNG NHẬP / REFRESH TOKEN (RefreshTokens)
IF OBJECT_ID('dbo.RefreshTokens', 'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[RefreshTokens] (
        [TokenId]   UNIQUEIDENTIFIER NOT NULL DEFAULT (NEWID()),
        [UserId]    UNIQUEIDENTIFIER NOT NULL,
        [Token]     NVARCHAR(500)    NOT NULL,
        [ExpiresAt] DATETIME2(7)     NOT NULL,
        [CreatedAt] DATETIME2(7)     NOT NULL DEFAULT (SYSUTCDATETIME()),
        [RevokedAt] DATETIME2(7)     NULL,
        CONSTRAINT [PK_RefreshTokens] PRIMARY KEY CLUSTERED ([TokenId] ASC),
        CONSTRAINT [FK_RefreshTokens_Users] FOREIGN KEY ([UserId]) REFERENCES [dbo].[Users] ([UserId]) ON DELETE CASCADE
    );
END
GO

-- 13. BẢNG THÔNG BÁO HỆ THỐNG (Notifications)
IF OBJECT_ID('dbo.Notifications', 'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[Notifications] (
        [NotificationId] UNIQUEIDENTIFIER NOT NULL DEFAULT (NEWID()),
        [UserId]         UNIQUEIDENTIFIER NOT NULL,
        [Title]          NVARCHAR(250)    NOT NULL,
        [Content]        NVARCHAR(MAX)    NULL,
        [Type]           NVARCHAR(50)     NULL,
        [IsRead]         BIT              NOT NULL DEFAULT (0),
        [CreatedAt]      DATETIME2(7)     NOT NULL DEFAULT (SYSUTCDATETIME()),
        [ReadAt]         DATETIME2(7)     NULL,
        CONSTRAINT [PK_Notifications] PRIMARY KEY CLUSTERED ([NotificationId] ASC),
        CONSTRAINT [FK_Notifications_Users] FOREIGN KEY ([UserId]) REFERENCES [dbo].[Users] ([UserId]) ON DELETE CASCADE
    );
END
GO

-- 14. BẢNG CÀI ĐẶT THÔNG BÁO (NotificationSettings)
IF OBJECT_ID('dbo.NotificationSettings', 'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[NotificationSettings] (
        [SettingId]           UNIQUEIDENTIFIER NOT NULL DEFAULT (NEWID()),
        [UserId]              UNIQUEIDENTIFIER NOT NULL,
        [EmailNotifications]  BIT              NOT NULL DEFAULT (1),
        [PushNotifications]   BIT              NOT NULL DEFAULT (1),
        [SmsNotifications]    BIT              NOT NULL DEFAULT (0),
        [UpdatedAt]           DATETIME2(7)     NULL,
        CONSTRAINT [PK_NotificationSettings] PRIMARY KEY CLUSTERED ([SettingId] ASC),
        CONSTRAINT [FK_NotificationSettings_Users] FOREIGN KEY ([UserId]) REFERENCES [dbo].[Users] ([UserId]) ON DELETE CASCADE
    );
END
GO

-- 15. BẢNG CHIẾN DỊCH & TIN TUYỂN DỤNG (Jobs - Đầy đủ cột V2, V3, V5)
IF OBJECT_ID('dbo.Jobs', 'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[Jobs] (
        [JobId]                       UNIQUEIDENTIFIER NOT NULL DEFAULT (NEWID()),
        [Code]                        NVARCHAR(50)     NOT NULL,
        [Title]                       NVARCHAR(250)    NOT NULL,
        [Department]                  NVARCHAR(100)    NULL,
        [DepartmentId]                UNIQUEIDENTIFIER NULL,
        [Location]                    NVARCHAR(250)    NULL,
        [EmploymentType]              NVARCHAR(50)     NULL,
        [ExperienceLevel]             NVARCHAR(50)     NULL,
        [SeniorityLevel]              NVARCHAR(50)     NULL,
        [SalaryMin]                   DECIMAL(18, 2)   NULL,
        [SalaryMax]                   DECIMAL(18, 2)   NULL,
        [Currency]                    NVARCHAR(10)     NOT NULL DEFAULT ('VND'),
        [Description]                 NVARCHAR(MAX)    NULL,
        [Requirements]                NVARCHAR(MAX)    NULL,
        [Benefits]                    NVARCHAR(MAX)    NULL,
        [NumberOfPositions]           INT              NULL DEFAULT (1),
        [Status]                      NVARCHAR(50)     NOT NULL DEFAULT ('OPEN'),
        [CreatedBy]                   UNIQUEIDENTIFIER NULL,
        [CreatedAt]                   DATETIME2(7)     NOT NULL DEFAULT (SYSUTCDATETIME()),
        [UpdatedAt]                   DATETIME2(7)     NULL,
        [ClosedAt]                    DATETIME2(7)     NULL,
        [ClosedById]                  UNIQUEIDENTIFIER NULL,
        [ClosedByName]                NVARCHAR(200)    NULL,
        [Deadline]                    DATETIME2(7)     NULL,
        [IsDeleted]                   BIT              NOT NULL DEFAULT (0),
        -- Cột bổ sung V2, V3, V5:
        [CampaignFormSource]          NVARCHAR(50)     NULL DEFAULT ('INTERNAL'),
        [GoogleFormId]                NVARCHAR(200)    NULL,
        [GoogleFormPublicUrl]         NVARCHAR(500)    NULL,
        [GoogleFormEditUrl]           NVARCHAR(500)    NULL,
        [QrCodeImagePath]             NVARCHAR(500)    NULL,
        [RecruitmentTrack]            NVARCHAR(30)     NOT NULL DEFAULT ('PROFESSIONAL'),
        [TargetQuantity]              INT              NOT NULL DEFAULT (50),
        [HiredCount]                  INT              NOT NULL DEFAULT (0),
        [WaitlistBufferPercent]       INT              NOT NULL DEFAULT (15),
        [CurrentScoringWeightVersion] INT              NOT NULL DEFAULT (1),
        [GoogleFormUrl]               NVARCHAR(500)    NULL,
        [RequisitionStatus]           NVARCHAR(30)     NOT NULL DEFAULT ('Approved'),
        [RequestedByUserId]           UNIQUEIDENTIFIER NULL,
        [ApprovedByUserId]            UNIQUEIDENTIFIER NULL,
        [ApprovedAt]                  DATETIME2(7)     NULL,
        [RequisitionNote]             NVARCHAR(500)    NULL,
        [RequireNightShift]           BIT              NOT NULL DEFAULT (0),
        CONSTRAINT [PK_Jobs] PRIMARY KEY CLUSTERED ([JobId] ASC),
        CONSTRAINT [UQ_Jobs_Code] UNIQUE NONCLUSTERED ([Code] ASC),
        CONSTRAINT [FK_Jobs_Departments] FOREIGN KEY ([DepartmentId]) REFERENCES [dbo].[Departments] ([DepartmentId]),
        CONSTRAINT [FK_Jobs_Users_CreatedBy] FOREIGN KEY ([CreatedBy]) REFERENCES [dbo].[Users] ([UserId])
    );
    CREATE NONCLUSTERED INDEX [IX_Jobs_DepartmentId] ON [dbo].[Jobs]([DepartmentId]);
    CREATE NONCLUSTERED INDEX [IX_Jobs_Status] ON [dbo].[Jobs]([Status]);
    CREATE NONCLUSTERED INDEX [IX_Jobs_RecruitmentTrack] ON [dbo].[Jobs]([RecruitmentTrack]);
END
GO

-- 16. BẢNG HỒ SƠ ỨNG VIÊN (Candidates)
IF OBJECT_ID('dbo.Candidates', 'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[Candidates] (
        [CandidateId]    UNIQUEIDENTIFIER NOT NULL DEFAULT (NEWID()),
        [UserId]         UNIQUEIDENTIFIER NULL,
        [FullName]       NVARCHAR(200)    NOT NULL,
        [Email]          NVARCHAR(256)    NULL,
        [Phone]          NVARCHAR(50)     NULL,
        [DateOfBirth]    DATETIME2(7)     NULL,
        [CitizenId]      NVARCHAR(20)     NULL,
        [CurrentAddress] NVARCHAR(500)    NULL,
        [Hometown]       NVARCHAR(200)    NULL,
        [EducationLevel] NVARCHAR(100)    NULL,
        [Headline]       NVARCHAR(250)    NULL,
        [Summary]        NVARCHAR(MAX)    NULL,
        [CreatedAt]      DATETIME2(7)     NOT NULL DEFAULT (SYSUTCDATETIME()),
        [UpdatedAt]      DATETIME2(7)     NULL,
        [IsDeleted]      BIT              NOT NULL DEFAULT (0),
        CONSTRAINT [PK_Candidates] PRIMARY KEY CLUSTERED ([CandidateId] ASC),
        CONSTRAINT [FK_Candidates_Users] FOREIGN KEY ([UserId]) REFERENCES [dbo].[Users] ([UserId]) ON DELETE SET NULL
    );
    CREATE NONCLUSTERED INDEX [IX_Candidates_CitizenId] ON [dbo].[Candidates]([CitizenId]);
    CREATE NONCLUSTERED INDEX [IX_Candidates_Phone] ON [dbo].[Candidates]([Phone]);
    CREATE NONCLUSTERED INDEX [IX_Candidates_Email] ON [dbo].[Candidates]([Email]);
END
GO

-- 17. BẢNG ÁNH XẠ KỸ NĂNG VÀ TRỌNG SỐ CHO CÔNG VIỆC (JobSkillMap)
IF OBJECT_ID('dbo.JobSkillMap', 'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[JobSkillMap] (
        [JobId]   UNIQUEIDENTIFIER NOT NULL,
        [SkillId] UNIQUEIDENTIFIER NOT NULL,
        [Weight]  TINYINT          NOT NULL DEFAULT (1),
        CONSTRAINT [PK_JobSkillMap] PRIMARY KEY CLUSTERED ([JobId] ASC, [SkillId] ASC),
        CONSTRAINT [FK_JobSkillMap_Jobs] FOREIGN KEY ([JobId]) REFERENCES [dbo].[Jobs] ([JobId]) ON DELETE CASCADE,
        CONSTRAINT [FK_JobSkillMap_Skills] FOREIGN KEY ([SkillId]) REFERENCES [dbo].[Skills] ([SkillId]) ON DELETE CASCADE
    );
END
GO

-- 18. BẢNG ÁNH XẠ THẺ NHÃN CÔNG VIỆC (JobTagMap)
IF OBJECT_ID('dbo.JobTagMap', 'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[JobTagMap] (
        [JobId] UNIQUEIDENTIFIER NOT NULL,
        [TagId] UNIQUEIDENTIFIER NOT NULL,
        CONSTRAINT [PK_JobTagMap] PRIMARY KEY CLUSTERED ([JobId] ASC, [TagId] ASC),
        CONSTRAINT [FK_JobTagMap_Jobs] FOREIGN KEY ([JobId]) REFERENCES [dbo].[Jobs] ([JobId]) ON DELETE CASCADE,
        CONSTRAINT [FK_JobTagMap_JobTags] FOREIGN KEY ([TagId]) REFERENCES [dbo].[JobTags] ([TagId]) ON DELETE CASCADE
    );
END
GO

-- 19. BẢNG LƯU VIỆC LÀM YÊU THÍCH (SavedJobs)
IF OBJECT_ID('dbo.SavedJobs', 'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[SavedJobs] (
        [CandidateId] UNIQUEIDENTIFIER NOT NULL,
        [JobId]       UNIQUEIDENTIFIER NOT NULL,
        [SavedAt]     DATETIME2(7)     NOT NULL DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT [PK_SavedJobs] PRIMARY KEY CLUSTERED ([CandidateId] ASC, [JobId] ASC),
        CONSTRAINT [FK_SavedJobs_Candidates] FOREIGN KEY ([CandidateId]) REFERENCES [dbo].[Candidates] ([CandidateId]) ON DELETE CASCADE,
        CONSTRAINT [FK_SavedJobs_Jobs] FOREIGN KEY ([JobId]) REFERENCES [dbo].[Jobs] ([JobId]) ON DELETE CASCADE
    );
END
GO

-- 20. BẢNG TÀI LIỆU ỨNG VIÊN / CV ĐÍNH KÈM (CandidateDocuments)
IF OBJECT_ID('dbo.CandidateDocuments', 'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[CandidateDocuments] (
        [DocumentId]   UNIQUEIDENTIFIER NOT NULL DEFAULT (NEWID()),
        [CandidateId]  UNIQUEIDENTIFIER NOT NULL,
        [FileId]       UNIQUEIDENTIFIER NOT NULL,
        [DocumentType] NVARCHAR(50)     NOT NULL DEFAULT ('CV'),
        [Title]        NVARCHAR(250)    NULL,
        [IsPrimary]    BIT              NOT NULL DEFAULT (0),
        [CreatedAt]    DATETIME2(7)     NOT NULL DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT [PK_CandidateDocuments] PRIMARY KEY CLUSTERED ([DocumentId] ASC),
        CONSTRAINT [FK_CandidateDocuments_Candidates] FOREIGN KEY ([CandidateId]) REFERENCES [dbo].[Candidates] ([CandidateId]) ON DELETE CASCADE,
        CONSTRAINT [FK_CandidateDocuments_Files] FOREIGN KEY ([FileId]) REFERENCES [dbo].[Files] ([FileId])
    );
END
GO

-- 21. BẢNG HỌC VẤN ỨNG VIÊN (CandidateEducations)
IF OBJECT_ID('dbo.CandidateEducations', 'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[CandidateEducations] (
        [EducationId] UNIQUEIDENTIFIER NOT NULL DEFAULT (NEWID()),
        [CandidateId] UNIQUEIDENTIFIER NOT NULL,
        [SchoolName]  NVARCHAR(250)    NOT NULL,
        [Degree]      NVARCHAR(100)    NULL,
        [Major]       NVARCHAR(200)    NULL,
        [StartDate]   DATETIME2(7)     NULL,
        [EndDate]     DATETIME2(7)     NULL,
        [Gpa]         DECIMAL(4, 2)    NULL,
        [Description] NVARCHAR(MAX)    NULL,
        CONSTRAINT [PK_CandidateEducations] PRIMARY KEY CLUSTERED ([EducationId] ASC),
        CONSTRAINT [FK_CandidateEducations_Candidates] FOREIGN KEY ([CandidateId]) REFERENCES [dbo].[Candidates] ([CandidateId]) ON DELETE CASCADE
    );
END
GO

-- 22. BẢNG KINH NGHIỆM LÀM VIỆC ỨNG VIÊN (CandidateExperiences)
IF OBJECT_ID('dbo.CandidateExperiences', 'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[CandidateExperiences] (
        [ExperienceId] UNIQUEIDENTIFIER NOT NULL DEFAULT (NEWID()),
        [CandidateId]  UNIQUEIDENTIFIER NOT NULL,
        [CompanyName]  NVARCHAR(250)    NOT NULL,
        [Position]     NVARCHAR(100)    NOT NULL,
        [StartDate]    DATETIME2(7)     NULL,
        [EndDate]      DATETIME2(7)     NULL,
        [IsCurrent]    BIT              NOT NULL DEFAULT (0),
        [Description]  NVARCHAR(MAX)    NULL,
        CONSTRAINT [PK_CandidateExperiences] PRIMARY KEY CLUSTERED ([ExperienceId] ASC),
        CONSTRAINT [FK_CandidateExperiences_Candidates] FOREIGN KEY ([CandidateId]) REFERENCES [dbo].[Candidates] ([CandidateId]) ON DELETE CASCADE
    );
END
GO

-- 23. BẢNG KỸ NĂNG CỦA ỨNG VIÊN (CandidateSkills)
IF OBJECT_ID('dbo.CandidateSkills', 'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[CandidateSkills] (
        [CandidateId]        UNIQUEIDENTIFIER NOT NULL,
        [SkillId]            UNIQUEIDENTIFIER NOT NULL,
        [ProficiencyLevel]   NVARCHAR(50)     NULL,
        [YearsOfExperience]  INT              NULL,
        CONSTRAINT [PK_CandidateSkills] PRIMARY KEY CLUSTERED ([CandidateId] ASC, [SkillId] ASC),
        CONSTRAINT [FK_CandidateSkills_Candidates] FOREIGN KEY ([CandidateId]) REFERENCES [dbo].[Candidates] ([CandidateId]) ON DELETE CASCADE,
        CONSTRAINT [FK_CandidateSkills_Skills] FOREIGN KEY ([SkillId]) REFERENCES [dbo].[Skills] ([SkillId]) ON DELETE CASCADE
    );
END
GO

-- 24. BẢNG THƯ XIN VIỆC (CoverLetters)
IF OBJECT_ID('dbo.CoverLetters', 'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[CoverLetters] (
        [CoverLetterId] UNIQUEIDENTIFIER NOT NULL DEFAULT (NEWID()),
        [CandidateId]   UNIQUEIDENTIFIER NOT NULL,
        [JobId]         UNIQUEIDENTIFIER NULL,
        [Title]         NVARCHAR(250)    NULL,
        [Content]       NVARCHAR(MAX)    NOT NULL,
        [CreatedAt]     DATETIME2(7)     NOT NULL DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT [PK_CoverLetters] PRIMARY KEY CLUSTERED ([CoverLetterId] ASC),
        CONSTRAINT [FK_CoverLetters_Candidates] FOREIGN KEY ([CandidateId]) REFERENCES [dbo].[Candidates] ([CandidateId]) ON DELETE CASCADE,
        CONSTRAINT [FK_CoverLetters_Jobs] FOREIGN KEY ([JobId]) REFERENCES [dbo].[Jobs] ([JobId])
    );
END
GO

-- 25. BẢNG CA PHỎNG VẤN TẬP TRUNG / HỘI ĐỒNG (InterviewBatches - V5 Factory Operations)
IF OBJECT_ID('dbo.InterviewBatches', 'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[InterviewBatches] (
        [Id]              INT IDENTITY(1,1) NOT NULL,
        [BatchCode]       NVARCHAR(50)      NOT NULL,
        [JobId]           UNIQUEIDENTIFIER  NOT NULL,
        [ShiftSlot]       NVARCHAR(50)      NOT NULL DEFAULT ('MORNING_08_11'),
        [ShiftName]       NVARCHAR(100)     NOT NULL,
        [ScheduledDate]   DATE              NOT NULL,
        [LocationRoom]    NVARCHAR(200)     NOT NULL,
        [MaxCapacity]     INT               NOT NULL DEFAULT (50),
        [CurrentEnrolled] INT               NOT NULL DEFAULT (0),
        [InterviewerId]   UNIQUEIDENTIFIER  NULL,
        [Status]          NVARCHAR(30)      NOT NULL DEFAULT ('Open'),
        [CreatedAt]       DATETIME2(7)      NOT NULL DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT [PK_InterviewBatches] PRIMARY KEY CLUSTERED ([Id] ASC),
        CONSTRAINT [UQ_InterviewBatches_Code] UNIQUE NONCLUSTERED ([BatchCode] ASC),
        CONSTRAINT [FK_InterviewBatches_Jobs] FOREIGN KEY ([JobId]) REFERENCES [dbo].[Jobs] ([JobId]),
        CONSTRAINT [FK_InterviewBatches_Users] FOREIGN KEY ([InterviewerId]) REFERENCES [dbo].[Users] ([UserId])
    );
END
GO

-- 26. BẢNG QUẢN LÝ ĐƠN ỨNG TUYỂN (Applications - Đầy đủ 100% cột V2, V3, V5)
IF OBJECT_ID('dbo.Applications', 'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[Applications] (
        [ApplicationId]              UNIQUEIDENTIFIER NOT NULL DEFAULT (NEWID()),
        [JobId]                      UNIQUEIDENTIFIER NOT NULL,
        [CandidateId]                UNIQUEIDENTIFIER NOT NULL,
        [CurrentStageId]             UNIQUEIDENTIFIER NOT NULL,
        [AppliedAt]                  DATETIME2(7)     NOT NULL DEFAULT (SYSUTCDATETIME()),
        [LastStageChangedAt]         DATETIME2(7)     NOT NULL DEFAULT (SYSUTCDATETIME()),
        [ResumeDocumentId]           UNIQUEIDENTIFIER NULL,
        [Source]                     NVARCHAR(50)     NULL,
        [Status]                     NVARCHAR(50)     NOT NULL DEFAULT ('PENDING'),
        [ContactName]                NVARCHAR(200)    NULL,
        [ContactEmail]               NVARCHAR(256)    NULL,
        [ContactPhone]               NVARCHAR(50)     NULL,
        -- Cột nghiệp vụ & Vận hành KCN (V2, V3, V5):
        [ApplicationSource]          NVARCHAR(30)     NOT NULL DEFAULT ('PDF_UPLOAD'),
        [ScoringMethod]              NVARCHAR(30)     NOT NULL DEFAULT ('GEMINI_AI'),
        [CvFileUrl]                  NVARCHAR(500)    NULL,
        [CitizenId]                  NVARCHAR(20)     NULL,
        [PhoneNormalized]            NVARCHAR(20)     NULL,
        [DuplicateCount]             INT              NOT NULL DEFAULT (1),
        [PreviousApplicationId]      INT              NULL,
        [IsBlacklisted]              BIT              NOT NULL DEFAULT (0),
        [BlacklistWarningNote]       NVARCHAR(500)    NULL,
        [IsScannedOrUnparseablePdf]  BIT              NOT NULL DEFAULT (0),
        [RawResumeText]              NVARCHAR(MAX)    NULL,
        [AiAnalysisSummary]          NVARCHAR(MAX)    NULL,
        [AppliedScoringVersion]      INT              NULL DEFAULT (1),
        [AppliedScoringConfigJson]   NVARCHAR(MAX)    NULL,
        [OralInterviewChecklistJson] NVARCHAR(MAX)    NULL,
        [MedicalCheckStatus]         NVARCHAR(30)     NOT NULL DEFAULT ('Pending'),
        [BankAccountNumber]          NVARCHAR(50)     NULL,
        [BankName]                   NVARCHAR(100)    NULL,
        [OnboardingDocsCompleted]    BIT              NOT NULL DEFAULT (0),
        [ConvertedEmployeeCode]      NVARCHAR(50)     NULL,
        [SlaDeadlineAt]              DATETIME2(7)     NULL,
        [IsSlaEscalated]             BIT              NOT NULL DEFAULT (0),
        [SlaOverrideReason]          NVARCHAR(500)    NULL,
        [DateOfBirth]                DATE             NULL,
        [IsPiiMaskedBeforeAi]        BIT              NOT NULL DEFAULT (1),
        [InterviewScore]             DECIMAL(5, 2)    NULL,
        [IsAiOverriddenByHuman]      BIT              NOT NULL DEFAULT (0),
        [AiOverrideReason]           NVARCHAR(500)    NULL,
        [OfferStatus]                NVARCHAR(30)     NOT NULL DEFAULT ('Not_Required'),
        [OfferedSalary]              DECIMAL(18, 2)   NULL,
        [OfferResponseNote]          NVARCHAR(500)    NULL,
        [TransferredFromJobId]       UNIQUEIDENTIFIER NULL,
        [TransferReason]             NVARCHAR(500)    NULL,
        [IsExEmployeeRehire]         BIT              NOT NULL DEFAULT (0),
        [PreviousEmployeeCode]       NVARCHAR(50)     NULL,
        [InterviewRescheduleCount]   INT              NOT NULL DEFAULT (0),
        [OfferExpiresAt]             DATETIME2(7)     NULL,
        [IsInTalentPool]             BIT              NOT NULL DEFAULT (0),
        [Probation30DayStatus]       NVARCHAR(30)     NOT NULL DEFAULT ('Not_Started'),
        [InterviewBatchId]           INT              NULL,
        [RowVersion]                 ROWVERSION,
        CONSTRAINT [PK_Applications] PRIMARY KEY CLUSTERED ([ApplicationId] ASC),
        CONSTRAINT [FK_Applications_Jobs] FOREIGN KEY ([JobId]) REFERENCES [dbo].[Jobs] ([JobId]),
        CONSTRAINT [FK_Applications_Candidates] FOREIGN KEY ([CandidateId]) REFERENCES [dbo].[Candidates] ([CandidateId]),
        CONSTRAINT [FK_Applications_PipelineStages] FOREIGN KEY ([CurrentStageId]) REFERENCES [dbo].[PipelineStages] ([StageId]),
        CONSTRAINT [FK_Applications_CandidateDocuments] FOREIGN KEY ([ResumeDocumentId]) REFERENCES [dbo].[CandidateDocuments] ([DocumentId])
    );
    CREATE NONCLUSTERED INDEX [IX_Applications_JobId] ON [dbo].[Applications]([JobId]);
    CREATE NONCLUSTERED INDEX [IX_Applications_CandidateId] ON [dbo].[Applications]([CandidateId]);
    CREATE NONCLUSTERED INDEX [IX_Applications_CurrentStageId] ON [dbo].[Applications]([CurrentStageId]);
    CREATE NONCLUSTERED INDEX [IX_Applications_Status] ON [dbo].[Applications]([Status]);
    CREATE NONCLUSTERED INDEX [IX_Applications_CitizenId] ON [dbo].[Applications]([CitizenId]);
    CREATE NONCLUSTERED INDEX [IX_Applications_PhoneNormalized] ON [dbo].[Applications]([PhoneNormalized]);
END
GO

-- 27. BẢNG KẾT QUẢ CHẤM ĐIỂM AI CHO CV (ApplicationAiScores)
IF OBJECT_ID('dbo.ApplicationAiScores', 'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[ApplicationAiScores] (
        [AiScoreId]         UNIQUEIDENTIFIER NOT NULL DEFAULT (NEWID()),
        [ApplicationId]     UNIQUEIDENTIFIER NOT NULL,
        [MatchingScore]     DECIMAL(5, 2)    NOT NULL,
        [MatchedSkillsJson] NVARCHAR(MAX)    NULL,
        [BreakdownJson]     NVARCHAR(MAX)    NULL,
        [Model]             NVARCHAR(100)    NOT NULL DEFAULT ('gemini-2.5-flash'),
        [CreatedAt]         DATETIME2(7)     NOT NULL DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT [PK_ApplicationAiScores] PRIMARY KEY CLUSTERED ([AiScoreId] ASC),
        CONSTRAINT [FK_ApplicationAiScores_Applications] FOREIGN KEY ([ApplicationId]) REFERENCES [dbo].[Applications] ([ApplicationId]) ON DELETE CASCADE
    );
    CREATE NONCLUSTERED INDEX [IX_ApplicationAiScores_AppId] ON [dbo].[ApplicationAiScores]([ApplicationId]);
END
GO

-- 28. BẢNG GHI CHÚ NỘI BỘ VỀ ỨNG VIÊN (ApplicationNotes)
IF OBJECT_ID('dbo.ApplicationNotes', 'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[ApplicationNotes] (
        [NoteId]        UNIQUEIDENTIFIER NOT NULL DEFAULT (NEWID()),
        [ApplicationId] UNIQUEIDENTIFIER NOT NULL,
        [CreatedBy]     UNIQUEIDENTIFIER NOT NULL,
        [Content]       NVARCHAR(MAX)    NOT NULL,
        [CreatedAt]     DATETIME2(7)     NOT NULL DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT [PK_ApplicationNotes] PRIMARY KEY CLUSTERED ([NoteId] ASC),
        CONSTRAINT [FK_ApplicationNotes_Applications] FOREIGN KEY ([ApplicationId]) REFERENCES [dbo].[Applications] ([ApplicationId]) ON DELETE CASCADE,
        CONSTRAINT [FK_ApplicationNotes_Users] FOREIGN KEY ([CreatedBy]) REFERENCES [dbo].[Users] ([UserId])
    );
END
GO

-- 29. BẢNG LỊCH SỬ CHUYỂN BƯỚC TUYỂN DỤNG (ApplicationStageHistory)
IF OBJECT_ID('dbo.ApplicationStageHistory', 'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[ApplicationStageHistory] (
        [HistoryId]     UNIQUEIDENTIFIER NOT NULL DEFAULT (NEWID()),
        [ApplicationId] UNIQUEIDENTIFIER NOT NULL,
        [FromStageId]   UNIQUEIDENTIFIER NULL,
        [ToStageId]     UNIQUEIDENTIFIER NOT NULL,
        [ChangedBy]     UNIQUEIDENTIFIER NOT NULL,
        [ChangedAt]     DATETIME2(7)     NOT NULL DEFAULT (SYSUTCDATETIME()),
        [Note]          NVARCHAR(500)    NULL,
        CONSTRAINT [PK_ApplicationStageHistory] PRIMARY KEY CLUSTERED ([HistoryId] ASC),
        CONSTRAINT [FK_ApplicationStageHistory_Applications] FOREIGN KEY ([ApplicationId]) REFERENCES [dbo].[Applications] ([ApplicationId]) ON DELETE CASCADE,
        CONSTRAINT [FK_ApplicationStageHistory_FromStage] FOREIGN KEY ([FromStageId]) REFERENCES [dbo].[PipelineStages] ([StageId]),
        CONSTRAINT [FK_ApplicationStageHistory_ToStage] FOREIGN KEY ([ToStageId]) REFERENCES [dbo].[PipelineStages] ([StageId]),
        CONSTRAINT [FK_ApplicationStageHistory_Users] FOREIGN KEY ([ChangedBy]) REFERENCES [dbo].[Users] ([UserId])
    );
END
GO

-- 30. BẢNG THEO DÕI LƯỢT XEM HỒ SƠ (ApplicationViews)
IF OBJECT_ID('dbo.ApplicationViews', 'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[ApplicationViews] (
        [ViewId]        UNIQUEIDENTIFIER NOT NULL DEFAULT (NEWID()),
        [ApplicationId] UNIQUEIDENTIFIER NOT NULL,
        [ViewedBy]      UNIQUEIDENTIFIER NOT NULL,
        [ViewedAt]      DATETIME2(7)     NOT NULL DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT [PK_ApplicationViews] PRIMARY KEY CLUSTERED ([ViewId] ASC),
        CONSTRAINT [FK_ApplicationViews_Applications] FOREIGN KEY ([ApplicationId]) REFERENCES [dbo].[Applications] ([ApplicationId]) ON DELETE CASCADE,
        CONSTRAINT [FK_ApplicationViews_Users] FOREIGN KEY ([ViewedBy]) REFERENCES [dbo].[Users] ([UserId])
    );
END
GO

-- 31. BẢNG NGÂN HÀNG CÂU HỎI TRẮC NGHIỆM KỸ THUẬT (QuestionBank)
IF OBJECT_ID('dbo.QuestionBank', 'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[QuestionBank] (
        [BankQuestionId] UNIQUEIDENTIFIER NOT NULL DEFAULT (NEWID()),
        [Content]        NVARCHAR(MAX)    NOT NULL,
        [Type]           NVARCHAR(50)     NOT NULL DEFAULT ('MCQ_SINGLE'),
        [Difficulty]     NVARCHAR(20)     NOT NULL DEFAULT ('MEDIUM'),
        [Tags]           NVARCHAR(500)    NULL,
        [Explanation]    NVARCHAR(MAX)    NULL,
        [IsActive]       BIT              NOT NULL DEFAULT (1),
        [CreatedBy]      UNIQUEIDENTIFIER NULL,
        [CreatedAt]      DATETIME2(7)     NOT NULL DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT [PK_QuestionBank] PRIMARY KEY CLUSTERED ([BankQuestionId] ASC),
        CONSTRAINT [FK_QuestionBank_Users] FOREIGN KEY ([CreatedBy]) REFERENCES [dbo].[Users] ([UserId])
    );
END
GO

-- 32. BẢNG CÁC LỰA CHỌN ĐÁP ÁN TRẮC NGHIỆM (QuestionOptions)
IF OBJECT_ID('dbo.QuestionOptions', 'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[QuestionOptions] (
        [OptionId]       UNIQUEIDENTIFIER NOT NULL DEFAULT (NEWID()),
        [BankQuestionId] UNIQUEIDENTIFIER NOT NULL,
        [Content]        NVARCHAR(MAX)    NOT NULL,
        [IsCorrect]      BIT              NOT NULL DEFAULT (0),
        [SortOrder]      INT              NOT NULL DEFAULT (1),
        CONSTRAINT [PK_QuestionOptions] PRIMARY KEY CLUSTERED ([OptionId] ASC),
        CONSTRAINT [FK_QuestionOptions_QuestionBank] FOREIGN KEY ([BankQuestionId]) REFERENCES [dbo].[QuestionBank] ([BankQuestionId]) ON DELETE CASCADE
    );
END
GO

-- 33. BẢNG ĐỀ THI / BÀI ĐÁNH GIÁ NĂNG LỰC (Assessments)
IF OBJECT_ID('dbo.Assessments', 'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[Assessments] (
        [AssessmentId]           UNIQUEIDENTIFIER NOT NULL DEFAULT (NEWID()),
        [JobId]                  UNIQUEIDENTIFIER NULL,
        [Name]                   NVARCHAR(250)    NOT NULL,
        [Description]            NVARCHAR(MAX)    NULL,
        [Mode]                   NVARCHAR(50)     NOT NULL DEFAULT ('FIXED'),
        [DurationMinutes]        INT              NOT NULL DEFAULT (30),
        [PassScore]              DECIMAL(5, 2)    NOT NULL DEFAULT (50.0),
        [ShowResultToCandidate]  BIT              NOT NULL DEFAULT (1),
        [IsActive]               BIT              NOT NULL DEFAULT (1),
        [CreatedBy]              UNIQUEIDENTIFIER NULL,
        [CreatedAt]              DATETIME2(7)     NOT NULL DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT [PK_Assessments] PRIMARY KEY CLUSTERED ([AssessmentId] ASC),
        CONSTRAINT [FK_Assessments_Jobs] FOREIGN KEY ([JobId]) REFERENCES [dbo].[Jobs] ([JobId]),
        CONSTRAINT [FK_Assessments_Users] FOREIGN KEY ([CreatedBy]) REFERENCES [dbo].[Users] ([UserId])
    );
END
GO

-- 34. BẢNG ÁNH XẠ CÂU HỎI VÀO ĐỀ THI (AssessmentQuestions)
IF OBJECT_ID('dbo.AssessmentQuestions', 'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[AssessmentQuestions] (
        [AssessmentId]   UNIQUEIDENTIFIER NOT NULL,
        [BankQuestionId] UNIQUEIDENTIFIER NOT NULL,
        [SortOrder]      INT              NOT NULL DEFAULT (1),
        [Points]         DECIMAL(5, 2)    NOT NULL DEFAULT (1.0),
        CONSTRAINT [PK_AssessmentQuestions] PRIMARY KEY CLUSTERED ([AssessmentId] ASC, [BankQuestionId] ASC),
        CONSTRAINT [FK_AssessmentQuestions_Assessments] FOREIGN KEY ([AssessmentId]) REFERENCES [dbo].[Assessments] ([AssessmentId]) ON DELETE CASCADE,
        CONSTRAINT [FK_AssessmentQuestions_QuestionBank] FOREIGN KEY ([BankQuestionId]) REFERENCES [dbo].[QuestionBank] ([BankQuestionId]) ON DELETE CASCADE
    );
END
GO

-- 35. BẢNG MÃ MỜI THI TRỰC TUYẾN (AssessmentInvites)
IF OBJECT_ID('dbo.AssessmentInvites', 'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[AssessmentInvites] (
        [InviteId]      UNIQUEIDENTIFIER NOT NULL DEFAULT (NEWID()),
        [AssessmentId]  UNIQUEIDENTIFIER NOT NULL,
        [ApplicationId] UNIQUEIDENTIFIER NOT NULL,
        [InviteCode]    NVARCHAR(50)     NOT NULL,
        [ExpiresAt]     DATETIME2(7)     NOT NULL,
        [Status]        NVARCHAR(50)     NOT NULL DEFAULT ('Pending'),
        [CreatedAt]     DATETIME2(7)     NOT NULL DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT [PK_AssessmentInvites] PRIMARY KEY CLUSTERED ([InviteId] ASC),
        CONSTRAINT [UQ_AssessmentInvites_Code] UNIQUE NONCLUSTERED ([InviteCode] ASC),
        CONSTRAINT [FK_AssessmentInvites_Assessments] FOREIGN KEY ([AssessmentId]) REFERENCES [dbo].[Assessments] ([AssessmentId]),
        CONSTRAINT [FK_AssessmentInvites_Applications] FOREIGN KEY ([ApplicationId]) REFERENCES [dbo].[Applications] ([ApplicationId]) ON DELETE CASCADE
    );
END
GO

-- 36. BẢNG LƯỢT LÀM BÀI THI CỦA ỨNG VIÊN (AssessmentAttempts)
IF OBJECT_ID('dbo.AssessmentAttempts', 'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[AssessmentAttempts] (
        [AttemptId]   UNIQUEIDENTIFIER NOT NULL DEFAULT (NEWID()),
        [InviteId]    UNIQUEIDENTIFIER NOT NULL,
        [StartedAt]   DATETIME2(7)     NOT NULL DEFAULT (SYSUTCDATETIME()),
        [SubmittedAt] DATETIME2(7)     NULL,
        [TotalScore]  DECIMAL(5, 2)    NULL,
        [IsPassed]    BIT              NULL,
        [Status]      NVARCHAR(50)     NOT NULL DEFAULT ('InProgress'),
        CONSTRAINT [PK_AssessmentAttempts] PRIMARY KEY CLUSTERED ([AttemptId] ASC),
        CONSTRAINT [FK_AssessmentAttempts_AssessmentInvites] FOREIGN KEY ([InviteId]) REFERENCES [dbo].[AssessmentInvites] ([InviteId]) ON DELETE CASCADE
    );
END
GO

-- 37. BẢNG CÂU TRẢ LỜI CỦA ỨNG VIÊN (AssessmentAnswers)
IF OBJECT_ID('dbo.AssessmentAnswers', 'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[AssessmentAnswers] (
        [AnswerId]         UNIQUEIDENTIFIER NOT NULL DEFAULT (NEWID()),
        [AttemptId]        UNIQUEIDENTIFIER NOT NULL,
        [BankQuestionId]   UNIQUEIDENTIFIER NOT NULL,
        [SelectedOptionId] UNIQUEIDENTIFIER NULL,
        [TextAnswer]       NVARCHAR(MAX)    NULL,
        [Score]            DECIMAL(5, 2)    NULL,
        [EvaluatedAt]      DATETIME2(7)     NULL,
        CONSTRAINT [PK_AssessmentAnswers] PRIMARY KEY CLUSTERED ([AnswerId] ASC),
        CONSTRAINT [FK_AssessmentAnswers_Attempts] FOREIGN KEY ([AttemptId]) REFERENCES [dbo].[AssessmentAttempts] ([AttemptId]) ON DELETE CASCADE,
        CONSTRAINT [FK_AssessmentAnswers_QuestionBank] FOREIGN KEY ([BankQuestionId]) REFERENCES [dbo].[QuestionBank] ([BankQuestionId]),
        CONSTRAINT [FK_AssessmentAnswers_Options] FOREIGN KEY ([SelectedOptionId]) REFERENCES [dbo].[QuestionOptions] ([OptionId])
    );
END
GO

-- 38. BẢNG NHẬT KÝ CHỐNG GIAN LẬN KHI THI (AntiCheatEvents)
IF OBJECT_ID('dbo.AntiCheatEvents', 'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[AntiCheatEvents] (
        [EventId]   UNIQUEIDENTIFIER NOT NULL DEFAULT (NEWID()),
        [AttemptId] UNIQUEIDENTIFIER NOT NULL,
        [EventType] NVARCHAR(100)    NOT NULL,
        [Details]   NVARCHAR(MAX)    NULL,
        [LoggedAt]  DATETIME2(7)     NOT NULL DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT [PK_AntiCheatEvents] PRIMARY KEY CLUSTERED ([EventId] ASC),
        CONSTRAINT [FK_AntiCheatEvents_Attempts] FOREIGN KEY ([AttemptId]) REFERENCES [dbo].[AssessmentAttempts] ([AttemptId]) ON DELETE CASCADE
    );
END
GO

-- 39. BẢNG LỊCH PHỎNG VẤN (Interviews)
IF OBJECT_ID('dbo.Interviews', 'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[Interviews] (
        [InterviewId]    UNIQUEIDENTIFIER NOT NULL DEFAULT (NEWID()),
        [ApplicationId]  UNIQUEIDENTIFIER NOT NULL,
        [ScheduledStart] DATETIME2(7)     NOT NULL,
        [ScheduledEnd]   DATETIME2(7)     NOT NULL,
        [Location]       NVARCHAR(250)    NULL,
        [MeetingLink]    NVARCHAR(500)    NULL,
        [Status]         NVARCHAR(50)     NOT NULL DEFAULT ('SCHEDULED'),
        [Notes]          NVARCHAR(MAX)    NULL,
        [CreatedAt]      DATETIME2(7)     NOT NULL DEFAULT (SYSUTCDATETIME()),
        [CreatedBy]      UNIQUEIDENTIFIER NULL,
        CONSTRAINT [PK_Interviews] PRIMARY KEY CLUSTERED ([InterviewId] ASC),
        CONSTRAINT [FK_Interviews_Applications] FOREIGN KEY ([ApplicationId]) REFERENCES [dbo].[Applications] ([ApplicationId]) ON DELETE CASCADE,
        CONSTRAINT [FK_Interviews_Users] FOREIGN KEY ([CreatedBy]) REFERENCES [dbo].[Users] ([UserId])
    );
    CREATE NONCLUSTERED INDEX [IX_Interviews_AppId] ON [dbo].[Interviews]([ApplicationId]);
    CREATE NONCLUSTERED INDEX [IX_Interviews_Start] ON [dbo].[Interviews]([ScheduledStart]);
END
GO

-- 40. BẢNG HỘI ĐỒNG PHỎNG VẤN VIÊN (Interviewers)
IF OBJECT_ID('dbo.Interviewers', 'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[Interviewers] (
        [InterviewId] UNIQUEIDENTIFIER NOT NULL,
        [UserId]      UNIQUEIDENTIFIER NOT NULL,
        [IsPrimary]   BIT              NOT NULL DEFAULT (0),
        CONSTRAINT [PK_Interviewers] PRIMARY KEY CLUSTERED ([InterviewId] ASC, [UserId] ASC),
        CONSTRAINT [FK_Interviewers_Interviews] FOREIGN KEY ([InterviewId]) REFERENCES [dbo].[Interviews] ([InterviewId]) ON DELETE CASCADE,
        CONSTRAINT [FK_Interviewers_Users] FOREIGN KEY ([UserId]) REFERENCES [dbo].[Users] ([UserId])
    );
END
GO

-- 41. BẢNG PHIẾU ĐIỂM TỔNG HỢP PHỎNG VẤN (InterviewScorecards)
IF OBJECT_ID('dbo.InterviewScorecards', 'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[InterviewScorecards] (
        [ScorecardId]     UNIQUEIDENTIFIER NOT NULL DEFAULT (NEWID()),
        [InterviewId]     UNIQUEIDENTIFIER NOT NULL,
        [OverallScore]    DECIMAL(5, 2)    NULL,
        [Recommendation]  NVARCHAR(50)     NULL,
        [SummaryComments] NVARCHAR(MAX)    NULL,
        [CreatedAt]       DATETIME2(7)     NOT NULL DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT [PK_InterviewScorecards] PRIMARY KEY CLUSTERED ([ScorecardId] ASC),
        CONSTRAINT [FK_InterviewScorecards_Interviews] FOREIGN KEY ([InterviewId]) REFERENCES [dbo].[Interviews] ([InterviewId]) ON DELETE CASCADE
    );
END
GO

-- 42. BẢNG ĐÁNH GIÁ CỦA TỪNG PHỎNG VẤN VIÊN (InterviewFeedback)
IF OBJECT_ID('dbo.InterviewFeedback', 'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[InterviewFeedback] (
        [FeedbackId]    UNIQUEIDENTIFIER NOT NULL DEFAULT (NEWID()),
        [InterviewId]   UNIQUEIDENTIFIER NOT NULL,
        [InterviewerId] UNIQUEIDENTIFIER NOT NULL,
        [Rating]        INT              NOT NULL DEFAULT (3),
        [Comments]      NVARCHAR(MAX)    NULL,
        [CreatedAt]     DATETIME2(7)     NOT NULL DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT [PK_InterviewFeedback] PRIMARY KEY CLUSTERED ([FeedbackId] ASC),
        CONSTRAINT [FK_InterviewFeedback_Interviews] FOREIGN KEY ([InterviewId]) REFERENCES [dbo].[Interviews] ([InterviewId]) ON DELETE CASCADE,
        CONSTRAINT [FK_InterviewFeedback_Users] FOREIGN KEY ([InterviewerId]) REFERENCES [dbo].[Users] ([UserId])
    );
END
GO

-- 43. BẢNG ĐIỂM CHI TIẾT THEO TIÊU CHÍ (InterviewEvaluations)
IF OBJECT_ID('dbo.InterviewEvaluations', 'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[InterviewEvaluations] (
        [EvaluationId]  UNIQUEIDENTIFIER NOT NULL DEFAULT (NEWID()),
        [InterviewId]   UNIQUEIDENTIFIER NOT NULL,
        [CriterionName] NVARCHAR(100)    NOT NULL,
        [Score]         DECIMAL(5, 2)    NOT NULL DEFAULT (0.0),
        [Comments]      NVARCHAR(MAX)    NULL,
        CONSTRAINT [PK_InterviewEvaluations] PRIMARY KEY CLUSTERED ([EvaluationId] ASC),
        CONSTRAINT [FK_InterviewEvaluations_Interviews] FOREIGN KEY ([InterviewId]) REFERENCES [dbo].[Interviews] ([InterviewId]) ON DELETE CASCADE
    );
END
GO

-- 44. BẢNG CÂU HỎI THƯỜNG GẶP CHATBOT AI (ChatbotFaqs)
IF OBJECT_ID('dbo.ChatbotFaqs', 'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[ChatbotFaqs] (
        [Id]        INT IDENTITY(1,1) NOT NULL,
        [Question]  NVARCHAR(500)     NOT NULL,
        [Answer]    NVARCHAR(MAX)     NOT NULL,
        [Category]  NVARCHAR(100)     NULL,
        [Keywords]  NVARCHAR(500)     NULL,
        [Priority]  INT               NOT NULL DEFAULT (0),
        [IsActive]  BIT               NOT NULL DEFAULT (1),
        [CreatedAt] DATETIME2(7)      NOT NULL DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT [PK_ChatbotFaqs] PRIMARY KEY CLUSTERED ([Id] ASC)
    );
END
GO

-- 45. BẢNG PHIÊN HỘI THOẠI CHATBOT (ChatSessions)
IF OBJECT_ID('dbo.ChatSessions', 'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[ChatSessions] (
        [SessionId]     UNIQUEIDENTIFIER NOT NULL DEFAULT (NEWID()),
        [ApplicationId] UNIQUEIDENTIFIER NULL,
        [CandidateId]   UNIQUEIDENTIFIER NULL,
        [StartedAt]     DATETIME2(7)     NOT NULL DEFAULT (SYSUTCDATETIME()),
        [EndedAt]       DATETIME2(7)     NULL,
        [Status]        NVARCHAR(50)     NOT NULL DEFAULT ('ACTIVE'),
        CONSTRAINT [PK_ChatSessions] PRIMARY KEY CLUSTERED ([SessionId] ASC),
        CONSTRAINT [FK_ChatSessions_Applications] FOREIGN KEY ([ApplicationId]) REFERENCES [dbo].[Applications] ([ApplicationId]) ON DELETE SET NULL,
        CONSTRAINT [FK_ChatSessions_Candidates] FOREIGN KEY ([CandidateId]) REFERENCES [dbo].[Candidates] ([CandidateId]) ON DELETE SET NULL
    );
END
GO

-- 46. BẢNG TIN NHẮN CHATBOT (ChatMessages)
IF OBJECT_ID('dbo.ChatMessages', 'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[ChatMessages] (
        [MessageId]  UNIQUEIDENTIFIER NOT NULL DEFAULT (NEWID()),
        [SessionId]  UNIQUEIDENTIFIER NOT NULL,
        [SenderType] NVARCHAR(50)     NOT NULL,
        [Content]    NVARCHAR(MAX)    NOT NULL,
        [SentAt]     DATETIME2(7)     NOT NULL DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT [PK_ChatMessages] PRIMARY KEY CLUSTERED ([MessageId] ASC),
        CONSTRAINT [FK_ChatMessages_ChatSessions] FOREIGN KEY ([SessionId]) REFERENCES [dbo].[ChatSessions] ([SessionId]) ON DELETE CASCADE
    );
END
GO

-- 47. BẢNG PHÂN TÍCH CHỈ SỐ HỘI THOẠI CHATBOT (ChatAnalytics)
IF OBJECT_ID('dbo.ChatAnalytics', 'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[ChatAnalytics] (
        [AnalyticsId] UNIQUEIDENTIFIER NOT NULL DEFAULT (NEWID()),
        [SessionId]   UNIQUEIDENTIFIER NOT NULL,
        [MetricName]  NVARCHAR(100)    NOT NULL,
        [MetricValue] DECIMAL(18, 2)   NOT NULL,
        [RecordedAt]  DATETIME2(7)     NOT NULL DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT [PK_ChatAnalytics] PRIMARY KEY CLUSTERED ([AnalyticsId] ASC),
        CONSTRAINT [FK_ChatAnalytics_ChatSessions] FOREIGN KEY ([SessionId]) REFERENCES [dbo].[ChatSessions] ([SessionId]) ON DELETE CASCADE
    );
END
GO

-- 48. BẢNG ĐÁNH GIÁ TRẢI NGHIỆM CHATBOT (ChatFeedbacks)
IF OBJECT_ID('dbo.ChatFeedbacks', 'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[ChatFeedbacks] (
        [FeedbackId] UNIQUEIDENTIFIER NOT NULL DEFAULT (NEWID()),
        [SessionId]  UNIQUEIDENTIFIER NOT NULL,
        [Rating]     INT              NOT NULL,
        [Comment]    NVARCHAR(MAX)    NULL,
        [CreatedAt]  DATETIME2(7)     NOT NULL DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT [PK_ChatFeedbacks] PRIMARY KEY CLUSTERED ([FeedbackId] ASC),
        CONSTRAINT [FK_ChatFeedbacks_ChatSessions] FOREIGN KEY ([SessionId]) REFERENCES [dbo].[ChatSessions] ([SessionId]) ON DELETE CASCADE
    );
END
GO

-- 49. BẢNG CÂU HỎI TIỀN SÀNG LỌC (PreScreenQuestions)
IF OBJECT_ID('dbo.PreScreenQuestions', 'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[PreScreenQuestions] (
        [PreScreenQuestionId] UNIQUEIDENTIFIER NOT NULL DEFAULT (NEWID()),
        [JobId]               UNIQUEIDENTIFIER NOT NULL,
        [QuestionText]        NVARCHAR(500)    NOT NULL,
        [QuestionType]        NVARCHAR(50)     NOT NULL DEFAULT ('TEXT'),
        [IsRequired]          BIT              NOT NULL DEFAULT (1),
        [SortOrder]           INT              NOT NULL DEFAULT (1),
        CONSTRAINT [PK_PreScreenQuestions] PRIMARY KEY CLUSTERED ([PreScreenQuestionId] ASC),
        CONSTRAINT [FK_PreScreenQuestions_Jobs] FOREIGN KEY ([JobId]) REFERENCES [dbo].[Jobs] ([JobId]) ON DELETE CASCADE
    );
END
GO

-- 50. BẢNG CÂU TRẢ LỜI TIỀN SÀNG LỌC (PreScreenAnswers)
IF OBJECT_ID('dbo.PreScreenAnswers', 'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[PreScreenAnswers] (
        [AnswerId]            UNIQUEIDENTIFIER NOT NULL DEFAULT (NEWID()),
        [ApplicationId]       UNIQUEIDENTIFIER NOT NULL,
        [PreScreenQuestionId] UNIQUEIDENTIFIER NOT NULL,
        [AnswerText]          NVARCHAR(MAX)    NOT NULL,
        [AnsweredAt]          DATETIME2(7)     NOT NULL DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT [PK_PreScreenAnswers] PRIMARY KEY CLUSTERED ([AnswerId] ASC),
        CONSTRAINT [FK_PreScreenAnswers_Applications] FOREIGN KEY ([ApplicationId]) REFERENCES [dbo].[Applications] ([ApplicationId]) ON DELETE CASCADE,
        CONSTRAINT [FK_PreScreenAnswers_PreScreenQuestions] FOREIGN KEY ([PreScreenQuestionId]) REFERENCES [dbo].[PreScreenQuestions] ([PreScreenQuestionId])
    );
END
GO

-- 51. BẢNG MẪU EMAIL TỰ ĐỘNG (EmailTemplates)
IF OBJECT_ID('dbo.EmailTemplates', 'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[EmailTemplates] (
        [TemplateId] UNIQUEIDENTIFIER NOT NULL DEFAULT (NEWID()),
        [Code]       NVARCHAR(50)     NOT NULL,
        [Subject]    NVARCHAR(250)    NOT NULL,
        [BodyHtml]   NVARCHAR(MAX)    NOT NULL,
        [BodyText]   NVARCHAR(MAX)    NULL,
        [IsActive]   BIT              NOT NULL DEFAULT (1),
        [CreatedAt]  DATETIME2(7)     NOT NULL DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT [PK_EmailTemplates] PRIMARY KEY CLUSTERED ([TemplateId] ASC),
        CONSTRAINT [UQ_EmailTemplates_Code] UNIQUE NONCLUSTERED ([Code] ASC)
    );
END
GO

-- 52. BẢNG QUY TẮC TỰ ĐỘNG HÓA PIPELINE (AutomationRules)
IF OBJECT_ID('dbo.AutomationRules', 'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[AutomationRules] (
        [RuleId]            UNIQUEIDENTIFIER NOT NULL DEFAULT (NEWID()),
        [Name]              NVARCHAR(200)    NOT NULL,
        [TriggerEvent]      NVARCHAR(100)    NOT NULL,
        [ConditionJson]     NVARCHAR(MAX)    NULL,
        [ActionType]        NVARCHAR(100)    NOT NULL,
        [ActionPayloadJson] NVARCHAR(MAX)    NULL,
        [IsActive]          BIT              NOT NULL DEFAULT (1),
        [CreatedAt]         DATETIME2(7)     NOT NULL DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT [PK_AutomationRules] PRIMARY KEY CLUSTERED ([RuleId] ASC)
    );
END
GO

-- 53. BẢNG HÀNG ĐỢI GỬI EMAIL (EmailQueue)
IF OBJECT_ID('dbo.EmailQueue', 'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[EmailQueue] (
        [QueueId]      UNIQUEIDENTIFIER NOT NULL DEFAULT (NEWID()),
        [ToEmail]      NVARCHAR(256)    NOT NULL,
        [Subject]      NVARCHAR(250)    NOT NULL,
        [Body]         NVARCHAR(MAX)    NOT NULL,
        [Status]       NVARCHAR(50)     NOT NULL DEFAULT ('Pending'),
        [RetryCount]   INT              NOT NULL DEFAULT (0),
        [ScheduledAt]  DATETIME2(7)     NOT NULL DEFAULT (SYSUTCDATETIME()),
        [SentAt]       DATETIME2(7)     NULL,
        [ErrorMessage] NVARCHAR(MAX)    NULL,
        [CreatedAt]    DATETIME2(7)     NOT NULL DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT [PK_EmailQueue] PRIMARY KEY CLUSTERED ([QueueId] ASC)
    );
END
GO

-- 54. BẢNG LỊCH SỬ GỬI EMAIL (EmailSendLog)
IF OBJECT_ID('dbo.EmailSendLog', 'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[EmailSendLog] (
        [LogId]            UNIQUEIDENTIFIER NOT NULL DEFAULT (NEWID()),
        [ToEmail]          NVARCHAR(256)    NOT NULL,
        [Subject]          NVARCHAR(250)    NOT NULL,
        [SentAt]           DATETIME2(7)     NOT NULL DEFAULT (SYSUTCDATETIME()),
        [Status]           NVARCHAR(50)     NOT NULL,
        [ProviderResponse] NVARCHAR(MAX)    NULL,
        CONSTRAINT [PK_EmailSendLog] PRIMARY KEY CLUSTERED ([LogId] ASC)
    );
END
GO

-- 55. BẢNG ĐỀ THI THỬ THÁCH LẬP TRÌNH (CodeChallenge)
IF OBJECT_ID('dbo.CodeChallenge', 'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[CodeChallenge] (
        [ChallengeId]      UNIQUEIDENTIFIER NOT NULL DEFAULT (NEWID()),
        [JobId]            UNIQUEIDENTIFIER NOT NULL,
        [Title]            NVARCHAR(250)    NOT NULL,
        [ProblemStatement] NVARCHAR(MAX)    NOT NULL,
        [InitialCodeText]  NVARCHAR(MAX)    NULL,
        [Language]         NVARCHAR(50)     NOT NULL DEFAULT ('csharp'),
        [Difficulty]       NVARCHAR(20)     NOT NULL DEFAULT ('Medium'),
        [MaxTimeMinutes]   INT              NOT NULL DEFAULT (45),
        [ExpectedOutput]   NVARCHAR(MAX)    NULL,
        [CreatedAt]        DATETIME2(7)     NOT NULL DEFAULT (SYSUTCDATETIME()),
        [IsActive]         BIT              NOT NULL DEFAULT (1),
        CONSTRAINT [PK_CodeChallenge] PRIMARY KEY CLUSTERED ([ChallengeId] ASC),
        CONSTRAINT [FK_CodeChallenge_Jobs] FOREIGN KEY ([JobId]) REFERENCES [dbo].[Jobs] ([JobId]) ON DELETE CASCADE
    );
END
GO

-- 56. BẢNG BÀI NỘP MÃ NGUỒN CỦA ỨNG VIÊN (CandidateCodeSubmission)
IF OBJECT_ID('dbo.CandidateCodeSubmission', 'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[CandidateCodeSubmission] (
        [SubmissionId]    UNIQUEIDENTIFIER NOT NULL DEFAULT (NEWID()),
        [ChallengeId]     UNIQUEIDENTIFIER NOT NULL,
        [CandidateId]     UNIQUEIDENTIFIER NOT NULL,
        [CodeText]        NVARCHAR(MAX)    NOT NULL,
        [Language]        NVARCHAR(50)     NOT NULL,
        [Status]          NVARCHAR(50)     NOT NULL DEFAULT ('Submitted'),
        [ExecutionResult] NVARCHAR(MAX)    NULL,
        [SubmittedAt]     DATETIME2(7)     NOT NULL DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT [PK_CandidateCodeSubmission] PRIMARY KEY CLUSTERED ([SubmissionId] ASC),
        CONSTRAINT [FK_CandidateCodeSubmission_Challenge] FOREIGN KEY ([ChallengeId]) REFERENCES [dbo].[CodeChallenge] ([ChallengeId]) ON DELETE CASCADE,
        CONSTRAINT [FK_CandidateCodeSubmission_Candidate] FOREIGN KEY ([CandidateId]) REFERENCES [dbo].[Candidates] ([CandidateId]) ON DELETE CASCADE
    );
END
GO

-- 57. BẢNG ĐÁNH GIÁ MÃ NGUỒN BẰNG AI (AiCodeReviewReport)
IF OBJECT_ID('dbo.AiCodeReviewReport', 'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[AiCodeReviewReport] (
        [ReportId]         UNIQUEIDENTIFIER NOT NULL DEFAULT (NEWID()),
        [SubmissionId]     UNIQUEIDENTIFIER NOT NULL,
        [Score]            DECIMAL(5, 2)    NOT NULL,
        [Feedback]         NVARCHAR(MAX)    NULL,
        [CleanCodeScore]   DECIMAL(5, 2)    NULL,
        [PerformanceScore] DECIMAL(5, 2)    NULL,
        [SecurityScore]    DECIMAL(5, 2)    NULL,
        [ReviewedAt]       DATETIME2(7)     NOT NULL DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT [PK_AiCodeReviewReport] PRIMARY KEY CLUSTERED ([ReportId] ASC),
        CONSTRAINT [FK_AiCodeReviewReport_Submission] FOREIGN KEY ([SubmissionId]) REFERENCES [dbo].[CandidateCodeSubmission] ([SubmissionId]) ON DELETE CASCADE
    );
END
GO

-- ====================================================================================================
-- PHẦN II: NẠP DỮ LIỆU KHỞI TẠO ĐẦY ĐỦ (DML - INSERT SEED DATA CHUẨN TIẾNG VIỆT UNIKEY)
-- ====================================================================================================

-- 1. SEED 7 PHÒNG BAN MA TRẬN NHÀ MÁY / KHU CÔNG NGHIỆP
IF NOT EXISTS (SELECT 1 FROM [dbo].[Departments] WHERE [DepartmentCode] = 'ADMIN_SYS')
BEGIN
    INSERT INTO [dbo].[Departments] ([DepartmentId], [DepartmentCode], [DepartmentName], [Description], [IsActive], [CreatedAt])
    VALUES
    ('10000000-0000-0000-0000-000000000001', 'ADMIN_SYS', N'Ban Giám Đốc', N'Cấp quản lý cao nhất – Ban Giám đốc và Quản trị hệ thống ATS.', 1, SYSUTCDATETIME()),
    ('10000000-0000-0000-0000-000000000002', 'HR_TEAM',   N'Phòng Nhân Sự (HR)', N'Tuyển dụng, sàng lọc hồ sơ, quản lý pipeline ứng viên toàn nhà máy.', 1, SYSUTCDATETIME()),
    ('10000000-0000-0000-0000-000000000003', 'INT_TEAM',   N'Hội Đồng Phỏng Vấn', N'Thực hiện phỏng vấn chuyên môn, chấm điểm và phản hồi ứng viên.', 1, SYSUTCDATETIME()),
    ('10000000-0000-0000-0000-000000000004', 'PROD_LINE', N'Xưởng Sản Xuất & Lắp Ráp SMT', N'Trực tiếp sản xuất, vận hành máy SMT, lắp ráp sản phẩm theo dây chuyền công nghiệp.', 1, SYSUTCDATETIME()),
    ('10000000-0000-0000-0000-000000000005', 'QA_QC',     N'Phòng Quản Lý Chất Lượng (QA/QC)', N'Kiểm tra, kiểm soát chất lượng linh kiện đầu vào và sản phẩm đầu ra.', 1, SYSUTCDATETIME()),
    ('10000000-0000-0000-0000-000000000006', 'WH_LOGS',   N'Khu Vực Kho & Logistics', N'Quản lý kho nguyên vật liệu, đóng gói, xuất-nhập kho tại KCN.', 1, SYSUTCDATETIME()),
    ('10000000-0000-0000-0000-000000000007', 'TECH_MNT',  N'Phòng Kỹ Thuật & Bảo Trì', N'Lập trình PLC, bảo dưỡng máy CNC, hệ thống cơ-điện trong khu sản xuất.', 1, SYSUTCDATETIME());
    PRINT N'✓ Đã nạp 7 Phòng ban ma trận KCN';
END
GO

-- 2. SEED 5 VAI TRÒ HỆ THỐNG (Roles)
IF NOT EXISTS (SELECT 1 FROM [dbo].[Roles] WHERE [Code] = 'ADMIN')
BEGIN
    INSERT INTO [dbo].[Roles] ([RoleId], [Code], [Name], [CreatedAt])
    VALUES
    ('20000000-0000-0000-0000-000000000001', 'ADMIN',       N'Ban Giám Đốc / Administrator', SYSUTCDATETIME()),
    ('20000000-0000-0000-0000-000000000002', 'MANAGER',     N'Trưởng Phòng / Quản Lý Phân Xưởng', SYSUTCDATETIME()),
    ('20000000-0000-0000-0000-000000000003', 'HR',          N'Chuyên Viên Tuyển Dụng (HR)', SYSUTCDATETIME()),
    ('20000000-0000-0000-0000-000000000004', 'INTERVIEWER', N'Hội Đồng Phỏng Vấn (Interviewer)', SYSUTCDATETIME()),
    ('20000000-0000-0000-0000-000000000005', 'CANDIDATE',   N'Ứng Viên (Candidate)', SYSUTCDATETIME());
    PRINT N'✓ Đã nạp 5 Roles hệ thống';
END
GO

-- 3. SEED 5 VÒNG TUYỂN DỤNG CHUẨN ATS (PipelineStages)
IF NOT EXISTS (SELECT 1 FROM [dbo].[PipelineStages] WHERE [Code] = 'NEW_APPLIED')
BEGIN
    INSERT INTO [dbo].[PipelineStages] ([StageId], [Code], [Name], [SortOrder], [IsTerminal], [CreatedAt], [SlaMaxDays], [SlaWarnBeforeDays], [IsSlaEnabled])
    VALUES
    ('40000000-0000-0000-0000-000000000001', 'NEW_APPLIED', N'Tiếp nhận hồ sơ', 1, 0, SYSUTCDATETIME(), 3, 1, 1),
    ('40000000-0000-0000-0000-000000000002', 'TEST_PASSED', N'Đã qua bài Test / Đánh giá', 2, 0, SYSUTCDATETIME(), 5, 1, 1),
    ('40000000-0000-0000-0000-000000000003', 'INTERVIEW',   N'Phỏng vấn chuyên môn', 3, 0, SYSUTCDATETIME(), 7, 2, 1),
    ('40000000-0000-0000-0000-000000000004', 'OFFER',       N'Đề nghị tuyển dụng (Offer)', 4, 0, SYSUTCDATETIME(), 5, 1, 1),
    ('40000000-0000-0000-0000-000000000005', 'REJECTED',    N'Từ chối hồ sơ', 5, 1, SYSUTCDATETIME(), NULL, NULL, 0);
    PRINT N'✓ Đã nạp 5 Pipeline Stages';
END
GO

-- 4. SEED HÌNH THỨC LÀM VIỆC (JobTypes)
IF NOT EXISTS (SELECT 1 FROM [dbo].[JobTypes])
BEGIN
    INSERT INTO [dbo].[JobTypes] ([JobTypeId], [Name], [Description])
    VALUES
    (NEWID(), N'Full-time',  N'Toàn thời gian – Làm việc 8 giờ/ngày theo ca hoặc giờ hành chính'),
    (NEWID(), N'Part-time',  N'Bán thời gian – Làm việc linh hoạt theo ca phân bổ'),
    (NEWID(), N'Contract',   N'Hợp đồng – Làm việc theo dự án ngắn hạn hoặc thỏa ước lao động'),
    (NEWID(), N'Internship', N'Thực tập sinh – Chương trình đào tạo và thực hành tại doanh nghiệp'),
    (NEWID(), N'Freelance',  N'Tự do – Chuyên gia tư vấn hoặc cộng tác viên độc lập'),
    (NEWID(), N'Remote',     N'Từ xa – Làm việc trực tuyến qua hệ thống');
    PRINT N'✓ Đã nạp JobTypes';
END
GO

-- 5. SEED THẺ NHÃN CÔNG VIỆC (JobTags)
IF NOT EXISTS (SELECT 1 FROM [dbo].[JobTags])
BEGIN
    INSERT INTO [dbo].[JobTags] ([TagId], [Name], [CreatedAt])
    VALUES
    (NEWID(), N'Tuyển gấp', SYSUTCDATETIME()),
    (NEWID(), N'Khu Công Nghiệp', SYSUTCDATETIME()),
    (NEWID(), N'Có xe đưa đón', SYSUTCDATETIME()),
    (NEWID(), N'Lương thưởng cạnh tranh', SYSUTCDATETIME()),
    (NEWID(), N'Làm việc theo ca 12h', SYSUTCDATETIME()),
    (NEWID(), N'Đào tạo tay nghề', SYSUTCDATETIME()),
    (NEWID(), N'Phụ cấp chuyên cần', SYSUTCDATETIME());
    PRINT N'✓ Đã nạp JobTags';
END
GO

-- 6. SEED KỸ NĂNG CHUẨN CÔNG NGHIỆP & CNTT (Skills)
IF NOT EXISTS (SELECT 1 FROM [dbo].[Skills])
BEGIN
    INSERT INTO [dbo].[Skills] ([SkillId], [Name], [NormalizedName], [CreatedAt], [IsDeleted])
    VALUES
    -- CNTT & Phần mềm
    (NEWID(), N'C#', 'csharp', SYSUTCDATETIME(), 0),
    (NEWID(), N'.NET Core / .NET 8', 'dotnetcore', SYSUTCDATETIME(), 0),
    (NEWID(), N'ASP.NET Core Web API', 'aspnetcore', SYSUTCDATETIME(), 0),
    (NEWID(), N'Angular', 'angular', SYSUTCDATETIME(), 0),
    (NEWID(), N'TypeScript', 'typescript', SYSUTCDATETIME(), 0),
    (NEWID(), N'SQL Server', 'sqlserver', SYSUTCDATETIME(), 0),
    (NEWID(), N'Entity Framework Core', 'efcore', SYSUTCDATETIME(), 0),
    (NEWID(), N'Docker / Container', 'docker', SYSUTCDATETIME(), 0),
    (NEWID(), N'Google Gemini AI API', 'geminiai', SYSUTCDATETIME(), 0),
    -- Sản xuất & Cơ điện KCN
    (NEWID(), N'Lập trình PLC Siemens / Mitsubishi', 'plcscada', SYSUTCDATETIME(), 0),
    (NEWID(), N'Vận hành máy SMT & Khí nén', 'smtoperation', SYSUTCDATETIME(), 0),
    (NEWID(), N'Vận hành máy tiện/phay CNC', 'cncoperation', SYSUTCDATETIME(), 0),
    (NEWID(), N'Lắp ráp linh kiện điện tử', 'electronicassembly', SYSUTCDATETIME(), 0),
    (NEWID(), N'Kiểm tra chất lượng QA/QC', 'qaqcinspection', SYSUTCDATETIME(), 0),
    (NEWID(), N'Đọc bản vẽ kỹ thuật cơ khí', 'blueprints', SYSUTCDATETIME(), 0),
    (NEWID(), N'Tiêu chuẩn 5S & Kaizen', '5skaizen', SYSUTCDATETIME(), 0),
    (NEWID(), N'An toàn lao động & Vệ sinh HSE', 'hsesafety', SYSUTCDATETIME(), 0),
    (NEWID(), N'Hệ thống quản lý ISO 9001', 'iso9001', SYSUTCDATETIME(), 0);
    PRINT N'✓ Đã nạp danh sách Skills';
END
GO

-- 7. SEED DANH SÁCH USERS NỘI BỘ (Mật khẩu mặc định: Password123!)
-- Mã băm BCrypt chuẩn của 'Password123!': .p0vvc9334BjbAdCvkXIO883CvKFTO9G0.cs3Tjhgd.RxSpbZ6lu
IF NOT EXISTS (SELECT 1 FROM [dbo].[Users] WHERE [Email] = 'admin@gmail.com')
BEGIN
    DECLARE @DefaultHash NVARCHAR(500) = N'.p0vvc9334BjbAdCvkXIO883CvKFTO9G0.cs3Tjhgd.RxSpbZ6lu';
    DECLARE @dAdmin UNIQUEIDENTIFIER = '10000000-0000-0000-0000-000000000001';
    DECLARE @dHR    UNIQUEIDENTIFIER = '10000000-0000-0000-0000-000000000002';
    DECLARE @dINT   UNIQUEIDENTIFIER = '10000000-0000-0000-0000-000000000003';
    DECLARE @dPROD  UNIQUEIDENTIFIER = '10000000-0000-0000-0000-000000000004';
    DECLARE @dQA    UNIQUEIDENTIFIER = '10000000-0000-0000-0000-000000000005';
    DECLARE @dWH    UNIQUEIDENTIFIER = '10000000-0000-0000-0000-000000000006';
    DECLARE @dTECH  UNIQUEIDENTIFIER = '10000000-0000-0000-0000-000000000007';

    -- 7.1. Ban Giám Đốc & Super Admin
    INSERT INTO [dbo].[Users] ([UserId], [Email], [PasswordHash], [FullName], [Phone], [IsActive], [DepartmentId], [ManagerId], [AuthProvider], [CreatedAt])
    VALUES
    ('30000000-0000-0000-0000-000000000099', 'admin@gmail.com', @DefaultHash, N'Võ Văn Sơn (Super Admin)', '0965376312', 1, @dAdmin, NULL, 'Local', SYSUTCDATETIME()),
    ('30000000-0000-0000-0000-000000000001', 'giamdoc@factory.com', @DefaultHash, N'Nguyễn Văn Giám Đốc', '0901000001', 1, @dAdmin, NULL, 'Local', SYSUTCDATETIME()),
    ('30000000-0000-0000-0000-000000000002', 'pho.giamdoc@factory.com', @DefaultHash, N'Trần Thị Phó Giám Đốc', '0901000002', 1, @dAdmin, '30000000-0000-0000-0000-000000000001', 'Local', SYSUTCDATETIME()),

    -- 7.2. Phòng Nhân Sự (HR)
    ('30000000-0000-0000-0000-000000000010', 'truong.hr@factory.com', @DefaultHash, N'Lê Thị Trưởng HR', '0901000010', 1, @dHR, '30000000-0000-0000-0000-000000000001', 'Local', SYSUTCDATETIME()),
    ('30000000-0000-0000-0000-000000000011', 'hr1@factory.com', @DefaultHash, N'Phạm Văn Nhân Sự 1', '0901000011', 1, @dHR, '30000000-0000-0000-0000-000000000010', 'Local', SYSUTCDATETIME()),
    ('30000000-0000-0000-0000-000000000012', 'hr2@factory.com', @DefaultHash, N'Hoàng Thị Nhân Sự 2', '0901000012', 1, @dHR, '30000000-0000-0000-0000-000000000010', 'Local', SYSUTCDATETIME()),
    ('30000000-0000-0000-0000-000000000013', 'sonvp2012003@gmail.com', @DefaultHash, N'Võ Văn Sơn (Phó Tuyển Dụng)', '0909654321', 1, @dHR, '30000000-0000-0000-0000-000000000010', 'Local', SYSUTCDATETIME()),

    -- 7.3. Hội Đồng Phỏng Vấn (INTERVIEWER)
    ('30000000-0000-0000-0000-000000000020', 'truong.pv@factory.com', @DefaultHash, N'Ngô Văn Trưởng Ban Phỏng Vấn', '0901000020', 1, @dINT, '30000000-0000-0000-0000-000000000001', 'Local', SYSUTCDATETIME()),
    ('30000000-0000-0000-0000-000000000021', 'pv1@factory.com', @DefaultHash, N'Vũ Thị Phỏng Vấn Viên 1', '0901000021', 1, @dINT, '30000000-0000-0000-0000-000000000020', 'Local', SYSUTCDATETIME()),
    ('30000000-0000-0000-0000-000000000022', 'pv2@factory.com', @DefaultHash, N'Bùi Văn Phỏng Vấn Viên 2', '0901000022', 1, @dINT, '30000000-0000-0000-0000-000000000020', 'Local', SYSUTCDATETIME()),
    ('30000000-0000-0000-0000-000000000023', 'sonit2012003@gmail.com', @DefaultHash, N'Võ Văn Sơn (Tech Lead / Phỏng Vấn)', '0909123456', 1, @dINT, '30000000-0000-0000-0000-000000000020', 'Local', SYSUTCDATETIME()),

    -- 7.4. Quản Đốc & Trưởng Bộ Phận Chuyên Trách
    ('30000000-0000-0000-0000-000000000030', 'truong.sx@factory.com', @DefaultHash, N'Trịnh Văn Quản Đốc Xưởng SMT', '0901000030', 1, @dPROD, '30000000-0000-0000-0000-000000000001', 'Local', SYSUTCDATETIME()),
    ('30000000-0000-0000-0000-000000000040', 'truong.qa@factory.com', @DefaultHash, N'Phan Thị Trưởng QA/QC', '0901000040', 1, @dQA, '30000000-0000-0000-0000-000000000001', 'Local', SYSUTCDATETIME()),
    ('30000000-0000-0000-0000-000000000050', 'truong.kho@factory.com', @DefaultHash, N'Lương Văn Trưởng Kho', '0901000050', 1, @dWH, '30000000-0000-0000-0000-000000000001', 'Local', SYSUTCDATETIME()),
    ('30000000-0000-0000-0000-000000000060', 'truong.kt@factory.com', @DefaultHash, N'Hứa Văn Trưởng Kỹ Thuật Bảo Trì', '0901000060', 1, @dTECH, '30000000-0000-0000-0000-000000000001', 'Local', SYSUTCDATETIME());

    -- 7.5. Gán vai trò UserRoles
    DECLARE @rAdmin UNIQUEIDENTIFIER = '20000000-0000-0000-0000-000000000001';
    DECLARE @rMgr   UNIQUEIDENTIFIER = '20000000-0000-0000-0000-000000000002';
    DECLARE @rHR    UNIQUEIDENTIFIER = '20000000-0000-0000-0000-000000000003';
    DECLARE @rPV    UNIQUEIDENTIFIER = '20000000-0000-0000-0000-000000000004';

    INSERT INTO [dbo].[UserRoles] ([UserId], [RoleId], [CreatedAt])
    VALUES
    ('30000000-0000-0000-0000-000000000099', @rAdmin, SYSUTCDATETIME()),
    ('30000000-0000-0000-0000-000000000001', @rAdmin, SYSUTCDATETIME()),
    ('30000000-0000-0000-0000-000000000002', @rAdmin, SYSUTCDATETIME()),
    ('30000000-0000-0000-0000-000000000010', @rMgr,   SYSUTCDATETIME()),
    ('30000000-0000-0000-0000-000000000011', @rHR,    SYSUTCDATETIME()),
    ('30000000-0000-0000-0000-000000000012', @rHR,    SYSUTCDATETIME()),
    ('30000000-0000-0000-0000-000000000013', @rHR,    SYSUTCDATETIME()),
    ('30000000-0000-0000-0000-000000000020', @rMgr,   SYSUTCDATETIME()),
    ('30000000-0000-0000-0000-000000000021', @rPV,    SYSUTCDATETIME()),
    ('30000000-0000-0000-0000-000000000022', @rPV,    SYSUTCDATETIME()),
    ('30000000-0000-0000-0000-000000000023', @rPV,    SYSUTCDATETIME()),
    ('30000000-0000-0000-0000-000000000030', @rMgr,   SYSUTCDATETIME()),
    ('30000000-0000-0000-0000-000000000040', @rMgr,   SYSUTCDATETIME()),
    ('30000000-0000-0000-0000-000000000050', @rMgr,   SYSUTCDATETIME()),
    ('30000000-0000-0000-0000-000000000060', @rMgr,   SYSUTCDATETIME());
    PRINT N'✓ Đã nạp danh sách Users & UserRoles chuẩn';
END
GO

-- 8. SEED CÁC CHIẾN DỊCH TUYỂN DỤNG THỰC TẾ (Jobs Mẫu)
IF NOT EXISTS (SELECT 1 FROM [dbo].[Jobs] WHERE [JobId] = '50000000-0000-0000-0000-000000000001')
BEGIN
    DECLARE @uAdmin UNIQUEIDENTIFIER = '30000000-0000-0000-0000-000000000099';
    DECLARE @dAdmin UNIQUEIDENTIFIER = '10000000-0000-0000-0000-000000000001';
    DECLARE @dHR    UNIQUEIDENTIFIER = '10000000-0000-0000-0000-000000000002';
    DECLARE @dPROD  UNIQUEIDENTIFIER = '10000000-0000-0000-0000-000000000004';
    DECLARE @dQA    UNIQUEIDENTIFIER = '10000000-0000-0000-0000-000000000005';
    DECLARE @dTECH  UNIQUEIDENTIFIER = '10000000-0000-0000-0000-000000000007';

    INSERT INTO [dbo].[Jobs]
    (
        [JobId], [Code], [Title], [Department], [DepartmentId], [Location], [EmploymentType],
        [ExperienceLevel], [SeniorityLevel], [SalaryMin], [SalaryMax], [Currency],
        [Description], [Requirements], [Benefits], [NumberOfPositions], [Status],
        [RecruitmentTrack], [TargetQuantity], [HiredCount], [WaitlistBufferPercent],
        [RequisitionStatus], [RequireNightShift], [CreatedBy], [CreatedAt], [IsDeleted]
    )
    VALUES
    -- Job 1: Kỹ sư Lập trình C# / .NET Core Fullstack (Khối PROFESSIONAL)
    (
        '50000000-0000-0000-0000-000000000001',
        'JOB-2026-IT01',
        N'Kỹ sư Lập trình C# / .NET Core Fullstack',
        N'Ban Giám Đốc / CNTT',
        @dAdmin,
        N'Hà Nội & Khu Công Nghiệp',
        N'Full-time',
        N'2-4 năm',
        N'Mid-Senior',
        18000000, 30000000, N'VND',
        N'Phát triển và bảo trì hệ thống Quản trị tuyển dụng ATS, tích hợp AI screening và API nội bộ.',
        N'Thành thạo C#, ASP.NET Core, Entity Framework Core, SQL Server. Có kinh nghiệm Angular hoặc React.',
        N'Lương thưởng tháng 13+, bảo hiểm đầy đủ, máy tính làm việc hiệu năng cao.',
        3, N'OPEN', 'PROFESSIONAL', 3, 0, 15, 'Approved', 0, @uAdmin, SYSUTCDATETIME(), 0
    ),
    -- Job 2: Kỹ sư Tự động hóa PLC & Bảo trì Dây chuyền (Khối PROFESSIONAL)
    (
        '50000000-0000-0000-0000-000000000002',
        'JOB-2026-PLC01',
        N'Kỹ sư Tự động hóa PLC & Bảo trì Dây chuyền (Mitsubishi/Siemens)',
        N'Phòng Kỹ Thuật & Bảo Trì',
        @dTECH,
        N'KCN Yên Phong - Bắc Ninh',
        N'Full-time',
        N'2-3 năm',
        N'Mid-Senior',
        16000000, 25000000, N'VND',
        N'Lập trình, đấu nối và hiệu chỉnh hệ thống PLC Siemens S7-1200/1500, Mitsubishi FX5U; bảo trì biến tần, cảm biến, robot gắp linh kiện.',
        N'Tốt nghiệp ĐH/CĐ chuyên ngành Tự động hóa, Cơ điện tử, Điện công nghiệp. Nắm vững sơ đồ đấu nối tủ điện và phần mềm TIA Portal/GX Works.',
        N'Thưởng dự án theo tiến độ mở chuyền, phụ cấp điện thoại, xe đưa đón Hà Nội - Bắc Ninh.',
        5, N'OPEN', 'PROFESSIONAL', 5, 0, 15, 'Approved', 0, @uAdmin, SYSUTCDATETIME(), 0
    ),
    -- Job 3: Kỹ sư Quản lý Chất Lượng QA/QC Nhà Máy (Khối PROFESSIONAL)
    (
        '50000000-0000-0000-0000-000000000003',
        'JOB-2026-QA01',
        N'Kỹ sư Quản lý Chất Lượng QA/QC Nhà Máy',
        N'Phòng Quản Lý Chất Lượng (QA/QC)',
        @dQA,
        N'Khu Công Nghiệp Đình Vũ',
        N'Full-time',
        N'1-3 năm',
        N'Junior-Mid',
        12000000, 18000000, N'VND',
        N'Kiểm soát quy trình và chất lượng linh kiện, sản phẩm đầu ra theo tiêu chuẩn ISO 9001 và 5S.',
        N'Tốt nghiệp Cao đẳng/Đại học khối kỹ thuật. Am hiểu công cụ đo lường và thống kê lỗi SPC/Kaizen.',
        N'Xe đưa đón hàng ngày, phụ cấp ăn trưa tại căng tin, thưởng hiệu suất hàng quý.',
        5, N'OPEN', 'PROFESSIONAL', 5, 0, 15, 'Approved', 0, @uAdmin, SYSUTCDATETIME(), 0
    ),
    -- Job 4: Công nhân vận hành dây chuyền SMT (Tuyển gấp 100 người - Khối MASS_LABOR)
    (
        '60000000-0000-0000-0000-000000000101',
        'JOB-SMT-100',
        N'Công nhân vận hành dây chuyền SMT (Tuyển gấp 100 người)',
        N'Xưởng Sản Xuất & Lắp Ráp SMT',
        @dPROD,
        N'KCN Yên Phong - Bắc Ninh',
        N'Full-time',
        N'Không yêu cầu kinh nghiệm',
        N'Lao Động Phổ Thông',
        8500000, 13000000, N'VND',
        N'Vận hành máy cấp cuộn linh kiện SMT, kiểm tra bo mạch sau hàn dán, đóng gói sản phẩm hoàn thiện theo dây chuyền.',
        N'Nam/Nữ từ đủ 18 tuổi, tốt nghiệp THPT trở lên. Thị lực tốt (10/10), tay chân nhanh nhẹn, cam kết xoay ca kíp 12h (ca ngày/đêm).',
        N'Lương cơ bản + Phụ cấp chuyên cần 500k + Trợ cấp làm ca đêm 30% + Ký túc xá miễn phí có điều hòa nóng lạnh.',
        100, N'OPEN', 'MASS_LABOR', 100, 98, 15, 'Approved', 1, @uAdmin, SYSUTCDATETIME(), 0
    ),
    -- Job 5: Công nhân lắp ráp linh kiện cơ khí & máy CNC (Khối MASS_LABOR)
    (
        '60000000-0000-0000-0000-000000000102',
        'JOB-CNC-50',
        N'Công nhân lắp ráp linh kiện cơ khí & Máy CNC',
        N'Xưởng Sản Xuất & Lắp Ráp SMT',
        @dPROD,
        N'Hà Nội & KCN lân cận',
        N'Full-time',
        N'Dưới 1 năm / Mới tốt nghiệp',
        N'Fresher-Junior',
        9000000, 14000000, N'VND',
        N'Gá lắp phôi kim loại vào đồ gá máy CNC, đo kiểm kích thước bằng thước kẹp panme, vệ sinh bavia chi tiết máy.',
        N'Có bằng sơ cấp nghề/trung cấp cơ khí là một lợi thế. Chịu khó, cẩn thận, có khả năng làm việc theo ca.',
        N'Thưởng sản lượng hàng tháng, bao ăn 2 bữa chính tại nhà máy, chế độ bảo hiểm đầy đủ.',
        50, N'OPEN', 'MASS_LABOR', 50, 0, 15, 'Approved', 1, @uAdmin, SYSUTCDATETIME(), 0
    );
    PRINT N'✓ Đã nạp 5 Jobs mẫu thực tế (Kỹ sư & Công nhân KCN)';
END
GO

-- 9. SEED CƠ SỞ TRI THỨC CHATBOT AI (ChatbotFaqs)
IF NOT EXISTS (SELECT 1 FROM [dbo].[ChatbotFaqs])
BEGIN
    INSERT INTO [dbo].[ChatbotFaqs] ([Question], [Answer], [Category], [Keywords], [Priority], [IsActive])
    VALUES
    (N'Tôi muốn ứng tuyển thì làm thế nào?', N'Bạn chỉ cần vào danh sách Việc làm tuyển dụng, chọn vị trí phù hợp, nhấn nút "Ứng tuyển" và tải file CV định dạng PDF lên hệ thống để Gemini AI tự động phân tích độ phù hợp.', N'Ứng tuyển', N'ứng tuyển,apply,nộp hồ sơ,cv,hướng dẫn ứng tuyển', 100, 1),
    (N'Bao lâu thì nhận được phản hồi sau khi nộp hồ sơ?', N'Hệ thống AI sẽ sàng lọc và đối chiếu yêu cầu công việc ngay lập tức. Bạn có thể theo dõi trạng thái hồ sơ thời gian thực trong mục "Hồ sơ của tôi".', N'Ứng tuyển', N'phản hồi,kết quả,thời gian,chờ đợi,tiến độ hồ sơ', 90, 1),
    (N'Nhà máy có chế độ xe đưa đón và phụ cấp không?', N'Công ty có tuyến xe bus đưa đón cán bộ công nhân viên tại các điểm trung tâm thành phố đến khu công nghiệp, cùng phụ cấp ăn trưa tại căng tin và tiền chuyên cần hàng tháng.', N'Phúc lợi', N'xe đưa đón,phụ cấp,chế độ,ăn trưa,kcn', 85, 1),
    (N'Chatbot AI này hỗ trợ những nội dung gì?', N'Tôi hỗ trợ giải đáp chính sách tuyển dụng, vị trí đang tuyển, tiêu chuẩn kỹ năng, thông tin phỏng vấn và hướng dẫn làm bài test đánh giá năng lực.', N'Chung', N'chatbot,hỗ trợ,nội dung,hỏi đáp,tính năng', 80, 1);
    PRINT N'✓ Đã nạp ChatbotFaqs';
END
GO

-- 10. SEED MẪU THƯ ĐIỆN TỬ TỰ ĐỘNG (EmailTemplates)
IF NOT EXISTS (SELECT 1 FROM [dbo].[EmailTemplates])
BEGIN
    INSERT INTO [dbo].[EmailTemplates] ([TemplateId], [Code], [Subject], [BodyHtml], [BodyText], [IsActive], [CreatedAt])
    VALUES
    (
        NEWID(), 'INVITE_TEST',
        N'[V9 TECH / ATS] Thư mời tham gia bài kiểm tra đánh giá năng lực',
        N'<h3>Kính gửi Ứng viên,</h3><p>Hồ sơ của bạn đã vượt qua vòng sơ tuyển ban đầu. Chúng tôi trân trọng mời bạn tham gia bài kiểm tra năng lực trực tuyến.</p><p>Mã dự thi của bạn: <b>{{InviteCode}}</b></p><p>Hạn chót làm bài: {{ExpiresAt}}</p><p>Trân trọng,<br>Ban Tuyển Dụng</p>',
        N'Kính gửi Ứng viên, Hồ sơ của bạn đã vượt qua vòng sơ tuyển. Mã dự thi của bạn: {{InviteCode}}. Hạn chót: {{ExpiresAt}}',
        1, SYSUTCDATETIME()
    ),
    (
        NEWID(), 'INVITE_INTERVIEW',
        N'[V9 TECH / ATS] Thư mời tham gia phỏng vấn chuyên môn',
        N'<h3>Kính gửi Ứng viên,</h3><p>Chúng tôi trân trọng kính mời bạn tham gia buổi phỏng vấn chuyên môn cho vị trí ứng tuyển.</p><p>Thời gian: <b>{{ScheduledStart}}</b></p><p>Địa điểm: {{Location}}</p><p>Link trực tuyến (nếu có): {{MeetingLink}}</p><p>Trân trọng,<br>Hội Đồng Phỏng Vấn</p>',
        N'Kính gửi Ứng viên, Trân trọng mời bạn tham gia phỏng vấn lúc: {{ScheduledStart}}. Địa điểm: {{Location}}',
        1, SYSUTCDATETIME()
    ),
    (
        NEWID(), 'OFFER_LETTER',
        N'[V9 TECH / ATS] Thư mời nhận việc (Job Offer Letter)',
        N'<h3>Chúc mừng bạn!</h3><p>Hội đồng tuyển dụng trân trọng gửi đến bạn lời mời làm việc chính thức tại công ty.</p><p>Vui lòng xem chi tiết thỏa ước lương và phản hồi trong vòng 3 ngày làm việc.</p><p>Trân trọng,<br>Phòng Nhân Sự</p>',
        N'Chúc mừng bạn đã trúng tuyển. Vui lòng kiểm tra email chi tiết để hoàn tất thủ tục nhận việc.',
        1, SYSUTCDATETIME()
    ),
    (
        NEWID(), 'REJECTION',
        N'[V9 TECH / ATS] Cảm ơn bạn đã quan tâm ứng tuyển',
        N'<h3>Kính gửi Ứng viên,</h3><p>Cảm ơn bạn đã dành thời gian tham gia quy trình ứng tuyển. Dù rất tiếc chưa thể đồng hành cùng bạn ở vị trí này, thông tin của bạn đã được bảo lưu vào Kho Nhân Tài (Talent Pool) cho các cơ hội tiếp theo.</p><p>Chúc bạn luôn thành công!</p>',
        N'Cảm ơn bạn đã ứng tuyển. Chúc bạn luôn thành công trên con đường sự nghiệp.',
        1, SYSUTCDATETIME()
    );
    PRINT N'✓ Đã nạp EmailTemplates';
END
GO

-- 11. SEED BÀI TEST & NGÂN HÀNG CÂU HỎI TRẮC NGHIỆM MẪU
IF NOT EXISTS (SELECT 1 FROM [dbo].[QuestionBank])
BEGIN
    DECLARE @uAdminQ UNIQUEIDENTIFIER = '30000000-0000-0000-0000-000000000099';
    DECLARE @q1 UNIQUEIDENTIFIER = NEWID();
    DECLARE @q2 UNIQUEIDENTIFIER = NEWID();
    DECLARE @q3 UNIQUEIDENTIFIER = NEWID();

    -- Câu 1: Lập trình C#
    INSERT INTO [dbo].[QuestionBank] ([BankQuestionId], [Content], [Type], [Difficulty], [Tags], [Explanation], [IsActive], [CreatedBy], [CreatedAt])
    VALUES (@q1, N'Trong C#, từ khóa nào được sử dụng để giải phóng bộ nhớ tài nguyên unmanaged (kết hợp với giao diện IDisposable)?', N'MCQ_SINGLE', N'EASY', N'C#,.NET', N'Giao diện IDisposable và phương thức Dispose() kết hợp khối using giải phóng tài nguyên unmanaged.', 1, @uAdminQ, SYSUTCDATETIME());

    INSERT INTO [dbo].[QuestionOptions] ([OptionId], [BankQuestionId], [Content], [IsCorrect], [SortOrder])
    VALUES
    (NEWID(), @q1, N'using / IDisposable', 1, 1),
    (NEWID(), @q1, N'delete', 0, 2),
    (NEWID(), @q1, N'free', 0, 3),
    (NEWID(), @q1, N'finalize', 0, 4);

    -- Câu 2: Tiêu chuẩn 5S Nhà máy
    INSERT INTO [dbo].[QuestionBank] ([BankQuestionId], [Content], [Type], [Difficulty], [Tags], [Explanation], [IsActive], [CreatedBy], [CreatedAt])
    VALUES (@q2, N'Tiêu chuẩn 5S trong môi trường sản xuất công nghiệp bao gồm những bước nào?', N'MCQ_SINGLE', N'EASY', N'KCN,5S,QA', N'5S gồm: Sàng lọc (Seiri), Sắp xếp (Seiton), Sạch sẽ (Seiso), Săn sóc (Seiketsu), Sẵn sàng (Shitsuke).', 1, @uAdminQ, SYSUTCDATETIME());

    INSERT INTO [dbo].[QuestionOptions] ([OptionId], [BankQuestionId], [Content], [IsCorrect], [SortOrder])
    VALUES
    (NEWID(), @q2, N'Sàng lọc, Sắp xếp, Sạch sẽ, Săn sóc, Sẵn sàng', 1, 1),
    (NEWID(), @q2, N'Sản xuất, Sắp xếp, Sạch sẽ, Săn sóc, Sẵn sàng', 0, 2),
    (NEWID(), @q2, N'Sàng lọc, Sáng tạo, Sạch sẽ, Săn sóc, Sẵn sàng', 0, 3),
    (NEWID(), @q2, N'Sản lượng, Sắp xếp, Sạch sẽ, Săn sóc, Sáng suốt', 0, 4);

    -- Câu 3: Tối ưu SQL Server
    INSERT INTO [dbo].[QuestionBank] ([BankQuestionId], [Content], [Type], [Difficulty], [Tags], [Explanation], [IsActive], [CreatedBy], [CreatedAt])
    VALUES (@q3, N'Trong cơ sở dữ liệu SQL Server, thuộc tính nào cho phép tối ưu truy vấn bằng cách lưu trữ kết quả tính toán vật lý lên đĩa?', N'MCQ_SINGLE', N'MEDIUM', N'SQL,Database', N'Computed columns có từ khóa PERSISTED được lưu trữ vật lý trên ổ đĩa giúp tạo index hiệu quả.', 1, @uAdminQ, SYSUTCDATETIME());

    INSERT INTO [dbo].[QuestionOptions] ([OptionId], [BankQuestionId], [Content], [IsCorrect], [SortOrder])
    VALUES
    (NEWID(), @q3, N'PERSISTED', 1, 1),
    (NEWID(), @q3, N'CLUSTERED', 0, 2),
    (NEWID(), @q3, N'INDEXED', 0, 3),
    (NEWID(), @q3, N'STORED', 0, 4);

    -- Đề thi đánh giá năng lực đầu vào
    DECLARE @assId UNIQUEIDENTIFIER = NEWID();
    INSERT INTO [dbo].[Assessments] ([AssessmentId], [Name], [Mode], [DurationMinutes], [PassScore], [ShowResultToCandidate], [IsActive], [CreatedBy], [CreatedAt])
    VALUES (@assId, N'Bài Đánh Giá Năng Lực Đầu Vào Tổng Hợp 2026', N'FIXED', 30, 60.0, 1, 1, @uAdminQ, SYSUTCDATETIME());

    INSERT INTO [dbo].[AssessmentQuestions] ([AssessmentId], [BankQuestionId], [SortOrder], [Points])
    VALUES
    (@assId, @q1, 1, 3.33),
    (@assId, @q2, 2, 3.33),
    (@assId, @q3, 3, 3.34);
    PRINT N'✓ Đã nạp QuestionBank & Đề thi mẫu';
END
GO

-- ====================================================================================================
-- PHẦN III: BÁO CÁO TỔNG KẾT TRẠNG THÁI CÁC BẢNG SAU KHI THIẾT LẬP
-- ====================================================================================================
PRINT N'';
PRINT N'====================================================================================';
PRINT N'✓ HOÀN THÀNH TẠO BẢNG & NẠP DỮ LIỆU CƠ SỞ CHO HỆ THỐNG ATS UTC_DATN THÀNH CÔNG';
PRINT N'====================================================================================';
SELECT 
    s.name AS [Schema],
    t.name AS [TableName],
    p.rows AS [RowCount]
FROM sys.tables t
INNER JOIN sys.schemas s ON t.schema_id = s.schema_id
INNER JOIN sys.partitions p ON t.object_id = p.object_id AND p.index_id IN (0, 1)
ORDER BY t.name ASC;
GO
