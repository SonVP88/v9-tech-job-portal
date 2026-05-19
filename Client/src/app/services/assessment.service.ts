import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

export interface Assessment {
  assessmentId: string;
  title: string;
  description: string;
  durationMinutes: number;
  passingScore: number;
  sections: AssessmentSection[];
}

export interface AssessmentSection {
  sectionId: string;
  title: string;
  questions: AssessmentQuestion[];
}

export interface AssessmentQuestion {
  questionId: string;
  questionText: string;
  type: 'MULTIPLE_CHOICE' | 'TEXT' | 'CODE';
  choices?: QuestionChoice[];
}

export interface QuestionChoice {
  choiceId: string;
  text: string;
}

@Injectable({
  providedIn: 'root'
})
export class AssessmentService {
  private apiUrl = 'http://localhost:7181/api/v2/assessment';

  constructor(private http: HttpClient) {}

  getAssessment(assessmentId: string): Observable<any> {
    return this.http.get(`${this.apiUrl}/${assessmentId}`);
  }

  startAssessment(assessmentId: string): Observable<any> {
    return this.http.post(`${this.apiUrl}/${assessmentId}/start`, {});
  }

  submitAssessment(sessionId: string, answers: any[]): Observable<any> {
    return this.http.post(`${this.apiUrl}/sessions/${sessionId}/submit`, { answers });
  }

  getScore(assessmentId: string, candidateId: string): Observable<any> {
    return this.http.get(`${this.apiUrl}/${assessmentId}/candidates/${candidateId}/score`);
  }
}
