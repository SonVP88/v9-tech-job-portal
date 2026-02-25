namespace UTC_DATN.DTOs.Candidate
{
    public class CandidateSkillDto
    {
        public Guid SkillId { get; set; }
        public string SkillName { get; set; } = string.Empty;
        public byte? Level { get; set; }
        public decimal? Years { get; set; }
    }
}
