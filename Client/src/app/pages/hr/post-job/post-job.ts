import { CommonModule } from '@angular/common';
import { Component, OnInit, ChangeDetectorRef, NgZone } from '@angular/core';
import { FormBuilder, FormGroup, Validators, ReactiveFormsModule } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';

import { BehaviorSubject, of, forkJoin } from 'rxjs';
import { tap, catchError, finalize } from 'rxjs/operators';

import { MasterDataService, JobType, Skill, Province, Ward } from '../../../services/master-data.service';
import { JobService, CreateJobRequest } from '../../../services/job.service';

@Component({
  selector: 'app-post-job',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule],
  templateUrl: './post-job.html',
  styleUrl: './post-job.scss',
})
export class PostJob implements OnInit {
  jobForm!: FormGroup;
  jobTypes: JobType[] = [];
  skills: Skill[] = [];

  // Sử dụng BehaviorSubject để lưu state và support AsyncPipe
  provinces$ = new BehaviorSubject<Province[]>([]);
  wards$ = new BehaviorSubject<Ward[]>([]);

  selectedSkillIds: string[] = [];
  selectedProvinceCode: number = 0;
  isSubmitting = false;

  isEditMode = false;
  jobId: string | null = null;

  constructor(
    private fb: FormBuilder,
    private masterDataService: MasterDataService,
    private jobService: JobService,
    private cdr: ChangeDetectorRef,
    private route: ActivatedRoute,
    private router: Router,
    private ngZone: NgZone
  ) { }

  ngOnInit(): void {
    // Khởi tạo form với validation
    this.initForm();

    // Gọi API để lấy dữ liệu Master Data
    this.loadMasterData();

    // Listen to province changes to reset/load wards
    this.jobForm.get('province')?.valueChanges.subscribe(code => {
      this.onProvinceChange(code);
    });

    // Check query params for edit mode
    const id = this.route.snapshot.paramMap.get('id');
    if (id) {
      this.isEditMode = true;
      this.jobId = id;
      this.loadJobData(id);
    }
  }

  /**
   * Khởi tạo Reactive Form với các validators
   */
  private initForm(): void {
    this.jobForm = this.fb.group({
      title: ['', [Validators.required, Validators.minLength(3)]],
      employmentType: ['', Validators.required],
      province: [null, [Validators.required, Validators.min(1)]],
      ward: [null],
      salaryMin: [null],
      salaryMax: [null],
      deadline: [null],
      description: ['', [Validators.required, Validators.minLength(10)]],
      requirements: [''],
      benefits: [''],
      numberOfPositions: [null, [Validators.min(1)]]
    });
  }

  /**
   * Load dữ liệu job để edit
   */
  private loadJobData(id: string): void {
    this.jobService.getJobById(id).subscribe({
      next: (job) => {
        // Patch form values
        this.jobForm.patchValue({
          title: job.title,
          employmentType: job.employmentType,
          salaryMin: job.salaryMin,
          salaryMax: job.salaryMax,
          description: job.description,
          requirements: job.requirements,
          benefits: job.benefits,
          numberOfPositions: job.numberOfPositions,
          deadline: job.deadline ? job.deadline.substring(0, 10) : null
        });

        // Set selected skills
        if (job.skillIds) {
          this.selectedSkillIds = job.skillIds;
        }

        // Try to parse location: "WardName, ProvinceName"
        if (job.location) {
          const parts = job.location.split(',').map(p => p.trim());
          if (parts.length >= 2) {
            const provinceName = parts[parts.length - 1]; // Last part is province
            const wardName = parts[parts.length - 2]; // Second to last is ward (usually)

            // Find province in loaded provinces (might not be loaded yet, wait for it)
            // Since master stats load async, we might need a better way.
            // But usually master data is fast. Let's subscribe to provinces$
            this.provinces$.subscribe(provinces => {
              if (provinces.length > 0) {
                // Find province by name (ignore case or minor diffs?)
                // Simple check
                const province = provinces.find(p => p.name.includes(provinceName) || provinceName.includes(p.name));
                if (province) {
                  this.jobForm.patchValue({ province: province.code });

                  // Load wards and then find ward
                  this.masterDataService.getWards(province.code).subscribe(wards => {
                    this.wards$.next(wards);
                    const ward = wards.find(w => w.name.includes(wardName) || wardName.includes(w.name));
                    if (ward) {
                      this.jobForm.patchValue({ ward: ward.code });
                    }
                  });
                }
              }
            });
          }
        }
      },
      error: (err) => {
        console.error('Error loading job:', err);
        alert('Không thể tải thông tin công việc.');
        this.router.navigate(['/hr/jobs']);
      }
    });
  }

