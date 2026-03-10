import { Component, OnInit, inject, signal, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { JobService, JobDto } from '../../../services/job.service';
import { ToastService } from '../../../services/toast.service';

@Component({
    selector: 'app-job-list',
    standalone: true,
    imports: [CommonModule, RouterModule, FormsModule],
    templateUrl: './job-list.html'
})
export class JobListComponent implements OnInit {
    private jobService = inject(JobService);
    private cdr = inject(ChangeDetectorRef);
    private toast = inject(ToastService);

    // Using signals like employee management
    jobs = signal<JobDto[]>([]);
    filteredJobs = signal<JobDto[]>([]);
    isLoading = signal(false);

    // Search and filter
    searchTerm: string = '';
    statusFilter: string = '';

    // Pagination
    currentPage = signal(1);
    itemsPerPage = 9;

    // Expose Math for template
    Math = Math;

    // properties for pagination
    totalPages = (): number => Math.ceil(this.filteredJobs().length / this.itemsPerPage);

    paginatedJobs = (): JobDto[] => {
        const start = (this.currentPage() - 1) * this.itemsPerPage;
        return this.filteredJobs().slice(start, start + this.itemsPerPage);
    };

    ngOnInit(): void {
        console.log('[JobList] 🚀 Component initialized');
        this.loadJobs();
    }

    loadJobs(): void {
        this.isLoading.set(true);
        const loadStart = performance.now();
        console.log('[JobList] ⏳ Đang bắt đầu quá trình nạp dữ liệu...');

        this.jobService.fetchJobs().subscribe({
            next: (data: JobDto[]) => {
                const fetchEnd = performance.now();
                console.log(`[JobList] 📥 Đã nhận ${(data || []).length} jobs từ service (Mất ${(fetchEnd - loadStart).toFixed(2)}ms)`);

                const processStart = performance.now();
                this.jobs.set(data);
                this.filterJobs();
                this.isLoading.set(false);
                this.cdr.detectChanges();

                const processEnd = performance.now();
                console.log(`[JobList] ✨ Đã render xong giao diện (Mất ${(processEnd - processStart).toFixed(2)}ms)`);
                console.log(`[JobList] 🏁 Tổng thời gian từ lúc vào trang đến khi xong: ${(processEnd - loadStart).toFixed(2)}ms`);
            },
            error: (err: any) => {
                console.error('[JobList] ❌ Lỗi khi tải jobs:', err);
                this.isLoading.set(false);
                this.cdr.detectChanges();
            }
        });
    }

    filterJobs(): void {
        let tempJobs = [...this.jobs()];

        // Filter by search term
        if (this.searchTerm) {
            const term = this.searchTerm.toLowerCase();
            tempJobs = tempJobs.filter(job =>
                job.title.toLowerCase().includes(term) ||
                (job.location && job.location.toLowerCase().includes(term))
            );
        }

        // Filter by status
        if (this.statusFilter) {
            if (this.statusFilter === 'Only') {
                tempJobs = tempJobs.filter(job => job.status === 'OPEN');
            } else if (this.statusFilter === 'Closed') {
                tempJobs = tempJobs.filter(job => job.status === 'CLOSED');
            }
        }

        this.filteredJobs.set(tempJobs);
        this.cdr.detectChanges();
    }

    formatSalary(min?: number, max?: number): string {
        if (!min && !max) return 'Thỏa thuận';
        if (min && !max) return `Từ ${min.toLocaleString()} VNĐ`;
        if (!min && max) return `Đến ${max.toLocaleString()} VNĐ`;
        return `${min?.toLocaleString()} - ${max?.toLocaleString()} VNĐ`;
    }

    formatDate(dateStr: string): string {
        if (!dateStr) return '';
        // Fix: Nếu backend gửi UTC mà không có Z ở cuối, browser sẽ hiểu nhầm là giờ Local.
        if (!dateStr.endsWith('Z') && !dateStr.includes('+')) {
            dateStr += 'Z';
        }
        const date = new Date(dateStr);
        return date.toLocaleDateString('vi-VN', {
            year: 'numeric',
            month: '2-digit',
            day: '2-digit',
            hour: '2-digit',
            minute: '2-digit'
        });
    }

    onDelete(id: string): void {
        if (confirm('Bạn có chắc chắn muốn xóa tin tuyển dụng này không? Hành động này không thể hoàn tác.')) {
            this.jobService.deleteJob(id).subscribe({
                next: () => {
                    this.toast.success('Xóa thành công', 'Tin tuyển dụng đã được xóa.');
                    this.loadJobs();
                },
                error: () => this.toast.error('Xóa thất bại', 'Đã xảy ra lỗi khi xóa tin công việc.')
            });
        }
    }

    onClose(id: string): void {
        if (confirm('Bạn có chắc chắn muốn ngừng đăng tin này?')) {
            this.jobService.closeJob(id).subscribe({
                next: () => {
                    this.toast.success('Đã ngừng đăng tin', 'Tin tuyển dụng sẽ bị ẩn khỏi trang tìm kiếm.');
                    this.loadJobs();
                },
                error: () => this.toast.error('Thất bại', 'Đã xảy ra lỗi khi ngừng đăng tin.')
            });
        }
    }

    onOpen(id: string): void {
        if (confirm('Bạn có muốn mở lại tin tuyển dụng này?')) {
            this.jobService.openJob(id).subscribe({
                next: () => {
                    this.toast.success('Đã mở lại tin', 'Tin tuyển dụng đã xuất hiện lại trên trang tìm kiếm.');
                    this.loadJobs();
                },
                error: (err: any) => {
                    const errorMsg = err.error?.message || 'Đã xảy ra lỗi khi mở lại tin.';
                    this.toast.error('Thất bại', errorMsg);
                }
            });
        }
    }

    // Pagination methods
    changePage(page: number): void {
        if (page >= 1 && page <= this.totalPages()) {
            this.currentPage.set(page);
            this.cdr.detectChanges();
        }
    }

    nextPage(): void {
        if (this.currentPage() < this.totalPages()) {
            this.currentPage.set(this.currentPage() + 1);
            this.cdr.detectChanges();
        }
    }

    previousPage(): void {
        if (this.currentPage() > 1) {
            this.currentPage.set(this.currentPage() - 1);
            this.cdr.detectChanges();
        }
    }

    getPageNumbers(): number[] {
        const total = this.totalPages();
        const current = this.currentPage();
        const delta = 2;

        const range: number[] = [];
        for (let i = Math.max(2, current - delta); i <= Math.min(total - 1, current + delta); i++) {
            range.push(i);
        }

        if (current - delta > 2) {
            range.unshift(-1); // ellipsis
        }
        if (current + delta < total - 1) {
            range.push(-1); // ellipsis
        }

        range.unshift(1);
        if (total > 1) {
            range.push(total);
        }

        return range;
    }
}
