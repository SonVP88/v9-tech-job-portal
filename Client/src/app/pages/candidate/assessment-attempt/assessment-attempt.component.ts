import { Component, OnInit } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { AssessmentQuestionCardComponent } from '../../../components/hr/assessment-question-card/assessment-question-card.component';
import { TimerWidgetComponent } from '../../../components/shared/timer-widget/timer-widget.component';
import { AssessmentService } from '../../../services/assessment.service';

@Component({
  selector: 'app-assessment-attempt',
  standalone: true,
  imports: [CommonModule, FormsModule, AssessmentQuestionCardComponent, TimerWidgetComponent],
  template: `
    <div class="assessment-container">
      <div class="assessment-sidebar">
        <div class="exam-info">
          <h2>{{ assessment?.title }}</h2>
          <p>{{ assessment?.description }}</p>
          <div class="passing-score">
            <span>Passing Score: {{ assessment?.passingScore }}%</span>
          </div>
        </div>

        <app-timer-widget [totalSeconds]="remainingSeconds"></app-timer-widget>

        <div class="progress-section">
          <h4>Progress</h4>
          <div class="progress-bar">
            <div class="progress-fill" [style.width.%]="progressPercent"></div>
          </div>
          <span class="progress-text">{{ currentQuestion }} of {{ totalQuestions }} questions</span>
        </div>

        <div class="questions-navigator">
          <h4>Questions</h4>
          <div class="nav-grid">
            <button 
              *ngFor="let q of questionNumbers; let i = index"
              class="nav-btn"
              [ngClass]="{'active': i === currentQuestionIndex, 'visited': visitedQuestions[i]}"
              (click)="onGoToQuestion(i)"
            >
              {{ i + 1 }}
            </button>
          </div>
        </div>

        <button class="btn-submit" (click)="onSubmitAssessment()">
          ✓ Submit Exam
        </button>
      </div>

      <div class="assessment-main">
        <app-assessment-question-card
          *ngIf="currentQuestion"
          [question]="currentQuestion"
          [questionNumber]="currentQuestionIndex + 1"
          [totalQuestions]="totalQuestions"
          (previous)="onPreviousQuestion()"
          (next)="onNextQuestion()"
          (answerChanged)="onAnswerChanged($event)"
        ></app-assessment-question-card>
      </div>
    </div>
  `,
  styles: [`
    .assessment-container {
      display: flex;
      height: 100vh;
      background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
      font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, 'Helvetica Neue', Arial, sans-serif;
    }

    .assessment-sidebar {
      width: 320px;
      background: white;
      border-right: 1px solid #e5e7eb;
      padding: 24px 16px;
      overflow-y: auto;
      box-shadow: 0 10px 30px rgba(0, 0, 0, 0.15);
      display: flex;
      flex-direction: column;
      gap: 20px;
    }

    .exam-info {
      background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
      padding: 20px;
      border-radius: 12px;
      color: white;
      box-shadow: 0 4px 15px rgba(102, 126, 234, 0.3);
    }

    .exam-info h2 {
      margin: 0 0 8px 0;
      font-size: 18px;
      font-weight: 700;
    }

    .exam-info p {
      margin: 0 0 12px 0;
      font-size: 13px;
      opacity: 0.95;
      line-height: 1.5;
    }

    .passing-score {
      padding: 8px 12px;
      background: rgba(255, 255, 255, 0.2);
      border-radius: 6px;
      font-size: 12px;
      font-weight: 600;
      backdrop-filter: blur(10px);
    }

    .progress-section {
      background: #f8fafc;
      padding: 16px;
      border-radius: 10px;
      border-left: 4px solid #667eea;
    }

    .progress-section h4 {
      margin: 0 0 12px 0;
      color: #1f2937;
      font-size: 13px;
      font-weight: 600;
      text-transform: uppercase;
      letter-spacing: 0.5px;
    }

    .progress-bar {
      width: 100%;
      height: 8px;
      background: #e5e7eb;
      border-radius: 4px;
      overflow: hidden;
      margin-bottom: 8px;
    }

    .progress-fill {
      height: 100%;
      background: linear-gradient(90deg, #667eea 0%, #764ba2 100%);
      transition: width 0.4s cubic-bezier(0.4, 0, 0.2, 1);
      border-radius: 4px;
    }

    .progress-text {
      font-size: 12px;
      color: #6b7280;
      font-weight: 500;
    }

    .questions-navigator {
      background: #f8fafc;
      padding: 16px;
      border-radius: 10px;
      border-top: 2px solid #f0f0f0;
    }

    .questions-navigator h4 {
      margin: 0 0 12px 0;
      color: #1f2937;
      font-size: 13px;
      font-weight: 600;
      text-transform: uppercase;
      letter-spacing: 0.5px;
    }

    .nav-grid {
      display: grid;
      grid-template-columns: repeat(5, 1fr);
      gap: 8px;
    }

    .nav-btn {
      aspect-ratio: 1;
      border: 2px solid #e5e7eb;
      background: white;
      border-radius: 8px;
      cursor: pointer;
      font-size: 12px;
      font-weight: 600;
      transition: all 0.2s ease;
      color: #6b7280;
      display: flex;
      align-items: center;
      justify-content: center;
    }

    .nav-btn:hover {
      border-color: #667eea;
      background: #ede9fe;
      color: #667eea;
    }

    .nav-btn.active {
      background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
      color: white;
      border-color: #667eea;
      box-shadow: 0 4px 12px rgba(102, 126, 234, 0.4);
    }

    .nav-btn.visited {
      background: #d1fae5;
      border-color: #10b981;
      color: #059669;
    }

    .btn-submit {
      padding: 12px 16px;
      background: linear-gradient(135deg, #10b981 0%, #059669 100%);
      color: white;
      border: none;
      border-radius: 8px;
      font-size: 13px;
      font-weight: 600;
      cursor: pointer;
      transition: all 0.3s ease;
      box-shadow: 0 4px 12px rgba(16, 185, 129, 0.3);
      text-transform: uppercase;
      letter-spacing: 0.5px;
      margin-top: auto;
    }

    .btn-submit:hover {
      background: linear-gradient(135deg, #059669 0%, #047857 100%);
      box-shadow: 0 6px 20px rgba(16, 185, 129, 0.4);
      transform: translateY(-2px);
    }

    .assessment-main {
      flex: 1;
      padding: 32px;
      overflow-y: auto;
      display: flex;
      align-items: flex-start;
      justify-content: center;
    }

    @media (max-width: 1200px) {
      .assessment-container {
        flex-direction: column;
      }

      .assessment-sidebar {
        width: 100%;
        border-right: none;
        border-bottom: 1px solid #e5e7eb;
        max-height: 200px;
        display: grid;
        grid-template-columns: repeat(auto-fit, minmax(200px, 1fr));
        padding: 16px;
        gap: 16px;
      }

      .assessment-main {
        padding: 16px;
      }
    }
  `]
})
export class AssessmentAttemptComponent implements OnInit {
  assessment: any;
  questions: any[] = [];
  currentQuestionIndex: number = 0;
  currentQuestion: any;
  totalQuestions: number = 0;
  questionNumbers: number[] = [];
  remainingSeconds: number = 3600;
  visitedQuestions: boolean[] = [];
  answers: Map<number, any> = new Map();