  /**
   * Load tất cả Master Data (tối ưu với NgZone)
   */
  private loadMasterData(): void {
    // Chạy bên ngoài Angular zone để tránh trigger CD nhiều lần
    this.ngZone.runOutsideAngular(() => {
      // Load tất cả data song song bằng forkJoin
      forkJoin({
        jobTypes: this.masterDataService.getJobTypes(),
        skills: this.masterDataService.getSkills(),
        provinces: this.masterDataService.getProvinces()
      }).subscribe({
        next: (result) => {
          // Cập nhật data trong Angular zone (trigger CD 1 lần duy nhất)
          this.ngZone.run(() => {
            this.jobTypes = result.jobTypes;
            this.skills = result.skills;
            this.provinces$.next(result.provinces);
            this.cdr.detectChanges();
          });
        },
        error: (error) => {
          console.error('Error loading master data:', error);
          // Fallback: Set empty arrays
          this.ngZone.run(() => {
            this.jobTypes = [];
            this.skills = [];
            this.provinces$.next([]);
            this.cdr.detectChanges();
          });
        }
      });
    });
  }

  /**
   * Toggle skill selection - Thêm hoặc xóa skill ID khỏi mảng selectedSkillIds
   */
  toggleSkill(skillId: string): void {
    const index = this.selectedSkillIds.indexOf(skillId);

    if (index > -1) {
      // Skill đã được chọn -> Xóa khỏi mảng
      this.selectedSkillIds.splice(index, 1);
    } else {
      // Skill chưa được chọn -> Thêm vào mảng
      this.selectedSkillIds.push(skillId);
    }
  }

  /**
   * Kiểm tra xem skill có được chọn hay không
   */
  isSkillSelected(skillId: string): boolean {
    return this.selectedSkillIds.includes(skillId);
  }

  /**
   * Xử lý khi chọn tỉnh/thành phố - V2 API load wards trực tiếp
   */
  onProvinceChange(provinceCode: any): void {
    // console.log('🔍 Province change:', provinceCode);

    // Only reset wards if changed by user manually (interactive)
    // But valueChanges fires on patchValue too.
    // If we are in loadJobData, we might patch province then ward.
    // We need to be careful not to clear ward immediately if it was just patched?
    // Actually default behavior: clearing ward is fine, we just reload wards.

    // But if we patch province, this triggers. Then we load wards.
    // Then we patch ward.
    // So distinct is important.

    if (this.selectedProvinceCode === provinceCode) return;

    this.wards$.next([]);
    // this.jobForm.patchValue({ ward: null }); // Don't clear immediately if it might be patched next?
    // Let's clear it, re-patching will handle it.
    this.jobForm.get('ward')?.setValue(null, { emitEvent: false }); // Avoid loop

    this.selectedProvinceCode = provinceCode ? Number(provinceCode) : 0;

    if (this.selectedProvinceCode) {
      this.loadWards(this.selectedProvinceCode);
    }
  }

