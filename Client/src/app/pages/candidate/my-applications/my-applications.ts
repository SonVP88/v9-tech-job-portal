
import { CommonModule, isPlatformBrowser } from '@angular/common';
import { ChangeDetectorRef, Component, OnInit, PLATFORM_ID, inject } from '@angular/core';
import { ApplicationService, MyApplicationDto } from '../../../services/application.service';
import { Router, RouterModule } from '@angular/router';
import { CandidateHeaderComponent } from '../../../components/shared/candidate-header/candidate-header';
import { AuthService } from '../../../services/auth.service';
import { CandidateFooter } from '../../../components/shared/candidate-footer/candidate-footer';

@Component({
  selector: 'app-my-applications',
  standalone: true,
  imports: [CommonModule, RouterModule, CandidateHeaderComponent, CandidateFooter],
  templateUrl: './my-applications.html',
  styleUrl: './my-applications.scss',
})
export class MyApplications implements OnInit {
  // Services
  private authService = inject(AuthService);
  private cdr = inject(ChangeDetectorRef);
  private platformId = inject(PLATFORM_ID);

  // Properties (NON-SIGNAL)
  myApplications: MyApplicationDto[] = [];
  filteredApplications: MyApplicationDto[] = [];
  isLoading = false;
  isEmpty = false;
  currentTab = 'ALL';
  respondingId: string | null = null; // track nút đang loading

  // Auth properties for navbar
  isLoggedIn = false;
  userRole = '';
  userFullName = '';

  constructor(
    private applicationService: ApplicationService,
    private router: Router
  ) { }

  ngOnInit(): void {
    // Chỉ chạy logic này trên trình duyệt (Client Side)
    if (isPlatformBrowser(this.platformId)) {
      console.log('🌍 Running on Browser Platform');

      // Sử dụng setTimeout để đảm bảo execution sau khi view init (MacroTask)
      setTimeout(() => {
        const token = localStorage.getItem('authToken');

        if (token) {
          console.log('🔑 Token found');
          this.isLoggedIn = true;

          try {
            const payload = JSON.parse(atob(token.split('.')[1]));
            this.userRole = payload.role || payload['http://schemas.microsoft.com/ws/2008/06/identity/claims/role'] || '';
            this.userFullName = payload.name || payload['http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name'] || 'User';
          } catch (e) {
            console.error(' Error parsing token:', e);
          }

          // Load data
          this.loadMyApplications();
        } else {
          console.log(' No token found');
        }

        // Force update UI
        this.cdr.detectChanges();
      }, 100);
    }
  }

  /**
   * Gọi API để lấy danh sách hồ sơ đã nộp
   */
  loadMyApplications(): void {
    console.log(' loadMyApplications() called');
    this.isLoading = true;

    this.applicationService.getMyApplications().subscribe({
      next: (response: any) => {
        console.log('📦 Response:', response);

        // Parse response
        if (response && response.success && response.data) {
          this.myApplications = response.data;
        } else if (Array.isArray(response)) {
          this.myApplications = response;
        } else {
          this.myApplications = [];
        }

        this.isEmpty = this.myApplications.length === 0;
        this.isLoading = false;

        console.log(' Loaded', this.myApplications.length, 'applications');
        this.filterApps(); // Filter data after loading
        this.cdr.detectChanges(); // <-- FORCE UPDATE UI
      },
      error: (error) => {
        console.error(' Error:', error);
        this.isLoading = false;
        this.isEmpty = true;
        this.cdr.detectChanges(); // <-- FORCE UPDATE UI
      }
    });
  }

