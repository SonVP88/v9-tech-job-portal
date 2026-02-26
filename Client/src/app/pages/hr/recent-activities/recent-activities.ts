import { Component, OnInit, inject, signal, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { DashboardService, DashboardActivityDto } from '../../../services/dashboard.service';

@Component({
    selector: 'app-recent-activities',
    standalone: true,
    imports: [CommonModule, RouterModule],
    templateUrl: './recent-activities.html'
})
export class RecentActivitiesComponent implements OnInit {
    private dashboardService = inject(DashboardService);
    private cdr = inject(ChangeDetectorRef);

    activities = signal<DashboardActivityDto[]>([]);
    isLoading = signal(false);

    // Pagination
    currentPage = signal(1);
    totalPages = signal(1);
    totalItems = signal(0);
    itemsPerPage = 15;

    ngOnInit(): void {
        this.loadActivities(this.currentPage());
    }

    loadActivities(page: number): void {
        if (page < 1 || (this.totalPages() > 0 && page > this.totalPages())) return;

        this.isLoading.set(true);

        this.dashboardService.getPagedActivities(page, this.itemsPerPage).subscribe({
            next: (res) => {
                this.activities.set(res.items);
                this.currentPage.set(res.currentPage);
                this.totalPages.set(res.totalPages);
                this.totalItems.set(res.totalItems);
                this.isLoading.set(false);
                this.cdr.detectChanges();
            },
            error: (err) => {
                console.error('Error loading paged activities', err);
                this.isLoading.set(false);
                this.cdr.detectChanges();
            }
        });
    }

    getRelativeTime(timestamp: string): string {
        const utcStr = timestamp.endsWith('Z') ? timestamp : timestamp + 'Z';
        const date = new Date(utcStr);
        return date.toLocaleString('vi-VN', {
            hour: '2-digit',
            minute: '2-digit',
            day: '2-digit',
            month: '2-digit',
            year: 'numeric'
        });
    }
}
