namespace ExpenseTracker.Api.Models;

public class ExpenseAttachment
{
    public int Id { get; set; }
    public int ExpenseId { get; set; }
    public string Kind { get; set; } = "";          // Receipt | VoiceNote
    public string BlobName { get; set; } = "";
    public string ContentType { get; set; } = "";
    public long SizeBytes { get; set; }
    public DateTime UploadedAt { get; set; }

    public Expense? Expense { get; set; }
}

public static class AttachmentKind
{
    public const string Receipt = "Receipt";
    public const string VoiceNote = "VoiceNote";
}

public record AttachmentResponse(string Kind, string ContentType, long SizeBytes, DateTime UploadedAt, string Url);