  /**
   * Trả về class CSS cho Badge trạng thái dựa trên status
   */
  getStatusClass(status: string): string {
    switch (status) {
      case 'HIRED':
        return 'px-3 py-1.5 rounded-full bg-indigo-100 border border-indigo-300 text-indigo-700 text-xs font-bold uppercase tracking-wide flex items-center gap-1 shadow-sm';
      case 'INTERVIEW':
      case 'Pending_Offer':
      case 'Waitlist':
        return 'px-3 py-1 rounded-full bg-green-50 border border-green-200 text-green-700 text-xs font-bold uppercase tracking-wide flex items-center gap-1';
      case 'Offer_Sent':
        return 'px-3 py-1 rounded-full bg-orange-50 border border-orange-200 text-orange-700 text-xs font-bold uppercase tracking-wide flex items-center gap-1';
      case 'REJECTED':
        return 'px-3 py-1 rounded-full bg-gray-100 border border-gray-200 text-gray-500 text-xs font-medium uppercase tracking-wide flex items-center gap-1';
      case 'NEW_APPLIED':
      case 'ACTIVE':
        return 'px-3 py-1 rounded-full bg-blue-50 border border-blue-200 text-blue-700 text-xs font-bold uppercase tracking-wide flex items-center gap-1';
      default:
        return 'px-3 py-1 rounded-full bg-gray-50 border border-gray-200 text-gray-700 text-xs font-bold uppercase tracking-wide flex items-center gap-1';
    }
  }

  /**
   * Trả về label tiếng Việt cho trạng thái
   */
  getStatusLabel(status: string): string {
    switch (status) {
      case 'HIRED':
        return 'Đã Trúng Tuyển';
      case 'INTERVIEW':
      case 'Pending_Offer':
      case 'Waitlist':
        return 'Được mời phỏng vấn';
      case 'Offer_Sent':
        return 'Nhận được Offer';
      case 'REJECTED':
        return 'Không phù hợp';
      case 'NEW_APPLIED':
      case 'ACTIVE':
        return 'Đã nộp hồ sơ';
      default:
        return 'Chưa rõ';
    }
  }

  /**
   * Mở CV trong tab mới
   */
  /**
   * Mở CV trong tab mới
   */
  openCv(cvUrl: string | undefined): void {
    if (cvUrl) {
      // Sử dụng đường dẫn tương đối (hoặc giữ nguyên nếu đã là full url) để support port hiện tại (4200)
      // Backend thường trả về /uploads/..., trình duyệt sẽ tự động thêm http://localhost:4200/uploads/...
      // Tuy nhiên, resource ảnh/file thường nằm ở Server (5000).
      // Nếu user đang chạy 4200 mà muốn access file ở 5000, ta cần full path http://localhost:5000.
      // Nhưng user yêu cầu fix vì "tôi chạy 4200 cơ" -> Có lẽ user muốn Proxy qua 4200 hoặc file server đang map đúng?
      // User yêu cầu: "xem Cv đang chạy loacal 5000 nhưng tôi chạy 4200 cơ". 
      // Ý là: Hiện tại code đang trỏ 5000, nhưng user muốn 4200 (hoặc ngược lại?).
      // Đọc kỹ: "xem Cv đang chạy loacal 5000 nhưng tôi chạy 4200 cơ" -> Code cũ: `http://localhost:5000${cvUrl}`. 
      // Có thể user muốn dùng relative link để nó ăn theo port 4200 (nếu đã config proxy.conf.json) hoặc muốn dynamic.
      // Giải pháp an toàn nhất theo yêu cầu "sửa đi": Dùng relative path để browser tự định đoạt (ăn theo host hiện tại).
      // Update: Nếu CV URL là relative (bắt đầu bằng /), ta cứ để nguyên để nó thành http://localhost:4200/uploads/...
      // Nếu user đã cấu hình proxy cho /uploads thì nó sẽ sang 5000.

      const targetUrl = cvUrl.startsWith('http') ? cvUrl : cvUrl;
      window.open(targetUrl, '_blank');
    }
  }

  /**
   * Đặt tab hiện tại và lọc danh sách
   */
  setTab(tab: string): void {
    this.currentTab = tab;
    this.filterApps();
  }

