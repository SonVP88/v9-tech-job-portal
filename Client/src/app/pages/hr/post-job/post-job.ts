import { CommonModule } from '@angular/common';
import { Component, OnInit, ChangeDetectorRef, NgZone } from '@angular/core';
import { FormBuilder, FormGroup, Validators, ReactiveFormsModule, ValidatorFn, AbstractControl, ValidationErrors } from '@angular/forms';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';

import { BehaviorSubject, forkJoin } from 'rxjs';

import { MasterDataService, JobType, Skill, Province, Ward, DepartmentLookup } from '../../../services/master-data.service';
import { JobService, CreateJobRequest, AiGenerateJdRequest, CampaignQrDto } from '../../../services/job.service';
import { ToastService } from '../../../services/toast.service';
import { PopupService } from '../../../services/popup.service';
import { CampaignQrModalComponent } from '../../../components/hr/campaign-qr-modal/campaign-qr-modal';

@Component({
  selector: 'app-post-job',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, FormsModule, CampaignQrModalComponent],
  templateUrl: './post-job.html',
  styleUrl: './post-job.scss',
})
export class PostJob implements OnInit {
  jobForm!: FormGroup;
  jobTypes: JobType[] = [];
  departments: DepartmentLookup[] = [];
  skills: Skill[] = [];
  readonly employmentTypeOptions = [
    { value: 'full-time', label: 'Toàn thời gian' },
    { value: 'shift', label: 'Theo ca' },
    { value: 'seasonal', label: 'Thời vụ' },
    { value: 'contract', label: 'Hợp đồng ngắn hạn' }
  ];
  readonly experienceOptions: string[] = [
    'Chưa có kinh nghiệm',
    'Dưới 6 tháng',
    '6-12 tháng',
    '1-2 năm',
    'Trên 2 năm'
  ];
  readonly shiftOptions: string[] = [
    'Ca hành chính',
    'Ca sáng',
    'Ca chiều',
    'Ca đêm',
    'Xoay 3 ca'
  ];

  // Sử dụng BehaviorSubject để lưu state và support AsyncPipe
  provinces$ = new BehaviorSubject<Province[]>([]);
  wards$ = new BehaviorSubject<Ward[]>([]);

  selectedSkillIds: string[] = [];
  selectedProvinceCode = 0;
  isSubmitting = false;
  isGeneratingJd = false;
  skillsError = false;
  keyRequirements = '';
  showAdvancedFields = false;

  isEditMode = false;
  jobId: string | null = null;
  campaignQr: CampaignQrDto | null = null;
  regeneratingQr = false;

  constructor(
    private fb: FormBuilder,
    private masterDataService: MasterDataService,
    private jobService: JobService,
    private cdr: ChangeDetectorRef,
    private route: ActivatedRoute,
    private router: Router,
    private ngZone: NgZone,
    private toast: ToastService,
    private popup: PopupService
  ) { }

