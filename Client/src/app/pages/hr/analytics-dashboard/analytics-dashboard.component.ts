import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MetricsCardComponent } from '../../../components/shared/metrics-card/metrics-card.component';

@Component({
  selector: 'app-analytics-dashboard',
  standalone: true,
  imports: [CommonModule, MetricsCardComponent],
  template: `
    <div class="analytics-container">
      <div class="page-header">
        <h1>Analytics & Insights</h1>
        <p>Real-time recruitment metrics powered by Groq AI</p>
      </div>

      <!-- Key Metrics -->
      <div class="metrics-section">
        <h2>Key Performance Indicators</h2>
        <div class="metrics-grid">
          <app-metrics-card
            title="Total Applications"
            [value]="metrics.totalApplications"
            label="This Month"
            icon="📝"
            color="blue"
            [trend]="12"
          ></app-metrics-card>

          <app-metrics-card
            title="Conversion Rate"
            [value]="metrics.conversionRate + '%'"
            label="Applicants → Hired"
            icon="📈"
            color="green"
            [trend]="8"
          ></app-metrics-card>

          <app-metrics-card
            title="Avg. Time to Hire"
            [value]="metrics.avgTimeToHire + ' days'"
            label="Current Pipeline"
            icon="⏱"
            color="orange"
            [trend]="-5"
          ></app-metrics-card>

          <app-metrics-card
            title="Pending Reviews"
            [value]="metrics.pendingReviews"
            label="Awaiting evaluation"
            icon="⏳"
            color="red"
            [trend]="3"
          ></app-metrics-card>
        </div>
      </div>

      <!-- Pipeline Analysis -->
      <div class="section">
        <h2>Pipeline Stage Breakdown</h2>
        <div class="pipeline-grid">
          <div class="stage-card" *ngFor="let stage of pipelineStages">
            <div class="stage-header">
              <h4>{{ stage.name }}</h4>
              <span class="count">{{ stage.count }}</span>
            </div>
            <div class="stage-bar">
              <div class="bar-fill" [style.width.%]="stage.percentage"></div>
            </div>
            <div class="stage-details">
              <span>{{ stage.percentage }}%</span>
              <span class="avg-time">Avg: {{ stage.avgTime }}d</span>
            </div>
          </div>
        </div>
      </div>

      <!-- AI Predictions -->
      <div class="section">
        <h2>AI Predictions (Groq Llama-3)</h2>
        <div class="predictions-grid">
          <div class="prediction-card">
            <h3>Offer Acceptance Rate</h3>
            <div class="prediction-value">{{ predictions.offerAcceptance }}%</div>
            <div class="confidence">Confidence: {{ predictions.offerConfidence }}%</div>
          </div>

          <div class="prediction-card">
            <h3>Candidate Dropout Risk</h3>
            <div class="prediction-value">{{ predictions.dropoutRisk }}%</div>
            <div class="confidence">Confidence: {{ predictions.dropoutConfidence }}%</div>
          </div>

          <div class="prediction-card">
            <h3>Time to Fill (Forecast)</h3>
            <div class="prediction-value">{{ predictions.timeToFill }} days</div>
            <div class="confidence">Confidence: {{ predictions.timeConfidence }}%</div>
          </div>
        </div>
      </div>

      <!-- Recent Activity -->
      <div class="section">
        <h2>Recent Activity</h2>
        <div class="activity-list">
          <div class="activity-item" *ngFor="let activity of activities">
            <span class="activity-icon">{{ activity.icon }}</span>
            <div class="activity-content">
              <p class="activity-text">{{ activity.text }}</p>
              <span class="activity-time">{{ activity.time }}</span>
            </div>
          </div>
        </div>
      </div>
    </div>
  `,
  styles: [`
    .analytics-container {
      padding: 32px;
      background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
      min-height: 100vh;
      font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, 'Helvetica Neue', Arial, sans-serif;
    }

    .page-header {
      margin-bottom: 40px;
      color: white;
    }

    .page-header h1 {
      margin: 0 0 8px 0;
      font-size: 36px;
      font-weight: 800;
      letter-spacing: -0.5px;
    }

    .page-header p {
      margin: 0;
      font-size: 16px;
      opacity: 0.95;
      font-weight: 500;
    }

    .section {
      background: white;
      border-radius: 16px;
      padding: 32px;
      margin-bottom: 28px;
      box-shadow: 0 10px 40px rgba(0, 0, 0, 0.15);
      border: 1px solid rgba(0, 0, 0, 0.05);
    }

    .section h2 {
      margin: 0 0 28px 0;
      color: #1f2937;
      font-size: 22px;
      font-weight: 700;
      border-bottom: 2px solid #f0f4f8;
      padding-bottom: 16px;
    }

    .metrics-section {
      margin-bottom: 0;
    }

    .metrics-grid {
      display: grid;
      grid-template-columns: repeat(auto-fit, minmax(260px, 1fr));
      gap: 24px;
    }

    .pipeline-grid {
      display: grid;
      grid-template-columns: repeat(auto-fit, minmax(200px, 1fr));
      gap: 16px;
    }

    .stage-card {
      background: linear-gradient(135deg, #f8fafc 0%, #f1f5f9 100%);
      padding: 20px;
      border-radius: 12px;
      border: 1px solid #e2e8f0;
      transition: all 0.3s ease;
    }

    .stage-card:hover {
      transform: translateY(-4px);
      box-shadow: 0 8px 20px rgba(0, 0, 0, 0.1);
      border-color: #cbd5e1;
    }

    .stage-header {
      display: flex;
      justify-content: space-between;
      align-items: center;
      margin-bottom: 14px;
    }

    .stage-header h4 {
      margin: 0;
      color: #1f2937;
      font-size: 14px;
      font-weight: 700;
    }

    .count {
      background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
      color: white;
      padding: 6px 12px;
      border-radius: 6px;
      font-size: 12px;
      font-weight: 700;
      text-transform: uppercase;
      letter-spacing: 0.5px;
      box-shadow: 0 4px 12px rgba(102, 126, 234, 0.3);
    }

    .stage-bar {
      width: 100%;
      height: 8px;
      background: #e5e7eb;
      border-radius: 4px;
      margin-bottom: 12px;
      overflow: hidden;
    }

    .bar-fill {
      height: 100%;
      background: linear-gradient(90deg, #10b981 0%, #059669 100%);
      transition: width 0.4s cubic-bezier(0.4, 0, 0.2, 1);
      border-radius: 4px;
    }

    .stage-details {
      display: flex;
      justify-content: space-between;
      font-size: 12px;
      color: #6b7280;
      font-weight: 600;
    }

    .avg-time {
      color: #9ca3af;
    }

    .predictions-grid {
      display: grid;
      grid-template-columns: repeat(auto-fit, minmax(220px, 1fr));
      gap: 20px;
    }

    .prediction-card {
      background: linear-gradient(135deg, #f8fafc 0%, #f1f5f9 100%);
      padding: 24px;
      border-radius: 12px;
      border: 1px solid #e2e8f0;
      text-align: center;
      transition: all 0.3s ease;
    }

    .prediction-card:hover {
      transform: translateY(-4px);
      box-shadow: 0 8px 20px rgba(0, 0, 0, 0.1);
    }

    .prediction-card h3 {
      margin: 0 0 14px 0;
      color: #6b7280;
      font-size: 13px;
      font-weight: 700;
      text-transform: uppercase;
      letter-spacing: 0.5px;
    }

    .prediction-value {
      font-size: 32px;
      font-weight: 800;
      background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
      -webkit-background-clip: text;
      -webkit-text-fill-color: transparent;
      background-clip: text;
      margin-bottom: 10px;
    }

    .confidence {
      font-size: 12px;
      color: #9ca3af;
      font-weight: 600;
    }

    .activity-list {
      display: flex;
      flex-direction: column;
      gap: 12px;
    }

    .activity-item {
      display: flex;
      gap: 16px;
      padding: 16px;
      background: linear-gradient(135deg, #f8fafc 0%, #f1f5f9 100%);
      border-radius: 10px;
      border-left: 4px solid #667eea;
      transition: all 0.3s ease;
    }

    .activity-item:hover {
      transform: translateX(4px);
      box-shadow: 0 4px 12px rgba(0, 0, 0, 0.08);
    }

    .activity-icon {
      font-size: 22px;
      line-height: 1.5;
    }

    .activity-content {
      flex: 1;
    }

    .activity-text {
      margin: 0 0 6px 0;
      color: #1f2937;
      font-size: 14px;
      font-weight: 500;
    }

    .activity-time {
      font-size: 12px;
      color: #9ca3af;
      font-weight: 500;
    }

    @media (max-width: 1024px) {
      .analytics-container {
        padding: 20px;
      }

      .page-header h1 {
        font-size: 28px;
      }

      .section {
        padding: 24px;
      }

      .metrics-grid {
        grid-template-columns: repeat(auto-fit, minmax(220px, 1fr));
        gap: 16px;
      }
    }

    @media (max-width: 768px) {
      .analytics-container {
        padding: 16px;
      }

      .page-header h1 {
        font-size: 24px;
      }

      .section h2 {
        font-size: 18px;
      }

      .metrics-grid {
        grid-template-columns: 1fr;
      }

      .pipeline-grid {
        grid-template-columns: 1fr;
      }

      .predictions-grid {
        grid-template-columns: 1fr;
      }
    }
  `]
})
export class AnalyticsDashboardComponent implements OnInit {
  metrics = {
    totalApplications: 247,
    conversionRate: 18.5,
    avgTimeToHire: 23,
    pendingReviews: 12
  };

