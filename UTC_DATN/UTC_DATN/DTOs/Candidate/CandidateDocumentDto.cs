namespace UTC_DATN.DTOs.Candidate
{
    public class CandidateDocumentDto
    {
        public Guid DocumentId { get; set; }
        public string FileName { get; set; } = string.Empty;
        public string FileUrl { get; set; } = string.Empty;
        public string DocType { get; set; } = string.Empty;
        public long? SizeBytes { get; set; }
        public bool IsPrimary { get; set; }
        public string DisplayName { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }
}
