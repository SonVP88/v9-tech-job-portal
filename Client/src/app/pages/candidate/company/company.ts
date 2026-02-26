import { Component, OnInit, ChangeDetectorRef, NgZone } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router } from '@angular/router';
import { CompanyService, CompanyInfoDto } from '../../../services/company.service';

@Component({
    selector: 'app-company',
    standalone: true,
    imports: [CommonModule],
    templateUrl: './company.html',
    styleUrl: './company.scss'
})
export class CompanyComponent implements OnInit {
    companyInfo: CompanyInfoDto = {
        name: 'V9 Tech',
        website: 'https://jobportal.com',
        industry: 'Công nghệ thông tin',
        address: 'Hà Nội, Việt Nam',
        description: 'Nền tảng tuyển dụng hàng đầu Việt Nam, kết nối ứng viên tài năng với các cơ hội việc làm tốt nhất. Chúng tôi cam kết mang đến trải nghiệm tuyển dụng chuyên nghiệp, minh bạch và hiệu quả.',
        logoUrl: ''
    };


    values = [
        {
            icon: 'lightbulb',
            title: 'Đổi Mới',
            description: 'Chúng tôi luôn tìm kiếm những ý tưởng sáng tạo và giải pháp tiên tiến để giải quyết thách thức.'
        },
        {
            icon: 'groups',
            title: 'Làm Việc Nhóm',
            description: 'Hợp tác và hỗ trợ lẫn nhau là nền tảng để chúng tôi đạt được mục tiêu chung.'
        },
        {
            icon: 'star',
            title: 'Xuất Sắc',
            description: 'Cam kết mang đến chất lượng tốt nhất trong mọi dự án và dịch vụ của chúng tôi.'
        },
        {
            icon: 'verified',
            title: 'Chính Trực',
            description: 'Minh bạch, trung thực và có trách nhiệm trong mọi hành động và quyết định.'
        }
    ];

    benefits = [
        {
            icon: 'payments',
            title: 'Lương thưởng cạnh tranh',
            description: 'Mức lương hấp dẫn và thưởng theo hiệu suất công việc'
        },
        {
            icon: 'health_and_safety',
            title: 'Bảo hiểm toàn diện',
            description: 'BHYT, BHXH và bảo hiểm sức khỏe cao cấp cho nhân viên'
        },
        {
            icon: 'school',
            title: 'Đào tạo & phát triển',
            description: 'Cơ hội học hỏi, nâng cao kỹ năng và thăng tiến trong sự nghiệp'
        },
        {
            icon: 'beach_access',
            title: 'Nghỉ phép linh hoạt',
            description: 'Chế độ nghỉ phép hợp lý, work-life balance tốt'
        }
    ];

    constructor(
        private companyService: CompanyService,
        private router: Router,
        private cdr: ChangeDetectorRef,
        private ngZone: NgZone
    ) { }

    ngOnInit(): void {
        this.loadCompanyInfo();
    }

    private loadCompanyInfo(): void {
        this.companyService.getPublicCompanyInfo().subscribe({
            next: (data) => {
                if (data && data.name) {
                    this.ngZone.run(() => {
                        this.companyInfo = { ...this.companyInfo, ...data };
                        this.cdr.detectChanges();
                    });
                }
            },
            error: (err) => {
                console.error('[DEBUG] Company API Error:', err);
                console.log('Using fallback company info (API not available)');
            }
        });
    }

    navigateToJobs(): void {
        this.router.navigate(['/candidate/jobs']);
    }
}
