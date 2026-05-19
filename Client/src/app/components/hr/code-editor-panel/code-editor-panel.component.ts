import { Component, Input, Output, EventEmitter } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';

@Component({
  selector: 'app-code-editor-panel',
  standalone: true,
  imports: [CommonModule, FormsModule],
  template: `
    <div class="code-editor-container">
      <div class="editor-header">
        <div class="language-selector">
          <label>Language:</label>
          <select [(ngModel)]="selectedLanguage" (change)="onLanguageChange()">
            <option value="csharp">C#</option>
            <option value="javascript">JavaScript</option>
            <option value="python">Python</option>
            <option value="java">Java</option>
          </select>
        </div>
        <div class="editor-actions">
          <button (click)="onRunCode()" class="btn-run">▶ Run Code</button>
          <button (click)="onResetCode()" class="btn-reset">↻ Reset</button>
        </div>
      </div>

      <div class="editor-body">
        <textarea 
          [(ngModel)]="code"
          placeholder="Write your code here..."
          class="code-textarea"
        ></textarea>
      </div>

      <div class="editor-output" *ngIf="showOutput">
        <div class="output-header">Output</div>
        <div class="output-content">{{ executionOutput }}</div>
      </div>
    </div>
  `,
  styles: [`
    .code-editor-container {
      display: flex;
      flex-direction: column;
      height: 100%;
      background: #0f172a;
      border-radius: 12px;
      overflow: hidden;
      border: 1px solid rgba(148, 163, 184, 0.1);
      box-shadow: 0 10px 30px rgba(0, 0, 0, 0.3);
    }

    .editor-header {
      display: flex;
      justify-content: space-between;
      align-items: center;
      padding: 16px 20px;
      background: linear-gradient(90deg, #1e293b 0%, #0f172a 100%);
      border-bottom: 1px solid rgba(148, 163, 184, 0.1);
    }

    .language-selector {
      display: flex;
      gap: 10px;
      align-items: center;
    }

    .language-selector label {
      color: #cbd5e1;
      font-size: 13px;
      font-weight: 600;
      text-transform: uppercase;
      letter-spacing: 0.5px;
    }

    .language-selector select {
      padding: 8px 12px;
      background: #1e293b;
      color: #e2e8f0;
      border: 1px solid rgba(148, 163, 184, 0.3);
      border-radius: 6px;
      font-size: 13px;
      font-weight: 500;
      cursor: pointer;
      transition: all 0.3s ease;
    }

    .language-selector select:hover {
      border-color: rgba(148, 163, 184, 0.5);
      background: #334155;
    }

    .language-selector select:focus {
      outline: none;
      border-color: #6366f1;
      box-shadow: 0 0 0 3px rgba(99, 102, 241, 0.1);
    }

    .editor-actions {
      display: flex;
      gap: 8px;
    }

    .btn-run, .btn-reset {
      padding: 8px 16px;
      border: none;
      border-radius: 6px;
      cursor: pointer;
      font-size: 13px;
      font-weight: 600;
      transition: all 0.3s ease;
      text-transform: uppercase;
      letter-spacing: 0.5px;
    }

    .btn-run {
      background: linear-gradient(135deg, #6366f1 0%, #8b5cf6 100%);
      color: white;
      box-shadow: 0 4px 12px rgba(99, 102, 241, 0.3);
    }

    .btn-run:hover {
      background: linear-gradient(135deg, #4f46e5 0%, #7c3aed 100%);
      box-shadow: 0 6px 16px rgba(99, 102, 241, 0.4);
      transform: translateY(-2px);
    }

    .btn-reset {
      background: #334155;
      color: #cbd5e1;
      border: 1px solid rgba(148, 163, 184, 0.3);
    }

    .btn-reset:hover {
      background: #475569;
      border-color: rgba(148, 163, 184, 0.5);
    }

    .editor-body {
      flex: 1;
      overflow: hidden;
      display: flex;
    }

    .code-textarea {
      flex: 1;
      background: #0f172a;
      color: #e2e8f0;
      border: none;
      padding: 20px;
      font-family: 'Fira Code', 'Jetbrains Mono', 'Courier New', monospace;
      font-size: 13px;
      line-height: 1.6;
      resize: none;
      transition: all 0.3s ease;
    }

    .code-textarea::selection {
      background: rgba(99, 102, 241, 0.3);
      color: #e2e8f0;
    }

    .code-textarea:focus {
      outline: none;
      background: #0f172a;
      box-shadow: inset 0 0 0 1px rgba(99, 102, 241, 0.3);
    }

    .editor-output {
      border-top: 1px solid rgba(148, 163, 184, 0.1);
      max-height: 160px;
      overflow-y: auto;
      background: #0f172a;
    }

    .output-header {
      padding: 12px 20px;
      background: linear-gradient(90deg, rgba(30, 41, 59, 0.6) 0%, rgba(15, 23, 42, 0.6) 100%);
      color: #cbd5e1;
      font-size: 12px;
      font-weight: 700;
      text-transform: uppercase;
      letter-spacing: 0.5px;
      border-bottom: 1px solid rgba(148, 163, 184, 0.1);
    }

    .output-content {
      padding: 16px 20px;
      color: #cbd5e1;
      font-family: 'Fira Code', 'Jetbrains Mono', monospace;
      font-size: 12px;
      line-height: 1.6;
      white-space: pre-wrap;
      word-break: break-word;
    }

    /* Scrollbar styling */
    .code-textarea::-webkit-scrollbar,
    .editor-output::-webkit-scrollbar {
      width: 8px;
    }

    .code-textarea::-webkit-scrollbar-track,
    .editor-output::-webkit-scrollbar-track {
      background: transparent;
    }

    .code-textarea::-webkit-scrollbar-thumb,
    .editor-output::-webkit-scrollbar-thumb {
      background: rgba(148, 163, 184, 0.2);
      border-radius: 4px;
    }

    .code-textarea::-webkit-scrollbar-thumb:hover,
    .editor-output::-webkit-scrollbar-thumb:hover {
      background: rgba(148, 163, 184, 0.4);
    }
  `]
})
export class CodeEditorPanelComponent {
  @Input() initialCode: string = '';
  @Output() codeSubmitted = new EventEmitter<string>();

  code: string = '';
  selectedLanguage: string = 'csharp';
  executionOutput: string = '';
  showOutput: boolean = false;

  ngOnInit() {
    this.code = this.initialCode;
  }

  onLanguageChange() {
    // Language change handler
  }

  onRunCode() {
    this.executionOutput = 'Code execution simulation...';
    this.showOutput = true;
  }

  onResetCode() {
    this.code = this.initialCode;
    this.showOutput = false;
  }

  submitCode() {
    this.codeSubmitted.emit(this.code);
  }
}
