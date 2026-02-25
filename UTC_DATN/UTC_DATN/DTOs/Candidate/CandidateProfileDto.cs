namespace UTC_DATN.DTOs.Candidate
{
    public class CandidateProfileDto
    {
        public Guid CandidateId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? Phone { get; set; }
        public string? Location { get; set; }
        public string? Headline { get; set; }
        public string? Summary { get; set; }
        public string? LinkedIn { get; set; }
        public string? GitHub { get; set; }
        public string? Avatar { get; set; }
        public List<CandidateSkillDto> Skills { get; set; } = new();
        public List<CandidateDocumentDto> Documents { get; set; } = new();
    }
}
