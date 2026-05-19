import { Component, Input, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { PadZeroPipe } from '../../../pipes/pad-zero.pipe';

@Component({
  selector: 'app-timer-widget',
  standalone: true,
  imports: [CommonModule, PadZeroPipe],
  template: `
    <div class="timer-widget" [ngClass]="{'timer-warning': isWarning, 'timer-critical': isCritical}">
      <div class="timer-icon">⏱</div>
      <div class="timer-display">
        <span class="time">{{ hours }}:{{ minutes | padZero }}:{{ seconds | padZero }}</span>
        <span class="label">Time Remaining</span>
      </div>
    </div>
  `,
  styles: [`
    .timer-widget {
      display: flex;
      align-items: center;
      gap: 12px;
      padding: 16px 20px;
      background: linear-gradient(135deg, #dbeafe 0%, #bfdbfe 100%);
      border-radius: 12px;
      border-left: 4px solid #667eea;
      box-shadow: 0 4px 12px rgba(102, 126, 234, 0.2);
    }

    .timer-widget.timer-warning {
      background: linear-gradient(135deg, #fef3c7 0%, #fde68a 100%);
      border-left-color: #fbbf24;
      box-shadow: 0 4px 12px rgba(251, 191, 36, 0.2);
    }

    .timer-widget.timer-critical {
      background: linear-gradient(135deg, #fee2e2 0%, #fecaca 100%);
      border-left-color: #ef4444;
      box-shadow: 0 4px 12px rgba(239, 68, 68, 0.2);
    }

    .timer-icon {
      font-size: 24px;
      line-height: 1;
    }

    .timer-display {
      display: flex;
      flex-direction: column;
      gap: 4px;
    }

    .time {
      font-size: 18px;
      font-weight: 700;
      color: #0c4a6e;
      font-family: 'Fira Code', 'Jetbrains Mono', 'Courier New', monospace;
      letter-spacing: 0.5px;
    }

    .timer-widget.timer-warning .time {
      color: #b45309;
    }

    .timer-widget.timer-critical .time {
      color: #b91c1c;
      animation: pulse-critical 1s infinite;
    }

    .label {
      font-size: 11px;
      font-weight: 600;
      color: #6b7280;
      text-transform: uppercase;
      letter-spacing: 0.5px;
    }

    .timer-widget.timer-warning .label {
      color: #b45309;
    }

    .timer-widget.timer-critical .label {
      color: #b91c1c;
    }

    @keyframes pulse-critical {
      0%, 100% {
        opacity: 1;
      }
      50% {
        opacity: 0.6;
      }
    }
  `]
})
export class TimerWidgetComponent implements OnInit {
  @Input() totalSeconds: number = 3600; // 1 hour default

  hours: number = 0;
  minutes: number = 0;
  seconds: number = 0;
  isWarning: boolean = false;
  isCritical: boolean = false;

  ngOnInit() {
    this.startTimer();
  }

  startTimer() {
    setInterval(() => {
      if (this.totalSeconds > 0) {
        this.totalSeconds--;
        this.updateDisplay();
      }
    }, 1000);
  }

  updateDisplay() {
    this.hours = Math.floor(this.totalSeconds / 3600);
    this.minutes = Math.floor((this.totalSeconds % 3600) / 60);
    this.seconds = this.totalSeconds % 60;

    if (this.totalSeconds <= 300) {
      this.isCritical = true;
      this.isWarning = false;
    } else if (this.totalSeconds <= 900) {
      this.isWarning = true;
      this.isCritical = false;
    }
  }
}
