-- ============================================================================
-- Migration: Add SLA Analytics & Bottleneck Detection Tables
-- Date: 2026-05-12
-- Purpose: Support advanced SLA tracking, bottleneck analysis, and pipeline insights
-- ============================================================================

IF OBJECT_ID('dbo.SlaStageConfiguration', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.SlaStageConfiguration
    (
        SlaStageConfigId UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_SlaStageConfiguration PRIMARY KEY DEFAULT NEWID(),
        StageCode NVARCHAR(100) NOT NULL CONSTRAINT UQ_SlaStageConfiguration_StageCode UNIQUE,
        StageName NVARCHAR(200) NOT NULL,
        SlaMaxDays INT NOT NULL CONSTRAINT DF_SlaStageConfiguration_SlaMaxDays DEFAULT 7,
        SlaWarnBeforeDays INT NOT NULL CONSTRAINT DF_SlaStageConfiguration_SlaWarnBeforeDays DEFAULT 2,
        IsActive BIT NOT NULL CONSTRAINT DF_SlaStageConfiguration_IsActive DEFAULT 1,
        OrderSequence INT NOT NULL CONSTRAINT DF_SlaStageConfiguration_OrderSequence DEFAULT 0,
        CreatedAt DATETIME2 NOT NULL CONSTRAINT DF_SlaStageConfiguration_CreatedAt DEFAULT SYSUTCDATETIME(),
        UpdatedAt DATETIME2 NULL,
        CreatedBy UNIQUEIDENTIFIER NULL
    );

    CREATE INDEX IX_SlaStageConfiguration_OrderSequence ON dbo.SlaStageConfiguration (OrderSequence);
    CREATE INDEX IX_SlaStageConfiguration_IsActive ON dbo.SlaStageConfiguration (IsActive);
END;
GO

IF OBJECT_ID('dbo.ApplicationSlaTracking', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.ApplicationSlaTracking
    (
        SlaTrackingId UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_ApplicationSlaTracking PRIMARY KEY DEFAULT NEWID(),
        ApplicationId UNIQUEIDENTIFIER NOT NULL CONSTRAINT FK_ApplicationSlaTracking_Application FOREIGN KEY (ApplicationId) REFERENCES dbo.Applications(ApplicationId),
        CurrentStageCode NVARCHAR(100) NOT NULL,
        EnteredStageAt DATETIME2 NOT NULL,
        SlaMaxDays INT NOT NULL,
        SlaDueAt DATETIME2 NOT NULL,
        SlaWarningAt DATETIME2 NOT NULL,
        SlaStatus NVARCHAR(20) NOT NULL CONSTRAINT DF_ApplicationSlaTracking_SlaStatus DEFAULT 'ON_TRACK', -- ON_TRACK, WARNING, BREACH
        OverdueDays INT NULL,
        IsResolved BIT NOT NULL CONSTRAINT DF_ApplicationSlaTracking_IsResolved DEFAULT 0,
        ResolutionNotes NVARCHAR(500) NULL,
        CreatedAt DATETIME2 NOT NULL CONSTRAINT DF_ApplicationSlaTracking_CreatedAt DEFAULT SYSUTCDATETIME(),
        UpdatedAt DATETIME2 NOT NULL CONSTRAINT DF_ApplicationSlaTracking_UpdatedAt DEFAULT SYSUTCDATETIME()
    );

    CREATE INDEX IX_ApplicationSlaTracking_ApplicationId ON dbo.ApplicationSlaTracking (ApplicationId);
    CREATE INDEX IX_ApplicationSlaTracking_SlaStatus ON dbo.ApplicationSlaTracking (SlaStatus);
    CREATE INDEX IX_ApplicationSlaTracking_CurrentStageCode ON dbo.ApplicationSlaTracking (CurrentStageCode);
    CREATE INDEX IX_ApplicationSlaTracking_IsResolved ON dbo.ApplicationSlaTracking (IsResolved);
END;
GO

IF OBJECT_ID('dbo.StageBottleneckAnalysis', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.StageBottleneckAnalysis
    (
        BottleneckAnalysisId UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_StageBottleneckAnalysis PRIMARY KEY DEFAULT NEWID(),
        StageCode NVARCHAR(100) NOT NULL,
        StageName NVARCHAR(200) NOT NULL,
        AnalysisDate DATE NOT NULL,
        TotalApplicationsInStage INT NOT NULL CONSTRAINT DF_StageBottleneckAnalysis_TotalApplicationsInStage DEFAULT 0,
        ApplicationsOnTrack INT NOT NULL CONSTRAINT DF_StageBottleneckAnalysis_ApplicationsOnTrack DEFAULT 0,
        ApplicationsWarning INT NOT NULL CONSTRAINT DF_StageBottleneckAnalysis_ApplicationsWarning DEFAULT 0,
        ApplicationsBreach INT NOT NULL CONSTRAINT DF_StageBottleneckAnalysis_ApplicationsBreach DEFAULT 0,
        AverageDaysInStage DECIMAL(10, 2) NOT NULL CONSTRAINT DF_StageBottleneckAnalysis_AverageDaysInStage DEFAULT 0,
        BottleneckScore DECIMAL(5, 2) NOT NULL CONSTRAINT DF_StageBottleneckAnalysis_BottleneckScore DEFAULT 0, -- 0-100 (higher = worse bottleneck)
        IsBottleneck BIT NOT NULL CONSTRAINT DF_StageBottleneckAnalysis_IsBottleneck DEFAULT 0,
        PreviousDayBottleneckScore DECIMAL(5, 2) NULL,
        TrendDirection NVARCHAR(20) NULL, -- IMPROVING, STABLE, WORSENING
        CreatedAt DATETIME2 NOT NULL CONSTRAINT DF_StageBottleneckAnalysis_CreatedAt DEFAULT SYSUTCDATETIME()
    );

    CREATE INDEX IX_StageBottleneckAnalysis_StageCode_AnalysisDate ON dbo.StageBottleneckAnalysis (StageCode, AnalysisDate DESC);
    CREATE INDEX IX_StageBottleneckAnalysis_IsBottleneck ON dbo.StageBottleneckAnalysis (IsBottleneck);
END;
GO

IF OBJECT_ID('dbo.SlaBreachAlert', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.SlaBreachAlert
    (
        AlertId UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_SlaBreachAlert PRIMARY KEY DEFAULT NEWID(),
        ApplicationId UNIQUEIDENTIFIER NOT NULL CONSTRAINT FK_SlaBreachAlert_Application FOREIGN KEY (ApplicationId) REFERENCES dbo.Applications(ApplicationId),
        AlertType NVARCHAR(50) NOT NULL, -- WARNING, BREACH
        StageCode NVARCHAR(100) NOT NULL,
        TriggeredAt DATETIME2 NOT NULL,
        SlaDueAt DATETIME2 NOT NULL,
        AlertMessage NVARCHAR(500) NOT NULL,
        IsAcknowledged BIT NOT NULL CONSTRAINT DF_SlaBreachAlert_IsAcknowledged DEFAULT 0,
        AcknowledgedBy UNIQUEIDENTIFIER NULL,
        AcknowledgedAt DATETIME2 NULL,
        AcknowledgmentNotes NVARCHAR(500) NULL,
        CreatedAt DATETIME2 NOT NULL CONSTRAINT DF_SlaBreachAlert_CreatedAt DEFAULT SYSUTCDATETIME()
    );

    CREATE INDEX IX_SlaBreachAlert_ApplicationId ON dbo.SlaBreachAlert (ApplicationId);
    CREATE INDEX IX_SlaBreachAlert_AlertType ON dbo.SlaBreachAlert (AlertType);
    CREATE INDEX IX_SlaBreachAlert_IsAcknowledged ON dbo.SlaBreachAlert (IsAcknowledged);
    CREATE INDEX IX_SlaBreachAlert_TriggeredAt ON dbo.SlaBreachAlert (TriggeredAt DESC);
END;
GO

-- Insert default SLA stage configurations
IF NOT EXISTS (SELECT 1 FROM dbo.SlaStageConfiguration WHERE StageCode = 'NEW_APPLIED')
BEGIN
    INSERT INTO dbo.SlaStageConfiguration (StageCode, StageName, SlaMaxDays, SlaWarnBeforeDays, OrderSequence)
    VALUES 
        ('NEW_APPLIED', 'Ứng tuyển mới', 3, 1, 1),
        ('SCREENING', 'Đang xét duyệt', 2, 1, 2),
        ('INTERVIEW', 'Phỏng vấn', 7, 2, 3),
        ('OFFER', 'Chờ Offer', 5, 2, 4),
        ('OFFER_SENT', 'Đã gửi Offer', 3, 1, 5),
        ('HIRED', 'Đã tuyển', 1, 0, 6),
        ('REJECTED', 'Đã loại', 0, 0, 7),
        ('WAITLIST', 'Dự phòng', 30, 5, 8);
END;
GO

PRINT 'Migration completed successfully!';
