-- ====================================================================================================
-- HỆ THỐNG QUẢN TRỊ TUYỂN DỤNG VÀ SÀNG LỌC ỨNG VIÊN (ATS - SMART RECRUITMENT & SCREENING SYSTEM)
-- FILE SCRIPT CƠ SỞ DỮ LIỆU ĐẦY ĐỦ (FULL SQL SERVER SCRIPT)
-- ĐỒ ÁN TỐT NGHIỆP - ĐẠI HỌC GIAO THÔNG VẬN TẢI (UTC)
-- CẬP NHẬT ĐỒNG BỘ 100% VỚI ENTITY FRAMEWORK CORE 9.0 & CẤU TRÚC MA TRẬN NHÀ MÁY / KHU CÔNG NGHIỆP
-- ====================================================================================================

-- 1. TẠO HOẶC CHỌN DATABASE
IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = N'UTC_DATN')
BEGIN
    CREATE DATABASE [UTC_DATN];
    PRINT N'✓ Đã tạo mới cơ sở dữ liệu UTC_DATN';
END
GO

USE [UTC_DATN];
GO
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

-- 2. DỌN DẸP BẢNG CŨ THEO THỨ TỰ RÀNG BUỘC KHÓA NGOẠI (NẾU CẦN RESET TOÀN BỘ)
-- Chạy khối này nếu bạn muốn khởi tạo lại sạch sẽ hoàn toàn từ đầu:
/*
IF OBJECT_ID('dbo.AiCodeReviewReport', 'U') IS NOT NULL DROP TABLE dbo.AiCodeReviewReport;
IF OBJECT_ID('dbo.CandidateCodeSubmission', 'U') IS NOT NULL DROP TABLE dbo.CandidateCodeSubmission;
IF OBJECT_ID('dbo.CodeChallenge', 'U') IS NOT NULL DROP TABLE dbo.CodeChallenge;
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
IF OBJECT_ID('dbo.PreScreenAnswers', 'U') IS NOT NULL DROP TABLE dbo.PreScreenAnswers;
IF OBJECT_ID('dbo.PreScreenQuestions', 'U') IS NOT NULL DROP TABLE dbo.PreScreenQuestions;
IF OBJECT_ID('dbo.ApplicationViews', 'U') IS NOT NULL DROP TABLE dbo.ApplicationViews;
IF OBJECT_ID('dbo.ApplicationStageHistory', 'U') IS NOT NULL DROP TABLE dbo.ApplicationStageHistory;
IF OBJECT_ID('dbo.ApplicationNotes', 'U') IS NOT NULL DROP TABLE dbo.ApplicationNotes;
IF OBJECT_ID('dbo.ApplicationAiScores', 'U') IS NOT NULL DROP TABLE dbo.ApplicationAiScores;
IF OBJECT_ID('dbo.Applications', 'U') IS NOT NULL DROP TABLE dbo.Applications;
IF OBJECT_ID('dbo.SavedJobs', 'U') IS NOT NULL DROP TABLE dbo.SavedJobs;
IF OBJECT_ID('dbo.JobTagMap', 'U') IS NOT NULL DROP TABLE dbo.JobTagMap;
IF OBJECT_ID('dbo.JobSkillMap', 'U') IS NOT NULL DROP TABLE dbo.JobSkillMap;
IF OBJECT_ID('dbo.Jobs', 'U') IS NOT NULL DROP TABLE dbo.Jobs;
IF OBJECT_ID('dbo.CoverLetters', 'U') IS NOT NULL DROP TABLE dbo.CoverLetters;
IF OBJECT_ID('dbo.CandidateSkills', 'U') IS NOT NULL DROP TABLE dbo.CandidateSkills;
IF OBJECT_ID('dbo.CandidateExperiences', 'U') IS NOT NULL DROP TABLE dbo.CandidateExperiences;
IF OBJECT_ID('dbo.CandidateEducations', 'U') IS NOT NULL DROP TABLE dbo.CandidateEducations;
IF OBJECT_ID('dbo.CandidateDocuments', 'U') IS NOT NULL DROP TABLE dbo.CandidateDocuments;
IF OBJECT_ID('dbo.Candidates', 'U') IS NOT NULL DROP TABLE dbo.Candidates;
IF OBJECT_ID('dbo.Files', 'U') IS NOT NULL DROP TABLE dbo.Files;
IF OBJECT_ID('dbo.EmailSendLog', 'U') IS NOT NULL DROP TABLE dbo.EmailSendLog;
IF OBJECT_ID('dbo.EmailQueue', 'U') IS NOT NULL DROP TABLE dbo.EmailQueue;
IF OBJECT_ID('dbo.AutomationRules', 'U') IS NOT NULL DROP TABLE dbo.AutomationRules;
IF OBJECT_ID('dbo.EmailTemplates', 'U') IS NOT NULL DROP TABLE dbo.EmailTemplates;
IF OBJECT_ID('dbo.NotificationSettings', 'U') IS NOT NULL DROP TABLE dbo.NotificationSettings;
IF OBJECT_ID('dbo.Notifications', 'U') IS NOT NULL DROP TABLE dbo.Notifications;
IF OBJECT_ID('dbo.RefreshTokens', 'U') IS NOT NULL DROP TABLE dbo.RefreshTokens;
IF OBJECT_ID('dbo.UserRoles', 'U') IS NOT NULL DROP TABLE dbo.UserRoles;
IF OBJECT_ID('dbo.Users', 'U') IS NOT NULL DROP TABLE dbo.Users;
IF OBJECT_ID('dbo.PipelineStages', 'U') IS NOT NULL DROP TABLE dbo.PipelineStages;
IF OBJECT_ID('dbo.JobTypes', 'U') IS NOT NULL DROP TABLE dbo.JobTypes;
IF OBJECT_ID('dbo.JobTags', 'U') IS NOT NULL DROP TABLE dbo.JobTags;
IF OBJECT_ID('dbo.Skills', 'U') IS NOT NULL DROP TABLE dbo.Skills;
IF OBJECT_ID('dbo.Roles', 'U') IS NOT NULL DROP TABLE dbo.Roles;
IF OBJECT_ID('dbo.Departments', 'U') IS NOT NULL DROP TABLE dbo.Departments;
*/
GO

-- ====================================================================================================
-- PHẦN 1: BẢNG DANH MỤC CƠ SỞ (LOOKUPS & MASTER DATA)
-- ====================================================================================================

-- 1.1. Bảng Phòng ban / Khu vực nhà máy (Matrix Organization)
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

-- 1.2. Bảng Vai trò người dùng (Roles)
IF OBJECT_ID('dbo.Roles', 'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[Roles] (
        [RoleId]    UNIQUEIDENTIFIER NOT NULL DEFAULT (NEWID()),
        [Code]      NVARCHAR(50)     NOT NULL, -- ADMIN, MANAGER, HR, INTERVIEWER, CANDIDATE
        [Name]      NVARCHAR(100)    NOT NULL,
        [CreatedAt] DATETIME2(7)     NOT NULL DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT [PK_Roles] PRIMARY KEY CLUSTERED ([RoleId] ASC),
        CONSTRAINT [UQ_Roles_Code] UNIQUE NONCLUSTERED ([Code] ASC)
    );
END
GO

-- 1.3. Bảng Kỹ năng (Skills)
IF OBJECT_ID('dbo.Skills', 'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[Skills] (
        [SkillId]        UNIQUEIDENTIFIER NOT NULL DEFAULT (NEWID()),
        [Name]           NVARCHAR(200)    NOT NULL,
        [NormalizedName] AS (UPPER(LTRIM(RTRIM([Name])))) PERSISTED,
        [CreatedAt]      DATETIME2(7)     NOT NULL DEFAULT (SYSUTCDATETIME()),
        [IsDeleted]      BIT              NOT NULL DEFAULT (0),
        CONSTRAINT [PK_Skills] PRIMARY KEY CLUSTERED ([SkillId] ASC),
        CONSTRAINT [UQ_Skills_NormalizedName] UNIQUE NONCLUSTERED ([NormalizedName] ASC)
    );
END
GO

-- 1.4. Bảng Thẻ nhãn công việc (JobTags)
IF OBJECT_ID('dbo.JobTags', 'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[JobTags] (
        [TagId]          UNIQUEIDENTIFIER NOT NULL DEFAULT (NEWID()),
        [Name]           NVARCHAR(100)    NOT NULL,
        [NormalizedName] AS (UPPER(LTRIM(RTRIM([Name])))) PERSISTED,
        [CreatedAt]      DATETIME2(7)     NOT NULL DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT [PK_JobTags] PRIMARY KEY CLUSTERED ([TagId] ASC),
        CONSTRAINT [UQ_JobTags_NormalizedName] UNIQUE NONCLUSTERED ([NormalizedName] ASC)
    );
END
GO

-- 1.5. Bảng Hình thức làm việc (JobTypes)
IF OBJECT_ID('dbo.JobTypes', 'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[JobTypes] (
        [JobTypeId]   UNIQUEIDENTIFIER NOT NULL DEFAULT (NEWID()),
        [Name]        NVARCHAR(200)    NULL,
        [Description] NVARCHAR(500)    NULL,
        CONSTRAINT [PK_JobTypes] PRIMARY KEY CLUSTERED ([JobTypeId] ASC)
    );
END
GO

-- 1.6. Bảng Các vòng tuyển dụng (PipelineStages)
IF OBJECT_ID('dbo.PipelineStages', 'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[PipelineStages] (
        [StageId]           UNIQUEIDENTIFIER NOT NULL DEFAULT (NEWID()),
        [Code]              NVARCHAR(30)     NOT NULL, -- NEW_APPLIED, TEST_PASSED, INTERVIEW, OFFER, REJECTED
        [Name]              NVARCHAR(100)    NOT NULL,
        [SortOrder]         INT              NOT NULL,
        [IsTerminal]        BIT              NOT NULL DEFAULT (0),
        [CreatedAt]         DATETIME2(7)     NOT NULL DEFAULT (SYSUTCDATETIME()),
        [SlaMaxDays]        INT              NULL,
        [SlaWarnBeforeDays] INT              NULL DEFAULT (1),
        [IsSlaEnabled]      BIT              NULL DEFAULT (0),
        CONSTRAINT [PK_PipelineStages] PRIMARY KEY CLUSTERED ([StageId] ASC),
        CONSTRAINT [UQ_PipelineStages_Code] UNIQUE NONCLUSTERED ([Code] ASC)
    );
END
GO

-- 1.7. Bảng Lưu trữ file đính kèm/CV (Files)
IF OBJECT_ID('dbo.Files', 'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[Files] (
        [FileId]           UNIQUEIDENTIFIER NOT NULL DEFAULT (NEWID()),
        [Provider]         NVARCHAR(50)     NOT NULL DEFAULT (N'LOCAL'), -- LOCAL, CLOUDINARY, AZURE
        [OriginalFileName] NVARCHAR(260)    NOT NULL,
        [StoredFileName]   NVARCHAR(260)    NOT NULL,
        [MimeType]         NVARCHAR(100)    NULL,
        [SizeBytes]        BIGINT           NULL,
        [Sha256]           NVARCHAR(80)     NULL,
        [Url]              NVARCHAR(1000)   NULL,
        [LocalPath]        NVARCHAR(1000)   NULL,
        [CreatedAt]        DATETIME2(7)     NOT NULL DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT [PK_Files] PRIMARY KEY CLUSTERED ([FileId] ASC)
    );
END
GO

-- ====================================================================================================
-- PHẦN 2: TÀI KHOẢN, PHÂN QUYỀN VÀ THIẾT LẬP (AUTH, USERS & ROLES)
-- ====================================================================================================

-- 2.1. Bảng Người dùng hệ thống (Users)
IF OBJECT_ID('dbo.Users', 'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[Users] (
        [UserId]             UNIQUEIDENTIFIER NOT NULL DEFAULT (NEWID()),
        [Email]              NVARCHAR(320)    NOT NULL,
        [NormalizedEmail]    AS (UPPER(LTRIM(RTRIM([Email])))) PERSISTED,
        [PasswordHash]       NVARCHAR(500)    NOT NULL,
        [FullName]           NVARCHAR(200)    NULL,
        [Phone]              NVARCHAR(50)     NULL,
        [AvatarUrl]          NVARCHAR(MAX)    NULL,
        [IsActive]           BIT              NOT NULL DEFAULT (1),
        [AuthProvider]       NVARCHAR(50)     NULL DEFAULT (N'Local'),
        [CreatedAt]          DATETIME2(7)     NOT NULL DEFAULT (SYSUTCDATETIME()),
        [UpdatedAt]          DATETIME2(7)     NULL,
        
        -- Khóa tài khoản / Audit
        [LockedAt]           DATETIME2(7)     NULL,
        [LockedById]         UNIQUEIDENTIFIER NULL,
        [LockedByName]       NVARCHAR(255)    NULL,
        [LockReason]         NVARCHAR(500)    NULL,

        -- Thông tin doanh nghiệp
        [CompanyName]        NVARCHAR(255)    NULL,
        [CompanyWebsite]     NVARCHAR(500)    NULL,
        [CompanyIndustry]    NVARCHAR(200)    NULL,
        [CompanyAddress]     NVARCHAR(500)    NULL,
        [CompanyDescription] NVARCHAR(MAX)    NULL,
        [CompanyLogoUrl]     NVARCHAR(MAX)    NULL,

        -- Cấu trúc ma trận nhà máy (Matrix Org & Isolation)
        [DepartmentId]       UNIQUEIDENTIFIER NULL,
        [ManagerId]          UNIQUEIDENTIFIER NULL,

        CONSTRAINT [PK_Users] PRIMARY KEY CLUSTERED ([UserId] ASC),
        CONSTRAINT [FK_Users_Department] FOREIGN KEY ([DepartmentId]) REFERENCES [dbo].[Departments] ([DepartmentId]) ON DELETE SET NULL,
        CONSTRAINT [FK_Users_Manager] FOREIGN KEY ([ManagerId]) REFERENCES [dbo].[Users] ([UserId])
    );
    CREATE NONCLUSTERED INDEX [IX_Users_DepartmentId] ON [dbo].[Users] ([DepartmentId] ASC);
    CREATE NONCLUSTERED INDEX [IX_Users_ManagerId] ON [dbo].[Users] ([ManagerId] ASC);
    CREATE NONCLUSTERED INDEX [IX_Users_NormalizedEmail] ON [dbo].[Users] ([NormalizedEmail] ASC);
END
GO

-- 2.2. Bảng Phân quyền Người dùng - Vai trò (UserRoles)
IF OBJECT_ID('dbo.UserRoles', 'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[UserRoles] (
        [UserId]    UNIQUEIDENTIFIER NOT NULL,
        [RoleId]    UNIQUEIDENTIFIER NOT NULL,
        [CreatedAt] DATETIME2(7)     NOT NULL DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT [PK_UserRoles] PRIMARY KEY CLUSTERED ([UserId] ASC, [RoleId] ASC),
        CONSTRAINT [FK_UserRoles_Roles] FOREIGN KEY ([RoleId]) REFERENCES [dbo].[Roles] ([RoleId]),
        CONSTRAINT [FK_UserRoles_Users] FOREIGN KEY ([UserId]) REFERENCES [dbo].[Users] ([UserId])
    );
    CREATE NONCLUSTERED INDEX [IX_UserRoles_RoleId] ON [dbo].[UserRoles] ([RoleId] ASC);
END
GO

