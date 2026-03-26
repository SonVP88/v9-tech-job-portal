import { CommonModule } from '@angular/common';
import { Component, OnInit, inject, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Router } from '@angular/router';
import { OfferModalComponent } from '../offer-modal/offer-modal';
import { ApplicationService, CandidateProfileDto } from '../../../services/application.service';
import { ToastService } from '../../../services/toast.service';

// HR chỉ được advance đến Offer_Sent.
// HIRED chỉ do ứng viên set khi chấp nhận offer (respond-offer API).
const PIPELINE_STAGES = [
  { key: 'PENDING', label: 'Ứng tuyển', icon: 'description', hrCanAdvance: true },
  { key: 'INTERVIEW', label: 'Phỏng vấn', icon: 'calendar_month', hrCanAdvance: true },
  { key: 'Pending_Offer', label: 'Chờ Offer', icon: 'schedule', hrCanAdvance: true },
  { key: 'Offer_Sent', label: 'Đã gửi Offer', icon: 'send', hrCanAdvance: false },
  { key: 'HIRED', label: 'Đã tuyển', icon: 'verified', hrCanAdvance: false },
];

@Component({
  selector: 'app-candidate-detail',
  standalone: true,
  imports: [CommonModule, OfferModalComponent],
  templateUrl: './candidate-detail.html',
  styleUrl: './candidate-detail.scss',
})
export class CandidateDetail implements OnInit {
  private router = inject(Router);
  private http = inject(HttpClient);
  private appService = inject(ApplicationService);
  private toast = inject(ToastService);

  profile = signal<CandidateProfileDto | null>(null);
  isLoadingProfile = signal(false);
  isUpdatingStatus = signal(false);
  isDownloading = signal(false);
  routerDataSig = signal<any>(null);
  showOfferModal = signal(false);

  pipelineStages = PIPELINE_STAGES;

  constructor() {
    const navigation = this.router.getCurrentNavigation();
    const state = navigation?.extras.state as { candidate: any };
    const data = state?.candidate ?? {
      applicationId: '', candidateId: '',
      candidateName: 'Demo', email: '', phone: '',
      jobTitle: '', appliedAt: new Date().toISOString(),
      status: 'PENDING', cvUrl: '', matchScore: null,
    };
    this.routerDataSig.set(data);
  }

  ngOnInit(): void {
    const id = this.routerDataSig()?.candidateId;
    if (id) this.loadCandidateProfile(id);
  }

  // ── Getters ──────────────────────────────────────────
  get rd() { return this.routerDataSig(); }
  get name() { return this.profile()?.fullName || this.rd?.candidateName || 'Ứng viên'; }
  get email() { return this.profile()?.email || this.rd?.email || ''; }
  get phone() { return this.profile()?.phone || this.rd?.phone || ''; }
  get location() { return this.profile()?.location || ''; }
  get summary() { return this.profile()?.summary || 'Ứng viên chưa cập nhật giới thiệu.'; }
  get avatar() { return this.profile()?.avatar || null; }
  get linkedIn() { return this.profile()?.linkedIn || null; }
  get gitHub() { return this.profile()?.gitHub || null; }
  get skills() { return this.profile()?.skills?.map(s => s.skillName) || []; }
  get documents() { return this.profile()?.documents || []; }
  get currentStatus() { return this.rd?.status || 'PENDING'; }
  get currentStatusLabel() {
    return PIPELINE_STAGES.find(s => s.key === this.currentStatus)?.label || this.currentStatus;
  }
  get appliedDate() {
    return this.rd?.appliedAt ? new Date(this.rd.appliedAt).toLocaleDateString('vi-VN') : '';
  }
  /** HR chỉ được advanced đến Offer_Sent, không được tự set HIRED */
  canAdvanceTo(target: string): boolean {
    const stage = PIPELINE_STAGES.find(s => s.key === target);
    if (!stage?.hrCanAdvance) return false; // HIRED và Offer_Sent không cho HR tự click
    const order = PIPELINE_STAGES.map(s => s.key);
    return order.indexOf(target) === order.indexOf(this.currentStatus) + 1;
  }

  getInitials(name: string): string {
    const p = name.trim().split(' ');
    return p.length >= 2 ? (p[0][0] + p[p.length - 1][0]).toUpperCase() : name.substring(0, 2).toUpperCase();
  }
  formatFileSize(bytes?: number): string {
    if (!bytes) return '';
    if (bytes < 1024) return `${bytes} B`;
    if (bytes < 1048576) return `${(bytes / 1024).toFixed(0)} KB`;
    return `${(bytes / 1048576).toFixed(1)} MB`;
  }

  // ── Load profile ──────────────────────────────────────
  loadCandidateProfile(candidateId: string): void {
    this.isLoadingProfile.set(true);
    this.appService.getCandidateProfile(candidateId).subscribe({
      next: (data: CandidateProfileDto) => {
        this.profile.set(data);
        this.isLoadingProfile.set(false);
      },
      error: (err: any) => {
        console.error('❌ Profile load error:', err);
        this.isLoadingProfile.set(false);
      }
    });
  }

  // ── Pipeline helpers ──────────────────────────────────
  isStageActive(key: string): boolean {
    const order = PIPELINE_STAGES.map(s => s.key);
    return order.indexOf(key) <= order.indexOf(this.currentStatus);
  }
  isStageCompleted(key: string): boolean {
    const order = PIPELINE_STAGES.map(s => s.key);
    return order.indexOf(key) < order.indexOf(this.currentStatus);
  }

