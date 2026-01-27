namespace EventRegistrator.Application.DTOs
{
    public class MessageDTO
    {
        public long ChatId { get; set; }
        public int Id { get; set; }
        public long? UserId { get; set; }
        public int? ThreadId { get; set; }
        public string? Text { get; set; }
        public bool IsReply { get; set; }
        public int? ReplyToMessageId { get; set; }
        public DateTime Created { get; set; }
        public ChatDTO? ForwardFromChat { get; set; }
        public MessageDTO? ReplyToMessage { get; set; }
        public bool IsEdit { get; set; }
        public bool IsFromAdmin { get; set; }
    }

    public class ChatDTO
    {
        public long Id { get; set; }
        public string? Title { get; set; }
        public string? Type { get; set; }
    }
}
