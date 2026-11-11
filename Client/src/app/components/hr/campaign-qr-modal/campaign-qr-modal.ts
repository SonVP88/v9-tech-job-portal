import { CommonModule } from '@angular/common';
import { Component, EventEmitter, Input, Output, OnChanges, SimpleChanges, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { CampaignQrDto, JobService } from '../../../services/job.service';
import { ApplicationService } from '../../../services/application.service';
import { ToastService } from '../../../services/toast.service';

@Component({
  selector: 'app-campaign-qr-modal',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './campaign-qr-modal.html'
})
export class CampaignQrModalComponent implements OnChanges {
  @Input() campaign: CampaignQrDto | null = null;
  @Input() regenerating = false;
  @Output() closed = new EventEmitter<void>();
  @Output() regenerate = new EventEmitter<void>();
  @Output() synced = new EventEmitter<void>();

  private jobService = inject(JobService);
  private applicationService = inject(ApplicationService);
  private toast = inject(ToastService);

  activeTab: 'qr' | 'sheet-sync' | 'webhook' = 'qr';

  // Copy states
  copied = false;
  copiedWebhook = false;
  copiedSecret = false;
  copiedScript = false;

  // Custom Form Link
  customFormUrl = '';
  isSavingCustomForm = false;

  // Ngrok / Public URLs
  ngrokApiUrl = '';
  ngrokClientUrl = '';
  isApplyingNgrok = false;

  // 1-Click Google Sheet Sync
  sheetUrl = '';
  isSyncingSheet = false;
  syncResult: any = null;
  isLoadingColumns = false;
  showColumnConfig = false;
  sheetHeaders: string[] = [];
  sheetTotalRows = 0;
  sheetSampleRow: Record<string, string> = {};
  columnMapping = {
    fullNameColumn: '',
    phoneColumn: '',
    emailColumn: '',
    experienceColumn: '',
    hometownColumn: '',
    yearOfBirthColumn: ''
  };

  // Webhook Test Simulation
  isTestingWebhook = false;
  testResult: any = null;
  testFullName = 'Nguyễn Văn Test';
  testPhone = '0988' + Math.floor(100000 + Math.random() * 900000);

  ngOnChanges(changes: SimpleChanges): void {
    if (changes['campaign'] && this.campaign) {
      this.ngrokApiUrl = this.campaign.publicApiBaseUrl || '';
      this.ngrokClientUrl = this.campaign.publicClientBaseUrl || '';
      const url = (this.campaign.formUrl || '').trim();
      this.customFormUrl = url && !url.includes('/apply/') ? url : '';
    }
  }

  get isGoogle(): boolean {
    return (this.campaign?.source || '').toUpperCase() === 'GOOGLE_FORM';
  }

  get hasValidQr(): boolean {
    const url = (this.campaign?.formUrl || '').trim();
    return !!url && !url.includes('/apply/') && !!this.campaign?.qrCodeDataUrl;
  }

  copyLink(): void {
    const url = this.campaign?.formUrl;
    if (!url) return;
    navigator.clipboard.writeText(url).then(() => {
      this.copied = true;
      setTimeout(() => (this.copied = false), 1600);
      this.toast.success('Đã copy', 'Đã copy link form ứng tuyển.');
    });
  }

  copyWebhookUrl(): void {
    const url = this.campaign?.webhookUrl;
    if (!url) return;
    navigator.clipboard.writeText(url).then(() => {
      this.copiedWebhook = true;
      setTimeout(() => (this.copiedWebhook = false), 1600);
      this.toast.success('Đã copy', 'Đã copy Webhook URL.');
    });
  }

  copyWebhookSecret(): void {
    const secret = this.campaign?.webhookSecret;
    if (!secret) return;
    navigator.clipboard.writeText(secret).then(() => {
      this.copiedSecret = true;
      setTimeout(() => (this.copiedSecret = false), 1600);
      this.toast.success('Đã copy', 'Đã copy Secret Token.');
    });
  }

  downloadQr(): void {
    if (!this.campaign?.qrCodeDataUrl) return;
    const a = document.createElement('a');
    a.href = this.campaign.qrCodeDataUrl;
    a.download = `qr-chien-dich-${this.campaign.jobId}.png`;
    a.click();
  }

  copyScript(): void {
    if (!this.campaign?.appsScript) return;
    navigator.clipboard.writeText(this.campaign.appsScript).then(() => {
      this.copiedScript = true;
      setTimeout(() => (this.copiedScript = false), 1600);
      this.toast.success('Đã copy', 'Đã copy mã Google Apps Script.');
    });
  }

  // 1. Cập nhật link Form tùy chọn (Dán Google Form có sẵn của HR)
  saveCustomFormUrl(): void {
    if (!this.campaign?.jobId) return;

    this.isSavingCustomForm = true;
    this.jobService.updateCustomFormUrl(this.campaign.jobId, this.customFormUrl.trim()).subscribe({
      next: (updated) => {
        this.campaign = updated;
        this.isSavingCustomForm = false;
        this.toast.success('Thành công', 'Đã cập nhật link form và sinh lại mã QR!');
      },
      error: (err) => {
        this.isSavingCustomForm = false;
        this.toast.error('Lỗi', err.error?.message || 'Không thể cập nhật link form.');
      }
    });
  }

  resetToInternalForm(): void {
    if (!this.campaign?.jobId) return;
    this.customFormUrl = '';
    this.isSavingCustomForm = true;
    this.jobService.updateCustomFormUrl(this.campaign.jobId, '').subscribe({
      next: (updated) => {
        this.campaign = updated;
        this.isSavingCustomForm = false;
        this.toast.success('Thành công', 'Đã chuyển về sử dụng Form ứng tuyển nội bộ ATS.');
      },
      error: (err) => {
        this.isSavingCustomForm = false;
        this.toast.error('Lỗi', err.error?.message || 'Không thể chuyển form.');
      }
    });
  }

  // 2. Cấu hình ngrok
  applyNgrokConfig(): void {
    if (!this.campaign?.jobId) return;
    if (!this.ngrokApiUrl.trim()) {
      this.toast.warning('Thiếu URL', 'Vui lòng nhập Public URL của Backend API (ví dụ: https://xxxx.ngrok-free.app).');
      return;
    }

    this.isApplyingNgrok = true;
    const cleanApi = this.ngrokApiUrl.trim().replace(/\/$/, '');
    const cleanClient = this.ngrokClientUrl.trim() ? this.ngrokClientUrl.trim().replace(/\/$/, '') : undefined;

    this.jobService.ensureCampaignQr(this.campaign.jobId, false, cleanApi, cleanClient).subscribe({
      next: (updated) => {
        this.campaign = updated;
        this.ngrokApiUrl = updated.publicApiBaseUrl || cleanApi;
        this.ngrokClientUrl = updated.publicClientBaseUrl || (cleanClient ?? '');
        this.isApplyingNgrok = false;
        this.toast.success('Thành công', 'Đã cập nhật ngrok URL và tạo lại Apps Script + Webhook!');
      },
      error: (err) => {
        this.isApplyingNgrok = false;
        this.toast.error('Lỗi', err.error?.message || 'Không thể cập nhật cấu hình ngrok.');
      }
    });
  }

  loadSheetColumns(): void {
    if (!this.sheetUrl.trim()) {
      this.toast.warning('Thiếu đường link', 'Vui lòng dán link file Google Sheet trước khi đọc cột.');
      return;
    }
    if (this.sheetUrl.includes('/forms/') || this.sheetUrl.includes('forms.google.com')) {
      this.toast.warning(
        'Nhầm link Google Form!',
        'Bạn đang dán link của Google Form (Biểu mẫu). Mục này cần link file Google Sheet (Trang tính). Hãy vào Google Form -> bấm tab "Câu trả lời" -> bấm "Liên kết với Trang tính" để mở bảng tính.'
      );
      return;
    }

    this.isLoadingColumns = true;
    this.applicationService.previewGoogleSheets(this.sheetUrl.trim()).subscribe({
      next: (res) => {
        this.isLoadingColumns = false;
        const data = res.data;
        this.sheetHeaders = data?.headers || [];
        this.sheetTotalRows = data?.totalRows || 0;
        this.sheetSampleRow = data?.sampleRow || {};
        const suggested = data?.suggestedMapping || {};
        this.columnMapping = {
          fullNameColumn: suggested.fullNameColumn || '',
          phoneColumn: suggested.phoneColumn || '',
          emailColumn: suggested.emailColumn || '',
          experienceColumn: suggested.experienceColumn || '',
          hometownColumn: suggested.hometownColumn || '',
          yearOfBirthColumn: suggested.yearOfBirthColumn || ''
        };
        this.showColumnConfig = true;
        this.toast.success('Đã đọc cấu trúc Sheet', `Tìm thấy ${this.sheetHeaders.length} cột và ${this.sheetTotalRows} dòng dữ liệu.`);
      },
      error: (err) => {
        this.isLoadingColumns = false;
        this.toast.error('Lỗi đọc Google Sheet', err.error?.message || 'Không thể đọc danh sách cột từ Google Sheet.');
      }
    });
  }

  // 3. Đồng bộ 1-Click từ Google Sheet
  syncGoogleSheet(): void {
    if (!this.campaign?.jobId) return;
    if (!this.sheetUrl.trim()) {
      this.toast.warning('Thiếu đường link', 'Vui lòng dán link file Google Sheet chứa danh sách ứng viên.');
      return;
    }
    if (this.sheetUrl.includes('/forms/') || this.sheetUrl.includes('forms.google.com')) {
      this.toast.warning(
        'Nhầm link Google Form!',
        'Bạn đang dán link của Google Form (Biểu mẫu). Mục này cần link file Google Sheet (Trang tính). Hãy vào Google Form -> bấm tab "Câu trả lời" -> bấm "Liên kết với Trang tính" để mở bảng tính.'
      );
      return;
    }

    this.isSyncingSheet = true;
    this.syncResult = null;
    const mappingPayload = this.showColumnConfig ? this.columnMapping : undefined;

    this.applicationService.syncGoogleSheets(this.campaign.jobId, this.sheetUrl.trim(), mappingPayload).subscribe({
      next: (res) => {
        this.isSyncingSheet = false;
        this.syncResult = res.data;
        this.toast.success('Đồng bộ thành công', res.message || 'Đã kéo dữ liệu ứng viên về hệ thống.');
        this.synced.emit();
      },
      error: (err) => {
        this.isSyncingSheet = false;
        this.toast.error('Đồng bộ thất bại', err.error?.message || 'Không thể đọc dữ liệu từ Google Sheet. Hãy kiểm tra quyền truy cập file.');
      }
    });
  }

  // 4. Bắn thử Webhook mẫu
  sendTestWebhook(): void {
    if (!this.campaign?.jobId) return;

    this.isTestingWebhook = true;
    this.testResult = null;

    const payload = {
      jobId: this.campaign.jobId,
      fullName: this.testFullName.trim(),
      phone: this.testPhone.trim(),
      yearOfBirth: '2000',
      hometown: 'Hà Nội',
      experienceNotes: 'Ứng viên mẫu test từ giao diện ATS Web',
      shiftReady: 'Có',
      email: `${this.testPhone}@test-ats.local`
    };

    this.applicationService.testGoogleFormWebhook(payload).subscribe({
      next: (res) => {
        this.isTestingWebhook = false;
        this.testResult = res;
        this.toast.success('Test thành công', 'Webhook đã nhận và tạo ứng viên thành công trong ATS!');
        // Đổi SĐT test cho lần sau
        this.testPhone = '0988' + Math.floor(100000 + Math.random() * 900000);
        this.synced.emit();
      },
      error: (err) => {
        this.isTestingWebhook = false;
        this.testResult = { success: false, message: err.error?.message || 'Lỗi khi gửi webhook test.' };
        this.toast.error('Test thất bại', err.error?.message || 'Webhook từ chối request.');
      }
    });
  }
}
