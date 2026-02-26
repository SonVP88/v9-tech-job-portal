import { CommonModule } from '@angular/common';
import { Component } from '@angular/core';
import { Router } from '@angular/router';
import { OfferModalComponent } from '../offer-modal/offer-modal';

@Component({
  selector: 'app-candidate-detail',
  standalone: true,
  imports: [CommonModule, OfferModalComponent],
  templateUrl: './candidate-detail.html',
  styleUrl: './candidate-detail.scss',
})
export class CandidateDetail {
  // Candidate data (initialize from Router state or default mock)
  candidate: any;

  constructor(private router: Router) {
    const navigation = this.router.getCurrentNavigation();
    const state = navigation?.extras.state as { candidate: any };

    if (state && state.candidate) {
      console.log('📄 Candidate data received from router:', state.candidate);
      this.candidate = {
        ...state.candidate,
        name: state.candidate.candidateName, // Map fields if needed
        position: state.candidate.jobTitle || 'Unknown Position',
        appliedDate: new Date(state.candidate.appliedAt).toLocaleDateString('vi-VN'),
        status: state.candidate.status,
        email: state.candidate.email,
        phone: state.candidate.phone,
        // Keep other mock fields for demo purposes if real data is missing
        summary: 'Ứng viên chưa cập nhật giới thiệu.',
        skills: ['Chưa cập nhật'],
        experience: [],
        education: [],
        files: []
      };
    } else {
      // Fallback to mock data
      this.candidate = {
        name: 'Nguyễn Văn A',
        position: 'Frontend Developer',
        location: 'Hà Nội',
        appliedDate: '15/01/2026',
        status: 'Pending',
        email: 'nguyenvana@example.com',
        phone: '+84 123 456 789',
        summary: 'Full-stack developer với 5 năm kinh nghiệm...',
        skills: ['Angular', 'TypeScript'],
        experience: [],
        education: [],
        files: []
      };
    }
  }

  // ==================== Offer Modal ====================
  showOfferModal: boolean = false;

  onOpenOffer(): void {
    this.showOfferModal = true;
  }

  onOfferClosed(): void {
    this.showOfferModal = false;
    // TODO: Reload candidate data nếu cần
  }

  onOfferSent(payload: any): void {
    console.log(' Offer sent successfully:', payload);
    this.onOfferClosed();
  }

  // ==================== Existing Methods ====================
  goBack(): void {
    this.router.navigate(['/hr/manage-applications']);
  }

  rejectCandidate(): void {
    // TODO: Implement reject logic
    console.log('Reject candidate:', this.candidate.name);
  }

  editProfile(): void {
    // TODO: Implement edit logic
    console.log('Edit profile:', this.candidate.name);
  }

  sendEmail(): void {
    // TODO: Implement send email logic
    console.log('Send email to:', this.candidate.email);
  }
}