  ngOnInit(): void {
    this.initForm();
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

  toggleAdvancedFields(): void {
    this.showAdvancedFields = !this.showAdvancedFields;
  }

  /**
   * Khởi tạo Reactive Form với các validators
   */
  private initForm(): void {
    this.jobForm = this.fb.group({
      title: ['', [Validators.required, Validators.minLength(10), Validators.maxLength(200)]],
      departmentId: ['', Validators.required],
      employmentType: ['', Validators.required],
      experienceLevel: ['', Validators.required],
      seniorityLevel: ['', Validators.required],
      province: [null, [Validators.required, Validators.min(1)]],
      ward: [null],
      salaryMin: [null, [Validators.min(0)]],
      salaryMax: [null, [Validators.min(0)]],
      deadline: [null, [Validators.required, this.futureDateValidator]],
      description: ['', [Validators.required, Validators.minLength(50), Validators.maxLength(4000)]],
      requirements: ['', [Validators.maxLength(4000)]],
      benefits: ['', [Validators.maxLength(4000)]],
      numberOfPositions: [null, [Validators.min(1)]],
      createCampaignQr: [true],
      requireNightShift: [false],
      requisitionNote: [''],
      recruitmentTrack: ['PROFESSIONAL'],
      targetQuantity: [50]
    }, { validators: this.salaryRangeValidator });
  }

  // Custom Validator cho Ngày hết hạn
  private futureDateValidator: ValidatorFn = (control: AbstractControl): ValidationErrors | null => {
    const dateStr = control.value;
    if (!dateStr) return null;

    const selectedDate = new Date(dateStr);
    const today = new Date();
    today.setHours(0, 0, 0, 0); // Reset time to midnight

    if (selectedDate <= today) {
      return { futureDate: true };
    }
    return null;
  };

  // Custom Validator cho Khoảng lương
  private salaryRangeValidator: ValidatorFn = (control: AbstractControl): ValidationErrors | null => {
    const min = control.get('salaryMin')?.value;
    const max = control.get('salaryMax')?.value;

    if (min != null && max != null && max < min) {
      if (control.get('salaryMax')) {
        control.get('salaryMax')?.setErrors({ ...control.get('salaryMax')?.errors, salaryRange: true });
      }
      return { salaryRange: true };
    }

    if (control.get('salaryMax')?.hasError('salaryRange')) {
      const errors = { ...control.get('salaryMax')?.errors };
      delete (errors as any).salaryRange;
      control.get('salaryMax')?.setErrors(Object.keys(errors).length > 0 ? errors : null);
    }

    return null;
  };

  /**
   * Load dữ liệu job để edit
   */
  private loadJobData(id: string): void {
    this.jobService.getJobById(id).subscribe({
      next: (job) => {
        // Patch form values
        this.jobForm.patchValue({
          title: job.title,
          departmentId: job.departmentId || '',
          employmentType: job.employmentType,
          experienceLevel: job.experienceLevel || this.extractExperienceLevel(job.requirements),
          seniorityLevel: job.seniorityLevel || this.extractShiftFromRequirements(job.requirements) || this.extractSeniorityFromTitle(job.title),
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

            this.provinces$.subscribe(provinces => {
              if (provinces.length > 0) {
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
        this.toast.error('Lỗi tải dữ liệu', 'Không thể tải thông tin công việc.');
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
        departments: this.masterDataService.getDepartments(),
        skills: this.masterDataService.getSkills(),
        provinces: this.masterDataService.getProvinces()
      }).subscribe({
        next: (result) => {
          // Cập nhật data trong Angular zone (trigger CD 1 lần duy nhất)
          this.ngZone.run(() => {
            this.jobTypes = result.jobTypes;
            this.departments = result.departments;
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
            this.departments = [];
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

    // Validate lại errors
    this.skillsError = this.selectedSkillIds.length === 0;
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
    if (this.selectedProvinceCode === provinceCode) return;

    this.wards$.next([]);
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

    this.skillsError = this.selectedSkillIds.length === 0;

    // Validate form
    if (this.jobForm.invalid || this.skillsError) {
      // Đánh dấu tất cả các field là touched để hiển thị lỗi
      Object.keys(this.jobForm.controls).forEach(key => {
        this.jobForm.get(key)?.markAsTouched();
      });

      this.toast.warning('Thiếu thông tin', 'Vui lòng điền đầy đủ thông tin bắt buộc hoặc kiểm tra lại các lỗi.');
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
      departmentId: formValue.departmentId || undefined,
      description: formValue.description,
      requirements: formValue.requirements,
      benefits: formValue.benefits,
      numberOfPositions: formValue.numberOfPositions ? Number(formValue.numberOfPositions) : undefined,
      salaryMin: formValue.salaryMin ? Number(formValue.salaryMin) : undefined,
      salaryMax: formValue.salaryMax ? Number(formValue.salaryMax) : undefined,
      location: fullAddress,  // Lưu địa chỉ đầy đủ: "Phường X, Tỉnh Y" (V2 API)
      employmentType: formValue.employmentType,
      experienceLevel: formValue.experienceLevel,
      seniorityLevel: formValue.seniorityLevel,
      deadline: formValue.deadline ? `${formValue.deadline}T12:00:00Z` : undefined,
      skillIds: this.selectedSkillIds,
      createCampaignQr: formValue.createCampaignQr !== false,
      requireNightShift: !!formValue.requireNightShift,
      requisitionNote: formValue.requisitionNote || '',
      recruitmentTrack: formValue.recruitmentTrack || 'PROFESSIONAL',
      targetQuantity: formValue.targetQuantity ? Number(formValue.targetQuantity) : 50
    };

    this.isSubmitting = true;

    if (this.isEditMode && this.jobId) {
      // Update
      this.jobService.updateJob(this.jobId, jobData).subscribe({
        next: () => {
          this.toast.success('Thành công', 'Cập nhật tin tuyển dụng thành công!');
          this.router.navigate(['/hr/jobs']);
        },
        error: (error) => {
          console.error('Error updating job:', error);
          const errorMessage = error.error?.message || 'Có lỗi xảy ra khi cập nhật.';
          this.toast.error('Thất bại', errorMessage);
          this.isSubmitting = false;
        },
        complete: () => {
          this.isSubmitting = false;
        }
      });
    } else {
      // Create
      this.jobService.createJob(jobData).subscribe({
        next: (res) => {
          this.toast.success('Thành công', 'Đăng tin tuyển dụng thành công!');
          this.campaignQr = res.campaign || null;
          this.jobId = res.jobId || this.jobId;
          this.resetForm();
        },
        error: (error) => {
          console.error('Error creating job:', error);
          const errorMessage = error.error?.message || 'Đã xảy ra lỗi khi đăng tin tuyển dụng. Vui lòng thử lại!';
          this.toast.error('Thất bại', errorMessage);
          this.isSubmitting = false;
        },
        complete: () => {
          this.isSubmitting = false;
        }
      });
    }
  }

  /**
   * Gọi AI sinh nội dung JD tự động
   */
  onAiGenerateJd(): void {
    const title = this.jobForm.get('title')?.value?.trim();
    if (!title) {
      this.toast.warning('Thiếu thông tin', 'Vui lòng nhập Tiêu đề vị trí trước khi dùng AI gợi ý.');
      return;
    }

    const req: AiGenerateJdRequest = {
      title,
      level: this.jobForm.get('seniorityLevel')?.value || undefined,
      keyRequirements: this.keyRequirements?.trim() || undefined,
      salaryMin: this.jobForm.get('salaryMin')?.value || undefined,
      salaryMax: this.jobForm.get('salaryMax')?.value || undefined,
    };

    this.isGeneratingJd = true;
    this.jobService.generateJd(req).subscribe({
      next: (result) => {
        this.jobForm.patchValue({
          description: result.description,
          requirements: result.requirements,
          benefits: result.benefits,
        });
        this.toast.success('AI gợi ý thành công!', 'Nội dung đã được điền tự động. Bạn có thể chỉnh sửa thêm.');
        this.isGeneratingJd = false;
      },
      error: (err) => {
        const msg = err?.error?.message || 'Không thể kết nối AI. Vui lòng thử lại.';
        this.toast.error('AI thất bại', msg);
        this.isGeneratingJd = false;
      }
    });
  }

  /**
   * Reset form về trạng thái ban đầu
   */
  closeCampaignQr(): void {
    this.campaignQr = null;
  }

  regenerateCampaignQr(): void {
    const id = this.campaignQr?.jobId || this.jobId;
    if (!id) return;
    this.regeneratingQr = true;
    this.jobService.ensureCampaignQr(id, true).subscribe({
      next: (campaign) => {
        this.campaignQr = campaign;
        this.regeneratingQr = false;
      },
      error: (err) => {
        this.toast.error('Không tạo được QR', err.error?.message || 'Vui lòng thử lại.');
        this.regeneratingQr = false;
      }
    });
  }

  openExistingCampaignQr(): void {
    if (!this.jobId) return;
    this.jobService.getCampaignQr(this.jobId).subscribe({
      next: (campaign) => (this.campaignQr = campaign),
      error: (err) => this.toast.error('Không tải được QR', err.error?.message || 'Vui lòng thử lại.')
    });
  }

  resetForm(): void {
    this.jobForm.reset();
    this.selectedSkillIds = [];
    this.wards$.next([]);

    // Reset về giá trị mặc định cho các select
    this.jobForm.patchValue({
      departmentId: '',
      employmentType: '',
      experienceLevel: '',
      seniorityLevel: '',
      province: null,
      ward: null,
      createCampaignQr: true
    });
  }

  /**
   * Hủy và quay lại
   */
  async onCancel(): Promise<void> {
    const confirmed = await this.popup.confirm({
      title: 'Xác nhận hủy',
      message: 'Bạn có chắc muốn hủy? Tất cả dữ liệu đã nhập sẽ bị mất.',
      confirmText: 'Hủy',
      cancelText: 'Không',
      tone: 'danger',
    });
    if (confirmed) {
      if (this.isEditMode) {
        this.router.navigate(['/hr/jobs']);
      } else {
        this.resetForm();
      }
    }
  }

  private extractExperienceLevel(requirements?: string | null): string {
    if (!requirements) return '';

    const lines = requirements
      .split('\n')
      .map(line => line.trim())
      .filter(Boolean);

    const expLine = lines.find(line => /^Kinh nghiệm\s*:/i.test(line));
    if (!expLine) return '';

    const value = expLine.replace(/^Kinh nghiệm\s*:/i, '').trim();
    return this.experienceOptions.includes(value) ? value : '';
  }

  private cleanRequirementsText(requirements?: string | null): string {
    if (!requirements) return '';

    return requirements
      .split('\n')
      .map(line => line.trim())
      .filter(line => line.length > 0 && !/^Kinh nghiệm\s*:/i.test(line))
      .join('\n')
      .trim();
  }

  private extractSeniorityFromTitle(title?: string | null): string {
    if (!title) return '';
    const normalized = title.toLowerCase();
    if (normalized.includes('intern') || normalized.includes('thực tập')) return 'Intern';
    if (normalized.includes('fresher')) return 'Fresher';
    if (normalized.includes('junior')) return 'Junior';
    if (normalized.includes('senior')) return 'Senior';
    if (normalized.includes('lead')) return 'Lead';
    if (normalized.includes('manager')) return 'Manager';
    if (normalized.includes('mid')) return 'Middle';
    return '';
  }

  private extractShiftFromRequirements(requirements?: string | null): string {
    if (!requirements) return '';

    const lines = requirements
      .split('\n')
      .map(line => line.trim())
      .filter(Boolean);

    const shiftLine = lines.find(line => /^Ca\s*làm\s*việc\s*:/i.test(line));
    if (!shiftLine) return '';

    const value = shiftLine.replace(/^Ca\s*làm\s*việc\s*:/i, '').trim();
    return this.shiftOptions.includes(value) ? value : '';
  }
}
