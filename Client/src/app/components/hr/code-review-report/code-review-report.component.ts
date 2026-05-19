import { Component, Input } from '@angular/core';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-code-review-report',
  standalone: true,
  imports: [CommonModule],
  template: `
    <div class="review-report" *ngIf="review">
      <div class="report-header">
        <h2>AI Code Review - Groq Llama-3</h2>
        <div class="score-badge" [ngClass]="'score-' + getScoreLevel()">
          Score: {{ review.totalScore }}/100
        </div>
      </div>

      <div class="metrics-grid">
        <div class="metric-item">
          <label>Time Complexity</label>
          <div class="metric-value">{{ review.timeComplexity }}</div>
        </div>
        <div class="metric-item">
          <label>Space Complexity</label>
          <div class="metric-value">{{ review.spaceComplexity }}</div>
        </div>
      </div>

      <div class="section">
        <h3>Overall Feedback</h3>
        <div class="feedback-text">{{ review.overallFeedback }}</div>
      </div>

      <div class="section" *ngIf="review.codeSmells.length > 0">
        <h3>⚠️ Code Smells Detected</h3>
        <ul class="items-list">
          <li *ngFor="let smell of review.codeSmells">{{ smell }}</li>
        </ul>
      </div>

      <div class="section" *ngIf="review.securityVulnerabilities.length > 0">
        <h3>🔒 Security Issues</h3>
        <ul class="items-list warning">
          <li *ngFor="let vuln of review.securityVulnerabilities">{{ vuln }}</li>
        </ul>
      </div>

      <div class="section" *ngIf="review.suggestions.length > 0">
        <h3>💡 Suggestions for Improvement</h3>
        <ul class="items-list success">
          <li *ngFor="let suggestion of review.suggestions">{{ suggestion }}</li>
        </ul>
      </div>
    </div>
  `,
  styles: [`
    .review-report {
      background: white;
      border-radius: 16px;
      padding: 32px;
      box-shadow: 0 10px 30px rgba(0, 0, 0, 0.08);
      border: 1px solid #f0f4f8;
    }

    .report-header {
      display: flex;
      justify-content: space-between;
      align-items: center;
      margin-bottom: 32px;
      padding-bottom: 20px;
      border-bottom: 2px solid #f0f4f8;
      gap: 16px;
    }

    .report-header h2 {
      margin: 0;
      color: #1f2937;
      font-size: 22px;
      font-weight: 700;
      flex: 1;
    }

    .score-badge {
      padding: 10px 20px;
      border-radius: 8px;
      font-weight: 700;
      font-size: 15px;
      text-transform: uppercase;
      letter-spacing: 0.5px;
      display: inline-block;
      box-shadow: 0 4px 12px rgba(0, 0, 0, 0.1);
    }

    .score-excellent {
      background: linear-gradient(135deg, #d1fae5 0%, #a7f3d0 100%);
      color: #065f46;
    }

    .score-good {
      background: linear-gradient(135deg, #dbeafe 0%, #bfdbfe 100%);
      color: #0c4a6e;
    }

    .score-average {
      background: linear-gradient(135deg, #fef3c7 0%, #fde68a 100%);
      color: #b45309;
    }

    .score-poor {
      background: linear-gradient(135deg, #fee2e2 0%, #fecaca 100%);
      color: #b91c1c;
    }

    .metrics-grid {
      display: grid;
      grid-template-columns: repeat(auto-fit, minmax(200px, 1fr));
      gap: 16px;
      margin-bottom: 32px;
    }

    .metric-item {
      background: linear-gradient(135deg, #f8fafc 0%, #f1f5f9 100%);
      padding: 20px;
      border-radius: 12px;
      border: 1px solid #e2e8f0;
      transition: all 0.3s ease;
    }

    .metric-item:hover {
      border-color: #cbd5e1;
      box-shadow: 0 4px 12px rgba(0, 0, 0, 0.05);
    }

    .metric-item label {
      display: block;
      font-size: 12px;
      color: #64748b;
      margin-bottom: 12px;
      font-weight: 700;
      text-transform: uppercase;
      letter-spacing: 0.5px;
    }

    .metric-value {
      font-size: 20px;
      font-weight: 700;
      color: #1f2937;
      font-family: 'Fira Code', 'Courier New', monospace;
      background: white;
      padding: 10px;
      border-radius: 8px;
      border: 1px solid #e2e8f0;
    }

    .section {
      margin-bottom: 28px;
    }

    .section h3 {
      margin: 0 0 16px 0;
      color: #1f2937;
      font-size: 16px;
      font-weight: 700;
      display: flex;
      align-items: center;
      gap: 8px;
    }

    .feedback-text {
      color: #4b5563;
      line-height: 1.8;
      padding: 16px;
      background: linear-gradient(135deg, #f8fafc 0%, #f1f5f9 100%);
      border-left: 4px solid #6366f1;
      border-radius: 8px;
      font-size: 15px;
    }

    .items-list {
      list-style: none;
      padding: 0;
      margin: 0;
      display: flex;
      flex-direction: column;
      gap: 10px;
    }

    .items-list li {
      padding: 12px 16px;
      background: #f8f9fa;
      border-radius: 8px;
      color: #1f2937;
      font-size: 14px;
      border-left: 4px solid #cbd5e1;
      transition: all 0.3s ease;
    }

    .items-list li:hover {
      transform: translateX(4px);
      box-shadow: 0 4px 12px rgba(0, 0, 0, 0.05);
    }

    .items-list.warning li {
      background: linear-gradient(135deg, #fef3c7 0%, #fef08a 100%);
      border-left-color: #fbbf24;
      color: #b45309;
    }

    .items-list.success li {
      background: linear-gradient(135deg, #d1fae5 0%, #ccfbf1 100%);
      border-left-color: #10b981;
      color: #065f46;
    }

    @media (max-width: 768px) {
      .review-report {
        padding: 24px;
      }

      .report-header {
        flex-direction: column;
        align-items: flex-start;
      }

      .metrics-grid {
        grid-template-columns: 1fr;
      }
    }
  `]
})
export class CodeReviewReportComponent {
  @Input() review: any;

  getScoreLevel(): string {
    if (this.review.totalScore >= 90) return 'excellent';
    if (this.review.totalScore >= 75) return 'good';
    if (this.review.totalScore >= 60) return 'average';
    return 'poor';
  }
}
