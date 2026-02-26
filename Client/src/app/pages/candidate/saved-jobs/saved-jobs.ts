import { Component, OnInit, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router, RouterModule } from '@angular/router';
import { BehaviorSubject } from 'rxjs';
import { CandidateHeaderComponent } from '../../../components/shared/candidate-header/candidate-header';
import { SavedJobService, SavedJobDto } from '../../../services/saved-job.service';
import { ToastService } from '../../../services/toast.service';

@Component({
    selector: 'app-saved-jobs',
    standalone: true,
    imports: [CommonModule, RouterModule, CandidateHeaderComponent],
    templateUrl: './saved-jobs.html',
    styleUrl: './saved-jobs.scss'
})
export class SavedJobsComponent implements OnInit, OnDestroy {
    // BehaviorSubject + async pipe = Angular-native, không cần detectChanges/NgZone
    private savedJobsSubject = new BehaviorSubject<SavedJobDto[] | null>(null);
    savedJobs$ = this.savedJobsSubject.asObservable();

    isLoggedIn = false;

    constructor(
        private savedJobService: SavedJobService,
        private router: Router,
        private toast: ToastService
    ) { }

    ngOnInit(): void {
        const token = localStorage.getItem('authToken');
        this.isLoggedIn = !!token;
        if (this.isLoggedIn) {
            this.loadSavedJobs();
        } else {
            this.savedJobsSubject.next([]);
        }
    }

    ngOnDestroy(): void {
        this.savedJobsSubject.complete();
    }

    loadSavedJobs(): void {
        this.savedJobService.getSavedJobs().subscribe({
            next: (data) => {
                this.savedJobsSubject.next(data);
            },
            error: (err) => {
                console.error('Error loading saved jobs:', err);
                this.savedJobsSubject.next([]);
            }
        });
    }

    unsaveJob(jobId: string): void {
        this.savedJobService.toggleSave(jobId).subscribe({
            next: () => {
                const current = this.savedJobsSubject.getValue() ?? [];
                this.savedJobsSubject.next(current.filter(s => s.job.jobId !== jobId));
                this.toast.success('Bỏ lưu thành công', 'Đã bỏ lưu công việc khỏi danh sách.');
            },
            error: (err) => {
                console.error('Error unsaving job:', err);
                this.toast.error('Có lỗi xảy ra', 'Không thể bỏ lưu công việc lúc này.');
            }
        });
    }

    formatSalary(min: number, max: number, currency: string): string {
        if (!min && !max) return 'Thỏa thuận';
        const fmt = (n: number) => {
            if (n >= 1_000_000) return (n / 1_000_000).toFixed(0) + ' Triệu';
            return n.toLocaleString('vi-VN');
        };
        if (min && max) return `${fmt(min)} - ${fmt(max)}`;
        if (min) return `Từ ${fmt(min)}`;
        return `Đến ${fmt(max)}`;
    }

    navigateToJob(jobId: string): void {
        this.router.navigate(['/candidate/job-detail', jobId]);
    }

    navigateToJobs(): void {
        this.router.navigate(['/candidate/jobs']);
    }
}
