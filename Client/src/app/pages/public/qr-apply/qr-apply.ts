import { CommonModule } from '@angular/common';
import { HttpClient } from '@angular/common/http';
import { Component, OnInit, inject } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute } from '@angular/router';
import { environment } from '../../../../environments/environment';

@Component({
  selector: 'app-qr-apply',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule],
  templateUrl: './qr-apply.html'
})
export class QrApplyComponent implements OnInit {
  private route = inject(ActivatedRoute);
  private http = inject(HttpClient);
  private fb = inject(FormBuilder);

  jobId = '';
  jobTitle = '';
  departmentName = '';
  location = '';
  isOpen = true;
  loading = true;
  submitting = false;
  submitted = false;
  errorMessage = '';

  form = this.fb.group({
    fullName: ['', [Validators.required, Validators.minLength(3)]],
    phone: ['', [Validators.required, Validators.minLength(9)]],
    yearOfBirth: [''],
    hometown: [''],
    experienceNotes: [''],
    shiftReady: ['Có', Validators.required],
    email: ['']
  });

  ngOnInit(): void {
    this.jobId = this.route.snapshot.paramMap.get('jobId') || '';
    if (!this.jobId) {
      this.loading = false;
      this.errorMessage = 'Thiếu mã chiến dịch.';
      return;
    }

    this.http.get<any>(`${environment.apiUrl}/public/jobs/${this.jobId}/qr-campaign`).subscribe({
      next: (info) => {
        this.jobTitle = info.title;
        this.departmentName = info.departmentName;
        this.location = info.location;
        this.isOpen = info.isOpen;
        this.loading = false;
      },
      error: () => {
        this.loading = false;
        this.errorMessage = 'Không tìm thấy chiến dịch tuyển dụng.';
      }
    });
  }

  submit(): void {
    if (this.form.invalid || !this.isOpen) {
      this.form.markAllAsTouched();
      return;
    }
    this.submitting = true;
    this.errorMessage = '';
    this.http.post<any>(`${environment.apiUrl}/public/jobs/${this.jobId}/qr-apply`, {
      jobId: this.jobId,
      ...this.form.value
    }).subscribe({
      next: () => {
        this.submitted = true;
        this.submitting = false;
      },
      error: (err) => {
        this.submitting = false;
        this.errorMessage = err.error?.message || 'Không gửi được hồ sơ. Vui lòng thử lại.';
      }
    });
  }
}
