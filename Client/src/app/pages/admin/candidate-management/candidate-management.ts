import { Component, OnInit, NgZone, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { AccountService, CandidateDto } from '../../../services/account.service';
import { ToastService } from '../../../services/toast.service';

@Component({
  selector: 'app-admin-candidate-management',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './candidate-management.html'
})
export class CandidateManagementComponent implements OnInit {
  candidates: CandidateDto[] = [];
  isLoading = false;
  
  // Trạng thái nút Loading rièng cho từ id
  isToggling: { [key: string]: boolean } = {};

  // Pagination & Search
  searchTerm = '';
  pageConfig = 1;
  pageSize = 6;
  totalItems = 0;
  totalPages = 0;
  
  Math = Math; // Export Math object cho Template HTML dùng

  constructor(
    private accountService: AccountService,
    private toastService: ToastService,
    private ngZone: NgZone,
    private cdr: ChangeDetectorRef
  ) {}

  ngOnInit() {
    // Tránh lỗi NG0100: ExpressionChangedAfterItHasBeenCheckedError 
    // vì chúng ta bật cờ isLoading = true ngay khi View đang dựng
    setTimeout(() => {
      this.loadCandidates();
    });
  }

  loadCandidates() {
    this.isLoading = true;
    // Báo Angular vẽ lại view ngay lúc isLoading = true
    this.cdr.detectChanges(); 
    
    this.accountService.getCandidates(this.searchTerm, this.pageConfig, this.pageSize)
      .subscribe({
        next: (res) => {
          this.candidates = res.data;
          this.totalItems = res.total;
          this.pageConfig = res.page;
          this.pageSize = res.pageSize;
          this.totalPages = Math.ceil(this.totalItems / this.pageSize) || 1;
          this.isLoading = false;
          this.cdr.detectChanges();
        },
        error: (err) => {
          console.error(err);
          this.toastService.error('Lỗi', 'Không thể tải danh sách ứng viên');
          this.isLoading = false;
          this.cdr.detectChanges();
        }
      });
  }

  changePage(newPage: number) {
    if (newPage >= 1 && newPage <= this.totalPages) {
      this.pageConfig = newPage;
      this.loadCandidates();
    }
  }

  getInitials(name: string): string {
    if (!name) return 'U';
    const parts = name.trim().split(' ');
    if (parts.length === 1) return parts[0].charAt(0).toUpperCase();
    return (parts[0].charAt(0) + parts[parts.length - 1].charAt(0)).toUpperCase();
  }

  // Chìa khoá quan trọng sử dụng NgZone để thay đổi trạng thái UI tức thời
  toggleLock(candidate: CandidateDto) {
    // Hỏi xác nhận
    const actionName = candidate.isActive ? 'Khóa' : 'Mở Khóa';
    let reason = '';
    
    if (candidate.isActive) {
      const p = prompt(`Nhập lý do Khóa tài khoản của ${candidate.fullName}:`);
      if (p === null) return; // Nhấn Cancel
      reason = p.trim();
    }

    if (!confirm(`Bạn có chắc muốn ${actionName} ứng viên này?`)) return;

    this.isToggling[candidate.userId] = true;

    this.accountService.toggleCandidateStatus(candidate.userId, reason)
      .subscribe({
        next: (res) => {
          this.toastService.success('Thành công', res.message);
          
          // Bọc trong NgZone.run để trigger Change Detection của Angular ngay lập tức
          this.ngZone.run(() => {
            candidate.isActive = res.isActive;
            
            // Xử lý ghi đè display Audit Log trên UI
            if (!res.isActive) {
              candidate.lockedByName = 'Bạn (Vừa xong)';
              candidate.lockedAt = new Date().toISOString();
              candidate.lockReason = reason || 'Bị khóa bởi quản trị viên';
            } else {
              candidate.lockedByName = undefined;
              candidate.lockedAt = undefined;
              candidate.lockReason = undefined;
            }

            this.isToggling[candidate.userId] = false;
            this.cdr.detectChanges(); // Bắt buộc Angular vẽ lại view
          });
        },
        error: (err) => {
          setTimeout(() => {
            this.toastService.error('Thất bại', err.error?.message || 'Có lỗi xảy ra!');
            this.isToggling[candidate.userId] = false;
            this.cdr.detectChanges(); // Cập nhật lại view khi nhả disable
          });
        }
      });
  }
}
