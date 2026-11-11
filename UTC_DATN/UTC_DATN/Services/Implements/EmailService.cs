using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using UTC_DATN.Services.Interfaces;

namespace UTC_DATN.Services.Implements;

public class EmailService : IEmailService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<EmailService> _logger;

    public EmailService(IConfiguration configuration, ILogger<EmailService> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public async Task SendEmailAsync(string toEmail, string subject, string body)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(toEmail))
            {
                _logger.LogWarning("⚠️ ToEmail rỗng, bỏ qua gửi email.");
                return;
            }

            // Lấy cấu hình SMTP từ appsettings.json
            var host = _configuration["SmtpSettings:Host"] ?? "smtp.gmail.com";
            var port = int.Parse(_configuration["SmtpSettings:Port"] ?? "587");
            var enableSsl = bool.Parse(_configuration["SmtpSettings:EnableSsl"] ?? "true");
            var userName = _configuration["SmtpSettings:UserName"];
            var rawAppPassword = _configuration["SmtpSettings:AppPassword"];
            var fromName = _configuration["SmtpSettings:FromName"] ?? "Hệ Thống Tuyển Dụng ATS";
            var fromEmail = _configuration["SmtpSettings:FromEmail"];

            if (string.IsNullOrEmpty(userName) || string.IsNullOrEmpty(rawAppPassword))
            {
                _logger.LogWarning("⚠️ SMTP credentials chưa được cấu hình. Email sẽ không được gửi.");
                return;
            }

            // Chuẩn hóa mật khẩu ứng dụng Gmail (loại bỏ mọi khoảng trắng)
            var cleanAppPassword = rawAppPassword.Replace(" ", "").Trim();
            var senderEmail = !string.IsNullOrWhiteSpace(fromEmail) ? fromEmail : userName;

            _logger.LogInformation("📧 Đang gửi email đến: {ToEmail}, Subject: {Subject}", toEmail, subject);

            // Tạo MailMessage với mã hóa UTF-8
            using var mailMessage = new MailMessage
            {
                From = new MailAddress(senderEmail, fromName, Encoding.UTF8),
                Subject = subject,
                SubjectEncoding = Encoding.UTF8,
                Body = body,
                BodyEncoding = Encoding.UTF8,
                HeadersEncoding = Encoding.UTF8,
                IsBodyHtml = true
            };
            mailMessage.To.Add(toEmail.Trim());

            // Cấu hình SMTP Client với thông số chuẩn TLS Gmail
            using var smtpClient = new SmtpClient(host, port)
            {
                EnableSsl = enableSsl,
                UseDefaultCredentials = false,
                Credentials = new NetworkCredential(userName, cleanAppPassword),
                DeliveryMethod = SmtpDeliveryMethod.Network,
                Timeout = 25000 // 25s
            };

            // Gửi email
            await smtpClient.SendMailAsync(mailMessage);
            _logger.LogInformation("✅ Đã gửi email thành công đến: {ToEmail}", toEmail);
        }
        catch (SmtpException smtpEx)
        {
            _logger.LogError(smtpEx, "❌ Lỗi SMTP khi gửi email đến: {ToEmail}. StatusCode: {StatusCode}, Error: {Message}", 
                toEmail, smtpEx.StatusCode, smtpEx.Message);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Lỗi khi gửi email đến: {ToEmail}: {Message}", toEmail, ex.Message);
            throw;
        }
    }

    public async Task SendEmailWithCcAsync(string toEmail, List<string> ccEmails, string subject, string body)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(toEmail))
            {
                _logger.LogWarning("⚠️ ToEmail rỗng, bỏ qua gửi email.");
                return;
            }

            // Lấy cấu hình SMTP từ appsettings.json
            var host = _configuration["SmtpSettings:Host"] ?? "smtp.gmail.com";
            var port = int.Parse(_configuration["SmtpSettings:Port"] ?? "587");
            var enableSsl = bool.Parse(_configuration["SmtpSettings:EnableSsl"] ?? "true");
            var userName = _configuration["SmtpSettings:UserName"];
            var rawAppPassword = _configuration["SmtpSettings:AppPassword"];
            var fromName = _configuration["SmtpSettings:FromName"] ?? "Hệ Thống Tuyển Dụng ATS";
            var fromEmail = _configuration["SmtpSettings:FromEmail"];

            if (string.IsNullOrEmpty(userName) || string.IsNullOrEmpty(rawAppPassword))
            {
                _logger.LogWarning("⚠️ SMTP credentials chưa được cấu hình. Email sẽ không được gửi.");
                return;
            }

            // Chuẩn hóa mật khẩu ứng dụng Gmail (loại bỏ mọi khoảng trắng)
            var cleanAppPassword = rawAppPassword.Replace(" ", "").Trim();
            var senderEmail = !string.IsNullOrWhiteSpace(fromEmail) ? fromEmail : userName;

            _logger.LogInformation("📧 Đang gửi email đến: {ToEmail} với {CcCount} CC, Subject: {Subject}", 
                toEmail, ccEmails?.Count ?? 0, subject);

            // Tạo MailMessage với mã hóa UTF-8
            using var mailMessage = new MailMessage
            {
                From = new MailAddress(senderEmail, fromName, Encoding.UTF8),
                Subject = subject,
                SubjectEncoding = Encoding.UTF8,
                Body = body,
                BodyEncoding = Encoding.UTF8,
                HeadersEncoding = Encoding.UTF8,
                IsBodyHtml = true
            };
            mailMessage.To.Add(toEmail.Trim());

            // Thêm CC
            if (ccEmails != null && ccEmails.Any())
            {
                foreach (var ccEmail in ccEmails.Where(e => !string.IsNullOrWhiteSpace(e)))
                {
                    mailMessage.CC.Add(ccEmail.Trim());
                }
            }

            // Cấu hình SMTP Client với thông số chuẩn TLS Gmail
            using var smtpClient = new SmtpClient(host, port)
            {
                EnableSsl = enableSsl,
                UseDefaultCredentials = false,
                Credentials = new NetworkCredential(userName, cleanAppPassword),
                DeliveryMethod = SmtpDeliveryMethod.Network,
                Timeout = 25000 // 25s
            };

            // Gửi email
            await smtpClient.SendMailAsync(mailMessage);
            _logger.LogInformation("✅ Đã gửi email thành công đến: {ToEmail} (CC: {CcCount})", 
                toEmail, mailMessage.CC.Count);
        }
        catch (SmtpException smtpEx)
        {
            _logger.LogError(smtpEx, "❌ Lỗi SMTP khi gửi email đến: {ToEmail}. StatusCode: {StatusCode}, Error: {Message}", 
                toEmail, smtpEx.StatusCode, smtpEx.Message);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Lỗi khi gửi email đến: {ToEmail}: {Message}", toEmail, ex.Message);
            throw;
        }
    }
}
