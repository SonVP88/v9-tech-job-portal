import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ToastService, Toast } from '../../services/toast.service';

@Component({
    selector: 'app-toast',
    standalone: true,
    imports: [CommonModule],
    template: `
    <div class="fixed top-5 right-5 z-[99999] flex flex-col gap-3 pointer-events-none" style="min-width:320px; max-width:420px; z-index: 999999 !important;">
      <div *ngFor="let toast of toastService.toasts(); trackBy: trackById"
        class="pointer-events-auto flex items-start gap-3 px-4 py-3.5 rounded-xl shadow-xl border transition-all duration-300 animate-slide-in bg-white dark:bg-slate-800"
        [ngClass]="getContainerClass(toast.type)">

        <!-- Icon -->
        <div class="shrink-0 w-8 h-8 rounded-full flex items-center justify-center mt-0.5"
          [ngClass]="getIconBgClass(toast.type)">
          <span class="material-symbols-outlined text-white text-[18px] font-bold">{{ getIcon(toast.type) }}</span>
        </div>

        <!-- Content -->
        <div class="flex-1 min-w-0">
          <p class="text-sm font-bold leading-snug" [ngClass]="getTitleClass(toast.type)">{{ toast.title }}</p>
          <p *ngIf="toast.message" class="text-xs mt-0.5 leading-relaxed" [ngClass]="getMessageClass(toast.type)">{{ toast.message }}</p>
        </div>

        <!-- Close -->
        <button (click)="dismiss(toast.id)"
          class="shrink-0 w-6 h-6 flex items-center justify-center rounded-full opacity-60 hover:opacity-100 transition-opacity"
          [ngClass]="getCloseBtnClass(toast.type)">
          <span class="material-symbols-outlined text-[16px]">close</span>
        </button>
      </div>
    </div>
  `,
    styles: [`
    @keyframes slideIn {
      from { opacity: 0; transform: translateX(100%); }
      to   { opacity: 1; transform: translateX(0); }
    }
    .animate-slide-in { animation: slideIn 0.3s cubic-bezier(.21,1.02,.73,1) forwards; }
  `]
})
export class ToastComponent {
    constructor(public toastService: ToastService) { }

    trackById(_: number, t: Toast) { return t.id; }

    dismiss(id: number): void {
        this.toastService.remove(id);
    }

    getContainerClass(type: string): string {
        const map: Record<string, string> = {
            success: 'bg-green-50 border-green-200 dark:bg-green-900/30 dark:border-green-700',
            error: 'bg-red-50 border-red-200 dark:bg-red-900/30 dark:border-red-700',
            warning: 'bg-amber-50 border-amber-200 dark:bg-amber-900/30 dark:border-amber-700',
            info: 'bg-blue-50 border-blue-200 dark:bg-blue-900/30 dark:border-blue-700',
        };
        return map[type] ?? map['info'];
    }

    getIconBgClass(type: string): string {
        const map: Record<string, string> = {
            success: 'bg-green-500',
            error: 'bg-red-500',
            warning: 'bg-amber-500',
            info: 'bg-blue-500',
        };
        return map[type] ?? map['info'];
    }

    getIcon(type: string): string {
        const map: Record<string, string> = {
            success: 'check_circle',
            error: 'cancel',
            warning: 'warning',
            info: 'info',
        };
        return map[type] ?? 'info';
    }

    getTitleClass(type: string): string {
        const map: Record<string, string> = {
            success: 'text-green-800 dark:text-green-200',
            error: 'text-red-800 dark:text-red-200',
            warning: 'text-amber-800 dark:text-amber-200',
            info: 'text-blue-800 dark:text-blue-200',
        };
        return map[type] ?? map['info'];
    }

    getMessageClass(type: string): string {
        const map: Record<string, string> = {
            success: 'text-green-700 dark:text-green-300',
            error: 'text-red-700 dark:text-red-300',
            warning: 'text-amber-700 dark:text-amber-300',
            info: 'text-blue-700 dark:text-blue-300',
        };
        return map[type] ?? map['info'];
    }

    getCloseBtnClass(type: string): string {
        const map: Record<string, string> = {
            success: 'text-green-700 hover:bg-green-100',
            error: 'text-red-700 hover:bg-red-100',
            warning: 'text-amber-700 hover:bg-amber-100',
            info: 'text-blue-700 hover:bg-blue-100',
        };
        return map[type] ?? map['info'];
    }
}