  pipelineStages = [
    { name: 'Applied', count: 247, percentage: 100, avgTime: 0 },
    { name: 'Pre-screening', count: 156, percentage: 63, avgTime: 3 },
    { name: 'Assessment', count: 89, percentage: 36, avgTime: 5 },
    { name: 'Interview', count: 34, percentage: 14, avgTime: 7 },
    { name: 'Offer', count: 12, percentage: 5, avgTime: 2 },
    { name: 'Hired', count: 9, percentage: 3.6, avgTime: 1 }
  ];

  predictions = {
    offerAcceptance: 87,
    offerConfidence: 94,
    dropoutRisk: 12,
    dropoutConfidence: 89,
    timeToFill: 21,
    timeConfidence: 91
  };

  activities = [
    { icon: '✅', text: 'Phạm Đỗ Sơn completed code assessment', time: '5 mins ago' },
    { icon: '🎯', text: 'Pre-screening completed for 3 candidates', time: '1 hour ago' },
    { icon: '📧', text: 'Offer sent to Nguyễn Văn Test', time: '2 hours ago' },
    { icon: '⏱', text: 'SLA warning: 2 applications pending review', time: '3 hours ago' },
    { icon: '📊', text: 'AI prediction: Offer acceptance rate increased to 87%', time: '5 hours ago' }
  ];

  ngOnInit() {}
}