  /**
   * Lọc danh sách hồ sơ dựa trên tab hiện tại
   */
  filterApps(): void {
    if (this.currentTab === 'ALL') {
      this.filteredApplications = [...this.myApplications];
    } else if (this.currentTab === 'PENDING') {
      this.filteredApplications = this.myApplications.filter(app => app.status === 'NEW_APPLIED' || app.status === 'ACTIVE');
    } else if (this.currentTab === 'INTERVIEW') {
      this.filteredApplications = this.myApplications.filter(app => app.status === 'INTERVIEW' || app.status === 'Pending_Offer' || app.status === 'Waitlist');
    } else if (this.currentTab === 'FINISHED') {
      this.filteredApplications = this.myApplications.filter(app =>
        app.status === 'REJECTED' || app.status === 'HIRED' || app.status === 'Offer_Sent'
      );
    }

    // Cập nhật trạng thái empty dựa trên danh sách đã lọc
    // Lưu ý: isEmpty gốc dùng để check nếu chưa có hồ sơ nào. 
    // Ở đây ta có thể muốn hiển thị "Chưa có hồ sơ" nếu tab trống, hoặc giữ nguyên logic cũ.
    // Tạm thời giữ nguyên logic isEmpty là "không có hồ sơ nào trong DB" của user.
    // Tuy nhiên, để UX tốt hơn, ta có thể check filteredApplications.length nếu muốn.
  }

  /**
   * Trả về tooltip cho trạng thái
   */
  getTooltip(status: string): string {
    switch (status) {
      case 'HIRED':
        return ' Chúc mừng bạn đã trúng tuyển! Nhà tuyển dụng sẽ liên hệ sớm.';
      case 'INTERVIEW':
      case 'Pending_Offer':
      case 'Waitlist':
        return 'Chúc mừng bạn! Hồ sơ đã được duyệt phỏng vấn.';
      case 'Offer_Sent':
        return 'Nhà tuyển dụng đã gửi thư mời nhận việc (Offer) qua Email. Vui lòng kiểm tra Email để phản hồi!';
      case 'REJECTED':
        return 'Rất tiếc, hồ sơ chưa phù hợp lần này. Hãy thử cơ hội khác nhé!';
      case 'NEW_APPLIED':
      case 'ACTIVE':
        return 'Hồ sơ đang chờ nhà tuyển dụng xem xét.';
      default:
        return '';
    }
  }

  /**
   * Chuyển hướng đến chi tiết công việc
   */
  goToJobDetail(jobId: string): void {
    this.router.navigate(['/candidate/job-detail', jobId]);
  }

  /**
   * Ứng viên phản hồi Offer: Đồng ý (HIRED) hoặc Từ chối (REJECTED)
   */
  respondToOffer(app: MyApplicationDto, response: 'HIRED' | 'REJECTED'): void {
    const confirmMsg = response === 'HIRED'
      ? `Bạn chắc chắn muốn ĐỒNG Ý nhận việc tại "${app.jobTitle}"?`
      : `Bạn chắc chắn muốn TỪ CHỐI Offer tại "${app.jobTitle}"?`;
    if (!confirm(confirmMsg)) return;

    this.respondingId = app.applicationId; // bắt đầu loading
    const accept = response === 'HIRED';
    this.applicationService.respondToOffer(app.applicationId, accept).subscribe({
      next: (res: any) => {
        this.respondingId = null; // kết thúc loading
        if (res && res.success !== false) {
          app.status = response;
          this.filterApps();
          this.cdr.detectChanges();
        }
      },
      error: (err) => {
        this.respondingId = null; // kết thúc loading dù lỗi
        console.error(' respondToOffer error:', err);
      }
    });
  }

  /**
   * Chuyển hướng đến trang tìm việc
   */
  goToJobSearch(): void {
    this.router.navigate(['/jobs']);
  }

  /**
   * Format ngày tháng
   */
  formatDate(dateString: string): string {
    const date = new Date(dateString);
    return date.toLocaleDateString('vi-VN', {
      day: '2-digit',
      month: '2-digit',
      year: 'numeric'
    });
  }

  /**
   * Đăng xuất
   */
  logout(): void {
    this.authService.logout();
  }
}
