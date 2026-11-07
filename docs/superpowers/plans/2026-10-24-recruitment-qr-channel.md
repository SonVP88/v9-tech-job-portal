# Recruitment QR Channel Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** HR gắn Google Form URL vào job, xem QR, tải PNG, in poster branding từ Admin company profile.

**Architecture:** Reuse `PUT/GET /api/jobs/{id}/google-form|qr-code`; enrich QR DTO with company/location/salary; Angular modal on job-list with `angularx-qrcode@20`.

**Tech Stack:** .NET 8 / EF Core, Angular 20 standalone, angularx-qrcode@20.0.0

## Tasks (executed in session)
1. Enrich `JobGoogleFormDto` + `GetQrCodeInfo` branding — done
2. SQL columns in `DB.sql` + `Scripts/AddJobGoogleFormColumns.sql` — done
3. JobService FE methods + RecruitmentChannelModal — done
4. Wire job-list button + install `angularx-qrcode@20.0.0` — in progress

## Verify
- Run SQL script on UTC_DATN
- HR: job-list → QR icon → paste forms.gle → Lưu → QR hiện → Tải PNG / In poster