-- 2.3. Bảng Refresh Token (RefreshTokens)
IF OBJECT_ID('dbo.RefreshTokens', 'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[RefreshTokens] (
        [RefreshTokenId] UNIQUEIDENTIFIER NOT NULL DEFAULT (NEWID()),
        [UserId]         UNIQUEIDENTIFIER NOT NULL,
        [Token]          NVARCHAR(500)    NOT NULL,
        [ExpiresAt]      DATETIME2(7)     NOT NULL,
        [RevokedAt]      DATETIME2(7)     NULL,
        [CreatedAt]      DATETIME2(7)     NOT NULL DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT [PK_RefreshTokens] PRIMARY KEY CLUSTERED ([RefreshTokenId] ASC),
        CONSTRAINT [FK_RefreshTokens_Users] FOREIGN KEY ([UserId]) REFERENCES [dbo].[Users] ([UserId])
    );
    CREATE NONCLUSTERED INDEX [IX_RefreshTokens_UserId] ON [dbo].[RefreshTokens] ([UserId] ASC);
END
GO

-- 2.4. Bảng Thông báo (Notifications)
IF OBJECT_ID('dbo.Notifications', 'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[Notifications] (
        [Id]        UNIQUEIDENTIFIER NOT NULL DEFAULT (NEWID()),
        [UserId]    UNIQUEIDENTIFIER NOT NULL,
        [Title]     NVARCHAR(200)    NOT NULL,
        [Message]   NVARCHAR(MAX)    NOT NULL,
        [Type]      NVARCHAR(50)     NOT NULL,
        [IsRead]    BIT              NOT NULL DEFAULT (0),
        [CreatedAt] DATETIME2(7)     NOT NULL DEFAULT (SYSUTCDATETIME()),
        [RelatedId] NVARCHAR(100)    NOT NULL,
        CONSTRAINT [PK_Notifications] PRIMARY KEY CLUSTERED ([Id] ASC),
        CONSTRAINT [FK_Notifications_Users] FOREIGN KEY ([UserId]) REFERENCES [dbo].[Users] ([UserId]) ON DELETE CASCADE
    );
    CREATE NONCLUSTERED INDEX [IX_Notifications_UserId] ON [dbo].[Notifications] ([UserId] ASC);
    CREATE NONCLUSTERED INDEX [IX_Notifications_IsRead] ON [dbo].[Notifications] ([IsRead] ASC);
END
GO

