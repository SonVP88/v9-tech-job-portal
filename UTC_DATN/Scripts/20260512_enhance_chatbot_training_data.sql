-- Script nâng cấp ChatbotFaqs với training data comprehensive
-- Mục đích: Train chatbot để hiểu sâu về jobs, skills, interview, career path

SET IDENTITY_INSERT [dbo].[ChatbotFaqs] OFF;
GO

-- Xóa FAQs cũ để reset (tuỳ chọn)
-- DELETE FROM [dbo].[ChatbotFaqs];
-- GO

-- ==================== CATEGORY: Application Process (Quy trình ứng tuyển) ====================
INSERT INTO [dbo].[ChatbotFaqs] 
  (Question, Answer, Category, Keywords, Priority, IsActive)
VALUES
-- Priority 100-110: Quy trình ứng tuyển cơ bản
(
  N'Quy trình ứng tuyển tại V9 TECH như thế nào?',
  N'Quy trình gồm 6 bước:
1. Nộp hồ sơ + CV: Bạn chọn công việc thích hợp và upload CV PDF
2. Kiểm tra hồ sơ (1-2 ngày): HR kiểm tra thông tin & CV
3. Phỏng vấn (30-60 phút): Nếu đạt, bạn sẽ được mời phỏng vấn (online/offline)
4. Đánh giá công nghệ: Tuỳ vị trí, có thể có bài test kỹ thuật
5. Offer: Nếu pass, HR sẽ gửi offer letter
6. Ký kết & Onboard: Hoàn tất thủ tục & bắt đầu làm việc
Tổng thời gian: khoảng 2-4 tuần',
  N'Application',
  N'quy trình,ứng tuyển,bước,interview,phỏng vấn',
  105,
  1
),
(
  N'Tôi phải chuẩn bị gì khi ứng tuyển?',
  N'Bạn cần chuẩn bị:
• CV (PDF format) - được update gần đây, kinh nghiệm rõ ràng
• Cover letter tuỳ chọn - giải thích tại sao bạn muốn vị trí này
• Portfolio (nếu có) - link GitHub, website hoặc demo project
• Thiết bị phỏng vấn - nếu online: webcam, mic, Internet ổn định
• Giấy tờ tùy thân - sẵn sàng cho giai đoạn offer
Lưu ý: Đọc kỹ job description để match kỹ năng của bạn với yêu cầu',
  N'Application',
  N'chuẩn bị,cv,cover letter,portfolio',
  104,
  1
),
(
  N'CV của tôi không được pass, sao vậy?',
  N'Một số lý do phổ biến:
1. CV không match job description - kỹ năng không liên quan
2. Kinh nghiệm chưa đủ - yêu cầu 2 năm nhưng bạn chỉ có 6 tháng
3. CV quá dài hoặc mập mờ - nên giới hạn 1-2 trang, clear & concrete
4. Không có examples cụ thể - thay vì "Tôi giỏi làm project", nói "Xây dựng RESTful API với 10k+ requests/day"
5. Formatting xấu - file bị lỗi khi mở hoặc không chuyên nghiệp
Gợi ý: Tải CV lại với mục tiêu specific cho từng vị trí',
  N'Application',
  N'cv fail,cv không pass,cv chưa được,cải thiện cv',
  102,
  1
),
(
  N'Tôi có thể nộp hồ sơ cho 2 vị trí cùng lúc không?',
  N'Có, bạn có thể ứng tuyển cho nhiều vị trí cùng lúc. Tuy nhiên:
• Hãy đảm bảo CV phù hợp với từng vị trí
• Nếu bạn pass 2 vị trí cùng lúc, bạn sẽ được HR tìm hiểu nhu cầu của bạn
• Cân nhắc kỹ về mục tiêu career của bạn để tránh phân tán
Lời khuyên: Focus vào 1-2 vị trí phù hợp nhất thay vì ứng tuyển tất cả',
  N'Application',
  N'nhiều vị trí,multiple,song song',
  90,
  1
),

