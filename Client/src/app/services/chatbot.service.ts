import { Injectable } from '@angular/core';
import { environment } from '../../environments/environment';

export interface ChatRequest {
    message: string;
    history: ChatMessage[];
}

export interface ChatMessage {
    role: 'user' | 'bot';
    content: string;
}

@Injectable({
    providedIn: 'root'
})
export class ChatbotService {
    private apiUrl = `${environment.apiUrl}/chatbot`;

    // State lưu trữ dữ liệu Chat xuyên suốt tất cả các màn hình (Singleton)
    public isOpen = false;
    public messages: ChatMessage[] = [
        { role: 'bot', content: 'Xin chào! Tôi là V9 Assistant. Tôi có thể giúp gì cho bạn về các công việc tại V9 TECH?' }
    ];

    constructor() { }

    // Xóa lịch sử trò chuyện khi người dùng đăng xuất
    clearChat(): void {
        this.isOpen = false;
        this.messages = [
            { role: 'bot', content: 'Xin chào! Tôi là V9 Assistant. Tôi có thể giúp gì cho bạn về các công việc tại V9 TECH?' }
        ];
    }

    // Trả về async generator thay vì Observable để hứng Streaming chunks
    async *askBotStream(message: string): AsyncGenerator<string, void, unknown> {
        // Gửi kèm lịch sử hội thoại (Loại bỏ tin nhắn hiện tại vì nó đã nằm trong thuộc tính 'message')
        const payload: ChatRequest = {
            message,
            history: this.messages.slice(0, -1)
        };

        // Authorization header (Cần thiết nếu API yêu cầu auth, hoặc API ẩn cần truyền Bearer)
        const token = localStorage.getItem('token');
        const headers: Record<string, string> = {
            'Content-Type': 'application/json'
        };
        if (token) {
            headers['Authorization'] = `Bearer ${token}`;
        }

        const response = await fetch(`${this.apiUrl}/ask`, {
            method: 'POST',
            headers: headers,
            body: JSON.stringify(payload)
        });

        if (!response.body) {
            throw new Error('ReadableStream not yet supported in this browser.');
        }

        const reader = response.body.getReader();
        const decoder = new TextDecoder('utf-8');
        let buffer = '';

        while (true) {
            const { done, value } = await reader.read();
            if (done) {
                if (buffer.trim() && buffer.startsWith('data: ')) {
                    try {
                        const jsonStr = buffer.replace('data: ', '').trim();
                        if (jsonStr !== '[DONE]') {
                            const parsed = JSON.parse(jsonStr);
                            if (parsed && parsed.text) yield parsed.text;
                        }
                    } catch (e) { }
                }
                break;
            }

            buffer += decoder.decode(value, { stream: true });
            const lines = buffer.split('\n');
            buffer = lines.pop() || '';

            for (const line of lines) {
                if (line.trim() && line.startsWith('data: ')) {
                    const jsonStr = line.replace('data: ', '').trim();
                    if (jsonStr === '[DONE]') return; // Thoát NGAY LẬP TỨC vòng lặp Generator thay vì `break` vòng lặp For
                    try {
                        const parsed = JSON.parse(jsonStr);
                        if (parsed && parsed.text) {
                            yield parsed.text;
                        }
                    } catch (e) {
                        console.error('Lỗi parse JSON stream:', e, jsonStr);
                    }
                }
            }
        }
    }
}
