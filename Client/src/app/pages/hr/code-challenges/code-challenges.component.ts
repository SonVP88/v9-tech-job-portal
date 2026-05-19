import { Component, OnInit } from '@angular/core';
import { Router } from '@angular/router';
import { CommonModule } from '@angular/common';
import { AiCodeAssessmentService, CodeChallenge } from '../../../services/ai-code-assessment.service';

@Component({
  selector: 'app-code-challenges',
  standalone: true,
  imports: [CommonModule],
  template: `
    <div class="code-challenges-container">
      <div class="page-header">
        <h1>Code Challenges</h1>
        <button class="btn-create" (click)="onCreateChallenge()">+ Create New Challenge</button>
      </div>

      <div class="challenges-grid">
        <div class="challenge-card" *ngFor="let challenge of challenges">
          <div class="card-header">
            <h3>{{ challenge.title }}</h3>
            <span class="difficulty-badge" [ngClass]="'difficulty-' + challenge.difficulty.toLowerCase()">
              {{ challenge.difficulty }}
            </span>
          </div>

          <div class="card-content">
            <p class="problem-preview">{{ challenge.problemStatement | slice:0:100 }}...</p>
            
            <div class="card-meta">
              <span class="meta-item">
                <span class="label">Language:</span>
                <span class="value">{{ challenge.language }}</span>
              </span>
              <span class="meta-item">
                <span class="label">Max Time:</span>
                <span class="value">{{ challenge.maxTimeMinutes }}min</span>
              </span>
            </div>
          </div>

          <div class="card-actions">
            <button class="btn-view" (click)="onViewChallenge(challenge)">View</button>
            <button class="btn-edit" (click)="onEditChallenge(challenge)">Edit</button>
            <button class="btn-delete" (click)="onDeleteChallenge(challenge)">Delete</button>
          </div>
        </div>
      </div>

      <div class="empty-state" *ngIf="challenges.length === 0">
        <div class="empty-icon">📝</div>
        <p>No code challenges created yet</p>
        <button class="btn-create-primary" (click)="onCreateChallenge()">Create First Challenge</button>
      </div>
    </div>
  `,
  styles: [`
    .code-challenges-container {
      padding: 24px;
      background: #f5f5f5;
      min-height: 100vh;
    }

    .page-header {
      display: flex;
      justify-content: space-between;
      align-items: center;
      margin-bottom: 32px;
    }

    .page-header h1 {
      margin: 0;
      color: #333;
      font-size: 28px;
    }

    .btn-create {
      padding: 10px 20px;
      background: #007acc;
      color: white;
      border: none;
      border-radius: 6px;
      font-size: 14px;
      font-weight: 600;
      cursor: pointer;
    }

    .btn-create:hover {
      background: #005a9e;
    }

    .challenges-grid {
      display: grid;
      grid-template-columns: repeat(auto-fill, minmax(320px, 1fr));
      gap: 20px;
      margin-bottom: 40px;
    }

    .challenge-card {
      background: white;
      border-radius: 8px;
      box-shadow: 0 2px 8px rgba(0,0,0,0.1);
      overflow: hidden;
      transition: transform 0.2s, box-shadow 0.2s;
    }

    .challenge-card:hover {
      transform: translateY(-4px);
      box-shadow: 0 4px 12px rgba(0,0,0,0.15);
    }

    .card-header {
      display: flex;
      justify-content: space-between;
      align-items: start;
      padding: 16px;
      border-bottom: 1px solid #f0f0f0;
    }

    .card-header h3 {
      margin: 0;
      color: #333;
      font-size: 16px;
      flex: 1;
    }

    .difficulty-badge {
      padding: 4px 8px;
      border-radius: 4px;
      font-size: 12px;
      font-weight: 600;
      white-space: nowrap;
      margin-left: 8px;
    }

    .difficulty-easy {
      background: #d4edda;
      color: #155724;
    }

    .difficulty-medium {
      background: #fff3cd;
      color: #997404;
    }

    .difficulty-hard {
      background: #f8d7da;
      color: #721c24;
    }

    .card-content {
      padding: 16px;
    }

    .problem-preview {
      margin: 0 0 12px 0;
      color: #555;
      font-size: 14px;
      line-height: 1.5;
    }

    .card-meta {
      display: flex;
      flex-direction: column;
      gap: 8px;
    }

    .meta-item {
      display: flex;
      justify-content: space-between;
      font-size: 13px;
    }

    .meta-item .label {
      color: #888;
      font-weight: 500;
    }

    .meta-item .value {
      color: #333;
      font-weight: 600;
    }

    .card-actions {
      display: flex;
      gap: 8px;
      padding: 12px 16px;
      background: #f9f9f9;
      border-top: 1px solid #f0f0f0;
    }

    .btn-view, .btn-edit, .btn-delete {
      flex: 1;
      padding: 6px 12px;
      border: 1px solid #ddd;
      background: white;
      border-radius: 4px;
      font-size: 12px;
      cursor: pointer;
      transition: all 0.2s;
    }

    .btn-view:hover {
      background: #007acc;
      color: white;
      border-color: #007acc;
    }

    .btn-edit:hover {
      background: #4CAF50;
      color: white;
      border-color: #4CAF50;
    }

    .btn-delete:hover {
      background: #f44336;
      color: white;
      border-color: #f44336;
    }

    .empty-state {
      text-align: center;
      padding: 60px 20px;
      background: white;
      border-radius: 8px;
    }

    .empty-icon {
      font-size: 64px;
      margin-bottom: 16px;
    }

    .empty-state p {
      margin: 0 0 20px 0;
      color: #888;
      font-size: 16px;
    }

    .btn-create-primary {
      padding: 12px 24px;
      background: #007acc;
      color: white;
      border: none;
      border-radius: 6px;
      font-size: 14px;
      font-weight: 600;
      cursor: pointer;
    }

    .btn-create-primary:hover {
      background: #005a9e;
    }
  `]
})
export class CodeChallengesComponent implements OnInit {
  challenges: CodeChallenge[] = [];

  constructor(private codeService: AiCodeAssessmentService, private router: Router) {}

  ngOnInit() {
    this.loadChallenges();
  }

  loadChallenges() {
    // In real app, get challenges for the job
    this.challenges = [];
  }

  onCreateChallenge() {
    this.router.navigate(['/hr/code-challenges/create']);
  }

  onViewChallenge(challenge: CodeChallenge) {
    this.router.navigate(['/hr/code-challenges', challenge.challengeId]);
  }

  onEditChallenge(challenge: CodeChallenge) {
    this.router.navigate(['/hr/code-challenges', challenge.challengeId, 'edit']);
  }

  onDeleteChallenge(challenge: CodeChallenge) {
    if (confirm('Are you sure you want to delete this challenge?')) {
      // Call delete service
    }
  }
}
