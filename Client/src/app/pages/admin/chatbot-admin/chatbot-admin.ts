import { Component, OnDestroy, OnInit, ChangeDetectorRef, NgZone } from '@angular/core';
import { CommonModule } from '@angular/common';
import { NgFor, NgIf } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ChatbotAdminService, ChatbotFaqItem, GeminiKeyStat, UpsertChatbotFaqPayload } from '../../../services/chatbot-admin.service';
import { ToastService } from '../../../services/toast.service';
import { PopupService } from '../../../services/popup.service';

@Component({
    selector: 'app-chatbot-admin',
    standalone: true,
    imports: [CommonModule, FormsModule, NgIf, NgFor],
    templateUrl: './chatbot-admin.html',
    styleUrl: './chatbot-admin.scss'
})
export class ChatbotAdminComponent implements OnInit, OnDestroy {
    faqs: ChatbotFaqItem[] = [];
    filteredFaqs: ChatbotFaqItem[] = [];

    searchQuery = '';
    statusFilter: 'all' | 'active' | 'inactive' = 'all';

    isLoading = false;
    keyStats: GeminiKeyStat[] = [];
    isKeysLoading = false;
    isModalOpen = false;
    isSaving = false;
    isKeyModalOpen = false;
    newApiKey = '';
    isSavingKey = false;
    modalMode: 'create' | 'edit' = 'create';
    editingFaqId: string | null = null;
    private keyRefreshHandle: ReturnType<typeof setInterval> | null = null;

    form: UpsertChatbotFaqPayload = {
        question: '',
        answer: '',
        category: 'Chung',
        keywords: '',
        priority: 0,
        isActive: true
    };

    constructor(
        private chatbotAdminService: ChatbotAdminService,
        private toast: ToastService,
        private popup: PopupService,
        private cdr: ChangeDetectorRef,
        private ngZone: NgZone
    ) { }

    ngOnInit(): void {
        this.loadFaqs();
        this.loadKeyStats();
        this.startKeyAutoRefresh();
    }

    ngOnDestroy(): void {
        if (this.keyRefreshHandle) {
            clearInterval(this.keyRefreshHandle);
            this.keyRefreshHandle = null;
        }
    }

    loadKeyStats(): void {
        this.isKeysLoading = true;
        this.chatbotAdminService.getGeminiKeys().subscribe({
            next: (items) => {
                this.ngZone.run(() => {
                    this.keyStats = items || [];
                    this.isKeysLoading = false;
                    setTimeout(() => {
                        this.cdr.detectChanges();
                    });
                });
            },
            error: (err) => {
                this.ngZone.run(() => {
                    this.isKeysLoading = false;
                    this.toast.error('Lỗi', err.error?.message || 'Không thể tải thống kê API keys.');
                    setTimeout(() => {
                        this.cdr.detectChanges();
                    });
                });
            }
        });
    }

    private startKeyAutoRefresh(): void {
        if (this.keyRefreshHandle) {
            clearInterval(this.keyRefreshHandle);
        }

        this.keyRefreshHandle = setInterval(() => {
            this.loadKeyStats();
        }, 15000);
    }

    loadFaqs(): void {
        this.isLoading = true;
        const status = this.statusFilter === 'all'
            ? undefined
            : this.statusFilter === 'active';

        this.chatbotAdminService.getFaqs(this.searchQuery, status).subscribe({
            next: (items) => {
                this.ngZone.run(() => {
                    this.faqs = items;
                    this.filteredFaqs = items;
                    this.isLoading = false;
                    setTimeout(() => {
                        this.cdr.detectChanges();
                    });
                });
            },
            error: (err) => {
                this.ngZone.run(() => {
                    this.isLoading = false;
                    this.toast.error('Lỗi', err.error?.message || 'Không thể tải dữ liệu FAQ chatbot.');
                    setTimeout(() => {
                        this.cdr.detectChanges();
                    });
                });
            }
        });
    }

    applyFilter(): void {
        this.loadFaqs();
    }

    openCreateModal(): void {
        this.modalMode = 'create';
        this.editingFaqId = null;
        this.form = {
            question: '',
            answer: '',
            category: 'Chung',
            keywords: '',
            priority: 0,
            isActive: true
        };
        this.isModalOpen = true;
    }

    openEditModal(item: ChatbotFaqItem): void {
        this.modalMode = 'edit';
        this.editingFaqId = item.faqId;
        this.form = {
            question: item.question,
            answer: item.answer,
            category: item.category,
            keywords: item.keywords || '',
            priority: item.priority,
            isActive: item.isActive
        };
        this.isModalOpen = true;
    }

    closeModal(): void {
        if (this.isSaving) {
            return;
        }
        this.isModalOpen = false;
    }

    saveFaq(): void {
        if (!this.form.question.trim() || !this.form.answer.trim()) {
            this.toast.warning('Thiếu dữ liệu', 'Câu hỏi và câu trả lời là bắt buộc.');
            return;
        }

        this.isSaving = true;
        const payload: UpsertChatbotFaqPayload = {
            question: this.form.question.trim(),
            answer: this.form.answer.trim(),
            category: this.form.category?.trim() || 'Chung',
            keywords: this.form.keywords?.trim() || '',
            priority: Number(this.form.priority) || 0,
            isActive: this.form.isActive
        };

        const request$ = this.modalMode === 'create'
            ? this.chatbotAdminService.createFaq(payload)
            : this.chatbotAdminService.updateFaq(this.editingFaqId!, payload);

        request$.subscribe({
            next: (res) => {
                this.isSaving = false;
                this.isModalOpen = false;
                this.toast.success('Thành công', res?.message || 'Lưu FAQ thành công.');
                this.loadFaqs();
            },
            error: (err) => {
                this.isSaving = false;
                this.toast.error('Lỗi', err.error?.message || 'Không thể lưu FAQ.');
            }
        });
    }

