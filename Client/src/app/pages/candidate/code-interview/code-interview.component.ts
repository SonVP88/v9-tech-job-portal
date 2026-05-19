import { Component, OnInit } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { CodeEditorPanelComponent } from '../../../components/hr/code-editor-panel/code-editor-panel.component';
import { CodeReviewReportComponent } from '../../../components/hr/code-review-report/code-review-report.component';
import { PadZeroPipe } from '../../../pipes/pad-zero.pipe';
import { AiCodeAssessmentService, CodeChallenge, AiCodeReview } from '../../../services/ai-code-assessment.service';

@Component({
  selector: 'app-code-interview',
  standalone: true,
  imports: [CommonModule, FormsModule, CodeEditorPanelComponent, CodeReviewReportComponent, PadZeroPipe],
  template: `
    <div class="code-interview-container">
      <div class="interview-header">
        <h1>{{ challenge?.title }}</h1>
        <div class="timer-info">
          <span class="time-remaining">⏱ {{ timeRemaining }}:{{ timeRemainingSeconds | padZero }}</span>
        </div>
      </div>

      <div class="problem-statement">
        <h2>Problem Statement</h2>
        <div class="statement-content">{{ challenge?.problemStatement }}</div>
        
        <div class="starter-code" *ngIf="challenge?.initialCodeText">
          <h3>Starter Code ({{ challenge?.language }})</h3>
          <pre><code>{{ challenge?.initialCodeText }}</code></pre>
        </div>
      </div>

      <div class="editor-section">
        <app-code-editor-panel 
          [initialCode]="challenge?.initialCodeText || ''"
          (codeSubmitted)="onCodeSubmitted($event)"
          #codeEditor
        ></app-code-editor-panel>
      </div>

      <div class="actions">
        <button (click)="onSubmit()" class="btn-submit" [disabled]="isSubmitting">
          {{ isSubmitting ? '⏳ Submitting...' : '✓ Submit Solution' }}
        </button>
      </div>

      <div class="review-section" *ngIf="reviewData">
        <app-code-review-report [review]="reviewData"></app-code-review-report>
      </div>

      <div class="loading-indicator" *ngIf="isReviewLoading">
        <div class="spinner"></div>
        <p>AI is reviewing your code...</p>
      </div>
    </div>
  `,
  styles: [`
    .code-interview-container {
      padding: 24px;
      background: linear-gradient(135deg, #1e293b 0%, #0f172a 100%);
      min-height: 100vh;
      font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, 'Helvetica Neue', Arial, sans-serif;
    }

    .interview-header {
      display: flex;
      justify-content: space-between;
      align-items: center;
      margin-bottom: 24px;
      background: linear-gradient(135deg, rgba(30, 41, 59, 0.8) 0%, rgba(15, 23, 42, 0.8) 100%);
      backdrop-filter: blur(10px);
      padding: 24px 32px;
      border-radius: 12px;
      border: 1px solid rgba(148, 163, 184, 0.1);
      box-shadow: 0 8px 32px rgba(0, 0, 0, 0.3);
    }

    .interview-header h1 {
      margin: 0;
      color: white;
      font-size: 28px;
      font-weight: 700;
    }

    .timer-info {
      font-size: 16px;
      font-weight: 600;
      color: #fbbf24;
      padding: 8px 16px;
      background: rgba(251, 191, 36, 0.1);
      border-radius: 8px;
      border: 1px solid rgba(251, 191, 36, 0.3);
    }

    .problem-statement {
      background: white;
      padding: 32px;
      border-radius: 12px;
      margin-bottom: 24px;
      box-shadow: 0 10px 30px rgba(0, 0, 0, 0.2);
      border: 1px solid #f0f4f8;
    }

    .problem-statement h2 {
      margin: 0 0 16px 0;
      color: #1f2937;
      font-size: 22px;
      font-weight: 700;
    }

    .difficulty-badge {
      display: inline-block;
      padding: 6px 12px;
      border-radius: 6px;
      font-size: 12px;
      font-weight: 600;
      text-transform: uppercase;
      letter-spacing: 0.5px;
      margin-bottom: 16px;
    }

    .difficulty-easy {
      background: #dcfce7;
      color: #15803d;
    }

    .difficulty-medium {
      background: #fef3c7;
      color: #b45309;
    }

    .difficulty-hard {
      background: #fee2e2;
      color: #b91c1c;
    }

    .statement-content {
      color: #4b5563;
      line-height: 1.8;
      margin-bottom: 20px;
      font-size: 15px;
    }

    .examples {
      margin-top: 20px;
      padding-top: 20px;
      border-top: 1px solid #f0f4f8;
    }

    .example {
      margin-bottom: 16px;
    }

    .example h4 {
      color: #1f2937;
      margin: 0 0 8px 0;
      font-size: 13px;
      font-weight: 600;
      text-transform: uppercase;
      letter-spacing: 0.5px;
    }

    .example-code {
      background: #f8fafc;
      padding: 12px;
      border-radius: 6px;
      border: 1px solid #e2e8f0;
      font-family: 'Fira Code', 'Courier New', monospace;
      font-size: 12px;
      color: #1f2937;
      overflow-x: auto;
    }

    .editor-section {
      background: #1e293b;
      border-radius: 12px;
      margin-bottom: 24px;
      overflow: hidden;
      box-shadow: 0 10px 30px rgba(0, 0, 0, 0.3);
      border: 1px solid rgba(148, 163, 184, 0.1);
    }

    .actions {
      display: flex;
      gap: 16px;
      margin-bottom: 24px;
    }

    .btn-submit {
      padding: 14px 32px;
      background: linear-gradient(135deg, #10b981 0%, #059669 100%);
      color: white;
      border: none;
      border-radius: 8px;
      font-size: 15px;
      font-weight: 700;
      cursor: pointer;
      transition: all 0.3s ease;
      flex: 1;
      text-transform: uppercase;
      letter-spacing: 0.5px;
      box-shadow: 0 4px 15px rgba(16, 185, 129, 0.3);
    }

    .btn-submit:hover:not(:disabled) {
      background: linear-gradient(135deg, #059669 0%, #047857 100%);
      box-shadow: 0 6px 20px rgba(16, 185, 129, 0.4);
      transform: translateY(-2px);
    }

    .btn-submit:disabled {
      background: #9ca3af;
      cursor: not-allowed;
      box-shadow: none;
    }

    .review-section {
      margin-bottom: 24px;
    }

    .loading-indicator {
      display: flex;
      flex-direction: column;
      align-items: center;
      justify-content: center;
      padding: 80px 24px;
      background: rgba(255, 255, 255, 0.05);
      backdrop-filter: blur(10px);
      border-radius: 12px;
      border: 1px solid rgba(148, 163, 184, 0.1);
    }

    .spinner {
      width: 50px;
      height: 50px;
      border: 4px solid rgba(148, 163, 184, 0.2);
      border-top: 4px solid #6366f1;
      border-radius: 50%;
      animation: spin 1s linear infinite;
      margin-bottom: 20px;
    }

    @keyframes spin {
      0% { transform: rotate(0deg); }
      100% { transform: rotate(360deg); }
    }

    .loading-indicator p {
      color: rgba(226, 232, 240, 0.8);
      font-size: 16px;
      font-weight: 500;
    }

    @media (max-width: 1400px) {
      .code-interview-container {
        padding: 16px;
      }

      .interview-header {
        flex-direction: column;
        gap: 16px;
        align-items: flex-start;
      }

      .problem-statement {
        padding: 24px;
      }
    }
  `]
})
export class CodeInterviewComponent implements OnInit {
  challenge: CodeChallenge | null = null;
  reviewData: AiCodeReview | null = null;
  isSubmitting = false;
  isReviewLoading = false;
  timeRemaining = 60;
  timeRemainingSeconds = 0;