-- 2.5. Bảng Cấu hình nhận thông báo (NotificationSettings)
IF OBJECT_ID('dbo.NotificationSettings', 'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[NotificationSettings] (
        [Id]                       UNIQUEIDENTIFIER NOT NULL DEFAULT (NEWID()),
        [UserId]                   UNIQUEIDENTIFIER NOT NULL,
        [NotifyJobOpportunities]   BIT              NOT NULL DEFAULT (1),
        [NotifyApplicationUpdates] BIT              NOT NULL DEFAULT (1),
        [NotifySecurityAlerts]     BIT              NOT NULL DEFAULT (1),
        [NotifyMarketing]          BIT              NOT NULL DEFAULT (0),
        [ChannelEmail]             BIT              NOT NULL DEFAULT (1),
        [ChannelPush]              BIT              NOT NULL DEFAULT (1),
        [UpdatedAt]                DATETIME2(7)     NOT NULL DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT [PK_NotificationSettings] PRIMARY KEY CLUSTERED ([Id] ASC),
        CONSTRAINT [FK_NotificationSettings_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [dbo].[Users] ([UserId]) ON DELETE CASCADE
    );
    CREATE NONCLUSTERED INDEX [IX_NotificationSettings_UserId] ON [dbo].[NotificationSettings] ([UserId] ASC);
END
GO

-- ====================================================================================================
-- PHẦN 3: THÔNG TIN ỨNG VIÊN & HỒ SƠ (CANDIDATES & PROFILES)
-- ====================================================================================================

-- 3.1. Bảng Ứng viên (Candidates)
IF OBJECT_ID('dbo.Candidates', 'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[Candidates] (
        [CandidateId]     UNIQUEIDENTIFIER NOT NULL DEFAULT (NEWID()),
        [Email]           NVARCHAR(320)    NULL,
        [NormalizedEmail] AS (UPPER(LTRIM(RTRIM([Email])))) PERSISTED,
        [FullName]        NVARCHAR(200)    NULL,
        [Phone]           NVARCHAR(50)     NULL,
        [Location]        NVARCHAR(200)    NULL,
        [Headline]        NVARCHAR(300)    NULL,
        [Summary]         NVARCHAR(MAX)    NULL,
        [Source]          NVARCHAR(100)    NULL,
        [LinkedIn]        NVARCHAR(500)    NULL,
        [GitHub]          NVARCHAR(500)    NULL,
        [Avatar]          NVARCHAR(MAX)    NULL,
        [CreatedAt]       DATETIME2(7)     NOT NULL DEFAULT (SYSUTCDATETIME()),
        [UpdatedAt]       DATETIME2(7)     NULL,
        [IsDeleted]       BIT              NOT NULL DEFAULT (0),
        [UserId]          UNIQUEIDENTIFIER NULL,
        CONSTRAINT [PK_Candidates] PRIMARY KEY CLUSTERED ([CandidateId] ASC),
        CONSTRAINT [FK_Candidates_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [dbo].[Users] ([UserId]) ON DELETE SET NULL
    );
    CREATE NONCLUSTERED INDEX [IX_Candidates_NormalizedEmail] ON [dbo].[Candidates] ([NormalizedEmail] ASC);
    CREATE NONCLUSTERED INDEX [IX_Candidates_UserId] ON [dbo].[Candidates] ([UserId] ASC);
END
GO

-- 3.2. Bảng Tài liệu ứng viên / CV (CandidateDocuments)
IF OBJECT_ID('dbo.CandidateDocuments', 'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[CandidateDocuments] (
        [CandidateDocumentId] UNIQUEIDENTIFIER NOT NULL DEFAULT (NEWID()),
        [CandidateId]         UNIQUEIDENTIFIER NOT NULL,
        [FileId]              UNIQUEIDENTIFIER NOT NULL,
        [DocType]             NVARCHAR(30)     NOT NULL DEFAULT (N'CV'),
        [ParsedJson]          NVARCHAR(MAX)    NULL,
        [IsPrimary]           BIT              NOT NULL DEFAULT (0),
        [DisplayName]         NVARCHAR(255)    NULL,
        [CreatedAt]           DATETIME2(7)     NOT NULL DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT [PK_CandidateDocuments] PRIMARY KEY CLUSTERED ([CandidateDocumentId] ASC),
        CONSTRAINT [FK_CandidateDocuments_Candidates] FOREIGN KEY ([CandidateId]) REFERENCES [dbo].[Candidates] ([CandidateId]),
        CONSTRAINT [FK_CandidateDocuments_Files] FOREIGN KEY ([FileId]) REFERENCES [dbo].[Files] ([FileId])
    );
    CREATE NONCLUSTERED INDEX [IX_CandidateDocuments_CandidateId] ON [dbo].[CandidateDocuments] ([CandidateId] ASC);
    CREATE NONCLUSTERED INDEX [IX_CandidateDocuments_FileId] ON [dbo].[CandidateDocuments] ([FileId] ASC);
END
GO

-- 3.3. Bảng Học vấn ứng viên (CandidateEducations)
IF OBJECT_ID('dbo.CandidateEducations', 'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[CandidateEducations] (
        [EducationId] UNIQUEIDENTIFIER NOT NULL DEFAULT (NEWID()),
        [CandidateId] UNIQUEIDENTIFIER NOT NULL,
        [School]      NVARCHAR(200)    NULL,
        [Degree]      NVARCHAR(200)    NULL,
        [Field]       NVARCHAR(200)    NULL,
        [StartDate]   DATE             NULL,
        [EndDate]     DATE             NULL,
        CONSTRAINT [PK_CandidateEducations] PRIMARY KEY CLUSTERED ([EducationId] ASC),
        CONSTRAINT [FK_CandidateEducations_Candidates] FOREIGN KEY ([CandidateId]) REFERENCES [dbo].[Candidates] ([CandidateId])
    );
    CREATE NONCLUSTERED INDEX [IX_CandidateEducations_CandidateId] ON [dbo].[CandidateEducations] ([CandidateId] ASC);
END
GO

-- 3.4. Bảng Kinh nghiệm làm việc (CandidateExperiences)
IF OBJECT_ID('dbo.CandidateExperiences', 'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[CandidateExperiences] (
        [ExperienceId] UNIQUEIDENTIFIER NOT NULL DEFAULT (NEWID()),
        [CandidateId]  UNIQUEIDENTIFIER NOT NULL,
        [Company]      NVARCHAR(200)    NULL,
        [Title]        NVARCHAR(200)    NULL,
        [StartDate]    DATE             NULL,
        [EndDate]      DATE             NULL,
        [Description]  NVARCHAR(MAX)    NULL,
        CONSTRAINT [PK_CandidateExperiences] PRIMARY KEY CLUSTERED ([ExperienceId] ASC),
        CONSTRAINT [FK_CandidateExperiences_Candidates] FOREIGN KEY ([CandidateId]) REFERENCES [dbo].[Candidates] ([CandidateId])
    );
    CREATE NONCLUSTERED INDEX [IX_CandidateExperiences_CandidateId] ON [dbo].[CandidateExperiences] ([CandidateId] ASC);
END
GO

-- 3.5. Bảng Kỹ năng ứng viên (CandidateSkills)
IF OBJECT_ID('dbo.CandidateSkills', 'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[CandidateSkills] (
        [CandidateId] UNIQUEIDENTIFIER NOT NULL,
        [SkillId]     UNIQUEIDENTIFIER NOT NULL,
        [Level]       TINYINT          NULL, -- 1..5
        [Years]       DECIMAL(4, 1)    NULL,
        CONSTRAINT [PK_CandidateSkills] PRIMARY KEY CLUSTERED ([CandidateId] ASC, [SkillId] ASC),
        CONSTRAINT [FK_CandidateSkills_Candidates] FOREIGN KEY ([CandidateId]) REFERENCES [dbo].[Candidates] ([CandidateId]),
        CONSTRAINT [FK_CandidateSkills_Skills] FOREIGN KEY ([SkillId]) REFERENCES [dbo].[Skills] ([SkillId])
    );
    CREATE NONCLUSTERED INDEX [IX_CandidateSkills_SkillId] ON [dbo].[CandidateSkills] ([SkillId] ASC);
END
GO

-- 3.6. Bảng Thư ngỏ ứng tuyển (CoverLetters)
IF OBJECT_ID('dbo.CoverLetters', 'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[CoverLetters] (
        [CoverLetterId] UNIQUEIDENTIFIER NOT NULL DEFAULT (NEWID()),
        [CandidateId]   UNIQUEIDENTIFIER NOT NULL,
        [Title]         NVARCHAR(200)    NOT NULL,
        [Content]       NVARCHAR(MAX)    NOT NULL,
        [IsDefault]     BIT              NOT NULL DEFAULT (0),
        [CreatedAt]     DATETIME2(7)     NOT NULL DEFAULT (SYSUTCDATETIME()),
        [UpdatedAt]     DATETIME2(7)     NULL,
        CONSTRAINT [PK_CoverLetters] PRIMARY KEY CLUSTERED ([CoverLetterId] ASC),
        CONSTRAINT [FK_CoverLetters_Candidates] FOREIGN KEY ([CandidateId]) REFERENCES [dbo].[Candidates] ([CandidateId]) ON DELETE CASCADE
    );
    CREATE NONCLUSTERED INDEX [IX_CoverLetters_CandidateId] ON [dbo].[CoverLetters] ([CandidateId] ASC);
END
GO

-- ====================================================================================================
-- PHẦN 4: TIN TUYỂN DỤNG & ÁNH XẠ (JOB POSTINGS & MAPPINGS)
-- ====================================================================================================

-- 4.1. Bảng Tin tuyển dụng (Jobs)
IF OBJECT_ID('dbo.Jobs', 'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[Jobs] (
        [JobId]             UNIQUEIDENTIFIER NOT NULL DEFAULT (NEWID()),
        [Code]              NVARCHAR(50)     NOT NULL,
        [Title]             NVARCHAR(300)    NOT NULL,
        [Department]        NVARCHAR(200)    NULL,
        [DepartmentId]      UNIQUEIDENTIFIER NULL,
        [Location]          NVARCHAR(200)    NULL,
        [EmploymentType]    NVARCHAR(50)     NULL,
        [ExperienceLevel]   NVARCHAR(100)    NULL,
        [SeniorityLevel]    NVARCHAR(100)    NULL,
        [SalaryMin]         DECIMAL(18, 2)   NULL,
        [SalaryMax]         DECIMAL(18, 2)   NULL,
        [Currency]          NVARCHAR(10)     NULL DEFAULT (N'VND'),
        [Description]       NVARCHAR(MAX)    NULL,
        [Requirements]      NVARCHAR(MAX)    NULL,
        [Benefits]          NVARCHAR(MAX)    NULL,
        [NumberOfPositions] INT              NULL,
        [Status]            NVARCHAR(30)     NOT NULL DEFAULT (N'OPEN'),
        [CreatedBy]         UNIQUEIDENTIFIER NULL,
        [CreatedAt]         DATETIME2(7)     NOT NULL DEFAULT (SYSUTCDATETIME()),
        [UpdatedAt]         DATETIME2(7)     NULL,
        [ClosedAt]          DATETIME2(7)     NULL,
        [ClosedById]        UNIQUEIDENTIFIER NULL,
        [ClosedByName]      NVARCHAR(255)    NULL,
        [Deadline]          DATETIME2(7)     NULL,
        [IsDeleted]         BIT              NOT NULL DEFAULT (0),
        CONSTRAINT [PK_Jobs] PRIMARY KEY CLUSTERED ([JobId] ASC),
        CONSTRAINT [UQ_Jobs_Code] UNIQUE NONCLUSTERED ([Code] ASC),
        CONSTRAINT [FK_Jobs_CreatedBy] FOREIGN KEY ([CreatedBy]) REFERENCES [dbo].[Users] ([UserId]),
        CONSTRAINT [FK_Jobs_Department] FOREIGN KEY ([DepartmentId]) REFERENCES [dbo].[Departments] ([DepartmentId]) ON DELETE SET NULL
    );
    CREATE NONCLUSTERED INDEX [IX_Jobs_Status] ON [dbo].[Jobs] ([Status] ASC);
    CREATE NONCLUSTERED INDEX [IX_Jobs_Title] ON [dbo].[Jobs] ([Title] ASC);
    CREATE NONCLUSTERED INDEX [IX_Jobs_DepartmentId] ON [dbo].[Jobs] ([DepartmentId] ASC);
    CREATE NONCLUSTERED INDEX [IX_Jobs_CreatedBy] ON [dbo].[Jobs] ([CreatedBy] ASC);
END
GO

-- 4.2. Bảng Ánh xạ Job - Kỹ năng yêu cầu (JobSkillMap)
IF OBJECT_ID('dbo.JobSkillMap', 'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[JobSkillMap] (
        [JobId]   UNIQUEIDENTIFIER NOT NULL,
        [SkillId] UNIQUEIDENTIFIER NOT NULL,
        [Weight]  TINYINT          NOT NULL DEFAULT (1),
        CONSTRAINT [PK_JobSkillMap] PRIMARY KEY CLUSTERED ([JobId] ASC, [SkillId] ASC),
        CONSTRAINT [FK_JobSkillMap_Jobs] FOREIGN KEY ([JobId]) REFERENCES [dbo].[Jobs] ([JobId]),
        CONSTRAINT [FK_JobSkillMap_Skills] FOREIGN KEY ([SkillId]) REFERENCES [dbo].[Skills] ([SkillId])
    );
    CREATE NONCLUSTERED INDEX [IX_JobSkillMap_SkillId] ON [dbo].[JobSkillMap] ([SkillId] ASC);
END
GO

-- 4.3. Bảng Ánh xạ Job - Thẻ nhãn (JobTagMap)
IF OBJECT_ID('dbo.JobTagMap', 'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[JobTagMap] (
        [JobId] UNIQUEIDENTIFIER NOT NULL,
        [TagId] UNIQUEIDENTIFIER NOT NULL,
        CONSTRAINT [PK_JobTagMap] PRIMARY KEY CLUSTERED ([JobId] ASC, [TagId] ASC),
        CONSTRAINT [FK_JobTagMap_Jobs] FOREIGN KEY ([JobId]) REFERENCES [dbo].[Jobs] ([JobId]),
        CONSTRAINT [FK_JobTagMap_Tags] FOREIGN KEY ([TagId]) REFERENCES [dbo].[JobTags] ([TagId])
    );
    CREATE NONCLUSTERED INDEX [IX_JobTagMap_TagId] ON [dbo].[JobTagMap] ([TagId] ASC);
END
GO

-- 4.4. Bảng Công việc ứng viên đã lưu (SavedJobs)
IF OBJECT_ID('dbo.SavedJobs', 'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[SavedJobs] (
        [SavedJobId]  UNIQUEIDENTIFIER NOT NULL DEFAULT (NEWID()),
        [CandidateId] UNIQUEIDENTIFIER NOT NULL,
        [JobId]       UNIQUEIDENTIFIER NOT NULL,
        [SavedAt]     DATETIME2(7)     NOT NULL DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT [PK_SavedJobs] PRIMARY KEY CLUSTERED ([SavedJobId] ASC),
        CONSTRAINT [UQ_SavedJobs_Candidate_Job] UNIQUE NONCLUSTERED ([CandidateId] ASC, [JobId] ASC),
        CONSTRAINT [FK_SavedJobs_Candidates] FOREIGN KEY ([CandidateId]) REFERENCES [dbo].[Candidates] ([CandidateId]) ON DELETE CASCADE,
        CONSTRAINT [FK_SavedJobs_Jobs] FOREIGN KEY ([JobId]) REFERENCES [dbo].[Jobs] ([JobId]) ON DELETE CASCADE
    );
    CREATE NONCLUSTERED INDEX [IX_SavedJobs_JobId] ON [dbo].[SavedJobs] ([JobId] ASC);
    CREATE NONCLUSTERED INDEX [IX_SavedJobs_CandidateId] ON [dbo].[SavedJobs] ([CandidateId] ASC);
END
GO

-- ====================================================================================================
-- PHẦN 5: QUY TRÌNH TUYỂN DỤNG & NỘP HỒ SƠ (APPLICATIONS & PIPELINE)
-- ====================================================================================================

-- 5.1. Bảng Đơn ứng tuyển (Applications)
IF OBJECT_ID('dbo.Applications', 'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[Applications] (
        [ApplicationId]      UNIQUEIDENTIFIER NOT NULL DEFAULT (NEWID()),
        [JobId]              UNIQUEIDENTIFIER NOT NULL,
        [CandidateId]        UNIQUEIDENTIFIER NOT NULL,
        [CurrentStageId]     UNIQUEIDENTIFIER NOT NULL,
        [AppliedAt]          DATETIME2(7)     NOT NULL DEFAULT (SYSUTCDATETIME()),
        [LastStageChangedAt] DATETIME2(7)     NOT NULL DEFAULT (SYSUTCDATETIME()),
        [ResumeDocumentId]   UNIQUEIDENTIFIER NULL,
        [Source]             NVARCHAR(100)    NULL,
        [Status]             NVARCHAR(30)     NOT NULL DEFAULT (N'ACTIVE'),
        [ContactName]        NVARCHAR(200)    NULL,
        [ContactEmail]       NVARCHAR(255)    NULL,
        [ContactPhone]       NVARCHAR(50)     NULL,
        [LastViewedAt]       DATETIME2(7)     NULL,
        [LastViewedBy]       UNIQUEIDENTIFIER NULL,
        CONSTRAINT [PK_Applications] PRIMARY KEY CLUSTERED ([ApplicationId] ASC),
        CONSTRAINT [UQ_Applications_Job_Candidate] UNIQUE NONCLUSTERED ([JobId] ASC, [CandidateId] ASC),
        CONSTRAINT [FK_Applications_Candidates] FOREIGN KEY ([CandidateId]) REFERENCES [dbo].[Candidates] ([CandidateId]),
        CONSTRAINT [FK_Applications_Jobs] FOREIGN KEY ([JobId]) REFERENCES [dbo].[Jobs] ([JobId]),
        CONSTRAINT [FK_Applications_ResumeDoc] FOREIGN KEY ([ResumeDocumentId]) REFERENCES [dbo].[CandidateDocuments] ([CandidateDocumentId]),
        CONSTRAINT [FK_Applications_Stage] FOREIGN KEY ([CurrentStageId]) REFERENCES [dbo].[PipelineStages] ([StageId])
    );
    CREATE NONCLUSTERED INDEX [IX_Applications_JobId] ON [dbo].[Applications] ([JobId] ASC);
    CREATE NONCLUSTERED INDEX [IX_Applications_CandidateId] ON [dbo].[Applications] ([CandidateId] ASC);
    CREATE NONCLUSTERED INDEX [IX_Applications_CurrentStageId] ON [dbo].[Applications] ([CurrentStageId] ASC);
    CREATE NONCLUSTERED INDEX [IX_Applications_ResumeDocumentId] ON [dbo].[Applications] ([ResumeDocumentId] ASC);
END
GO

-- 5.2. Bảng Lịch sử chuyển vòng tuyển dụng (ApplicationStageHistory)
IF OBJECT_ID('dbo.ApplicationStageHistory', 'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[ApplicationStageHistory] (
        [HistoryId]     UNIQUEIDENTIFIER NOT NULL DEFAULT (NEWID()),
        [ApplicationId] UNIQUEIDENTIFIER NOT NULL,
        [FromStageId]   UNIQUEIDENTIFIER NULL,
        [ToStageId]     UNIQUEIDENTIFIER NOT NULL,
        [ChangedBy]     UNIQUEIDENTIFIER NULL,
        [Reason]        NVARCHAR(500)    NULL,
        [ChangedAt]     DATETIME2(7)     NOT NULL DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT [PK_ApplicationStageHistory] PRIMARY KEY CLUSTERED ([HistoryId] ASC),
        CONSTRAINT [FK_StageHistory_App] FOREIGN KEY ([ApplicationId]) REFERENCES [dbo].[Applications] ([ApplicationId]),
        CONSTRAINT [FK_StageHistory_From] FOREIGN KEY ([FromStageId]) REFERENCES [dbo].[PipelineStages] ([StageId]),
        CONSTRAINT [FK_StageHistory_To] FOREIGN KEY ([ToStageId]) REFERENCES [dbo].[PipelineStages] ([StageId]),
        CONSTRAINT [FK_StageHistory_User] FOREIGN KEY ([ChangedBy]) REFERENCES [dbo].[Users] ([UserId])
    );
    CREATE NONCLUSTERED INDEX [IX_StageHistory_AppId] ON [dbo].[ApplicationStageHistory] ([ApplicationId] ASC);
    CREATE NONCLUSTERED INDEX [IX_ApplicationStageHistory_FromStageId] ON [dbo].[ApplicationStageHistory] ([FromStageId] ASC);
    CREATE NONCLUSTERED INDEX [IX_ApplicationStageHistory_ToStageId] ON [dbo].[ApplicationStageHistory] ([ToStageId] ASC);
    CREATE NONCLUSTERED INDEX [IX_ApplicationStageHistory_ChangedBy] ON [dbo].[ApplicationStageHistory] ([ChangedBy] ASC);
END
GO

-- 5.3. Bảng Ghi chú nội bộ cho hồ sơ (ApplicationNotes)
IF OBJECT_ID('dbo.ApplicationNotes', 'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[ApplicationNotes] (
        [NoteId]        UNIQUEIDENTIFIER NOT NULL DEFAULT (NEWID()),
        [ApplicationId] UNIQUEIDENTIFIER NOT NULL,
        [Note]          NVARCHAR(MAX)    NOT NULL,
        [CreatedBy]     UNIQUEIDENTIFIER NULL,
        [CreatedAt]     DATETIME2(7)     NOT NULL DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT [PK_ApplicationNotes] PRIMARY KEY CLUSTERED ([NoteId] ASC),
        CONSTRAINT [FK_ApplicationNotes_App] FOREIGN KEY ([ApplicationId]) REFERENCES [dbo].[Applications] ([ApplicationId]),
        CONSTRAINT [FK_ApplicationNotes_User] FOREIGN KEY ([CreatedBy]) REFERENCES [dbo].[Users] ([UserId])
    );
    CREATE NONCLUSTERED INDEX [IX_ApplicationNotes_AppId] ON [dbo].[ApplicationNotes] ([ApplicationId] ASC);
    CREATE NONCLUSTERED INDEX [IX_ApplicationNotes_CreatedBy] ON [dbo].[ApplicationNotes] ([CreatedBy] ASC);
END
GO

-- 5.4. Bảng Điểm đánh giá AI sàng lọc hồ sơ (ApplicationAiScores)
IF OBJECT_ID('dbo.ApplicationAiScores', 'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[ApplicationAiScores] (
        [AiScoreId]         UNIQUEIDENTIFIER NOT NULL DEFAULT (NEWID()),
        [ApplicationId]     UNIQUEIDENTIFIER NOT NULL,
        [MatchingScore]     DECIMAL(5, 2)    NOT NULL, -- 0..100
        [MatchedSkillsJson] NVARCHAR(MAX)    NULL,
        [Model]             NVARCHAR(50)     NULL,
        [BreakdownJson]     NVARCHAR(MAX)    NULL,     -- Chi tiết điểm 4 trụ cột
        [CreatedAt]         DATETIME2(7)     NOT NULL DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT [PK_ApplicationAiScores] PRIMARY KEY CLUSTERED ([AiScoreId] ASC),
        CONSTRAINT [FK_ApplicationAiScores_App] FOREIGN KEY ([ApplicationId]) REFERENCES [dbo].[Applications] ([ApplicationId])
    );
    CREATE NONCLUSTERED INDEX [IX_AiScores_AppId] ON [dbo].[ApplicationAiScores] ([ApplicationId] ASC);
END
GO

-- 5.5. Bảng Nhật ký xem hồ sơ (ApplicationViews)
IF OBJECT_ID('dbo.ApplicationViews', 'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[ApplicationViews] (
        [ViewId]        UNIQUEIDENTIFIER NOT NULL DEFAULT (NEWID()),
        [ApplicationId] UNIQUEIDENTIFIER NOT NULL,
        [ViewerId]      UNIQUEIDENTIFIER NOT NULL,
        [ViewedAt]      DATETIME2(7)     NOT NULL DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT [PK_ApplicationViews] PRIMARY KEY CLUSTERED ([ViewId] ASC),
        CONSTRAINT [FK_ApplicationViews_Applications] FOREIGN KEY ([ApplicationId]) REFERENCES [dbo].[Applications] ([ApplicationId]),
        CONSTRAINT [FK_ApplicationViews_Users] FOREIGN KEY ([ViewerId]) REFERENCES [dbo].[Users] ([UserId])
    );
    CREATE NONCLUSTERED INDEX [IX_ApplicationViews_ApplicationId] ON [dbo].[ApplicationViews] ([ApplicationId] ASC);
    CREATE NONCLUSTERED INDEX [IX_ApplicationViews_ViewerId] ON [dbo].[ApplicationViews] ([ViewerId] ASC);
END
GO

-- ====================================================================================================
-- PHẦN 6: ĐÁNH GIÁ NĂNG LỰC & TRẮC NGHIỆM (ASSESSMENTS & QUESTION BANK)
-- ====================================================================================================

-- 6.1. Ngân hàng câu hỏi (QuestionBank)
IF OBJECT_ID('dbo.QuestionBank', 'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[QuestionBank] (
        [BankQuestionId] UNIQUEIDENTIFIER NOT NULL DEFAULT (NEWID()),
        [Content]        NVARCHAR(MAX)    NOT NULL,
        [Type]           NVARCHAR(30)     NOT NULL DEFAULT (N'MCQ_SINGLE'),
        [Difficulty]     NVARCHAR(20)     NULL, -- EASY, MEDIUM, HARD
        [Tags]           NVARCHAR(300)    NULL,
        [Explanation]    NVARCHAR(MAX)    NULL,
        [IsActive]       BIT              NOT NULL DEFAULT (1),
        [CreatedBy]      UNIQUEIDENTIFIER NULL,
        [CreatedAt]      DATETIME2(7)     NOT NULL DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT [PK_QuestionBank] PRIMARY KEY CLUSTERED ([BankQuestionId] ASC),
        CONSTRAINT [FK_QuestionBank_User] FOREIGN KEY ([CreatedBy]) REFERENCES [dbo].[Users] ([UserId])
    );
    CREATE NONCLUSTERED INDEX [IX_QuestionBank_CreatedBy] ON [dbo].[QuestionBank] ([CreatedBy] ASC);
END
GO

-- 6.2. Các đáp án lựa chọn (QuestionOptions)
IF OBJECT_ID('dbo.QuestionOptions', 'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[QuestionOptions] (
        [OptionId]       UNIQUEIDENTIFIER NOT NULL DEFAULT (NEWID()),
        [BankQuestionId] UNIQUEIDENTIFIER NOT NULL,
        [Content]        NVARCHAR(1000)   NOT NULL,
        [IsCorrect]      BIT              NOT NULL DEFAULT (0),
        [SortOrder]      INT              NOT NULL DEFAULT (0),
        CONSTRAINT [PK_QuestionOptions] PRIMARY KEY CLUSTERED ([OptionId] ASC),
        CONSTRAINT [FK_QuestionOptions_Q] FOREIGN KEY ([BankQuestionId]) REFERENCES [dbo].[QuestionBank] ([BankQuestionId])
    );
    CREATE NONCLUSTERED INDEX [IX_QuestionOptions_QId] ON [dbo].[QuestionOptions] ([BankQuestionId] ASC);
END
GO

-- 6.3. Đề thi / Đợt kiểm tra đánh giá (Assessments)
IF OBJECT_ID('dbo.Assessments', 'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[Assessments] (
        [AssessmentId]          UNIQUEIDENTIFIER NOT NULL DEFAULT (NEWID()),
        [Name]                  NVARCHAR(200)    NOT NULL,
        [Mode]                  NVARCHAR(20)     NOT NULL DEFAULT (N'FIXED'), -- FIXED, RANDOM
        [DurationMinutes]       INT              NOT NULL DEFAULT (30),
        [PassScore]             DECIMAL(5, 2)    NOT NULL DEFAULT (60.0),
        [ShowResultToCandidate] BIT              NOT NULL DEFAULT (1),
        [RandomQuestionCount]   INT              NULL,
        [RandomTags]            NVARCHAR(300)    NULL,
        [IsActive]              BIT              NOT NULL DEFAULT (1),
        [CreatedBy]             UNIQUEIDENTIFIER NULL,
        [CreatedAt]             DATETIME2(7)     NOT NULL DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT [PK_Assessments] PRIMARY KEY CLUSTERED ([AssessmentId] ASC),
        CONSTRAINT [FK_Assessments_User] FOREIGN KEY ([CreatedBy]) REFERENCES [dbo].[Users] ([UserId])
    );
    CREATE NONCLUSTERED INDEX [IX_Assessments_CreatedBy] ON [dbo].[Assessments] ([CreatedBy] ASC);
END
GO

-- 6.4. Câu hỏi trong đề thi (AssessmentQuestions)
IF OBJECT_ID('dbo.AssessmentQuestions', 'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[AssessmentQuestions] (
        [AssessmentId]   UNIQUEIDENTIFIER NOT NULL,
        [BankQuestionId] UNIQUEIDENTIFIER NOT NULL,
        [SortOrder]      INT              NOT NULL DEFAULT (0),
        [Points]         DECIMAL(6, 2)    NOT NULL DEFAULT (1.0),
        CONSTRAINT [PK_AssessmentQuestions] PRIMARY KEY CLUSTERED ([AssessmentId] ASC, [BankQuestionId] ASC),
        CONSTRAINT [FK_AssessmentQuestions_A] FOREIGN KEY ([AssessmentId]) REFERENCES [dbo].[Assessments] ([AssessmentId]),
        CONSTRAINT [FK_AssessmentQuestions_Q] FOREIGN KEY ([BankQuestionId]) REFERENCES [dbo].[QuestionBank] ([BankQuestionId])
    );
    CREATE NONCLUSTERED INDEX [IX_AssessmentQuestions_BankQuestionId] ON [dbo].[AssessmentQuestions] ([BankQuestionId] ASC);
END
GO

-- 6.5. Lời mời làm bài test (AssessmentInvites)
IF OBJECT_ID('dbo.AssessmentInvites', 'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[AssessmentInvites] (
        [InviteId]      UNIQUEIDENTIFIER NOT NULL DEFAULT (NEWID()),
        [ApplicationId] UNIQUEIDENTIFIER NOT NULL,
        [AssessmentId]  UNIQUEIDENTIFIER NOT NULL,
        [Code]          NVARCHAR(20)     NOT NULL,
        [ExpiresAt]     DATETIME2(7)     NOT NULL,
        [SentToEmail]   NVARCHAR(320)    NULL,
        [CreatedAt]     DATETIME2(7)     NOT NULL DEFAULT (SYSUTCDATETIME()),
        [UsedAt]        DATETIME2(7)     NULL,
        CONSTRAINT [PK_AssessmentInvites] PRIMARY KEY CLUSTERED ([InviteId] ASC),
        CONSTRAINT [UQ_AssessmentInvites_Code] UNIQUE NONCLUSTERED ([Code] ASC),
        CONSTRAINT [FK_AssessmentInvites_App] FOREIGN KEY ([ApplicationId]) REFERENCES [dbo].[Applications] ([ApplicationId]),
        CONSTRAINT [FK_AssessmentInvites_Assessment] FOREIGN KEY ([AssessmentId]) REFERENCES [dbo].[Assessments] ([AssessmentId])
    );
    CREATE NONCLUSTERED INDEX [IX_AssessmentInvites_AppId] ON [dbo].[AssessmentInvites] ([ApplicationId] ASC);
    CREATE NONCLUSTERED INDEX [IX_AssessmentInvites_AssessmentId] ON [dbo].[AssessmentInvites] ([AssessmentId] ASC);
END
GO

-- 6.6. Lượt làm bài kiểm tra (AssessmentAttempts)
IF OBJECT_ID('dbo.AssessmentAttempts', 'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[AssessmentAttempts] (
        [AttemptId]   UNIQUEIDENTIFIER NOT NULL DEFAULT (NEWID()),
        [InviteId]    UNIQUEIDENTIFIER NOT NULL,
        [StartedAt]   DATETIME2(7)     NOT NULL DEFAULT (SYSUTCDATETIME()),
        [SubmittedAt] DATETIME2(7)     NULL,
        [Score]       DECIMAL(7, 2)    NULL,
        [Passed]      BIT              NULL,
        [Status]      NVARCHAR(20)     NOT NULL DEFAULT (N'IN_PROGRESS'),
        [CandidateIp] NVARCHAR(100)    NULL,
        [UserAgent]   NVARCHAR(500)    NULL,
        CONSTRAINT [PK_AssessmentAttempts] PRIMARY KEY CLUSTERED ([AttemptId] ASC),
        CONSTRAINT [FK_AssessmentAttempts_Invite] FOREIGN KEY ([InviteId]) REFERENCES [dbo].[AssessmentInvites] ([InviteId])
    );
    CREATE NONCLUSTERED INDEX [IX_AssessmentAttempts_InviteId] ON [dbo].[AssessmentAttempts] ([InviteId] ASC);
END
GO

-- 6.7. Câu trả lời của ứng viên (AssessmentAnswers)
IF OBJECT_ID('dbo.AssessmentAnswers', 'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[AssessmentAnswers] (
        [AnswerId]              UNIQUEIDENTIFIER NOT NULL DEFAULT (NEWID()),
        [AttemptId]             UNIQUEIDENTIFIER NOT NULL,
        [BankQuestionId]        UNIQUEIDENTIFIER NOT NULL,
        [SelectedOptionIdsJson] NVARCHAR(MAX)    NULL,
        [IsCorrect]             BIT              NULL,
        [PointsEarned]          DECIMAL(6, 2)    NULL,
        [CreatedAt]             DATETIME2(7)     NOT NULL DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT [PK_AssessmentAnswers] PRIMARY KEY CLUSTERED ([AnswerId] ASC),
        CONSTRAINT [FK_AssessmentAnswers_Attempt] FOREIGN KEY ([AttemptId]) REFERENCES [dbo].[AssessmentAttempts] ([AttemptId]),
        CONSTRAINT [FK_AssessmentAnswers_Q] FOREIGN KEY ([BankQuestionId]) REFERENCES [dbo].[QuestionBank] ([BankQuestionId])
    );
    CREATE NONCLUSTERED INDEX [IX_AssessmentAnswers_AttemptId] ON [dbo].[AssessmentAnswers] ([AttemptId] ASC);
    CREATE NONCLUSTERED INDEX [IX_AssessmentAnswers_BankQuestionId] ON [dbo].[AssessmentAnswers] ([BankQuestionId] ASC);
END
GO

-- 6.8. Nhật ký chống gian lận (AntiCheatEvents)
IF OBJECT_ID('dbo.AntiCheatEvents', 'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[AntiCheatEvents] (
        [AntiCheatEventId] UNIQUEIDENTIFIER NOT NULL DEFAULT (NEWID()),
        [AttemptId]        UNIQUEIDENTIFIER NOT NULL,
        [EventType]        NVARCHAR(50)     NOT NULL, -- TAB_SWITCH, FOCUS_LOST, FULLSCREEN_EXIT
        [EventAt]          DATETIME2(7)     NOT NULL DEFAULT (SYSUTCDATETIME()),
        [MetaJson]         NVARCHAR(MAX)    NULL,
        CONSTRAINT [PK_AntiCheatEvents] PRIMARY KEY CLUSTERED ([AntiCheatEventId] ASC),
        CONSTRAINT [FK_AntiCheatEvents_Attempt] FOREIGN KEY ([AttemptId]) REFERENCES [dbo].[AssessmentAttempts] ([AttemptId])
    );
    CREATE NONCLUSTERED INDEX [IX_AntiCheatEvents_AttemptId] ON [dbo].[AntiCheatEvents] ([AttemptId] ASC);
END
GO

-- ====================================================================================================
-- PHẦN 7: PHỎNG VẤN & ĐÁNH GIÁ HỘI ĐỒNG (INTERVIEWS & EVALUATIONS)
-- ====================================================================================================

-- 7.1. Lịch phỏng vấn (Interviews)
IF OBJECT_ID('dbo.Interviews', 'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[Interviews] (
        [InterviewId]    UNIQUEIDENTIFIER NOT NULL DEFAULT (NEWID()),
        [ApplicationId]  UNIQUEIDENTIFIER NOT NULL,
        [Title]          NVARCHAR(200)    NULL,
        [ScheduledStart] DATETIME2(7)     NOT NULL,
        [ScheduledEnd]   DATETIME2(7)     NOT NULL,
        [MeetingLink]    NVARCHAR(1000)   NULL,
        [Location]       NVARCHAR(300)    NULL,
        [Status]         NVARCHAR(30)     NOT NULL DEFAULT (N'SCHEDULED'),
        [CreatedBy]      UNIQUEIDENTIFIER NULL,
        [CreatedAt]      DATETIME2(7)     NOT NULL DEFAULT (SYSUTCDATETIME()),
        [InterviewerId]  UNIQUEIDENTIFIER NULL,
        CONSTRAINT [PK_Interviews] PRIMARY KEY CLUSTERED ([InterviewId] ASC),
        CONSTRAINT [FK_Interviews_App] FOREIGN KEY ([ApplicationId]) REFERENCES [dbo].[Applications] ([ApplicationId]),
        CONSTRAINT [FK_Interviews_User] FOREIGN KEY ([CreatedBy]) REFERENCES [dbo].[Users] ([UserId]),
        CONSTRAINT [FK_Interviews_Users_InterviewerId] FOREIGN KEY ([InterviewerId]) REFERENCES [dbo].[Users] ([UserId])
    );
    CREATE NONCLUSTERED INDEX [IX_Interviews_AppId] ON [dbo].[Interviews] ([ApplicationId] ASC);
    CREATE NONCLUSTERED INDEX [IX_Interviews_CreatedBy] ON [dbo].[Interviews] ([CreatedBy] ASC);
    CREATE NONCLUSTERED INDEX [IX_Interviews_InterviewerId] ON [dbo].[Interviews] ([InterviewerId] ASC);
END
GO

-- 7.2. Danh sách thành viên hội đồng phỏng vấn (Interviewers)
IF OBJECT_ID('dbo.Interviewers', 'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[Interviewers] (
        [InterviewId] UNIQUEIDENTIFIER NOT NULL,
        [UserId]      UNIQUEIDENTIFIER NOT NULL,
        [RoleInPanel] NVARCHAR(50)     NULL, -- Lead, Member
        CONSTRAINT [PK_Interviewers] PRIMARY KEY CLUSTERED ([InterviewId] ASC, [UserId] ASC),
        CONSTRAINT [FK_Interviewers_Interview] FOREIGN KEY ([InterviewId]) REFERENCES [dbo].[Interviews] ([InterviewId]),
        CONSTRAINT [FK_Interviewers_User] FOREIGN KEY ([UserId]) REFERENCES [dbo].[Users] ([UserId])
    );
    CREATE NONCLUSTERED INDEX [IX_Interviewers_UserId] ON [dbo].[Interviewers] ([UserId] ASC);
END
GO

-- 7.3. Tiêu chí chấm điểm phỏng vấn (InterviewScorecards)
IF OBJECT_ID('dbo.InterviewScorecards', 'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[InterviewScorecards] (
        [ScorecardId]   UNIQUEIDENTIFIER NOT NULL DEFAULT (NEWID()),
        [InterviewId]   UNIQUEIDENTIFIER NOT NULL,
        [CriteriaCode]  NVARCHAR(50)     NOT NULL,
        [CriteriaName]  NVARCHAR(100)    NOT NULL,
        [MaxScore]      INT              NOT NULL DEFAULT (10),
        [SortOrder]     INT              NOT NULL DEFAULT (0),
        CONSTRAINT [PK_InterviewScorecards] PRIMARY KEY CLUSTERED ([ScorecardId] ASC),
        CONSTRAINT [FK_InterviewScorecards_Interview] FOREIGN KEY ([InterviewId]) REFERENCES [dbo].[Interviews] ([InterviewId])
    );
    CREATE NONCLUSTERED INDEX [IX_InterviewScorecards_InterviewId] ON [dbo].[InterviewScorecards] ([InterviewId] ASC);
END
GO

-- 7.4. Đánh giá phản hồi phỏng vấn (InterviewFeedback)
IF OBJECT_ID('dbo.InterviewFeedback', 'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[InterviewFeedback] (
        [FeedbackId]       UNIQUEIDENTIFIER NOT NULL DEFAULT (NEWID()),
        [InterviewId]      UNIQUEIDENTIFIER NOT NULL,
        [UserId]           UNIQUEIDENTIFIER NOT NULL,
        [OverallScore]     DECIMAL(6, 2)    NULL,
        [Recommendation]   NVARCHAR(30)     NULL, -- STRONG_HIRE, HIRE, CONSIDER, NO_HIRE
        [Notes]            NVARCHAR(MAX)    NULL,
        [DetailScoresJson] NVARCHAR(MAX)    NULL,
        [CreatedAt]        DATETIME2(7)     NOT NULL DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT [PK_InterviewFeedback] PRIMARY KEY CLUSTERED ([FeedbackId] ASC),
        CONSTRAINT [FK_InterviewFeedback_Interview] FOREIGN KEY ([InterviewId]) REFERENCES [dbo].[Interviews] ([InterviewId]),
        CONSTRAINT [FK_InterviewFeedback_User] FOREIGN KEY ([UserId]) REFERENCES [dbo].[Users] ([UserId])
    );
    CREATE NONCLUSTERED INDEX [IX_InterviewFeedback_InterviewId] ON [dbo].[InterviewFeedback] ([InterviewId] ASC);
    CREATE NONCLUSTERED INDEX [IX_InterviewFeedback_UserId] ON [dbo].[InterviewFeedback] ([UserId] ASC);
END
GO

-- 7.5. Phiếu đánh giá phỏng vấn chính thức (InterviewEvaluations)
IF OBJECT_ID('dbo.InterviewEvaluations', 'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[InterviewEvaluations] (
        [EvaluationId]  UNIQUEIDENTIFIER NOT NULL DEFAULT (NEWID()),
        [InterviewId]   UNIQUEIDENTIFIER NOT NULL,
        [InterviewerId] UNIQUEIDENTIFIER NOT NULL,
        [SubmittedById] UNIQUEIDENTIFIER NULL,
        [Score]         INT              NOT NULL,
        [Comment]       NVARCHAR(MAX)    NULL,
        [Result]        NVARCHAR(20)     NOT NULL, -- Passed, Failed, Consider
        [Details]       NVARCHAR(MAX)    NULL,
        [CreatedAt]     DATETIME2(7)     NOT NULL DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT [PK_InterviewEvaluations] PRIMARY KEY CLUSTERED ([EvaluationId] ASC),
        CONSTRAINT [FK_InterviewEvaluations_Interview] FOREIGN KEY ([InterviewId]) REFERENCES [dbo].[Interviews] ([InterviewId]),
        CONSTRAINT [FK_InterviewEvaluations_Interviewer] FOREIGN KEY ([InterviewerId]) REFERENCES [dbo].[Users] ([UserId]),
        CONSTRAINT [FK_InterviewEvaluations_Users_SubmittedById] FOREIGN KEY ([SubmittedById]) REFERENCES [dbo].[Users] ([UserId])
    );
    CREATE NONCLUSTERED INDEX [IX_InterviewEvaluations_InterviewId] ON [dbo].[InterviewEvaluations] ([InterviewId] ASC);
    CREATE NONCLUSTERED INDEX [IX_InterviewEvaluations_InterviewerId] ON [dbo].[InterviewEvaluations] ([InterviewerId] ASC);
    CREATE NONCLUSTERED INDEX [IX_InterviewEvaluations_SubmittedById] ON [dbo].[InterviewEvaluations] ([SubmittedById] ASC);
END
GO

-- ====================================================================================================
-- PHẦN 8: CHATBOT AI, TRỢ LÝ HỎI ĐÁP & TIỀN SÀNG LỌC (CHATBOT & PRE-SCREENING)
-- ====================================================================================================

-- 8.1. Phiên hội thoại Chatbot (ChatSessions)
IF OBJECT_ID('dbo.ChatSessions', 'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[ChatSessions] (
        [ChatSessionId] UNIQUEIDENTIFIER NOT NULL DEFAULT (NEWID()),
        [ApplicationId] UNIQUEIDENTIFIER NOT NULL,
        [StartedAt]     DATETIME2(7)     NOT NULL DEFAULT (SYSUTCDATETIME()),
        [EndedAt]       DATETIME2(7)     NULL,
        [Channel]       NVARCHAR(30)     NOT NULL DEFAULT (N'CAREER_SITE'),
        CONSTRAINT [PK_ChatSessions] PRIMARY KEY CLUSTERED ([ChatSessionId] ASC),
        CONSTRAINT [FK_ChatSessions_App] FOREIGN KEY ([ApplicationId]) REFERENCES [dbo].[Applications] ([ApplicationId])
    );
    CREATE NONCLUSTERED INDEX [IX_ChatSessions_AppId] ON [dbo].[ChatSessions] ([ApplicationId] ASC);
END
GO

-- 8.2. Tin nhắn hội thoại (ChatMessages)
IF OBJECT_ID('dbo.ChatMessages', 'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[ChatMessages] (
        [ChatMessageId]    UNIQUEIDENTIFIER NOT NULL DEFAULT (NEWID()),
        [ChatSessionId]    UNIQUEIDENTIFIER NULL,
        [Sender]           NVARCHAR(20)     NOT NULL, -- user, assistant, bot
        [Message]          NVARCHAR(MAX)    NOT NULL,
        [Intent]           NVARCHAR(MAX)    NULL,
        [IntentConfidence] DECIMAL(18, 2)   NULL,
        [ResponseTimeMs]   INT              NULL,
        [WasEscalated]     BIT              NOT NULL DEFAULT (0),
        [FeedbackId]       UNIQUEIDENTIFIER NULL,
        [MetaJson]         NVARCHAR(MAX)    NULL,
        [CreatedAt]        DATETIME2(7)     NOT NULL DEFAULT (SYSUTCDATETIME()),
        [UpdatedAt]        DATETIME2(7)     NOT NULL DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT [PK_ChatMessages] PRIMARY KEY CLUSTERED ([ChatMessageId] ASC),
        CONSTRAINT [FK_ChatMessages_Session] FOREIGN KEY ([ChatSessionId]) REFERENCES [dbo].[ChatSessions] ([ChatSessionId])
    );
    CREATE NONCLUSTERED INDEX [IX_ChatMessages_SessionId] ON [dbo].[ChatMessages] ([ChatSessionId] ASC);
END
GO

-- 8.3. Cơ sở tri thức hỏi đáp Chatbot (ChatbotFaqs)
IF OBJECT_ID('dbo.ChatbotFaqs', 'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[ChatbotFaqs] (
        [FaqId]     UNIQUEIDENTIFIER NOT NULL DEFAULT (NEWID()),
        [Question]  NVARCHAR(500)    NOT NULL,
        [Answer]    NVARCHAR(MAX)    NOT NULL,
        [Category]  NVARCHAR(100)    NOT NULL DEFAULT (N'General'),
        [Keywords]  NVARCHAR(1000)   NOT NULL DEFAULT (''),
        [Priority]  INT              NOT NULL DEFAULT (0),
        [IsActive]  BIT              NOT NULL DEFAULT (1),
        [CreatedAt] DATETIME2(7)     NOT NULL DEFAULT (SYSUTCDATETIME()),
        [UpdatedAt] DATETIME2(7)     NULL,
        [CreatedBy] UNIQUEIDENTIFIER NULL,
        CONSTRAINT [PK_ChatbotFaqs] PRIMARY KEY CLUSTERED ([FaqId] ASC)
    );
    CREATE NONCLUSTERED INDEX [IX_ChatbotFaqs_IsActive_Priority] ON [dbo].[ChatbotFaqs] ([IsActive] ASC, [Priority] DESC);
END
GO

-- 8.4. Thống kê & phân tích hội thoại Chatbot (ChatAnalytics)
IF OBJECT_ID('dbo.ChatAnalytics', 'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[ChatAnalytics] (
        [AnalyticsId]      UNIQUEIDENTIFIER NOT NULL DEFAULT (NEWID()),
        [MessageId]        UNIQUEIDENTIFIER NULL,
        [Intent]           NVARCHAR(100)    NULL,
        [IntentConfidence] DECIMAL(5, 4)    NULL,
        [ResponseTimeMs]   INT              NULL,
        [ResponseLength]   INT              NULL,
        [UserRating]       INT              NULL,
        [WasEscalated]     BIT              NOT NULL DEFAULT (0),
        [EscalationReason] NVARCHAR(500)    NULL,
        [EntityCount]      INT              NULL,
        [EntityConfidence] DECIMAL(5, 4)    NULL,
        [UserId]           UNIQUEIDENTIFIER NULL,
        [CreatedAt]        DATETIME2(7)     NOT NULL DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT [PK_ChatAnalytics] PRIMARY KEY CLUSTERED ([AnalyticsId] ASC),
        CONSTRAINT [FK_ChatAnalytics_ChatMessage] FOREIGN KEY ([MessageId]) REFERENCES [dbo].[ChatMessages] ([ChatMessageId]) ON DELETE SET NULL,
        CONSTRAINT [FK_ChatAnalytics_User] FOREIGN KEY ([UserId]) REFERENCES [dbo].[Users] ([UserId]) ON DELETE SET NULL
    );
    CREATE NONCLUSTERED INDEX [IX_ChatAnalytics_MessageId] ON [dbo].[ChatAnalytics] ([MessageId] ASC);
    CREATE NONCLUSTERED INDEX [IX_ChatAnalytics_UserId] ON [dbo].[ChatAnalytics] ([UserId] ASC);
    CREATE NONCLUSTERED INDEX [IX_ChatAnalytics_Intent] ON [dbo].[ChatAnalytics] ([Intent] ASC);
    CREATE NONCLUSTERED INDEX [IX_ChatAnalytics_CreatedAt] ON [dbo].[ChatAnalytics] ([CreatedAt] ASC);
END
GO

-- 8.5. Phản hồi đánh giá tin nhắn Bot (ChatFeedbacks)
IF OBJECT_ID('dbo.ChatFeedbacks', 'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[ChatFeedbacks] (
        [FeedbackId]    UNIQUEIDENTIFIER NOT NULL DEFAULT (NEWID()),
        [ChatSessionId] UNIQUEIDENTIFIER NULL,
        [MessageId]     UNIQUEIDENTIFIER NULL,
        [Rating]        INT              NULL,
        [IsHelpful]     BIT              NULL,
        [Category]      NVARCHAR(100)    NULL,
        [Comments]      NVARCHAR(1000)   NULL,
        [UserSentiment] NVARCHAR(50)     NULL,
        [UserId]        UNIQUEIDENTIFIER NULL,
        [CreatedAt]     DATETIME2(7)     NOT NULL DEFAULT (SYSUTCDATETIME()),
        [UpdatedAt]     DATETIME2(7)     NULL DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT [PK_ChatFeedbacks] PRIMARY KEY CLUSTERED ([FeedbackId] ASC),
        CONSTRAINT [FK_ChatFeedback_ChatMessage] FOREIGN KEY ([MessageId]) REFERENCES [dbo].[ChatMessages] ([ChatMessageId]) ON DELETE SET NULL,
        CONSTRAINT [FK_ChatFeedback_ChatSession] FOREIGN KEY ([ChatSessionId]) REFERENCES [dbo].[ChatSessions] ([ChatSessionId]) ON DELETE SET NULL,
        CONSTRAINT [FK_ChatFeedback_User] FOREIGN KEY ([UserId]) REFERENCES [dbo].[Users] ([UserId]) ON DELETE SET NULL
    );
    CREATE NONCLUSTERED INDEX [IX_ChatFeedback_ChatSessionId] ON [dbo].[ChatFeedbacks] ([ChatSessionId] ASC);
    CREATE NONCLUSTERED INDEX [IX_ChatFeedback_UserId] ON [dbo].[ChatFeedbacks] ([UserId] ASC);
    CREATE UNIQUE NONCLUSTERED INDEX [IX_ChatFeedbacks_MessageId] ON [dbo].[ChatFeedbacks] ([MessageId] ASC) WHERE [MessageId] IS NOT NULL;
END
GO

-- 8.6. Câu hỏi tiền sàng lọc tự động (PreScreenQuestions)
IF OBJECT_ID('dbo.PreScreenQuestions', 'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[PreScreenQuestions] (
        [QuestionId]  UNIQUEIDENTIFIER NOT NULL DEFAULT (NEWID()),
        [Code]        NVARCHAR(50)     NOT NULL,
        [Question]    NVARCHAR(500)    NOT NULL,
        [AnswerType]  NVARCHAR(30)     NOT NULL DEFAULT (N'TEXT'), -- TEXT, YESNO, NUMBER, CHOICE
        [OptionsJson] NVARCHAR(MAX)    NULL,
        [IsActive]    BIT              NOT NULL DEFAULT (1),
        [SortOrder]   INT              NOT NULL DEFAULT (0),
        CONSTRAINT [PK_PreScreenQuestions] PRIMARY KEY CLUSTERED ([QuestionId] ASC),
        CONSTRAINT [UQ_PreScreenQuestions_Code] UNIQUE NONCLUSTERED ([Code] ASC)
    );
END
GO

-- 8.7. Câu trả lời tiền sàng lọc (PreScreenAnswers)
IF OBJECT_ID('dbo.PreScreenAnswers', 'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[PreScreenAnswers] (
        [AnswerId]      UNIQUEIDENTIFIER NOT NULL DEFAULT (NEWID()),
        [ApplicationId] UNIQUEIDENTIFIER NOT NULL,
        [QuestionId]    UNIQUEIDENTIFIER NOT NULL,
        [Answer]        NVARCHAR(MAX)    NULL,
        [CreatedAt]     DATETIME2(7)     NOT NULL DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT [PK_PreScreenAnswers] PRIMARY KEY CLUSTERED ([AnswerId] ASC),
        CONSTRAINT [FK_PreScreenAnswers_App] FOREIGN KEY ([ApplicationId]) REFERENCES [dbo].[Applications] ([ApplicationId]),
        CONSTRAINT [FK_PreScreenAnswers_Q] FOREIGN KEY ([QuestionId]) REFERENCES [dbo].[PreScreenQuestions] ([QuestionId])
    );
    CREATE NONCLUSTERED INDEX [IX_PreScreenAnswers_AppId] ON [dbo].[PreScreenAnswers] ([ApplicationId] ASC);
    CREATE NONCLUSTERED INDEX [IX_PreScreenAnswers_QuestionId] ON [dbo].[PreScreenAnswers] ([QuestionId] ASC);
END
GO

-- ====================================================================================================
-- PHẦN 9: EMAIL TỰ ĐỘNG & HÀNG ĐỢI GỬI THƯ (EMAILS & AUTOMATION)
-- ====================================================================================================

-- 9.1. Mẫu thư điện tử (EmailTemplates)
IF OBJECT_ID('dbo.EmailTemplates', 'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[EmailTemplates] (
        [TemplateId] UNIQUEIDENTIFIER NOT NULL DEFAULT (NEWID()),
        [Code]       NVARCHAR(50)     NOT NULL, -- INVITE_TEST, INVITE_INTERVIEW, OFFER, REJECT
        [Subject]    NVARCHAR(300)    NOT NULL,
        [BodyHtml]   NVARCHAR(MAX)    NOT NULL,
        [BodyText]   NVARCHAR(MAX)    NULL,
        [IsActive]   BIT              NOT NULL DEFAULT (1),
        [CreatedBy]  UNIQUEIDENTIFIER NULL,
        [CreatedAt]  DATETIME2(7)     NOT NULL DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT [PK_EmailTemplates] PRIMARY KEY CLUSTERED ([TemplateId] ASC),
        CONSTRAINT [UQ_EmailTemplates_Code] UNIQUE NONCLUSTERED ([Code] ASC),
        CONSTRAINT [FK_EmailTemplates_User] FOREIGN KEY ([CreatedBy]) REFERENCES [dbo].[Users] ([UserId])
    );
    CREATE NONCLUSTERED INDEX [IX_EmailTemplates_CreatedBy] ON [dbo].[EmailTemplates] ([CreatedBy] ASC);
END
GO

-- 9.2. Luật tự động hóa theo sự kiện (AutomationRules)
IF OBJECT_ID('dbo.AutomationRules', 'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[AutomationRules] (
        [RuleId]        UNIQUEIDENTIFIER NOT NULL DEFAULT (NEWID()),
        [Name]          NVARCHAR(200)    NOT NULL,
        [TriggerEvent]  NVARCHAR(50)     NOT NULL, -- STAGE_CHANGED, TEST_PASSED, TEST_FAILED
        [TemplateId]    UNIQUEIDENTIFIER NOT NULL,
        [DelayMinutes]  INT              NOT NULL DEFAULT (0),
        [IsActive]      BIT              NOT NULL DEFAULT (1),
        [ConditionJson] NVARCHAR(MAX)    NULL,
        [CreatedAt]     DATETIME2(7)     NOT NULL DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT [PK_AutomationRules] PRIMARY KEY CLUSTERED ([RuleId] ASC),
        CONSTRAINT [FK_AutomationRules_Template] FOREIGN KEY ([TemplateId]) REFERENCES [dbo].[EmailTemplates] ([TemplateId])
    );
    CREATE NONCLUSTERED INDEX [IX_AutomationRules_TemplateId] ON [dbo].[AutomationRules] ([TemplateId] ASC);
END
GO

-- 9.3. Hàng đợi thư điện tử (EmailQueue)
IF OBJECT_ID('dbo.EmailQueue', 'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[EmailQueue] (
        [EmailQueueId] UNIQUEIDENTIFIER NOT NULL DEFAULT (NEWID()),
        [ToEmail]      NVARCHAR(320)    NOT NULL,
        [ToName]       NVARCHAR(200)    NULL,
        [Subject]      NVARCHAR(300)    NOT NULL,
        [BodyHtml]     NVARCHAR(MAX)    NOT NULL,
        [BodyText]     NVARCHAR(MAX)    NULL,
        [Status]       NVARCHAR(30)     NOT NULL DEFAULT (N'QUEUED'), -- QUEUED, SENT, FAILED
        [RelatedType]  NVARCHAR(50)     NULL,
        [RelatedId]    UNIQUEIDENTIFIER NULL,
        [ScheduledAt]  DATETIME2(7)     NOT NULL DEFAULT (SYSUTCDATETIME()),
        [SentAt]       DATETIME2(7)     NULL,
        [FailReason]   NVARCHAR(1000)   NULL,
        [CreatedAt]    DATETIME2(7)     NOT NULL DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT [PK_EmailQueue] PRIMARY KEY CLUSTERED ([EmailQueueId] ASC)
    );
    CREATE NONCLUSTERED INDEX [IX_EmailQueue_Status] ON [dbo].[EmailQueue] ([Status] ASC);
    CREATE NONCLUSTERED INDEX [IX_EmailQueue_ScheduledAt] ON [dbo].[EmailQueue] ([ScheduledAt] ASC);
END
GO

-- 9.4. Nhật ký gửi thư (EmailSendLog)
IF OBJECT_ID('dbo.EmailSendLog', 'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[EmailSendLog] (
        [EmailSendLogId]    UNIQUEIDENTIFIER NOT NULL DEFAULT (NEWID()),
        [EmailQueueId]      UNIQUEIDENTIFIER NOT NULL,
        [Provider]          NVARCHAR(50)     NOT NULL DEFAULT (N'SMTP'),
        [ProviderMessageId] NVARCHAR(200)    NULL,
        [Result]            NVARCHAR(30)     NOT NULL, -- SENT, FAILED
        [Detail]            NVARCHAR(MAX)    NULL,
        [CreatedAt]         DATETIME2(7)     NOT NULL DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT [PK_EmailSendLog] PRIMARY KEY CLUSTERED ([EmailSendLogId] ASC),
        CONSTRAINT [FK_EmailSendLog_Queue] FOREIGN KEY ([EmailQueueId]) REFERENCES [dbo].[EmailQueue] ([EmailQueueId])
    );
    CREATE NONCLUSTERED INDEX [IX_EmailSendLog_QueueId] ON [dbo].[EmailSendLog] ([EmailQueueId] ASC);
END
GO

-- ====================================================================================================
-- PHẦN 10: ĐÁNH GIÁ LẬP TRÌNH BẰNG AI (AI CODE ASSESSMENT - MODULE 11)
-- ====================================================================================================

-- 10.1. Bài toán / Thử thách lập trình (CodeChallenge)
IF OBJECT_ID('dbo.CodeChallenge', 'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[CodeChallenge] (
        [ChallengeId]      UNIQUEIDENTIFIER NOT NULL DEFAULT (NEWID()),
        [JobId]            UNIQUEIDENTIFIER NOT NULL,
        [Title]            NVARCHAR(MAX)    NOT NULL,
        [ProblemStatement] NVARCHAR(MAX)    NOT NULL,
        [InitialCodeText]  NVARCHAR(MAX)    NULL,
        [Language]         NVARCHAR(MAX)    NOT NULL, -- csharp, python, javascript, java
        [Difficulty]       NVARCHAR(MAX)    NOT NULL DEFAULT (N'MEDIUM'),
        [MaxTimeMinutes]   INT              NOT NULL DEFAULT (60),
        [ExpectedOutput]   NVARCHAR(MAX)    NULL,
        [CreatedAt]        DATETIME2(7)     NOT NULL DEFAULT (SYSUTCDATETIME()),
        [IsActive]         BIT              NOT NULL DEFAULT (1),
        CONSTRAINT [PK_CodeChallenge] PRIMARY KEY CLUSTERED ([ChallengeId] ASC),
        CONSTRAINT [FK_CodeChallenge_Jobs] FOREIGN KEY ([JobId]) REFERENCES [dbo].[Jobs] ([JobId]) ON DELETE CASCADE
    );
    CREATE NONCLUSTERED INDEX [IX_CodeChallenge_JobId] ON [dbo].[CodeChallenge] ([JobId] ASC);
END
GO

-- 10.2. Bài nộp code của ứng viên (CandidateCodeSubmission)
IF OBJECT_ID('dbo.CandidateCodeSubmission', 'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[CandidateCodeSubmission] (
        [SubmissionId]  UNIQUEIDENTIFIER NOT NULL DEFAULT (NEWID()),
        [ChallengeId]   UNIQUEIDENTIFIER NOT NULL,
        [CandidateId]   UNIQUEIDENTIFIER NOT NULL,
        [ApplicationId] UNIQUEIDENTIFIER NOT NULL,
        [SubmittedCode] NVARCHAR(MAX)    NOT NULL,
        [Language]      NVARCHAR(MAX)    NOT NULL,
        [StartTime]     DATETIME2(7)     NOT NULL DEFAULT (SYSUTCDATETIME()),
        [SubmittedAt]   DATETIME2(7)     NOT NULL DEFAULT (SYSUTCDATETIME()),
        [Status]        NVARCHAR(MAX)    NOT NULL DEFAULT (N'PENDING_REVIEW'),
        CONSTRAINT [PK_CandidateCodeSubmission] PRIMARY KEY CLUSTERED ([SubmissionId] ASC),
        CONSTRAINT [FK_CandidateCodeSubmission_CodeChallenge_ChallengeId] FOREIGN KEY ([ChallengeId]) REFERENCES [dbo].[CodeChallenge] ([ChallengeId]) ON DELETE CASCADE
    );
    CREATE NONCLUSTERED INDEX [IX_CandidateCodeSubmission_ChallengeId] ON [dbo].[CandidateCodeSubmission] ([ChallengeId] ASC);
    CREATE NONCLUSTERED INDEX [IX_CandidateCodeSubmission_CandidateId] ON [dbo].[CandidateCodeSubmission] ([CandidateId] ASC);
    CREATE NONCLUSTERED INDEX [IX_CandidateCodeSubmission_ApplicationId] ON [dbo].[CandidateCodeSubmission] ([ApplicationId] ASC);
END
GO

-- 10.3. Báo cáo đánh giá code bằng AI (AiCodeReviewReport)
IF OBJECT_ID('dbo.AiCodeReviewReport', 'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[AiCodeReviewReport] (
        [ReviewId]                UNIQUEIDENTIFIER NOT NULL DEFAULT (NEWID()),
        [SubmissionId]            UNIQUEIDENTIFIER NOT NULL,
        [AiProvider]              NVARCHAR(MAX)    NOT NULL DEFAULT (N'OPENAI_GPT4O'),
        [TotalScore]              INT              NOT NULL,
        [TimeComplexity]          NVARCHAR(MAX)    NULL,
        [SpaceComplexity]         NVARCHAR(MAX)    NULL,
        [CodeSmells]              NVARCHAR(MAX)    NULL,
        [SecurityVulnerabilities] NVARCHAR(MAX)    NULL,
        [OverallFeedback]         NVARCHAR(MAX)    NOT NULL,
        [Suggestions]             NVARCHAR(MAX)    NULL,
        [ReviewedAt]              DATETIME2(7)     NOT NULL DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT [PK_AiCodeReviewReport] PRIMARY KEY CLUSTERED ([ReviewId] ASC),
        CONSTRAINT [FK_AiCodeReviewReport_CandidateCodeSubmission_SubmissionId] FOREIGN KEY ([SubmissionId]) REFERENCES [dbo].[CandidateCodeSubmission] ([SubmissionId]) ON DELETE CASCADE
    );
    CREATE NONCLUSTERED INDEX [IX_AiCodeReviewReport_SubmissionId] ON [dbo].[AiCodeReviewReport] ([SubmissionId] ASC);
END
GO

-- ====================================================================================================
-- PHẦN 11: NẠP DỮ LIỆU KHỞI TẠO ĐẦY ĐỦ (SEED DATA CHUẨN CỦA HỆ THỐNG)
-- ====================================================================================================

-- 11.1. Seed 7 Phòng ban ma trận khu công nghiệp
IF NOT EXISTS (SELECT 1 FROM [dbo].[Departments])
BEGIN
    INSERT INTO [dbo].[Departments] ([DepartmentId], [DepartmentCode], [DepartmentName], [Description], [IsActive], [CreatedAt])
    VALUES
    ('10000000-0000-0000-0000-000000000001', 'ADMIN_SYS', N'Ban Giám Đốc', N'Cấp quản lý cao nhất – Ban Giám đốc và Quản trị hệ thống ATS.', 1, SYSUTCDATETIME()),
    ('10000000-0000-0000-0000-000000000002', 'HR_TEAM', N'Phòng Nhân Sự (HR)', N'Tuyển dụng, sàng lọc hồ sơ, quản lý pipeline ứng viên toàn nhà máy.', 1, SYSUTCDATETIME()),
    ('10000000-0000-0000-0000-000000000003', 'INT_TEAM', N'Hội Đồng Phỏng Vấn', N'Thực hiện phỏng vấn chuyên môn, chấm điểm và phản hồi ứng viên.', 1, SYSUTCDATETIME()),
    ('10000000-0000-0000-0000-000000000004', 'PROD_LINE', N'Xưởng Sản Xuất & Lắp Ráp', N'Trực tiếp sản xuất, lắp ráp sản phẩm theo dây chuyền công nghiệp.', 1, SYSUTCDATETIME()),
    ('10000000-0000-0000-0000-000000000005', 'QA_QC', N'Phòng Quản Lý Chất Lượng (QA/QC)', N'Kiểm tra, kiểm soát chất lượng sản phẩm đầu vào và đầu ra.', 1, SYSUTCDATETIME()),
    ('10000000-0000-0000-0000-000000000006', 'WH_LOGS', N'Khu Vực Kho & Logistics', N'Quản lý kho hàng, vận chuyển, xuất-nhập kho tại khu công nghiệp.', 1, SYSUTCDATETIME()),
    ('10000000-0000-0000-0000-000000000007', 'TECH_MNT', N'Phòng Kỹ Thuật & Bảo Trì', N'Vận hành, bảo dưỡng máy móc, hệ thống điện-cơ trong khu sản xuất.', 1, SYSUTCDATETIME());
    PRINT N'✓ Đã nạp 7 Phòng ban ma trận KCN';
END
GO

-- 11.2. Seed 5 Vai trò hệ thống
IF NOT EXISTS (SELECT 1 FROM [dbo].[Roles])
BEGIN
    INSERT INTO [dbo].[Roles] ([RoleId], [Code], [Name], [CreatedAt])
    VALUES
    ('20000000-0000-0000-0000-000000000001', 'ADMIN', N'Ban Giám Đốc / Administrator', SYSUTCDATETIME()),
    ('20000000-0000-0000-0000-000000000002', 'MANAGER', N'Trưởng Phòng / Quản Lý Khu Vực', SYSUTCDATETIME()),
    ('20000000-0000-0000-0000-000000000003', 'HR', N'Chuyên Viên Tuyển Dụng (HR)', SYSUTCDATETIME()),
    ('20000000-0000-0000-0000-000000000004', 'INTERVIEWER', N'Hội Đồng Phỏng Vấn (Interviewer)', SYSUTCDATETIME()),
    ('20000000-0000-0000-0000-000000000005', 'CANDIDATE', N'Ứng Viên (Candidate)', SYSUTCDATETIME());
    PRINT N'✓ Đã nạp 5 Roles hệ thống';
END
ELSE IF NOT EXISTS (SELECT 1 FROM [dbo].[Roles] WHERE [Code] = 'CANDIDATE')
BEGIN
    INSERT INTO [dbo].[Roles] ([RoleId], [Code], [Name], [CreatedAt])
    VALUES ('20000000-0000-0000-0000-000000000005', 'CANDIDATE', N'Ứng Viên (Candidate)', SYSUTCDATETIME());
    PRINT N'✓ Đã nạp bổ sung Role CANDIDATE';
END
GO

-- 11.3. Seed 5 Vòng tuyển dụng mặc định (PipelineStages)
IF NOT EXISTS (SELECT 1 FROM [dbo].[PipelineStages])
BEGIN
    INSERT INTO [dbo].[PipelineStages] ([StageId], [Code], [Name], [SortOrder], [IsTerminal], [CreatedAt], [SlaMaxDays], [SlaWarnBeforeDays], [IsSlaEnabled])
    VALUES
    (NEWID(), 'NEW_APPLIED', N'Tiếp nhận hồ sơ', 1, 0, SYSUTCDATETIME(), 3, 1, 1),
    (NEWID(), 'TEST_PASSED', N'Đã qua bài Test / Đánh giá', 2, 0, SYSUTCDATETIME(), 5, 1, 1),
    (NEWID(), 'INTERVIEW',   N'Phỏng vấn chuyên môn', 3, 0, SYSUTCDATETIME(), 7, 2, 1),
    (NEWID(), 'OFFER',       N'Đề nghị tuyển dụng (Offer)', 4, 0, SYSUTCDATETIME(), 5, 1, 1),
    (NEWID(), 'REJECTED',    N'Từ chối hồ sơ', 5, 1, SYSUTCDATETIME(), NULL, NULL, 0);
    PRINT N'✓ Đã nạp 5 Pipeline Stages';
END
GO

-- 11.4. Seed Hình thức làm việc (JobTypes)
IF NOT EXISTS (SELECT 1 FROM [dbo].[JobTypes])
BEGIN
    INSERT INTO [dbo].[JobTypes] ([JobTypeId], [Name], [Description])
    VALUES
    (NEWID(), N'Full-time', N'Toàn thời gian – Làm việc 8 giờ/ngày theo ca hoặc giờ hành chính'),
    (NEWID(), N'Part-time', N'Bán thời gian – Làm việc linh hoạt theo ca phân bổ'),
    (NEWID(), N'Contract', N'Hợp đồng – Làm việc theo dự án ngắn hạn hoặc thỏa ước lao động'),
    (NEWID(), N'Internship', N'Thực tập sinh – Chương trình đào tạo và làm việc thực tế'),
    (NEWID(), N'Freelance', N'Tự do – Chuyên gia tư vấn hoặc chuyên trách độc lập'),
    (NEWID(), N'Remote', N'Từ xa – Làm việc trực tuyến qua hệ thống');
    PRINT N'✓ Đã nạp JobTypes';
END
GO

-- 11.5. Seed Kỹ năng chuẩn công nghiệp & CNTT (Skills)
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO
IF NOT EXISTS (SELECT 1 FROM [dbo].[Skills])
BEGIN
    INSERT INTO [dbo].[Skills] ([SkillId], [Name], [CreatedAt], [IsDeleted])
    VALUES
    -- CNTT & Phần mềm
    (NEWID(), 'C#', SYSUTCDATETIME(), 0),
    (NEWID(), '.NET Core', SYSUTCDATETIME(), 0),
    (NEWID(), 'ASP.NET', SYSUTCDATETIME(), 0),
    (NEWID(), 'Angular', SYSUTCDATETIME(), 0),
    (NEWID(), 'TypeScript', SYSUTCDATETIME(), 0),
    (NEWID(), 'JavaScript', SYSUTCDATETIME(), 0),
    (NEWID(), 'React', SYSUTCDATETIME(), 0),
    (NEWID(), 'Vue.js', SYSUTCDATETIME(), 0),
    (NEWID(), 'SQL Server', SYSUTCDATETIME(), 0),
    (NEWID(), 'Python', SYSUTCDATETIME(), 0),
    (NEWID(), 'Java', SYSUTCDATETIME(), 0),
    (NEWID(), 'Docker', SYSUTCDATETIME(), 0),
    (NEWID(), 'Kubernetes', SYSUTCDATETIME(), 0),
    (NEWID(), 'Git', SYSUTCDATETIME(), 0),
    (NEWID(), 'Azure', SYSUTCDATETIME(), 0),
    (NEWID(), 'REST API', SYSUTCDATETIME(), 0),
    -- Kỹ thuật & Sản xuất Nhà máy KCN
    (NEWID(), N'Vận hành máy CNC', SYSUTCDATETIME(), 0),
    (NEWID(), N'Lắp ráp linh kiện điện tử', SYSUTCDATETIME(), 0),
    (NEWID(), N'Lập trình PLC / SCADA', SYSUTCDATETIME(), 0),
    (NEWID(), N'Bảo trì cơ điện công nghiệp', SYSUTCDATETIME(), 0),
    (NEWID(), N'Đọc bản vẽ kỹ thuật cơ khí', SYSUTCDATETIME(), 0),
    (NEWID(), N'An toàn lao động & Vệ sinh HSE', SYSUTCDATETIME(), 0),
    (NEWID(), N'Tiêu chuẩn 5S & Kaizen', SYSUTCDATETIME(), 0),
    (NEWID(), N'Kiểm soát chất lượng QA/QC', SYSUTCDATETIME(), 0),
    (NEWID(), N'Hệ thống quản lý chất lượng ISO 9001', SYSUTCDATETIME(), 0),
    (NEWID(), N'Quản lý kho vận WMS & ERP', SYSUTCDATETIME(), 0);
    PRINT N'✓ Đã nạp danh sách Skills';
END
GO

-- 11.6. Seed Thẻ nhãn công việc (JobTags)
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO
IF NOT EXISTS (SELECT 1 FROM [dbo].[JobTags])
BEGIN
    INSERT INTO [dbo].[JobTags] ([TagId], [Name], [CreatedAt])
    VALUES
    (NEWID(), N'Tuyển gấp', SYSUTCDATETIME()),
    (NEWID(), N'Khu Công Nghiệp', SYSUTCDATETIME()),
    (NEWID(), N'Có xe đưa đón', SYSUTCDATETIME()),
    (NEWID(), N'Lương thưởng cạnh tranh', SYSUTCDATETIME()),
    (NEWID(), N'Làm việc theo ca', SYSUTCDATETIME()),
    (NEWID(), N'Đào tạo tay nghề', SYSUTCDATETIME()),
    (NEWID(), N'Làm từ xa', SYSUTCDATETIME());
    PRINT N'✓ Đã nạp JobTags';
END
GO

-- 11.7. Seed Danh sách Users phân cấp chuẩn (Mật khẩu mặc định: Password123!)
-- Hash BCrypt chuẩn của 'Password123!': $2a$11$A.p0vvc9334BjbAdCvkXIO883CvKFTO9G0.cs3Tjhgd.RxSpbZ6lu
IF NOT EXISTS (SELECT 1 FROM [dbo].[Users])
BEGIN
    -- Biến lưu password hash
    DECLARE @DefaultHash NVARCHAR(500) = N'$2a$11$A.p0vvc9334BjbAdCvkXIO883CvKFTO9G0.cs3Tjhgd.RxSpbZ6lu';

    -- 1. Ban Giám Đốc (ADMIN)
    INSERT INTO [dbo].[Users] ([UserId], [Email], [PasswordHash], [FullName], [Phone], [IsActive], [DepartmentId], [ManagerId], [AuthProvider], [CreatedAt])
    VALUES
    ('30000000-0000-0000-0000-000000000001', 'giamdoc@factory.com', @DefaultHash, N'Nguyễn Văn Giám Đốc', '0901000001', 1, '10000000-0000-0000-0000-000000000001', NULL, 'Local', SYSUTCDATETIME()),
    ('30000000-0000-0000-0000-000000000002', 'pho.giamdoc@factory.com', @DefaultHash, N'Trần Thị Phó Giám Đốc', '0901000002', 1, '10000000-0000-0000-0000-000000000001', '30000000-0000-0000-0000-000000000001', 'Local', SYSUTCDATETIME()),
    ('30000000-0000-0000-0000-000000000099', 'admin@gmail.com', @DefaultHash, N'Võ Văn Sơn (Super Admin)', '0965376312', 1, '10000000-0000-0000-0000-000000000001', NULL, 'Local', SYSUTCDATETIME());

    -- 2. Phòng Nhân Sự (HR)
    INSERT INTO [dbo].[Users] ([UserId], [Email], [PasswordHash], [FullName], [Phone], [IsActive], [DepartmentId], [ManagerId], [AuthProvider], [CreatedAt])
    VALUES
    ('30000000-0000-0000-0000-000000000010', 'truong.hr@factory.com', @DefaultHash, N'Lê Thị Trưởng HR', '0901000010', 1, '10000000-0000-0000-0000-000000000002', '30000000-0000-0000-0000-000000000001', 'Local', SYSUTCDATETIME()),
    ('30000000-0000-0000-0000-000000000011', 'hr1@factory.com', @DefaultHash, N'Phạm Văn Nhân Sự 1', '0901000011', 1, '10000000-0000-0000-0000-000000000002', '30000000-0000-0000-0000-000000000010', 'Local', SYSUTCDATETIME()),
    ('30000000-0000-0000-0000-000000000012', 'hr2@factory.com', @DefaultHash, N'Hoàng Thị Nhân Sự 2', '0901000012', 1, '10000000-0000-0000-0000-000000000002', '30000000-0000-0000-0000-000000000010', 'Local', SYSUTCDATETIME()),
    ('30000000-0000-0000-0000-000000000013', 'sonvp2012003@gmail.com', @DefaultHash, N'Võ Văn Sơn (Phó Tuyển Dụng)', '0909654321', 1, '10000000-0000-0000-0000-000000000002', '30000000-0000-0000-0000-000000000010', 'Local', SYSUTCDATETIME());

    -- 3. Hội Đồng Phỏng Vấn (INTERVIEWER)
    INSERT INTO [dbo].[Users] ([UserId], [Email], [PasswordHash], [FullName], [Phone], [IsActive], [DepartmentId], [ManagerId], [AuthProvider], [CreatedAt])
    VALUES
    ('30000000-0000-0000-0000-000000000020', 'truong.pv@factory.com', @DefaultHash, N'Ngô Văn Trưởng Phỏng Vấn', '0901000020', 1, '10000000-0000-0000-0000-000000000003', '30000000-0000-0000-0000-000000000001', 'Local', SYSUTCDATETIME()),
    ('30000000-0000-0000-0000-000000000021', 'pv1@factory.com', @DefaultHash, N'Vũ Thị Phỏng Vấn Viên 1', '0901000021', 1, '10000000-0000-0000-0000-000000000003', '30000000-0000-0000-0000-000000000020', 'Local', SYSUTCDATETIME()),
    ('30000000-0000-0000-0000-000000000022', 'pv2@factory.com', @DefaultHash, N'Bùi Văn Phỏng Vấn Viên 2', '0901000022', 1, '10000000-0000-0000-0000-000000000003', '30000000-0000-0000-0000-000000000020', 'Local', SYSUTCDATETIME()),
    ('30000000-0000-0000-0000-000000000023', 'sonit2012003@gmail.com', @DefaultHash, N'Võ Văn Sơn (Tech Lead / PV)', '0909123456', 1, '10000000-0000-0000-0000-000000000003', '30000000-0000-0000-0000-000000000020', 'Local', SYSUTCDATETIME());

    -- 4. Xưởng Sản Xuất (PROD)
    INSERT INTO [dbo].[Users] ([UserId], [Email], [PasswordHash], [FullName], [Phone], [IsActive], [DepartmentId], [ManagerId], [AuthProvider], [CreatedAt])
    VALUES
    ('30000000-0000-0000-0000-000000000030', 'truong.sx@factory.com', @DefaultHash, N'Trịnh Văn Trưởng Xưởng', '0901000030', 1, '10000000-0000-0000-0000-000000000004', '30000000-0000-0000-0000-000000000001', 'Local', SYSUTCDATETIME()),
    ('30000000-0000-0000-0000-000000000031', 'sx.hr1@factory.com', @DefaultHash, N'Lý Thị HR Xưởng Sản Xuất', '0901000031', 1, '10000000-0000-0000-0000-000000000004', '30000000-0000-0000-0000-000000000030', 'Local', SYSUTCDATETIME()),
    ('30000000-0000-0000-0000-000000000032', 'sx.pv1@factory.com', @DefaultHash, N'Mai Văn Kỹ Sư Trưởng Xưởng', '0901000032', 1, '10000000-0000-0000-0000-000000000004', '30000000-0000-0000-0000-000000000030', 'Local', SYSUTCDATETIME());

    -- 5. Phòng Quản Lý Chất Lượng (QA/QC)
    INSERT INTO [dbo].[Users] ([UserId], [Email], [PasswordHash], [FullName], [Phone], [IsActive], [DepartmentId], [ManagerId], [AuthProvider], [CreatedAt])
    VALUES
    ('30000000-0000-0000-0000-000000000040', 'truong.qa@factory.com', @DefaultHash, N'Phan Thị Trưởng QA', '0901000040', 1, '10000000-0000-0000-0000-000000000005', '30000000-0000-0000-0000-000000000001', 'Local', SYSUTCDATETIME()),
    ('30000000-0000-0000-0000-000000000041', 'qa.hr1@factory.com', @DefaultHash, N'Tô Văn HR Chất Lượng', '0901000041', 1, '10000000-0000-0000-0000-000000000005', '30000000-0000-0000-0000-000000000040', 'Local', SYSUTCDATETIME()),
    ('30000000-0000-0000-0000-000000000042', 'qa.pv1@factory.com', @DefaultHash, N'Cao Thị Chuyên Viên QA', '0901000042', 1, '10000000-0000-0000-0000-000000000005', '30000000-0000-0000-0000-000000000040', 'Local', SYSUTCDATETIME());

    -- 6. Kho & Logistics
    INSERT INTO [dbo].[Users] ([UserId], [Email], [PasswordHash], [FullName], [Phone], [IsActive], [DepartmentId], [ManagerId], [AuthProvider], [CreatedAt])
    VALUES
    ('30000000-0000-0000-0000-000000000050', 'truong.kho@factory.com', @DefaultHash, N'Lương Văn Trưởng Kho', '0901000050', 1, '10000000-0000-0000-0000-000000000006', '30000000-0000-0000-0000-000000000001', 'Local', SYSUTCDATETIME()),
    ('30000000-0000-0000-0000-000000000051', 'kho.hr1@factory.com', @DefaultHash, N'Đặng Thị HR Kho Vận', '0901000051', 1, '10000000-0000-0000-0000-000000000006', '30000000-0000-0000-0000-000000000050', 'Local', SYSUTCDATETIME());

    -- 7. Kỹ Thuật & Bảo Trì
    INSERT INTO [dbo].[Users] ([UserId], [Email], [PasswordHash], [FullName], [Phone], [IsActive], [DepartmentId], [ManagerId], [AuthProvider], [CreatedAt])
    VALUES
    ('30000000-0000-0000-0000-000000000060', 'truong.kt@factory.com', @DefaultHash, N'Hứa Văn Trưởng Kỹ Thuật', '0901000060', 1, '10000000-0000-0000-0000-000000000007', '30000000-0000-0000-0000-000000000001', 'Local', SYSUTCDATETIME()),
    ('30000000-0000-0000-0000-000000000061', 'kt.pv1@factory.com', @DefaultHash, N'Dương Thị Kỹ Sư Điện 1', '0901000061', 1, '10000000-0000-0000-0000-000000000007', '30000000-0000-0000-0000-000000000060', 'Local', SYSUTCDATETIME()),
    ('30000000-0000-0000-0000-000000000062', 'kt.pv2@factory.com', @DefaultHash, N'Thái Văn Kỹ Sư Cơ Khí 2', '0901000062', 1, '10000000-0000-0000-0000-000000000007', '30000000-0000-0000-0000-000000000060', 'Local', SYSUTCDATETIME());

    PRINT N'✓ Đã nạp danh sách Users phân cấp';
END
GO

-- Đảm bảo có tài khoản Super Admin admin@gmail.com
IF NOT EXISTS (SELECT 1 FROM [dbo].[Users] WHERE [UserId] = '30000000-0000-0000-0000-000000000099')
BEGIN
    DECLARE @DefaultHashAdmin NVARCHAR(500) = N'$2a$11$A.p0vvc9334BjbAdCvkXIO883CvKFTO9G0.cs3Tjhgd.RxSpbZ6lu';
    INSERT INTO [dbo].[Users] ([UserId], [Email], [PasswordHash], [FullName], [Phone], [IsActive], [DepartmentId], [ManagerId], [AuthProvider], [CreatedAt])
    VALUES ('30000000-0000-0000-0000-000000000099', 'admin@gmail.com', @DefaultHashAdmin, N'Võ Văn Sơn (Super Admin)', '0965376312', 1, '10000000-0000-0000-0000-000000000001', NULL, 'Local', SYSUTCDATETIME());

    IF NOT EXISTS (SELECT 1 FROM [dbo].[UserRoles] WHERE [UserId] = '30000000-0000-0000-0000-000000000099')
    BEGIN
        INSERT INTO [dbo].[UserRoles] ([UserId], [RoleId], [CreatedAt])
        VALUES ('30000000-0000-0000-0000-000000000099', '20000000-0000-0000-0000-000000000001', SYSUTCDATETIME());
    END
    PRINT N'✓ Đã nạp bổ sung Super Admin admin@gmail.com';
END
GO

-- 11.8. Seed Gán vai trò cho Users (UserRoles)
IF NOT EXISTS (SELECT 1 FROM [dbo].[UserRoles])
BEGIN
    DECLARE @rAdmin UNIQUEIDENTIFIER = '20000000-0000-0000-0000-000000000001';
    DECLARE @rMgr   UNIQUEIDENTIFIER = '20000000-0000-0000-0000-000000000002';
    DECLARE @rHR    UNIQUEIDENTIFIER = '20000000-0000-0000-0000-000000000003';
    DECLARE @rPV    UNIQUEIDENTIFIER = '20000000-0000-0000-0000-000000000004';

    INSERT INTO [dbo].[UserRoles] ([UserId], [RoleId], [CreatedAt])
    VALUES
    -- Admin
    ('30000000-0000-0000-0000-000000000001', @rAdmin, SYSUTCDATETIME()),
    ('30000000-0000-0000-0000-000000000002', @rAdmin, SYSUTCDATETIME()),
    ('30000000-0000-0000-0000-000000000099', @rAdmin, SYSUTCDATETIME()),
    -- Trưởng phòng (Manager)
    ('30000000-0000-0000-0000-000000000010', @rMgr, SYSUTCDATETIME()),
    ('30000000-0000-0000-0000-000000000020', @rMgr, SYSUTCDATETIME()),
    ('30000000-0000-0000-0000-000000000030', @rMgr, SYSUTCDATETIME()),
    ('30000000-0000-0000-0000-000000000040', @rMgr, SYSUTCDATETIME()),
    ('30000000-0000-0000-0000-000000000050', @rMgr, SYSUTCDATETIME()),
    ('30000000-0000-0000-0000-000000000060', @rMgr, SYSUTCDATETIME()),
    -- HR
    ('30000000-0000-0000-0000-000000000011', @rHR, SYSUTCDATETIME()),
    ('30000000-0000-0000-0000-000000000012', @rHR, SYSUTCDATETIME()),
    ('30000000-0000-0000-0000-000000000013', @rHR, SYSUTCDATETIME()),
    ('30000000-0000-0000-0000-000000000031', @rHR, SYSUTCDATETIME()),
    ('30000000-0000-0000-0000-000000000041', @rHR, SYSUTCDATETIME()),
    ('30000000-0000-0000-0000-000000000051', @rHR, SYSUTCDATETIME()),
    -- Interviewer
    ('30000000-0000-0000-0000-000000000021', @rPV, SYSUTCDATETIME()),
    ('30000000-0000-0000-0000-000000000022', @rPV, SYSUTCDATETIME()),
    ('30000000-0000-0000-0000-000000000023', @rPV, SYSUTCDATETIME()),
    ('30000000-0000-0000-0000-000000000032', @rPV, SYSUTCDATETIME()),
    ('30000000-0000-0000-0000-000000000042', @rPV, SYSUTCDATETIME()),
    ('30000000-0000-0000-0000-000000000061', @rPV, SYSUTCDATETIME()),
    ('30000000-0000-0000-0000-000000000062', @rPV, SYSUTCDATETIME());

    PRINT N'✓ Đã gán vai trò UserRoles';
END
GO

-- 11.9. Seed Cơ sở tri thức câu hỏi thường gặp Chatbot (ChatbotFaqs)
IF NOT EXISTS (SELECT 1 FROM [dbo].[ChatbotFaqs])
BEGIN
    INSERT INTO [dbo].[ChatbotFaqs] ([Question], [Answer], [Category], [Keywords], [Priority], [IsActive])
    VALUES
    (
        N'Tôi muốn ứng tuyển thì làm thế nào?',
        N'Bạn chỉ cần vào danh sách Việc làm tuyển dụng, chọn vị trí phù hợp, nhấn nút "Ứng tuyển" và tải file CV định dạng PDF lên hệ thống để AI tự động phân tích.',
        N'Ứng tuyển',
        N'ứng tuyển,apply,nộp hồ sơ,cv,hướng dẫn ứng tuyển',
        100,
        1
    ),
    (
        N'Bao lâu thì nhận được phản hồi sau khi nộp hồ sơ?',
        N'Hệ thống AI sẽ sàng lọc và phản hồi sơ bộ trong vòng 24-48 giờ. Bạn có thể tra cứu trạng thái xét duyệt thời gian thực trong mục "Hồ sơ của tôi".',
        N'Ứng tuyển',
        N'phản hồi,kết quả,thời gian,chờ đợi,tiến độ hồ sơ',
        90,
        1
    ),
    (
        N'Nhà máy có chế độ xe đưa đón và phụ cấp không?',
        N'Công ty có tuyến xe bus đưa đón cán bộ công nhân viên tại các điểm trung tâm thành phố đến khu công nghiệp, cùng phụ cấp ăn trưa và chuyên cần theo quy định.',
        N'Phúc lợi',
        N'xe đưa đón,phụ cấp,chế độ,ăn trưa,kcn',
        85,
        1
    ),
    (
        N'Chatbot AI này hỗ trợ những nội dung gì?',
        N'Tôi hỗ trợ giải đáp chính sách tuyển dụng, vị trí đang tuyển, tiêu chuẩn kỹ năng, thông tin phỏng vấn và hướng dẫn làm bài test đánh giá năng lực.',
        N'Chung',
        N'chatbot,hỗ trợ,nội dung,hỏi đáp,tính năng',
        80,
        1
    );
    PRINT N'✓ Đã nạp ChatbotFaqs';
END
GO

-- 11.10. Seed Mẫu Thư Điện Tử Tự Động (EmailTemplates)
IF NOT EXISTS (SELECT 1 FROM [dbo].[EmailTemplates])
BEGIN
    INSERT INTO [dbo].[EmailTemplates] ([TemplateId], [Code], [Subject], [BodyHtml], [BodyText], [IsActive], [CreatedAt])
    VALUES
    (
        NEWID(),
        'INVITE_TEST',
        N'[V9 TECH / ATS] Thư mời tham gia bài kiểm tra đánh giá năng lực',
        N'<h3>Kính gửi Ứng viên,</h3><p>Hồ sơ của bạn đã vượt qua vòng sàng lọc ban đầu. Chúng tôi trân trọng mời bạn tham gia bài kiểm tra năng lực trực tuyến.</p><p>Mã dự thi của bạn: <b>{{InviteCode}}</b></p><p>Hạn chót làm bài: {{ExpiresAt}}</p><p>Trân trọng,<br>Ban Tuyển Dụng</p>',
        N'Kính gửi Ứng viên, Hồ sơ của bạn đã vượt qua vòng sơ tuyển. Mã dự thi của bạn: {{InviteCode}}. Hạn chót: {{ExpiresAt}}',
        1,
        SYSUTCDATETIME()
    ),
    (
        NEWID(),
        'INVITE_INTERVIEW',
        N'[V9 TECH / ATS] Thư mời tham gia phỏng vấn chuyên môn',
        N'<h3>Kính gửi Ứng viên,</h3><p>Chúng tôi trân trọng kính mời bạn tham gia buổi phỏng vấn chuyên môn cho vị trí ứng tuyển.</p><p>Thời gian: <b>{{ScheduledStart}}</b></p><p>Hình thức / Địa điểm: {{Location}}</p><p>Link trực tuyến (nếu có): {{MeetingLink}}</p><p>Trân trọng,<br>Hội Đồng Phỏng Vấn</p>',
        N'Kính gửi Ứng viên, Trân trọng mời bạn tham gia phỏng vấn lúc: {{ScheduledStart}}. Địa điểm: {{Location}}',
        1,
        SYSUTCDATETIME()
    ),
    (
        NEWID(),
        'OFFER_LETTER',
        N'[V9 TECH / ATS] Thư mời nhận việc (Job Offer Letter)',
        N'<h3>Chúc mừng bạn!</h3><p>Hội đồng tuyển dụng trân trọng gửi đến bạn lời mời làm việc chính thức tại công ty.</p><p>Vui lòng xem chi tiết thỏa ước và xác nhận phản hồi trong vòng 3 ngày làm việc.</p><p>Trân trọng,<br>Phòng Nhân Sự</p>',
        N'Chúc mừng bạn đã trúng tuyển. Vui lòng kiểm tra email chi tiết để hoàn tất thủ tục nhận việc.',
        1,
        SYSUTCDATETIME()
    ),
    (
        NEWID(),
        'REJECTION',
        N'[V9 TECH / ATS] Cảm ơn bạn đã quan tâm ứng tuyển',
        N'<h3>Kính gửi Ứng viên,</h3><p>Cảm ơn bạn đã dành thời gian quan tâm và tham gia quy trình ứng tuyển. Dù rất tiếc chưa thể đồng hành cùng bạn ở vị trí này, thông tin của bạn đã được lưu trữ trong hệ thống nhân tài của chúng tôi cho các cơ hội tiếp theo.</p><p>Chúc bạn luôn thành công!</p>',
        N'Cảm ơn bạn đã ứng tuyển. Chúc bạn luôn thành công trên con đường sự nghiệp.',
        1,
        SYSUTCDATETIME()
    );
    PRINT N'✓ Đã nạp EmailTemplates';
END
GO

-- Đảm bảo constraint DEFAULT 0 cho IsDeleted
IF NOT EXISTS (SELECT 1 FROM sys.default_constraints WHERE parent_object_id = OBJECT_ID('Jobs') AND col_name(parent_object_id, parent_column_id) = 'IsDeleted')
BEGIN
    ALTER TABLE [dbo].[Jobs] ADD CONSTRAINT [DF_Jobs_IsDeleted] DEFAULT (0) FOR [IsDeleted];
END
GO
IF NOT EXISTS (SELECT 1 FROM sys.default_constraints WHERE parent_object_id = OBJECT_ID('Candidates') AND col_name(parent_object_id, parent_column_id) = 'IsDeleted')
BEGIN
    ALTER TABLE [dbo].[Candidates] ADD CONSTRAINT [DF_Candidates_IsDeleted] DEFAULT (0) FOR [IsDeleted];
END
GO

-- 11.11. Seed Một số Tin tuyển dụng mẫu thực tế (Jobs)
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO
IF NOT EXISTS (SELECT 1 FROM [dbo].[Jobs])
BEGIN
    DECLARE @uAdmin UNIQUEIDENTIFIER = '30000000-0000-0000-0000-000000000099';
    DECLARE @dIT    UNIQUEIDENTIFIER = '10000000-0000-0000-0000-000000000001';
    DECLARE @dQA    UNIQUEIDENTIFIER = '10000000-0000-0000-0000-000000000005';
    DECLARE @dPROD  UNIQUEIDENTIFIER = '10000000-0000-0000-0000-000000000004';
    DECLARE @dWH    UNIQUEIDENTIFIER = '10000000-0000-0000-0000-000000000006';

    INSERT INTO [dbo].[Jobs]
    (
        [JobId], [Code], [Title], [Department], [DepartmentId], [Location], [EmploymentType],
        [ExperienceLevel], [SeniorityLevel], [SalaryMin], [SalaryMax], [Currency],
        [Description], [Requirements], [Benefits], [NumberOfPositions], [Status], [CreatedBy], [CreatedAt], [IsDeleted]
    )
    VALUES
    (
        '50000000-0000-0000-0000-000000000001',
        'JOB-2026-IT01',
        N'Kỹ sư Lập trình C# / .NET Core Fullstack',
        N'Ban Giám Đốc / CNTT',
        @dIT,
        N'Hà Nội & Khu Công Nghiệp',
        N'Full-time',
        N'2-4 năm',
        N'Mid-Senior',
        18000000,
        30000000,
        N'VND',
        N'Phát triển và bảo trì hệ thống Quản trị tuyển dụng ATS, tích hợp AI screening và API nội bộ.',
        N'Thành thạo C#, ASP.NET Core, Entity Framework Core, SQL Server. Có kinh nghiệm Angular hoặc React.',
        N'Lương thưởng tháng 13+, bảo hiểm đầy đủ, máy tính làm việc hiệu năng cao.',
        3,
        N'OPEN',
        @uAdmin,
        SYSUTCDATETIME(),
        0
    ),
    (
        '50000000-0000-0000-0000-000000000002',
        'JOB-2026-QA01',
        N'Kỹ sư Quản lý Chất Lượng QA/QC Nhà Máy',
        N'Phòng Quản Lý Chất Lượng (QA/QC)',
        @dQA,
        N'Khu Công Nghiệp Đình Vũ',
        N'Full-time',
        N'1-3 năm',
        N'Junior-Mid',
        12000000,
        18000000,
        N'VND',
        N'Kiểm tra, kiểm soát quy trình và chất lượng linh kiện, sản phẩm đầu ra theo tiêu chuẩn ISO 9001.',
        N'Tốt nghiệp Cao đẳng/Đại học chuyên ngành Kỹ thuật, Cơ khí, Điện tử. Nắm rõ tiêu chuẩn 5S/Kaizen.',
        N'Xe đưa đón hàng ngày, phụ cấp ăn trưa tại căng tin, thưởng hiệu suất hàng quý.',
        5,
        N'OPEN',
        @uAdmin,
        SYSUTCDATETIME(),
        0
    ),
    (
        '50000000-0000-0000-0000-000000000003',
        'JOB-2026-PR01',
        N'Kỹ thuật viên Vận hành Dây chuyền & Máy CNC',
        N'Xưởng Sản Xuất & Lắp Ráp',
        @dPROD,
        N'Khu Công Nghiệp Đình Vũ',
        N'Full-time',
        N'Dưới 1 năm / Mới tốt nghiệp',
        N'Fresher-Junior',
        9000000,
        14000000,
        N'VND',
        N'Trực tiếp đứng máy, vận hành dây chuyền sản xuất tự động và báo cáo sự cố cơ khí.',
        N'Có bằng nghề cơ khí/chế tạo hoặc tương đương. Chăm chỉ, có khả năng làm việc theo ca.',
        N'Hỗ trợ ký túc xá cho công nhân xa nhà, phụ cấp ca đêm hấp dẫn.',
        15,
        N'OPEN',
        @uAdmin,
        SYSUTCDATETIME(),
        0
    );
    PRINT N'✓ Đã nạp các Jobs mẫu';
END
GO

-- 11.12. Seed Thử thách lập trình mẫu (CodeChallenge)
IF NOT EXISTS (SELECT 1 FROM [dbo].[CodeChallenge])
BEGIN
    DECLARE @jId UNIQUEIDENTIFIER = '50000000-0000-0000-0000-000000000001';
    INSERT INTO [dbo].[CodeChallenge] ([ChallengeId], [JobId], [Title], [ProblemStatement], [InitialCodeText], [Language], [Difficulty], [MaxTimeMinutes], [ExpectedOutput], [CreatedAt], [IsActive])
    VALUES
    (
        NEWID(),
        @jId,
        N'Thuật toán Tìm kiếm Phần tử Xuất hiện Nhiều nhất trong Mảng',
        N'Cho một mảng các số nguyên nums. Hãy viết phương thức trả về số nguyên xuất hiện nhiều lần nhất trong mảng. Nếu có nhiều số cùng tần suất lớn nhất, trả về số nhỏ nhất trong số đó.',
        N'public class Solution {' + CHAR(13) + CHAR(10) + N'    public int FindMostFrequent(int[] nums) {' + CHAR(13) + CHAR(10) + N'        // Viết code của bạn ở đây' + CHAR(13) + CHAR(10) + N'        return 0;' + CHAR(13) + CHAR(10) + N'    }' + CHAR(13) + CHAR(10) + N'}',
        'csharp',
        'MEDIUM',
        45,
        'int',
        SYSUTCDATETIME(),
        1
    );
    PRINT N'✓ Đã nạp CodeChallenge mẫu';
END
GO

-- 11.13. Seed Ngân hàng câu hỏi trắc nghiệm kỹ thuật & Bài thi mẫu
IF NOT EXISTS (SELECT 1 FROM [dbo].[QuestionBank])
BEGIN
    DECLARE @uAdminQ UNIQUEIDENTIFIER = '30000000-0000-0000-0000-000000000099';
    DECLARE @q1 UNIQUEIDENTIFIER = NEWID();
    DECLARE @q2 UNIQUEIDENTIFIER = NEWID();
    DECLARE @q3 UNIQUEIDENTIFIER = NEWID();

    -- Câu hỏi 1
    INSERT INTO [dbo].[QuestionBank] ([BankQuestionId], [Content], [Type], [Difficulty], [Tags], [Explanation], [IsActive], [CreatedBy], [CreatedAt])
    VALUES (@q1, N'Trong C#, từ khóa nào được sử dụng để giải phóng bộ nhớ không được quản lý (unmanaged resources)?', N'MCQ_SINGLE', N'EASY', N'C#,.NET', N'Giao diện IDisposable và phương thức Dispose() kết hợp khối using.', 1, @uAdminQ, SYSUTCDATETIME());

    INSERT INTO [dbo].[QuestionOptions] ([OptionId], [BankQuestionId], [Content], [IsCorrect], [SortOrder])
    VALUES
    (NEWID(), @q1, N'using / IDisposable', 1, 1),
    (NEWID(), @q1, N'delete', 0, 2),
    (NEWID(), @q1, N'free', 0, 3),
    (NEWID(), @q1, N'finalize', 0, 4);

    -- Câu hỏi 2
    INSERT INTO [dbo].[QuestionBank] ([BankQuestionId], [Content], [Type], [Difficulty], [Tags], [Explanation], [IsActive], [CreatedBy], [CreatedAt])
    VALUES (@q2, N'Tiêu chuẩn 5S trong môi trường sản xuất công nghiệp bao gồm những bước nào?', N'MCQ_SINGLE', N'EASY', N'KCN,5S,QA', N'5S gồm: Sàng lọc (Seiri), Sắp xếp (Seiton), Sạch sẽ (Seiso), Săn sóc (Seiketsu), Sẵn sàng (Shitsuke).', 1, @uAdminQ, SYSUTCDATETIME());

    INSERT INTO [dbo].[QuestionOptions] ([OptionId], [BankQuestionId], [Content], [IsCorrect], [SortOrder])
    VALUES
    (NEWID(), @q2, N'Sàng lọc, Sắp xếp, Sạch sẽ, Săn sóc, Sẵn sàng', 1, 1),
    (NEWID(), @q2, N'Sản xuất, Sắp xếp, Sạch sẽ, Săn sóc, Sẵn sàng', 0, 2),
    (NEWID(), @q2, N'Sàng lọc, Sáng tạo, Sạch sẽ, Săn sóc, Sẵn sàng', 0, 3),
    (NEWID(), @q2, N'Sản lượng, Sắp xếp, Sạch sẽ, Săn sóc, Sáng suốt', 0, 4);

    -- Câu hỏi 3
    INSERT INTO [dbo].[QuestionBank] ([BankQuestionId], [Content], [Type], [Difficulty], [Tags], [Explanation], [IsActive], [CreatedBy], [CreatedAt])
    VALUES (@q3, N'Trong cơ sở dữ liệu SQL Server, thuộc tính nào cho phép tối ưu truy vấn bằng cách lưu trữ kết quả tính toán?', N'MCQ_SINGLE', N'MEDIUM', N'SQL,Database', N'Computed columns có từ khóa PERSISTED được lưu trữ vật lý trên ổ đĩa.', 1, @uAdminQ, SYSUTCDATETIME());

    INSERT INTO [dbo].[QuestionOptions] ([OptionId], [BankQuestionId], [Content], [IsCorrect], [SortOrder])
    VALUES
    (NEWID(), @q3, N'PERSISTED', 1, 1),
    (NEWID(), @q3, N'CLUSTERED', 0, 2),
    (NEWID(), @q3, N'INDEXED', 0, 3),
    (NEWID(), @q3, N'STORED', 0, 4);

    -- Tạo đề thi mẫu liên kết
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
-- PHẦN 12: BÁO CÁO TỔNG KẾT TRẠNG THÁI DATABASE
-- ====================================================================================================
PRINT N'';
PRINT N'====================================================================================';
PRINT N'✓ HOÀN THÀNH CẬP NHẬT DATABASE UTC_DATN TOÀN DIỆN VÀ ĐẦY ĐỦ';
PRINT N'====================================================================================';
SELECT 
    t.name AS [TableName],
    p.rows AS [RowCount]
FROM sys.tables t
INNER JOIN sys.partitions p ON t.object_id = p.object_id AND p.index_id IN (0, 1)
ORDER BY t.name;
GO
