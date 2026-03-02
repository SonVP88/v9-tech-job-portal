import { Component, ElementRef, ViewChild, AfterViewChecked, NgZone, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ChatbotService, ChatMessage } from '../../../services/chatbot.service';

@Component({
    selector: 'app-chat-widget',
    standalone: true,
    imports: [CommonModule, FormsModule],
    templateUrl: './chat-widget.component.html',
    styleUrl: './chat-widget.component.scss'
})
export class ChatWidgetComponent implements AfterViewChecked {
    @ViewChild('scrollContainer') private scrollContainer!: ElementRef;

    // Delegate getter/setter sang Service để giữ State không bị xóa khi đổi Route
    get isOpen(): boolean { return this.chatbotService.isOpen; }
    set isOpen(value: boolean) { this.chatbotService.isOpen = value; }

    get messages(): ChatMessage[] { return this.chatbotService.messages; }

    isBotTyping = false;
    userInput = '';

    constructor(
        private chatbotService: ChatbotService,
        private ngZone: NgZone,
        private cdr: ChangeDetectorRef
    ) { }

    ngAfterViewChecked() {
        this.scrollToBottom();
    }

    toggleChat() {
        this.isOpen = !this.isOpen;
        if (this.isOpen) {
            setTimeout(() => this.scrollToBottom(), 100);
        }
    }

    async sendMessage() {
        if (!this.userInput.trim() || this.isBotTyping) return;

        const messageText = this.userInput.trim();
        this.messages.push({ role: 'user', content: messageText });
        this.userInput = '';
        this.isBotTyping = true;
        this.scrollToBottom();

        // Tạo sẵn một bong bóng chat rỗng cho Bot trước để hứng chữ
        const botMessageIndex = this.messages.push({ role: 'bot', content: '' }) - 1;

        try {
            // Lặp bất đồng bộ qua các chunk văn bản mà Server-Sent Events trả về
            for await (const chunk of this.chatbotService.askBotStream(messageText)) {
                // Ép Angular chạy Change Detection ngay lập tức để chữ hiện ra mượt mà
                this.ngZone.run(() => {
                    this.isBotTyping = false;
                    // Tạo reference mới cho content để Angular nhận diện thay đổi sâu
                    this.messages[botMessageIndex].content += chunk;
                    this.cdr.detectChanges(); // Cưỡng bức Render lại giao diện ngay tại đây
                    this.scrollToBottom();
                });
            }
        } catch (err) {
            console.error('Lỗi khi gọi chatbot streaming:', err);
            this.isBotTyping = false;
            if (this.messages[botMessageIndex].content === '') {
                this.messages[botMessageIndex].content = 'V9 Assistant đang gặp một chút gián đoạn kết nối. Bạn hãy thử nhắn lại sau ít phút nữa nhé!';
            } else {
                this.messages[botMessageIndex].content += '\n\n(Kết nối bị gián đoạn, vui lòng thử lại)';
            }
        } finally {
            this.isBotTyping = false;
        }
    }

    private scrollToBottom(): void {
        if (this.scrollContainer) {
            try {
                const nativeElement = this.scrollContainer.nativeElement;
                nativeElement.scrollTop = nativeElement.scrollHeight;
            } catch (err) { }
        }
    }
}
