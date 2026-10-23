import { Component, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { AuthService } from '../../../services/auth.service';
import { ToastService } from '../../../services/toast.service';

@Component({
  selector: 'app-header',
  standalone: true,
  imports: [CommonModule, RouterLink, FormsModule],
  templateUrl: './header.component.html'
})
export class HeaderComponent {
  public authService = inject(AuthService);
  private toastService = inject(ToastService);

  // Trạng thái Modal Đề xuất tuyển dụng
  isHiringModalOpen = signal<boolean>(false);
  isSubmitting = signal<boolean>(false);

  // Form Model đề xuất tuyển dụng
  requestForm = {
    jobTitle: '',
    targetHeadcount: 10,
    reason: 'EXPANSION',
    urgencyLevel: 'URGENT',
    shiftType: '3_SHIFTS',
    specialRequirements: ''
  };

  openHiringRequestModal(): void {
    const user = this.authService.currentUser();
    // Khởi tạo giá trị mặc định cho form
    this.requestForm = {
      jobTitle: '',
      targetHeadcount: 10,
      reason: 'EXPANSION',
      urgencyLevel: 'URGENT',
      shiftType: '3_SHIFTS',
      specialRequirements: ''
    };
    this.isHiringModalOpen.set(true);
  }

  closeHiringRequestModal(): void {
    this.isHiringModalOpen.set(false);
  }

  submitHiringRequest(): void {
    if (!this.requestForm.jobTitle.trim()) {
      this.toastService.warning('Thiếu thông tin', 'Vui lòng nhập vị trí công việc cần tuyển dụng.');
      return;
    }

    if (this.requestForm.targetHeadcount <= 0) {
      this.toastService.warning('Số lượng không hợp lệ', 'Số lượng nhân sự cần tuyển phải lớn hơn 0.');
      return;
    }

    this.isSubmitting.set(true);

    // Mô phỏng gọi API tạo bản ghi JobPosting ở trạng thái PendingHRApproval
    setTimeout(() => {
      this.isSubmitting.set(false);
      this.isHiringModalOpen.set(false);
      
      const dept = this.authService.currentUser()?.departmentName || 'Xưởng chuyên môn';
      this.toastService.success(
        'Đề xuất thành công',
        `Đã gửi yêu cầu tuyển dụng "${this.requestForm.jobTitle}" (${this.requestForm.targetHeadcount} người) từ ${dept} tới Phòng Nhân Sự. Trạng thái: Chờ HR phê duyệt (PendingHRApproval).`
      );
    }, 600);
  }
}
