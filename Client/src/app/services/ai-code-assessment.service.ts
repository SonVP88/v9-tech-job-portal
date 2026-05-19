import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

export interface CodeChallenge {
  challengeId: string;
  jobId: string;
  title: string;
  problemStatement: string;
  initialCodeText?: string;
  language: string;
  difficulty: 'EASY' | 'MEDIUM' | 'HARD';
  maxTimeMinutes: number;
}

export interface CodeSubmission {
  submissionId: string;
  challengeId: string;
  candidateId: string;
  applicationId: string;
  submittedCode: string;
  language: string;
  status: string;
  submittedAt: Date;
}

export interface AiCodeReview {
  reviewId: string;
  submissionId: string;
  totalScore: number;
  timeComplexity: string;
  spaceComplexity: string;
  codeSmells: string[];
  securityVulnerabilities: string[];
  overallFeedback: string;
  suggestions: string[];
  aiProvider: string;
}

@Injectable({
  providedIn: 'root'
})
export class AiCodeAssessmentService {
  private apiUrl = 'http://localhost:7181/api/v2/aicode-assessment';

  constructor(private http: HttpClient) {}

  // Get challenges for a job
  getChallengesByJob(jobId: string): Observable<{ success: boolean; data: CodeChallenge[] }> {
    return this.http.get<{ success: boolean; data: CodeChallenge[] }>(
      `${this.apiUrl}/jobs/${jobId}/challenges`
    );
  }

  // Get single challenge
  getChallenge(challengeId: string): Observable<{ success: boolean; data: CodeChallenge }> {
    return this.http.get<{ success: boolean; data: CodeChallenge }>(
      `${this.apiUrl}/challenges/${challengeId}`
    );
  }

  // Submit code solution
  submitCode(
    challengeId: string,
    candidateId: string,
    applicationId: string,
    submittedCode: string,
    language: string,
    startTime: Date
  ): Observable<{ success: boolean; data: CodeSubmission }> {
    return this.http.post<{ success: boolean; data: CodeSubmission }>(
      `${this.apiUrl}/submit`,
      {
        challengeId,
        candidateId,
        applicationId,
        submittedCode,
        language,
        startTime
      }
    );
  }

  // Get AI review
  getReview(submissionId: string): Observable<{ success: boolean; data: AiCodeReview }> {
    return this.http.get<{ success: boolean; data: AiCodeReview }>(
      `${this.apiUrl}/review/${submissionId}`
    );
  }
}
