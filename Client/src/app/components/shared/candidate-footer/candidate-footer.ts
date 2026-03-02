import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ChatWidgetComponent } from '../chat-widget/chat-widget.component';

@Component({
  selector: 'app-candidate-footer',
  standalone: true,
  imports: [CommonModule, ChatWidgetComponent],
  templateUrl: './candidate-footer.html',
  styleUrl: './candidate-footer.scss',
})
export class CandidateFooter {

}