  constructor(private route: ActivatedRoute, private assessmentService: AssessmentService) {}

  ngOnInit() {
    this.loadAssessment();
  }

  loadAssessment() {
    const assessmentId = this.route.snapshot.paramMap.get('id');
    if (assessmentId) {
      this.assessmentService.getAssessment(assessmentId).subscribe(
        res => {
          this.assessment = res.data;
          this.loadQuestions();
        },
        err => console.error('Error loading assessment:', err)
      );
    }
  }

  loadQuestions() {
    // Mock data - in real app, get from assessment
    this.questions = this.assessment?.sections?.flatMap((s: any) => s.questions) || [];
    this.totalQuestions = this.questions.length;
    this.questionNumbers = Array.from({ length: this.totalQuestions }, (_, i) => i + 1);
    this.visitedQuestions = new Array(this.totalQuestions).fill(false);
    this.currentQuestion = this.questions[0];
  }

  get progressPercent(): number {
    return (this.currentQuestionIndex + 1) / this.totalQuestions * 100;
  }

  onPreviousQuestion() {
    if (this.currentQuestionIndex > 0) {
      this.currentQuestionIndex--;
      this.currentQuestion = this.questions[this.currentQuestionIndex];
    }
  }

  onNextQuestion() {
    if (this.currentQuestionIndex < this.totalQuestions - 1) {
      this.visitedQuestions[this.currentQuestionIndex] = true;
      this.currentQuestionIndex++;
      this.currentQuestion = this.questions[this.currentQuestionIndex];
    }
  }

  onGoToQuestion(index: number) {
    this.currentQuestionIndex = index;
    this.currentQuestion = this.questions[index];
  }

  onAnswerChanged(answer: any) {
    this.answers.set(this.currentQuestionIndex, answer);
  }

  onSubmitAssessment() {
    if (confirm('Are you sure you want to submit? You cannot change your answers after submission.')) {
      // Submit assessment
      console.log('Answers:', Array.from(this.answers.entries()));
    }
  }
}
