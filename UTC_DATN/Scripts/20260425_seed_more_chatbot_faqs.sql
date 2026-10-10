IF OBJECT_ID('dbo.ChatbotFaqs', 'U') IS NULL
BEGIN
    PRINT N'Bảng dbo.ChatbotFaqs chưa tồn tại. Hãy chạy script 20260425_add_chatbot_faqs.sql trước.';
    RETURN;
END;
GO

DECLARE @FaqSeed TABLE
(
    Question NVARCHAR(500) NOT NULL,
    Answer NVARCHAR(MAX) NOT NULL,
    Category NVARCHAR(100) NOT NULL,
    Keywords NVARCHAR(1000) NULL,
    Priority INT NOT NULL,
    IsActive BIT NOT NULL
);

INSERT INTO @FaqSeed (Question, Answer, Category, Keywords, Priority, IsActive)
VALUES
(N'Cách tìm việc đang tuyển nhanh nhất trên hệ thống?', N'Bạn vào trang Việc làm, dùng bộ lọc theo vị trí, kỹ năng, địa điểm và mức lương. Ưu tiên chọn các job trạng thái mở và còn hạn nộp hồ sơ.', N'Việc làm', N'tìm việc,job,việc làm,lọc,open', 120, 1),
(N'Tôi có thể ứng tuyển nhiều vị trí cùng lúc không?', N'Bạn có thể ứng tuyển nhiều vị trí nếu hồ sơ phù hợp. Nên ưu tiên các vị trí đúng kỹ năng để tăng tỉ lệ được liên hệ.', N'Ứng tuyển', N'ứng tuyển nhiều vị trí,nộp nhiều job,apply nhiều', 118, 1),
(N'Hồ sơ ứng tuyển cần những thông tin gì?', N'Hồ sơ nên có họ tên, email, số điện thoại, vị trí ứng tuyển, kỹ năng chính, kinh nghiệm, học vấn và CV đính kèm dạng PDF.', N'Ứng tuyển', N'hồ sơ,thông tin ứng tuyển,cv pdf', 116, 1),
(N'Tôi có thể cập nhật CV sau khi đã nộp không?', N'Bạn có thể nộp lại hồ sơ mới cho vị trí phù hợp hoặc cập nhật hồ sơ cá nhân trước khi ứng tuyển các vị trí tiếp theo.', N'Ứng tuyển', N'cập nhật cv,sửa cv,nộp lại', 114, 1),
(N'Sau khi nộp hồ sơ thì theo dõi ở đâu?', N'Bạn vào mục My Applications để theo dõi trạng thái hồ sơ như: đã nhận, đang đánh giá, mời phỏng vấn, kết quả cuối cùng.', N'Ứng tuyển', N'my applications,trạng thái hồ sơ,theo dõi', 112, 1),
(N'Trạng thái hồ sơ nào là đã qua vòng CV?', N'Khi hồ sơ chuyển sang trạng thái mời phỏng vấn hoặc đang lên lịch phỏng vấn, nghĩa là bạn đã qua bước sàng lọc CV ban đầu.', N'Ứng tuyển', N'qua vòng cv,mời phỏng vấn,trạng thái', 110, 1),
(N'Quy trình tuyển dụng thường gồm những bước nào?', N'Thông thường gồm: nộp hồ sơ, sàng lọc CV, phỏng vấn chuyên môn/HR, đánh giá kết quả và gửi offer nếu phù hợp.', N'Quy trình', N'quy trình tuyển dụng,các bước,bước tuyển dụng', 108, 1),
(N'Phỏng vấn online hay onsite?', N'Tùy từng vị trí và giai đoạn, phỏng vấn có thể online hoặc onsite. Thông tin hình thức sẽ được ghi rõ trong thư mời phỏng vấn.', N'Phỏng vấn', N'phỏng vấn online,onsite,hình thức phỏng vấn', 106, 1),
(N'Tôi cần chuẩn bị gì trước buổi phỏng vấn?', N'Bạn nên xem lại JD, ôn kỹ kiến thức nền tảng theo vị trí, chuẩn bị dự án đã làm, kiểm tra kết nối mạng nếu phỏng vấn online và đến đúng giờ.', N'Phỏng vấn', N'chuẩn bị phỏng vấn,ôn tập,jd', 104, 1),
(N'Có bao nhiêu vòng phỏng vấn?', N'Số vòng phỏng vấn phụ thuộc vào vị trí. Thông thường có từ 1 đến 3 vòng gồm HR và chuyên môn.', N'Phỏng vấn', N'số vòng phỏng vấn,bao nhiêu vòng,hr,technical', 102, 1),
(N'Bao lâu có kết quả sau phỏng vấn?', N'Thời gian phản hồi thường từ 3 đến 10 ngày làm việc tùy vị trí và lịch hội đồng đánh giá.', N'Phỏng vấn', N'kết quả phỏng vấn,phản hồi sau phỏng vấn', 100, 1),
(N'Offer là gì?', N'Offer là thư mời nhận việc, trong đó có thông tin về vị trí, mức lương, chế độ, ngày bắt đầu và các điều khoản liên quan.', N'Offer', N'offer,thư mời nhận việc,mức lương', 98, 1),
(N'Khi nhận offer tôi cần phản hồi thế nào?', N'Bạn nên phản hồi xác nhận đồng ý hoặc đề xuất trao đổi thêm trong thời hạn ghi trên offer để bộ phận tuyển dụng kịp xử lý.', N'Offer', N'nhận offer,phản hồi offer,xác nhận', 96, 1),
(N'Tôi có thể thương lượng mức lương trong offer không?', N'Bạn có thể trao đổi lịch sự dựa trên năng lực, kinh nghiệm và mặt bằng thị trường. Hãy nêu rõ kỳ vọng và lý do cụ thể.', N'Offer', N'thương lượng lương,deal lương,mức lương', 94, 1),
(N'Intern/Fresher có vị trí nào phù hợp?', N'Bạn có thể tìm các vị trí có từ khóa intern, fresher hoặc junior trong trang Việc làm và đọc kỹ yêu cầu trước khi nộp.', N'Việc làm', N'intern,fresher,junior,sinh viên mới ra trường', 92, 1),
(N'Công ty hay yêu cầu kỹ năng gì cho backend?', N'Thường ưu tiên nền tảng lập trình tốt, kiến thức API, cơ sở dữ liệu, tư duy xử lý logic và khả năng đọc hiểu tài liệu kỹ thuật.', N'Kỹ năng', N'backend,kỹ năng backend,api,database', 90, 1),
(N'Công ty hay yêu cầu kỹ năng gì cho frontend?', N'Thường cần HTML/CSS/JavaScript vững, hiểu framework hiện tại, xử lý giao diện responsive và tư duy trải nghiệm người dùng.', N'Kỹ năng', N'frontend,kỹ năng frontend,responsive,ui ux', 88, 1),
(N'Tôi chưa có nhiều kinh nghiệm thì có nên ứng tuyển không?', N'Có. Bạn nên chọn vị trí phù hợp mức junior/intern, làm rõ dự án học tập cá nhân và thể hiện khả năng học nhanh trong CV.', N'Tư vấn', N'ít kinh nghiệm,junior,intern,có nên ứng tuyển', 86, 1),
(N'CV nên dài bao nhiêu trang?', N'CV nên ngắn gọn 1 đến 2 trang, tập trung kỹ năng liên quan vị trí ứng tuyển, kinh nghiệm chính và thành tích nổi bật.', N'CV', N'cv mấy trang,độ dài cv,viết cv', 84, 1),
(N'Nên ghi gì ở phần dự án trong CV IT?', N'Bạn nên ghi mục tiêu dự án, vai trò, công nghệ sử dụng, kết quả đạt được và đóng góp cụ thể của bạn.', N'CV', N'cv dự án,it project,kinh nghiệm dự án', 82, 1),
(N'Nếu bị từ chối thì có thể ứng tuyển lại không?', N'Bạn vẫn có thể ứng tuyển lại khi đã cải thiện kỹ năng và hồ sơ. Hãy chọn vị trí phù hợp hơn ở lần nộp tiếp theo.', N'Tư vấn', N'bị từ chối,ứng tuyển lại,re-apply', 80, 1),
(N'Chatbot có thể hỗ trợ những nội dung nào?', N'Chatbot hỗ trợ các câu hỏi về việc làm, kỹ năng IT, quy trình ứng tuyển, phỏng vấn và offer trong phạm vi tuyển dụng.', N'Chung', N'chatbot hỗ trợ gì,phạm vi hỗ trợ,tuyển dụng', 78, 1),
(N'Câu hỏi ngoài tuyển dụng có được trả lời không?', N'Chatbot ưu tiên nội dung tuyển dụng và kỹ năng liên quan. Các chủ đề ngoài phạm vi có thể được từ chối hoặc trả lời hạn chế.', N'Chung', N'ngoài tuyển dụng,phạm vi chatbot,từ chối', 76, 1);

INSERT INTO dbo.ChatbotFaqs (Question, Answer, Category, Keywords, Priority, IsActive)
SELECT s.Question, s.Answer, s.Category, s.Keywords, s.Priority, s.IsActive
FROM @FaqSeed s
WHERE NOT EXISTS
(
    SELECT 1
    FROM dbo.ChatbotFaqs f
    WHERE f.Question = s.Question
);

PRINT N'Da seed them FAQ cho chatbot (bo qua cac cau da ton tai theo Question).';
GO
