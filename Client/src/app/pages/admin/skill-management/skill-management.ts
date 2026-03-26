import { Component, OnInit, ChangeDetectorRef, NgZone } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { environment } from '../../../../environments/environment';
import { ToastService } from '../../../services/toast.service';

interface Skill {
    skillId: string;
    name: string;
    normalizedName: string;
    createdAt: Date;
    isDeleted: boolean;
}

@Component({
    selector: 'app-skill-management',
    standalone: true,
    imports: [CommonModule, FormsModule],
    templateUrl: './skill-management.html',
    styleUrl: './skill-management.scss'
})
export class SkillManagementComponent implements OnInit {
    skills: Skill[] = [];

    // States - không dùng isLoading để gate UI nữa
    isSaving = false;
    searchQuery = '';

    // Modal
    isModalOpen = false;
    modalMode: 'create' | 'edit' = 'create';
    currentSkill: Skill | null = null;
    skillName = '';

    // Pagination
    currentPage = 1;
    pageSize = 6;
    totalPages = 1;
    totalCount = 0;

    Math = Math;

    private apiUrl = `${environment.apiUrl}/admin/skills`;

    constructor(
        private http: HttpClient,
        private cdr: ChangeDetectorRef,
        private ngZone: NgZone,
        private toast: ToastService
    ) { }

    ngOnInit(): void {
        this.loadSkills();
    }

    private getAuthHeaders(): HttpHeaders {
        const token = localStorage.getItem('authToken') || '';
        return new HttpHeaders({ 'Authorization': `Bearer ${token}` });
    }

    loadSkills(): void {
        const params: any = { page: this.currentPage, pageSize: this.pageSize };
        if (this.searchQuery.trim()) {
            params.search = this.searchQuery.trim();
        }

        const headers = this.getAuthHeaders();

        this.ngZone.runOutsideAngular(() => {
            this.http.get<any>(this.apiUrl, { headers, params }).subscribe({
                next: (response) => {
                    this.ngZone.run(() => {
                        this.skills = (response.data ?? []).map((s: any) => ({
                            ...s,
                            createdAt: s.createdAt ? (s.createdAt.endsWith('Z') ? s.createdAt : s.createdAt + 'Z') : s.createdAt
                        }));
                        this.totalCount = response.total ?? 0;
                        this.totalPages = response.totalPages ?? 1;
                        this.cdr.markForCheck();
                        this.cdr.detectChanges();
                    });
                },
                error: (err) => {
                    console.error('Skills API error:', err.status, err.message);
                    this.ngZone.run(() => {
                        this.skills = [];
                        this.cdr.detectChanges();
                    });
                }
            });
        });
    }

    onSearchChange(): void {
        this.currentPage = 1;
        this.loadSkills();
    }

    openCreateModal(): void {
        this.modalMode = 'create';
        this.skillName = '';
        this.currentSkill = null;
        this.isModalOpen = true;
    }

    openEditModal(skill: Skill): void {
        this.modalMode = 'edit';
        this.skillName = skill.name;
        this.currentSkill = { ...skill };
        this.isModalOpen = true;
    }

    closeModal(): void {
        this.isModalOpen = false;
        this.skillName = '';
        this.currentSkill = null;
        this.isSaving = false;
    }

    saveSkill(): void {
        if (!this.skillName.trim()) {
            this.toast.warning('Thiếu thông tin', 'Vui lòng nhập tên kỹ năng!');
            return;
        }
        if (this.isSaving) return;

        this.isSaving = true;
        const body = { name: this.skillName.trim() };
        const headers = new HttpHeaders({
            'Authorization': `Bearer ${localStorage.getItem('authToken') || ''}`,
            'Content-Type': 'application/json'
        });

        const request$ = this.modalMode === 'create'
            ? this.http.post(this.apiUrl, body, { headers })
            : this.http.put(`${this.apiUrl}/${this.currentSkill!.skillId}`, body, { headers });

        this.ngZone.runOutsideAngular(() => {
            request$.subscribe({
                next: () => {
                    this.ngZone.run(() => {
                        this.closeModal();
                        this.loadSkills();
                        const msg = this.modalMode === 'create' ? 'Thêm kỹ năng thành công!' : 'Cập nhật kỹ năng thành công!';
                        this.toast.success('Thành công', msg);
                    });
                },
                error: (err) => {
                    this.ngZone.run(() => {
                        this.isSaving = false;
                        const msg = err.error?.message || (this.modalMode === 'create' ? 'Có lỗi khi thêm!' : 'Có lỗi khi cập nhật!');
                        this.toast.error('Thất bại', msg);
                    });
                }
            });
        });
    }

    toggleSkillStatus(skill: Skill): void {
        const actionStr = skill.isDeleted ? 'Mở khóa' : 'Khóa';
        if (!confirm(`Bạn có chắc muốn ${actionStr} kỹ năng "${skill.name}"?`)) return;

        const headers = this.getAuthHeaders();
        this.ngZone.runOutsideAngular(() => {
            this.http.delete<any>(`${this.apiUrl}/${skill.skillId}`, { headers }).subscribe({
                next: (res) => {
                    this.ngZone.run(() => {
                        this.loadSkills();
                        this.toast.success('Thành công', res.message || `${actionStr} kỹ năng thành công!`);
                    });
                },
                error: (err) => {
                    this.ngZone.run(() => {
                        this.toast.error('Lỗi', err.error?.message || `Có lỗi khi ${actionStr.toLowerCase()}!`);
                    });
                }
            });
        });
    }

    changePage(page: number): void {
        if (page >= 1 && page <= this.totalPages) {
            this.currentPage = page;
            this.loadSkills();
        }
    }

    getPageNumbers(): number[] {
        const pages: number[] = [];
        const maxVisible = 5;
        let start = Math.max(1, this.currentPage - Math.floor(maxVisible / 2));
        let end = Math.min(this.totalPages, start + maxVisible - 1);
        if (end - start < maxVisible - 1) start = Math.max(1, end - maxVisible + 1);
        for (let i = start; i <= end; i++) pages.push(i);
        return pages;
    }
}
