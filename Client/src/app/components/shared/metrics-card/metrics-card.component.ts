import { Component, Input } from '@angular/core';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-metrics-card',
  standalone: true,
  imports: [CommonModule],
  template: `
    <div class="metrics-card" [ngClass]="'metric-' + color">
      <div class="card-header">
        <h3>{{ title }}</h3>
        <span class="icon">{{ icon }}</span>
      </div>

      <div class="card-content">
        <div class="metric-value">{{ value }}</div>
        <div class="metric-label">{{ label }}</div>
      </div>

      <div class="card-footer" *ngIf="trend">
        <span class="trend" [ngClass]="'trend-' + (trend > 0 ? 'up' : 'down')">
          {{ trend > 0 ? '▲' : '▼' }} {{ Math.abs(trend) }}%
        </span>
        <span class="period">vs last month</span>
      </div>
    </div>
  `,
  styles: [`
    .metrics-card {
      background: white;
      border-radius: 14px;
      padding: 24px;
      box-shadow: 0 4px 12px rgba(0, 0, 0, 0.08);
      border-left: 4px solid;
      border-top: 1px solid rgba(0, 0, 0, 0.05);
      transition: all 0.3s cubic-bezier(0.4, 0, 0.2, 1);
    }

    .metrics-card:hover {
      transform: translateY(-6px);
      box-shadow: 0 12px 28px rgba(0, 0, 0, 0.12);
    }

    .metric-blue {
      border-left-color: #667eea;
    }

    .metric-blue:hover {
      background: linear-gradient(135deg, rgba(102, 126, 234, 0.02) 0%, rgba(120, 75, 162, 0.02) 100%);
    }

    .metric-green {
      border-left-color: #10b981;
    }

    .metric-green:hover {
      background: linear-gradient(135deg, rgba(16, 185, 129, 0.02) 0%, rgba(5, 150, 105, 0.02) 100%);
    }

    .metric-orange {
      border-left-color: #f59e0b;
    }

    .metric-orange:hover {
      background: linear-gradient(135deg, rgba(245, 158, 11, 0.02) 0%, rgba(217, 119, 6, 0.02) 100%);
    }

    .metric-red {
      border-left-color: #ef4444;
    }

    .metric-red:hover {
      background: linear-gradient(135deg, rgba(239, 68, 68, 0.02) 0%, rgba(220, 38, 38, 0.02) 100%);
    }

    .card-header {
      display: flex;
      justify-content: space-between;
      align-items: flex-start;
      margin-bottom: 16px;
      gap: 8px;
    }

    .card-header h3 {
      margin: 0;
      color: #6b7280;
      font-size: 13px;
      font-weight: 700;
      text-transform: uppercase;
      letter-spacing: 0.5px;
    }

    .icon {
      font-size: 28px;
      line-height: 1;
    }

    .card-content {
      margin-bottom: 16px;
    }

    .metric-value {
      font-size: 32px;
      font-weight: 800;
      color: #1f2937;
      margin-bottom: 6px;
      letter-spacing: -0.5px;
    }

    .metric-label {
      font-size: 13px;
      color: #9ca3af;
      font-weight: 500;
    }

    .card-footer {
      display: flex;
      align-items: center;
      gap: 8px;
      font-size: 12px;
      padding-top: 12px;
      border-top: 1px solid #f0f4f8;
    }

    .trend {
      font-weight: 700;
      display: flex;
      align-items: center;
      gap: 4px;
    }

    .trend-up {
      color: #10b981;
    }

    .trend-down {
      color: #ef4444;
    }

    .period {
      color: #9ca3af;
      font-weight: 500;
    }
  `]
})
export class MetricsCardComponent {
  @Input() title: string = '';
  @Input() value: string | number = 0;
  @Input() label: string = '';
  @Input() icon: string = '';
  @Input() color: 'blue' | 'green' | 'orange' | 'red' = 'blue';
  @Input() trend?: number;

  Math = Math;
}
