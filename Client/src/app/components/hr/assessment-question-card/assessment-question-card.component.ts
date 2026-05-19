import { Component, Input, Output, EventEmitter } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';

@Component({
  selector: 'app-assessment-question-card',
  standalone: true,
  imports: [CommonModule, FormsModule],
  template: `
    <div class="question-card">
      <div class="question-header">
        <span class="question-number">Question {{ questionNumber }} of {{ totalQuestions }}</span>
        <span class="question-type" [ngClass]="'type-' + question.type.toLowerCase()">
          {{ question.type }}
        </span>
      </div>

      <div class="question-content">
        <h3>{{ question.questionText }}</h3>

        <!-- Multiple Choice -->
        <div class="choices" *ngIf="question.type === 'MULTIPLE_CHOICE'">
          <div class="choice-item" *ngFor="let choice of question.choices; let i = index">
            <input 
              type="radio" 
              [id]="'choice-' + i"
              [name]="'question-' + question.questionId"
              [value]="choice.choiceId"
              [(ngModel)]="selectedAnswer"
              (change)="onAnswerChange()"
            />
            <label [for]="'choice-' + i">{{ choice.text }}</label>
          </div>
        </div>

        <!-- Text Answer -->
        <div class="text-answer" *ngIf="question.type === 'TEXT'">
          <textarea 
            [(ngModel)]="selectedAnswer"
            (change)="onAnswerChange()"
            placeholder="Enter your answer here..."
            rows="4"
          ></textarea>
        </div>

        <!-- Code Answer -->
        <div class="code-answer" *ngIf="question.type === 'CODE'">
          <textarea 
            [(ngModel)]="selectedAnswer"
            (change)="onAnswerChange()"
            placeholder="Write your code here..."
            rows="6"
            class="code-textarea"
          ></textarea>
        </div>
      </div>

      <div class="question-actions">
        <button 
          class="btn-prev"
          (click)="onPrevious()"
          [disabled]="questionNumber === 1"
        >
          ← Previous
        </button>
        <button 
          class="btn-next"
          (click)="onNext()"
          [disabled]="questionNumber === totalQuestions"
        >
          Next →
        </button>
      </div>
    </div>
  `,
  styles: [`
    .question-card {
      background: white;
      border-radius: 16px;
      padding: 32px;
      box-shadow: 0 10px 30px rgba(0, 0, 0, 0.08);
      border: 1px solid #f0f4f8;
      width: 100%;
      max-width: 700px;
    }

    .question-header {
      display: flex;
      justify-content: space-between;
      align-items: center;
      margin-bottom: 24px;
      padding-bottom: 16px;
      border-bottom: 2px solid #f0f4f8;
    }

    .question-number {
      color: #6b7280;
      font-size: 12px;
      font-weight: 600;
      text-transform: uppercase;
      letter-spacing: 0.5px;
    }

    .question-type {
      padding: 6px 12px;
      border-radius: 6px;
      font-size: 11px;
      font-weight: 700;
      text-transform: uppercase;
      letter-spacing: 0.5px;
    }

    .type-multiple_choice {
      background: linear-gradient(135deg, #dbeafe 0%, #bfdbfe 100%);
      color: #0c4a6e;
    }

    .type-text {
      background: linear-gradient(135deg, #d1fae5 0%, #a7f3d0 100%);
      color: #065f46;
    }

    .type-code {
      background: linear-gradient(135deg, #ede9fe 0%, #ddd6fe 100%);
      color: #6b21a8;
    }

    .question-content {
      margin-bottom: 32px;
    }

    .question-content h3 {
      margin: 0 0 24px 0;
      color: #1f2937;
      font-size: 20px;
      font-weight: 600;
      line-height: 1.6;
    }

    .choices {
      display: flex;
      flex-direction: column;
      gap: 12px;
    }

    .choice-item {
      display: flex;
      align-items: flex-start;
      gap: 12px;
      position: relative;
    }

    .choice-item input[type="radio"] {
      margin-top: 6px;
      cursor: pointer;
      width: 18px;
      height: 18px;
      accent-color: #667eea;
    }

    .choice-item label {
      flex: 1;
      cursor: pointer;
      padding: 12px 16px;
      border-radius: 8px;
      border: 2px solid #e5e7eb;
      transition: all 0.3s ease;
      background: white;
      color: #1f2937;
      font-size: 15px;
    }

    .choice-item input[type="radio"]:checked + label {
      background: linear-gradient(135deg, #ede9fe 0%, #f3e8ff 100%);
      border-color: #667eea;
      font-weight: 600;
      box-shadow: 0 4px 12px rgba(102, 126, 234, 0.2);
    }

    .text-answer textarea,
    .code-answer .code-textarea {
      width: 100%;
      padding: 14px 16px;
      border: 2px solid #e5e7eb;
      border-radius: 8px;
      font-size: 14px;
      line-height: 1.6;
      resize: vertical;
      min-height: 120px;
      transition: all 0.3s ease;
    }

    .code-answer .code-textarea {
      font-family: 'Fira Code', 'Courier New', monospace;
      background: #f8fafc;
      font-size: 13px;
    }

    textarea:focus {
      outline: none;
      border-color: #667eea;
      background: white;
      box-shadow: 0 0 0 4px rgba(102, 126, 234, 0.1);
    }

    .question-actions {
      display: flex;
      gap: 12px;
      justify-content: space-between;
      padding-top: 20px;
      border-top: 1px solid #f0f4f8;
    }

    .btn-prev, .btn-next {
      padding: 12px 24px;
      border: 2px solid #e5e7eb;
      background: white;
      border-radius: 8px;
      cursor: pointer;
      font-size: 14px;
      font-weight: 600;
      transition: all 0.3s ease;
      color: #1f2937;
    }

    .btn-prev:hover:not(:disabled) {
      border-color: #667eea;
      background: #f3e8ff;
      color: #667eea;
    }

    .btn-next:hover:not(:disabled) {
      background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
      border-color: #667eea;
      color: white;
      box-shadow: 0 4px 12px rgba(102, 126, 234, 0.3);
    }

    .btn-prev:disabled,
    .btn-next:disabled {
      opacity: 0.4;
      cursor: not-allowed;
    }
  `]
})
export class AssessmentQuestionCardComponent {
  @Input() question: any;
  @Input() questionNumber: number = 1;
  @Input() totalQuestions: number = 1;
  @Output() previous = new EventEmitter<void>();
  @Output() next = new EventEmitter<void>();
  @Output() answerChanged = new EventEmitter<string>();

  selectedAnswer: any = null;

  onPrevious() {
    this.previous.emit();
  }

  onNext() {
    this.next.emit();
  }

  onAnswerChange() {
    this.answerChanged.emit(this.selectedAnswer);
  }
}