-- ==================== CATEGORY: Job Roles & Career Path ====================
(
  N'V9 TECH đang tuyển những vị trí nào?',
  N'V9 TECH chuyên tuyển các vị trí IT như:
• Backend Developer (C#, .NET, Node.js, Java)
• Frontend Developer (Angular, React, Vue.js)
• Full-stack Developer
• Mobile Developer (iOS, Android, Flutter)
• Data Engineer / Data Scientist
• QA / Test Engineer
• DevOps / Cloud Engineer
• UI/UX Designer
• Business Analyst
Để xem chi tiết từng vị trí, hãy vào danh sách Jobs trên website hoặc hỏi tôi về vị trí cụ thể',
  N'JobRoles',
  N'tuyển dụng,vị trí,job openings,developer,engineer',
  110,
  1
),
(
  N'Vị trí Junior Developer yêu cầu những gì?',
  N'Yêu cầu chung cho Junior Developer:
Bắt buộc:
• Kinh nghiệm 0-1 năm hoặc fresh graduate
• Thành thạo 1 ngôn ngữ lập trình (Java, C#, JavaScript, Python...)
• Hiểu cơ bản về OOP, Data Structures, Algorithms
• SQL cơ bản / Database
Mong muốn:
• Có project hoàn thành (personal project, internship, capstone)
• Biết Git / version control
• Thực tập tại công ty IT
• English cơ bản (reading technical docs)
Lợi ích khi vào V9 TECH: Được mentor, training, grow nhanh',
  N'JobRoles',
  N'junior,fresh,graduate,entry-level,yêu cầu',
  105,
  1
),
(
  N'Vị trí Senior Developer yêu cầu những gì?',
  N'Yêu cầu cho Senior Developer (3+ năm):
Technical:
• 3-5+ năm kinh nghiệm lập trình production
• Thành thạo architecture, design patterns (MVC, SOLID, Clean Code)
• Có kinh nghiệm lead team, code review, mentor junior
• Database optimization, SQL tuning, caching
• Có kinh nghiệm deploy, CI/CD, DevOps cơ bản
Soft skills:
• Communication tốt, có thể giải thích kỹ thuật cho non-technical
• Problem-solving, tư duy hệ thống
• Tính chủ động, ownership
Lợi ích: Mức lương cao, flexible working, leadership opportunity',
  N'JobRoles',
  N'senior,5 năm,kinh nghiệm,lead,architect',
  104,
  1
),
(
  N'Backend vs Frontend - nên chọn cái nào?',
  N'Phụ thuộc vào sở thích & điểm mạnh của bạn:

BACKEND Developer (phù hợp nếu):
• Thích logic, algorithm, database
• Quan tâm đến performance, scalability
• Muốn làm việc với server, API, infrastructure
• Yêu thích "hidden" complexity

FRONTEND Developer (phù hợp nếu):
• Thích design, UI/UX, visual
• Muốn thấy kết quả ngay lập tức
• Quan tâm đến user experience
• Thích interactive, animation, responsive design

FULLSTACK (tốt nhất nếu):
• Muốn hiểu toàn bộ flow, end-to-end
• Linh hoạt hơn trong career
• Tại V9 TECH, fullstack dev có cơ hội đa dạng hơn',
  N'JobRoles',
  N'backend,frontend,fullstack,chọn,lựa chọn',
  100,
  1
),
(
  N'Làm sao để chuyển từ Frontend sang Backend?',
  N'Nếu bạn muốn chuyển hướng:

Bước 1: Học backend fundamentals (1-2 tháng)
• Chọn 1 ngôn ngữ backend (Node.js, C#, Java, Python)
• Hiểu RESTful API, HTTP protocols
• Học database (SQL, NoSQL)

Bước 2: Làm side projects (1-2 tháng)
• Xây dựng API từ đơn giản đến phức tạp
• Combine với frontend knowledge của bạn
• Deploy lên cloud (AWS, Azure, Heroku)

Bước 3: Ứng tuyển vị trí junior backend hoặc fullstack
• Highlight frontend knowledge (advantage: hiểu user, API consumption)
• Show projects, GitHub
• Sẵn sàng học thêm trong công ty

Tại V9 TECH: Chúng tôi welcome career changers & provide training!',
  N'JobRoles',
  N'chuyển ngành,career change,backend,frontend,switch',
  98,
  1
),

-- ==================== CATEGORY: Skills & Requirements ====================
(
  N'Công ty cần kỹ năng nào hiện tại?',
  N'TOP Skills được V9 TECH tìm kiếm:

Backend:
• C# / .NET Framework, .NET Core
• Node.js + Express
• SQL Server, MySQL, PostgreSQL
• RESTful API Design
• Microservices, Docker, Kubernetes

Frontend:
• Angular (version 12+)
• React, Vue.js
• TypeScript, ES6+
• HTML/CSS/SCSS
• State Management (NgRx, Redux)

DevOps/Cloud:
• AWS, Azure, Google Cloud
• Docker, Kubernetes
• CI/CD pipelines, Jenkins, GitHub Actions
• Infrastructure as Code (Terraform, CloudFormation)

Soft:
• Git, agile, communication
• English communication (technical)
Hãy focus vào 1-2 kỹ năng chính & đào sâu thay vì nông cạn toàn bộ',
  N'Skills',
  N'kỹ năng,skill,công nghệ,technology,trending',
  115,
  1
),
(
  N'Tôi cần biết .NET hoàn toàn trước khi ứng tuyển Backend không?',
  N'Không nhất thiết! Điều quan trọng:

Bắt buộc:
• Hiểu OOP, design patterns
• Có kinh nghiệm lập trình (thậm chí Java, Python là được)
• Hiểu API, database cơ bản

Mong muốn:
• 1-2 tháng học .NET trước khi ứng tuyển
• Làm được 1-2 project .NET demo
• Có GitHub repo để show

Tại V9 TECH:
• Công ty sẵn sàng train .NET cho candidates
• Mentor sẽ giúp bạn ramp-up nhanh
• Focus vào fundamental & learning ability > already knowing .NET

Lời khuyên: Học cơ bản .NET, làm 1 project, thì ứng tuyển đi!',
  N'Skills',
  N'.net,c#,học,training,không biết',
  108,
  1
),
(
  N'Tôi nên học cái gì tiếp theo?',
  N'Tùy vào vị trí hiện tại của bạn:

Nếu bạn là FRONTEND Developer:
• TypeScript (mandatory now)
• Advanced Angular / React patterns
• Performance optimization (lazy loading, bundling)
• Testing (Jasmine, Jest)
• Next.js hoặc Nuxt (SSR, full-stack)

Nếu bạn là BACKEND Developer:
• Caching (Redis)
• Message Queue (RabbitMQ, Kafka)
• Database optimization
• Security (OAuth, JWT, encryption)
• Containerization (Docker, K8s)

Nếu bạn là JUNIOR:
• Focus vào fundamentals first (OOP, Data Structures, Algorithms)
• Sau đó chuyên sâu vào backend/frontend
• English communication

Gợi ý: Xem job description của vị trí mục tiêu và học những skill được list',
  N'Skills',
  N'học thêm,next step,roadmap,development',
  106,
  1
),

-- ==================== CATEGORY: Interview Tips ====================
(
  N'Phỏng vấn tại V9 TECH bao lâu? Diễn ra như thế nào?',
  N'Phỏng vấn tại V9 TECH thường gồm:

ROUND 1: Technical Interview (30-45 phút)
• Kiểm tra kỹ năng technical cơ bản
• Coding questions, data structures, algorithms
• Discuss project từ CV
Format: Online (Google Meet/Zoom) hoặc offline

ROUND 2: Technical Deep-dive (45-60 phút)  
• Thiết kế hệ thống, architecture questions
• Real-world scenario
• Code review discussion
Format: 1-on-1 với senior engineer

ROUND 3: HR Round (20-30 phút)
• Discuss salary, benefits, career path
• Company culture fit
Format: HR manager + Tech lead

Tổng thời gian: 2-4 tuần từ lúc ứng tuyển đến offer
Lời khuyên: Chuẩn bị kỹ lưỡng, hỏi câu hỏi, show passion!',
  N'Interview',
  N'phỏng vấn,interview,round,technical',
  112,
  1
),
(
  N'Làm sao để pass technical interview?',
  N'Tips để pass technical interview:

TRƯỚC:
• Review data structures & algorithms (priority: array, linked list, tree, hash table)
• Practice coding trên LeetCode/HackerRank (medium level)
• Review projects của bạn - chuẩn bị nói sâu về architecture
• Setup máy tính, test audio/video nếu interview online

TRONG:
• Hiểu rõ câu hỏi trước khi code
• Think out loud - giải thích logic khi bạn code
• Hỏi clarifying questions nếu không rõ
• Viết clean code - readable, tested, optimized
• Discuss trade-offs (speed vs memory, simple vs scalable)

SAU:
• Hỏi interviewer về feedback
• Hỏi về team, project, growth opportunity
• Send thank you email

Thái độ quan trọng hơn kỹ năng - show your learning ability!',
  N'Interview',
  N'pass,tips,chuẩn bị,technical,succeed',
  111,
  1
),
(
  N'Interviewer hỏi "Hãy tự giới thiệu về bạn" - trả lời thế nào?',
  N'Câu này là câu mở đầu, nên trả lời ngắn gọn & convincing (~2 phút):

Cấu trúc:
1. Background (học tại đâu, experience bao nhiêu)
2. Key achievements (project nào, impact thế nào)
3. Why V9 TECH (tại sao bạn muốn vào)
4. What you are looking for (career growth, team, technology)

VD:
"Tôi là developer với 2 năm kinh nghiệm backend C#. Tại [công ty trước], tôi xây dựng API xử lý 10k+ requests/day, optimize database đạt 5x faster. Tôi passion với clean code, microservices, và muốn join team technical mạnh để học hỏi. V9 TECH là công ty tôi admire vì culture innovation & product-focused."

Tips:
• Chuẩn bị story của riêng bạn
• Practice trước mirror hoặc bạn
• Show enthusiasm, genuine interest',
  N'Interview',
  N'tự giới thiệu,introduce yourself,opening',
  109,
  1
),
(
  N'Interviewer hỏi câu khó đó làm sao?',
  N'Khi gặp câu khó trong technical interview:

KHÔNG nên:
✗ Ngồi câm lặng
✗ Nói "Tôi không biết"
✗ Panic, nói lộn xộn

NÊN làm:
✓ Gọi thêm thời gian: "Cho tôi 1 phút để think about this"
✓ Break down: "Vậy để giải quyết... tôi sẽ approach như này..."
✓ Hỏi: "Có hint được không?" hoặc "Can I use external library?"
✓ Bắt đầu từ brute force, sau đó optimize
✓ Explain your thought process
✓ Nói "Tôi chưa gặp case này, nhưng approach sẽ là..."

Interviewer không expect bạn biết tất cả - họ muốn thấy:
• Problem-solving approach
• Communication
• Ability to learn
• Humble + willing to improve

Mẹo: Dễ hơn là show reasoning good hơn là có perfect answer!',
  N'Interview',
  N'khó,not know,stuck,approach',
  107,
  1
),
(
  N'Tôi có thể hỏi gì khi kết thúc phỏng vấn?',
  N'Hỏi những câu này khi interviewer nói "Có câu hỏi nào cho chúng tôi không?":

GOOD QUESTIONS:
1. "Team này đang làm project gì? Có challenge nào không?"
   → Show genuine interest
2. "Cấu trúc team là sao? Tôi sẽ report cho ai?"
   → Hiểu role & structure
3. "Có career growth opportunity? Training budget?"
   → Long-term mindset
4. "Tech stack team dùng? Có dùng latest technology không?"
   → Technical interest
5. "Có mentor program không?"
   → Learning mindset

AVOID:
✗ "Lương bao nhiêu?" (HR sẽ mention sau)
✗ "Phải làm gì vậy?" (show you didn't read job desc)
✗ "Cơ quan ở đâu?" (basic info từ website)

Pro tip: Prepare 2-3 questions, ask genuine ones, listen carefully to answers!',
  N'Interview',
  N'hỏi câu,questions,kết thúc,closing',
  106,
  1
),

-- ==================== CATEGORY: Salary & Offer ====================
(
  N'Mức lương tại V9 TECH bao nhiêu?',
  N'Mức lương phụ thuộc vào:
• Vị trí (Junior/Middle/Senior)
• Kinh nghiệm
• Kỹ năng & skill set
• Performance interview

KỲ VỌNG THƯỜNG:
• Junior (0-1 năm): 8-12M VNĐ/tháng
• Middle (1-3 năm): 12-20M VNĐ/tháng
• Senior (3+ năm): 20-35M+ VNĐ/tháng
• (Giá này dành cho IT roles tại TP.HCM, có thể khác khu vực)

BENEFITS (ngoài lương):
• Health insurance (company + family)
• Annual bonus (13th month)
• Performance bonus
• Paid leave (15+ days/year)
• Training budget
• Team building activities
• Flexible working (WFH allowed)

Ghi chú: Lương sẽ thảo luận trong offer stage. Hãy research mức phổ biến trước khi negotiate!',
  N'Offer',
  N'lương,salary,mức,bao nhiêu,offer',
  115,
  1
),
(
  N'Tôi được offer từ V9 TECH, nên nhận không?',
  N'Xem xét những điểm:

PROS của V9 TECH:
✓ Strong tech culture, passionate team
✓ Good training & mentorship
✓ Work on interesting projects
✓ Career growth (junior → senior)
✓ Competitive salary
✓ Good work-life balance

CONS cần xem xét:
• Có phù hợp với career goal của bạn không?
• Salary có meet expectation không?
• Team size, project scope có match không?
• Commute/location có ok không?

DECISION FRAMEWORK:
1. Lương + benefits có acceptable không? (yes → continue)
2. Project & technology có align không? (yes → continue)
3. Team & mentor có good không? (yes → consider accepting!)

Lời khuyên: Nếu offer là từ company tốt, team ổn định, project interesting, thì chỉ cần lương acceptable là nhận. Có thể negotiate lương, vacation days, signing bonus. Nhưng sau khi sign, commit full.',
  N'Offer',
  N'nhận,accept,offer,quyết định',
  110,
  1
),

-- ==================== CATEGORY: General Q&A ====================
(
  N'V9 TECH là công ty gì? Chuyên về lĩnh vực nào?',
  N'V9 TECH là một công ty phần mềm Việt Nam chuyên về:

SERVICES:
• Outsourcing software development (build custom solutions)
• Nearshore development cho clients Japan, USA, EU
• Dedicated team model (long-term partnership)

TECHNOLOGY STACK:
• Backend: C# .NET, Node.js, Java
• Frontend: Angular, React
• Mobile: Flutter, React Native
• Cloud: AWS, Azure
• Databases: SQL Server, MongoDB, PostgreSQL

CULTURE:
• Tech-first, innovation-focused
• Agile team
• Strong mentorship
• Work-life balance
• Remote-friendly (hybrid model)

MARKET:
• ~200+ employees
• Offices: Vietnam (TP.HCM, Hanoi), Japan, USA
• Growing rapidly

Nếu bạn passion về technology & muốn làm việc với international clients trên sản phẩm real-world, V9 TECH là good fit!',
  N'Company',
  N'công ty,về,profile,lịch sử,mission',
  120,
  1
),
(
  N'V9 TECH có flex working time không?',
  N'Có! V9 TECH hỗ trợ flexible working:

Work Model:
• Core hours: 9:30 AM - 5:30 PM (bắt buộc có mặt)
• Flexible: Arrive 9 AM - 10 AM, leave 5 PM - 6 PM
• WFH (Work from Home): Allowed 1-2 days/week (tuỳ project)
• Full remote: Có thể negotiate với manager

Lợi ích:
• No strict punch-in
• Flexible lunch break
• Meeting schedule được arrange để không quá tối
• Understand life happens

Lưu ý:
• Phải finish work on time & attend meetings
• Productivity > presence
• Depend on project & team setup

Tất cả phụ thuộc vào team lead & project requirement. Khi join, bạn sẽ discuss với manager để setup best schedule!',
  N'Company',
  N'flex,working,time,wfh,remote',
  108,
  1
),
(
  N'Tôi là sinh viên, có thể intern tại V9 TECH không?',
  N'CÓ! V9 TECH tuyển internship. Quy trình:

INTERNSHIP PROGRAM:
Thời gian: 3-6 tháng (full-time)
Yêu cầu:
• Đang học semester cuối hoặc gap year
• Có basic knowledge về programming
• Passionate & willing to learn

Lợi ích:
• Real-world project experience
• Mentorship từ senior developer
• Stipend/salary (~5-8M VNĐ)
• High chance convert to full-time sau khi tốt nghiệp

Quy trình:
1. Apply (CV + transcript)
2. Technical interview (basic algorithm, problem-solving)
3. Meet team
4. Start internship

Gợi ý để pass:
• Có side project hoặc open-source contribution
• Show passion for learning
• Good communication
• Willing to work in team

Hãy ứng tuyển ngay nếu bạn quan tâm!',
  N'Company',
  N'internship,sinh viên,student,gap,junior',
  112,
  1
),
(
  N'V9 TECH có training program không?',
  N'Có! V9 TECH tầm nhìn trên development của team member:

ONBOARDING TRAINING:
• 1 tuần induction (company culture, systems, tools)
• 2-4 tuần ramp-up project (pair programming, learning codebase)
• Assigned mentor cho first 3 tháng

INTERNAL TRAINING:
• Tech talks (weekly)
• Internal workshop (design patterns, new framework, etc.)
• Sertification support (AWS, Azure, etc.)

EXTERNAL:
• Training budget per year (~2-3M VNĐ)
• Courses: Udemy, LinkedIn Learning, O'Reilly
• Conference attendance

CAREER PATH:
• Clear progression: Junior → Middle → Senior → Lead
• Performance review bi-annual
• Salary increase yearly (based on performance)

GHI CHÚ:
• Training là investment từ company
• Bạn cần show commitment & apply learning vào work
• Senior/Lead có responsibility mentor junior

Tại V9 TECH, bạn sẽ grow significantly nếu willing to learn!',
  N'Company',
  N'training,development,course,learning',
  110,
  1
),

-- ==================== CATEGORY: FAQ - Specific Job Variations ====================
(
  N'Sự khác nhau giữa Angular Developer vs React Developer là gì?',
  N'Dù cả hai đều là Frontend, nhưng khác nhau:

ANGULAR:
• Full framework (batteries included)
• TypeScript mandatory
• Opinionated (structure rõ ràng)
• Steeper learning curve
• Large apps (enterprise)
• Việt Nam: Nhiều công ty dùng (lợi thế)

REACT:
• Library (flexible hơn)
• JavaScript + TypeScript optional
• Less opinionated (freedom hơn)
• Gentle learning curve
• Facebook support, huge ecosystem
• Globally: Trending hơn

GIỮ LỮA:
• V9 TECH dùng Angular, React cả hai
• Nếu mới: React easier → Angular sau
• Nếu muốn job nhiều: React better
• Long-term: Biết cả hai là best

GHI CHÚ: Fundamentals (HTML/CSS, JavaScript, OOP) quan trọng hơn framework choice. Học 1 cái thật tốt, sau đó framework khác dễ pick up!',
  N'JobRoles',
  N'angular,react,frontend,difference,comparison',
  105,
  1
),
(
  N'Làm sao để tôi biết CV match job description không?',
  N'Tips để self-assess:

Step 1: Đọc kỹ job description
• List ra technical requirements (bắt buộc + mong muốn)
• Note cấp độ seniority mong muốn

Step 2: Compare với CV của bạn
• Check: Bạn có kinh nghiệm cao hơn/bằng/thấp hơn yêu cầu?
• Match skills: % kỹ năng bạn có / yêu cầu
  - 70%+: Tuyệt vời, ứng tuyển
  - 50-70%: Ok, worth trying (specially mong muốn skills)
  - <50%: Risky, cân nhắc hoặc prep thêm trước

Step 3: Assess potential
• Bạn có learning ability không?
• Timeline: Bạn có thể learn gap skills không? (1-3 tháng?)
• Cultural fit: Environment, team, project có match không?

Rule of thumb:
• Core skills: 70%+ match
• Nice-to-have: 50%+ ok
• Attitude & learning: Quan trọng nhất

PRO TIP: Nếu bạn 50-70% match + show passion + willing learn = still good candidate!',
  N'Application',
  N'match,cv,job description,assessment',
  100,
  1
),
(
  N'Tôi rơi vào case nào khi apply: overqualified vs underqualified?',
  N'Phân loại để hiểu position của bạn:

UNDERQUALIFIED (kỹ năng < yêu cầu):
• Ví dụ: Job request 3+ năm, bạn 1 năm
• Risk: Bạn có thể overwhelm, not productive ngay
• Strategy: Highlight learning ability, show projects, sẵn sàng train
• Chance: 20-30% (company take risk nếu potential tốt)

WELL-MATCHED (kỹ năng ≈ yêu cầu):
• Ví dụ: Match 70-80%
• Risk: None
• Strategy: Standard application
• Chance: 80%+ (company prefer này)

OVERQUALIFIED (kỹ năng > yêu cầu):
• Ví dụ: Job request Junior, bạn Senior
• Risk: Bạn chán job, leave sớm
• Strategy: Explain why you want this role, long-term goal
• Chance: 50% (company concern: retention)

GHI CHÚ SAU:
• Underqualified: Show growth mindset + concrete plan learn
• Overqualified: Explain genuine interest + stability
• Honest is best policy!',
  N'Application',
  N'underqualified,overqualified,position,level',
  98,
  1
);

GO

-- Index optimization
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_ChatbotFaqs_Category' AND object_id = OBJECT_ID('dbo.ChatbotFaqs'))
BEGIN
    CREATE INDEX IX_ChatbotFaqs_Category ON dbo.ChatbotFaqs (Category);
END;
GO

-- Verify data inserted
SELECT COUNT(*) as TotalFaqsCount FROM dbo.ChatbotFaqs;
SELECT Category, COUNT(*) as Count FROM dbo.ChatbotFaqs WHERE IsActive = 1 GROUP BY Category ORDER BY COUNT(*) DESC;
GO
