# Design: Kênh Tuyển Dụng & Mã QR (Job Campaign)

**Date:** 2026-10-24  
**Status:** Approved (user: làm đi)

## Goal
HR gắn Google Form URL vào từng chiến dịch tuyển dụng, xem QR động, tải PNG QR và in poster (logo công ty + JD tóm tắt) để dán cổng nhà máy.

## Decisions
- UI: modal từ `job-list` (option A)
- Poster: đầy đủ branding (option B)
- Branding: `Users.CompanyName` / `CompanyLogoUrl` của Admin (option B)
- API: tái sử dụng `/api/jobs/{id}/google-form` và `/api/jobs/{id}/qr-code` (không alias `job-postings`)
- QR render phía client (`angularx-qrcode`); `QrCodeData` = Google Form URL

## Backend
- Entity (partial): `GoogleFormUrl`, `RecruitmentPosterTitle`, `GoogleFormUpdatedAt`
- DTOs: `UpdateGoogleFormRequest`, `JobGoogleFormDto` (+ company/location/salary cho poster)
- SQL: cột trên `Jobs` + ALTER idempotent trong `DB.sql`

## Frontend
- `RecruitmentChannelModalComponent` standalone
- Nút QR trên mỗi dòng job-list
- Lưu cấu hình, preview QR, tải PNG, in poster