  /**
   * Load danh sách phường/xã theo mã tỉnh (V2 API - bỏ cấp huyện)
   */
  private loadWards(provinceCode: number): void {
    this.masterDataService.getWards(provinceCode).subscribe({
      next: (data) => {
        this.wards$.next(data);
        this.cdr.detectChanges();
      },
      error: (error) => {
        console.error('Error loading wards:', error);
        this.wards$.next([]);
      }
    });
  }

  /**
   * Xử lý submit form
   */
  onSubmit(event: Event): void {
    event.preventDefault();

    // Validate form
    if (this.jobForm.invalid) {
      // Đánh dấu tất cả các field là touched để hiển thị lỗi
      Object.keys(this.jobForm.controls).forEach(key => {
        this.jobForm.get(key)?.markAsTouched();
      });

      alert('Vui lòng điền đầy đủ thông tin bắt buộc!');
      return;
    }

    // Chuẩn bị dữ liệu để gửi
    const formValue = this.jobForm.value;

    const provinceCode = Number(formValue.province);
    const wardCode = Number(formValue.ward);

    const provinceName = this.provinces$.value.find(p => p.code === provinceCode)?.name || '';
    const wardName = this.wards$.value.find(w => w.code === wardCode)?.name || '';

    const fullAddress = [wardName, provinceName].filter(Boolean).join(', ');

    const jobData: CreateJobRequest = {
      title: formValue.title,
      description: formValue.description,
      requirements: formValue.requirements,
      benefits: formValue.benefits,
      numberOfPositions: formValue.numberOfPositions ? Number(formValue.numberOfPositions) : undefined,
      salaryMin: formValue.salaryMin ? Number(formValue.salaryMin) : undefined,
      salaryMax: formValue.salaryMax ? Number(formValue.salaryMax) : undefined,
      location: fullAddress,  // Lưu địa chỉ đầy đủ: "Phường X, Tỉnh Y" (V2 API)
      employmentType: formValue.employmentType,
      deadline: formValue.deadline ? `${formValue.deadline}T12:00:00Z` : undefined,
      skillIds: this.selectedSkillIds
    };

    console.log('Submitting job data:', jobData);
    this.isSubmitting = true;

    if (this.isEditMode && this.jobId) {
      // Update
      this.jobService.updateJob(this.jobId, jobData).subscribe({
        next: (response) => {
          alert('Cập nhật tin thành công! 🎉');
          this.router.navigate(['/hr/jobs']);
        },
        error: (error) => {
          console.error('Error updating job:', error);
          alert('Có lỗi xảy ra khi cập nhật.');
          this.isSubmitting = false;
        },
        complete: () => {
          this.isSubmitting = false;
        }
      });
    } else {
      // Create
      this.jobService.createJob(jobData).subscribe({
        next: (response) => {
          alert('Đăng tin thành công! 🎉');

          // Reset form và selected skills
          this.resetForm();
        },
        error: (error) => {
          console.error('Error creating job:', error);
          const errorMessage = error.error?.message || 'Đã xảy ra lỗi khi đăng tin tuyển dụng. Vui lòng thử lại!';
          alert(`Lỗi: ${errorMessage}`);
          this.isSubmitting = false;
        },
        complete: () => {
          this.isSubmitting = false;
        }
      });
    }
  }

  /**
   * Reset form về trạng thái ban đầu
   */
  resetForm(): void {
    this.jobForm.reset();
    this.selectedSkillIds = [];
    this.wards$.next([]); // Clear wards subject

    // Reset về giá trị mặc định cho các select
    this.jobForm.patchValue({
      employmentType: '',
      province: null,
      ward: null
    });
  }

  /**
   * Hủy và quay lại
   */
  onCancel(): void {
    if (confirm('Bạn có chắc muốn hủy? Tất cả dữ liệu đã nhập sẽ bị mất.')) {
      if (this.isEditMode) {
        this.router.navigate(['/hr/jobs']);
      } else {
        this.resetForm();
      }
    }
  }
}
