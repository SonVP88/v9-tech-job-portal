# Close Recruitment Gaps — Done

## Closed
1. **Job create KCN/Draft/Approve** — `DepartmentId`, `SaveAsDraft`, `SubmitForApproval`, `PUT submit-approval`, `PUT approve`; FE post-job + job-list
2. **QR/Google Form E2E** — auto portal apply link `POST .../recruitment-channel/auto`; Apps Script `Scripts/GoogleFormWebhook.gs`; webhook AI heuristic score → `ApplicationAiScores` + `AI_SCORED`
3. **AI screening** — `GeminiCvScreeningService` wired; persist Applications + scores; real campaigns (no fake GUID); forward → `HR_APPROVED` + notify managers; `POST rescreen/{jobId}`

## Run once
```sql
-- nếu chưa chạy
:r UTC_DATN/Scripts/AddJobGoogleFormColumns.sql
```
Set `Frontend:BaseUrl` in appsettings (default `http://localhost:4200`).
