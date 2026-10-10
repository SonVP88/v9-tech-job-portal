IF OBJECT_ID('dbo.ChatbotFaqs', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.ChatbotFaqs
    (
        FaqId UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_ChatbotFaqs PRIMARY KEY DEFAULT NEWID(),
        Question NVARCHAR(500) NOT NULL,
        Answer NVARCHAR(MAX) NOT NULL,
        Category NVARCHAR(100) NOT NULL CONSTRAINT DF_ChatbotFaqs_Category DEFAULT N'General',
        Keywords NVARCHAR(1000) NULL,
        Priority INT NOT NULL CONSTRAINT DF_ChatbotFaqs_Priority DEFAULT 0,
        IsActive BIT NOT NULL CONSTRAINT DF_ChatbotFaqs_IsActive DEFAULT 1,
        CreatedAt DATETIME2 NOT NULL CONSTRAINT DF_ChatbotFaqs_CreatedAt DEFAULT SYSUTCDATETIME(),
        UpdatedAt DATETIME2 NULL,
        CreatedBy UNIQUEIDENTIFIER NULL
    );
END;
GO

IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE name = 'IX_ChatbotFaqs_IsActive_Priority'
      AND object_id = OBJECT_ID('dbo.ChatbotFaqs')
)
BEGIN
    CREATE INDEX IX_ChatbotFaqs_IsActive_Priority
        ON dbo.ChatbotFaqs (IsActive, Priority DESC);
END;
GO

IF NOT EXISTS (
    SELECT 1
    FROM dbo.ChatbotFaqs
)
BEGIN
    INSERT INTO dbo.ChatbotFaqs (Question, Answer, Category, Keywords, Priority, IsActive)
    VALUES
    (
        N'Tôi muốn ứng tuyển thì làm thế nào?',
        N'Bạn vào trang chi tiết công việc, nhấn nút Ứng tuyển và điền đầy đủ thông tin họ tên, email, số điện thoại, sau đó tải CV PDF trước khi gửi hồ sơ.',
        N'Application',
        N'ứng tuyển,apply,nộp hồ sơ,cv',
        100,
        1
    ),
    (
        N'Bao lâu thì có phản hồi sau khi ứng tuyển?',
        N'Thời gian phản hồi phụ thuộc vào từng vị trí. Bạn có thể theo dõi trạng thái hồ sơ trong mục My Applications để biết tiến trình mới nhất.',
        N'Application',
        N'phản hồi,trạng thái,hồ sơ,my applications',
        90,
        1
    ),
    (
        N'Chatbot này hỗ trợ những nội dung gì?',
        N'Tôi hỗ trợ các câu hỏi liên quan việc làm tại V9 TECH, kỹ năng IT, quy trình ứng tuyển, phỏng vấn và offer. Các chủ đề ngoài tuyển dụng có thể không được hỗ trợ.',
        N'General',
        N'chatbot,hỗ trợ,nội dung,faq',
        80,
        1
    );
END;
GO