  updateStatus(newStatus: string): void {
    if (!this.rd?.applicationId) {
      this.toast.warning('Lỗi', 'Không tìm thấy mã hồ sơ!');
      return;
    }
    // Bảo vệ: HR không được set HIRED hay Offer_Sent trực tiếp qua method này
    const stage = PIPELINE_STAGES.find(s => s.key === newStatus);
    if (!stage?.hrCanAdvance) {
      this.toast.warning('Không được phép', 'Trạng thái này chỉ được cập nhật qua quy trình offer.');
      return;
    }
    this.isUpdatingStatus.set(true);
    this.appService.updateApplicationStatus(this.rd.applicationId, newStatus).subscribe({
      next: () => {
        const label = PIPELINE_STAGES.find(s => s.key === newStatus)?.label || newStatus;
        this.toast.success('Thành công', `Đã chuyển sang "${label}"`);
        this.routerDataSig.update(d => ({ ...d, status: newStatus }));
        this.isUpdatingStatus.set(false);
      },
      error: (err: any) => {
        console.error('❌ Update status error:', err);
        this.toast.error('Lỗi', 'Không thể cập nhật trạng thái.');
        this.isUpdatingStatus.set(false);
      }
    });
  }

  // ── File actions (copy từ candidate-profile) ──
  previewFileWithAuth(url: string, docType: string = 'OTHER'): void {
    if (!url) return;

    // Nếu là CV, thực hiện tracking
    if (docType === 'CV' && this.rd?.applicationId) {
      this.appService.trackCvView(this.rd.applicationId).subscribe({
        next: () => console.log('CV view tracked from detail'),
        error: (err: any) => console.error('CV view tracking error', err)
      });
    }

    // Fix URL (giống candidate-profile.previewCV)
    if (!url.startsWith('http')) {
      const clean = url.startsWith('/') ? url.substring(1) : url;
      url = `https://localhost:7181/${clean}`;
    }
    if (url.startsWith('http://') && window.location.protocol === 'https:') {
      url = url.replace('http://', 'https://');
    }
    // Static file không cần auth -> mở trực tiếp
    window.open(url, '_blank');
  }

  downloadFileWithAuth(url: string, fileName: string): void {
    if (!url) return;
    // Fix URL (giống candidate-profile.downloadCV)
    if (!url.startsWith('http')) {
      const clean = url.startsWith('/') ? url.substring(1) : url;
      url = `https://localhost:7181/${clean}`;
    }
    if (url.startsWith('http://') && window.location.protocol === 'https:') {
      url = url.replace('http://', 'https://');
    }
    if (url.includes('localhost:5000')) {
      url = url.replace('localhost:5000', 'localhost:7181');
    }

    this.isDownloading.set(true);
    this.http.get(url, { responseType: 'blob' }).subscribe({
      next: (blob) => {
        // Nếu backend trả về JSON/HTML (lỗi auth) -> fallback mở tab mới
        if (blob.type.includes('application/json') || blob.type.includes('text/html')) {
          console.warn('Got non-binary response, fallback to window.open');
          window.open(url, '_blank');
          this.isDownloading.set(false);
          return;
        }
        const a = document.createElement('a');
        const objectUrl = URL.createObjectURL(blob);
        a.href = objectUrl;
        a.download = fileName || 'cv_download.pdf';
        a.click();
        URL.revokeObjectURL(objectUrl);
        this.isDownloading.set(false);
      },
      error: (err) => {
        console.error('❌ Download error:', err);
        // Fallback: mở trực tiếp
        window.open(url, '_blank');
        this.isDownloading.set(false);
      }
    });
  }

  // ── Other actions ─────────────────────────────────────
  rejectCandidate(): void {
    if (!confirm(`Từ chối ứng viên "${this.name}"?`)) return;
    this.isUpdatingStatus.set(true);
    this.appService.updateApplicationStatus(this.rd.applicationId, 'REJECTED').subscribe({
      next: () => {
        this.toast.success('Đã từ chối', `Hồ sơ của ${this.name} đã bị từ chối.`);
        this.routerDataSig.update(d => ({ ...d, status: 'REJECTED' }));
        this.isUpdatingStatus.set(false);
      },
      error: () => {
        this.toast.error('Lỗi', 'Không thể từ chối. Vui lòng thử lại.');
        this.isUpdatingStatus.set(false);
      }
    });
  }

  sendEmail(): void {
    const sub = encodeURIComponent(`[V9 TECH] Phản hồi hồ sơ - ${this.rd?.jobTitle || ''}`);
    const body = encodeURIComponent(`Kính gửi ${this.name},\n\nCảm ơn bạn đã ứng tuyển.\n\nTrân trọng,\nPhòng Nhân sự`);
    window.open(`mailto:${this.email}?subject=${sub}&body=${body}`);
  }

  onOpenOffer(): void { this.showOfferModal.set(true); }
  onOfferClosed(): void { this.showOfferModal.set(false); }
  onOfferSent(_payload: any): void {
    this.onOfferClosed();
    // Sau khi gửi offer, chuyển sang Offer_Sent
    this.isUpdatingStatus.set(true);
    this.appService.updateApplicationStatus(this.rd.applicationId, 'Offer_Sent').subscribe({
      next: () => {
        this.routerDataSig.update(d => ({ ...d, status: 'Offer_Sent' }));
        this.isUpdatingStatus.set(false);
        this.toast.success('Thành công', 'Đã gửi Offer! Chờ ứng viên phản hồi.');
      },
      error: () => { this.isUpdatingStatus.set(false); }
    });
  }

  goBack(): void { this.router.navigate(['/hr/manage-applications']); }
}