  constructor(
    private route: ActivatedRoute,
    private router: Router,
    private codeService: AiCodeAssessmentService
  ) {}

  ngOnInit() {
    this.loadChallenge();
    this.startTimer();
  }

  loadChallenge() {
    const challengeId = this.route.snapshot.paramMap.get('id');
    if (challengeId) {
      this.codeService.getChallenge(challengeId).subscribe(
        res => {
          this.challenge = res.data;
        },
        err => console.error('Error loading challenge:', err)
      );
    }
  }

  startTimer() {
    setInterval(() => {
      if (this.timeRemaining > 0 || this.timeRemainingSeconds > 0) {
        if (this.timeRemainingSeconds === 0) {
          this.timeRemaining--;
          this.timeRemainingSeconds = 59;
        } else {
          this.timeRemainingSeconds--;
        }
      }
    }, 1000);
  }

  onCodeSubmitted(code: string) {
    // Handle code submission
  }

  onSubmit() {
    this.isSubmitting = true;
    // Simulate submission
    setTimeout(() => {
      this.isSubmitting = false;
      this.isReviewLoading = true;
      
      // Simulate review loading
      setTimeout(() => {
        this.reviewData = {
          reviewId: 'review-123',
          submissionId: 'sub-123',
          totalScore: 85,
          timeComplexity: 'O(N)',
          spaceComplexity: 'O(1)',
          codeSmells: ['Magic numbers used', 'Variable names unclear'],
          securityVulnerabilities: [],
          overallFeedback: 'Good approach but lacking edge case handling.',
          suggestions: ['Extract method to reduce complexity', 'Add null checks'],
          aiProvider: 'GROQ_LLAMA3_70B'
        };
        this.isReviewLoading = false;
      }, 3000);
    }, 500);
  }
}
