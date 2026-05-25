namespace UTC_DATN.Models
{
    public class ChatRequestDto
    {
        public string Message { get; set; } = string.Empty;
        public List<ChatMessageDto> History { get; set; } = new();
        public Guid? SessionId { get; set; } // Optional: if null, server generates a new session
    }

    public class ChatMessageDto
    {
        public string Role { get; set; } = string.Empty; // "user" hoặc "bot"
        public string Content { get; set; } = string.Empty;
    }

    public class ChatResponseDto
    {
        public string Reply { get; set; } = string.Empty;
    }
}