    async toggleFaq(item: ChatbotFaqItem): Promise<void> {
        const confirmed = await this.popup.confirm({
            title: item.isActive ? 'Tắt FAQ' : 'Bật FAQ',
            message: item.isActive
                ? 'FAQ sẽ không còn được dùng để trả lời nhanh.'
                : 'FAQ sẽ được dùng cho lớp trả lời nhanh trước AI.',
            confirmText: item.isActive ? 'Tắt' : 'Bật',
            cancelText: 'Hủy',
            tone: item.isActive ? 'danger' : 'primary'
        });

        if (!confirmed) {
            return;
        }

        this.chatbotAdminService.toggleFaq(item.faqId).subscribe({
            next: (res) => {
                this.toast.success('Thành công', res?.message || 'Cập nhật trạng thái thành công.');
                this.loadFaqs();
            },
            error: (err) => {
                this.toast.error('Lỗi', err.error?.message || 'Không thể cập nhật trạng thái FAQ.');
            }
        });
    }

    async deleteFaq(item: ChatbotFaqItem): Promise<void> {
        const confirmed = await this.popup.confirm({
            title: 'Xóa FAQ',
            message: 'FAQ sẽ bị xóa vĩnh viễn. Bạn có chắc muốn tiếp tục?',
            confirmText: 'Xóa',
            cancelText: 'Hủy',
            tone: 'danger'
        });

        if (!confirmed) {
            return;
        }

        this.chatbotAdminService.deleteFaq(item.faqId).subscribe({
            next: (res) => {
                this.toast.success('Thành công', res?.message || 'Xóa FAQ thành công.');
                this.loadFaqs();
            },
            error: (err) => {
                this.toast.error('Lỗi', err.error?.message || 'Không thể xóa FAQ.');
            }
        });
    }

    get keySummary() {
        return {
            total: this.keyStats.length,
            disabled: this.keyStats.filter(k => k.disabled).length,
            failures: this.keyStats.reduce((sum, k) => sum + (k.failureCount || 0), 0),
            success: this.keyStats.reduce((sum, k) => sum + (k.successCount || 0), 0)
        };
    }

    trackByKeyPreview(_: number, item: GeminiKeyStat): string {
        return item.keyPreview;
    }

    async disableKey(item: GeminiKeyStat): Promise<void> {
        const confirmed = await this.popup.confirm({
            title: 'Vô hiệu hóa key',
            message: `Bạn muốn tắt key ${item.keyPreview} khỏi vòng xoay sử dụng?`,
            confirmText: 'Vô hiệu hóa',
            cancelText: 'Hủy',
            tone: 'danger'
        });

        if (!confirmed) {
            return;
        }

        this.chatbotAdminService.disableGeminiKey(item.keyIndex).subscribe({
            next: (res) => {
                this.toast.success('Thành công', res?.message || 'Đã vô hiệu hóa key.');
                this.loadKeyStats();
            },
            error: (err) => {
                this.toast.error('Lỗi', err.error?.message || 'Không thể vô hiệu hóa key.');
            }
        });
    }

    async enableKey(item: GeminiKeyStat): Promise<void> {
        const confirmed = await this.popup.confirm({
            title: 'Kích hoạt key',
            message: `Bạn muốn bật lại key ${item.keyPreview} vào vòng xoay sử dụng?`,
            confirmText: 'Kích hoạt',
            cancelText: 'Hủy',
            tone: 'primary'
        });

        if (!confirmed) {
            return;
        }

        this.chatbotAdminService.enableGeminiKey(item.keyIndex).subscribe({
            next: (res) => {
                this.toast.success('Thành công', res?.message || 'Đã kích hoạt lại key.');
                this.loadKeyStats();
            },
            error: (err) => {
                this.toast.error('Lỗi', err.error?.message || 'Không thể kích hoạt key.');
            }
        });
    }

    openAddKeyModal(): void {
        this.newApiKey = '';
        this.isKeyModalOpen = true;
        this.cdr.detectChanges();
    }

    closeAddKeyModal(): void {
        if (this.isSavingKey) return;
        this.newApiKey = '';
        this.isKeyModalOpen = false;
        this.cdr.detectChanges();
    }

    saveNewGeminiKey(): void {
        const key = this.newApiKey.trim();
        if (!key) {
            this.toast.warning('Thiếu dữ liệu', 'Vui lòng nhập chuỗi Gemini API Key.');
            return;
        }

        this.isSavingKey = true;
        this.cdr.detectChanges();

        this.chatbotAdminService.addGeminiKey(key).subscribe({
            next: (res) => {
                this.isSavingKey = false;
                this.isKeyModalOpen = false;
                this.newApiKey = '';
                this.toast.success('Thành công', res?.message || 'Đã thêm Gemini API Key thành công.');
                this.loadKeyStats();
                this.cdr.detectChanges();
            },
            error: (err) => {
                this.isSavingKey = false;
                this.toast.error('Lỗi', err.error?.message || 'Không thể thêm Gemini API Key mới.');
                this.cdr.detectChanges();
            }
        });
    }
}
