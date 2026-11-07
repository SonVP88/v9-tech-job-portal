using System;
using System.Text.RegularExpressions;

namespace UTC_DATN.Helpers;

/// <summary>
/// Helper ẩn thông tin định danh cá nhân (PII Masking / Blind Screening)
/// Tuân thủ Nghị định 13/2023/NĐ-CP về bảo vệ dữ liệu cá nhân và chống thiên vị khi gọi AI.
/// </summary>
public static class PiiMaskingHelper
{
    // Regex SĐT Việt Nam: +84, 84 hoặc 03, 05, 07, 08, 09 kèm 8 chữ số tiếp theo
    private static readonly Regex PhoneRegex = new(
        @"(?:\+?84|0)(?:3[2-9]|5[25689]|7[06-9]|8[1-9]|9[0-46-9])[0-9]{7}\b|((84|0[3|5|7|8|9])+([0-9]{8})\b)",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    // Regex Số CCCD/CMND 9 hoặc 12 chữ số
    private static readonly Regex CitizenIdRegex = new(
        @"\b\d{12}\b|\b\d{9}\b",
        RegexOptions.Compiled);

    // Regex Email
    private static readonly Regex EmailRegex = new(
        @"[a-zA-Z0-9_\-\.]+@[a-zA-Z0-9_\-\.]+\.[a-zA-Z]{2,7}",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    /// <summary>
    /// Quét và thay thế toàn bộ SĐT, CCCD/CMND và Email bằng nhãn ẩn danh
    /// </summary>
    /// <param name="rawText">Văn bản gốc</param>
    /// <returns>Văn bản đã được làm sạch PII</returns>
    public static string MaskSensitivePii(string? rawText)
    {
        if (string.IsNullOrWhiteSpace(rawText))
            return string.Empty;

        var text = rawText;

        // 1. Ẩn Email trước (tránh trường hợp số trong email bị regex CCCD/SĐT bắt nhầm)
        text = EmailRegex.Replace(text, "[REDACTED_EMAIL]");

        // 2. Ẩn SĐT Việt Nam
        text = PhoneRegex.Replace(text, "[REDACTED_PHONE]");

        // 3. Ẩn số CCCD / CMND 9 hoặc 12 số
        text = CitizenIdRegex.Replace(text, "[REDACTED_CITIZEN_ID]");

        return text;
    }
}

