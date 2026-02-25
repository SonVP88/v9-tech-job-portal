using System;
using System.Collections.Generic;

namespace UTC_DATN.DTOs.Job
{
    public class JobPublicDto
    {
        public Guid JobId { get; set; }
        public string Title { get; set; }
        public string CompanyName { get; set; }
        public string CompanyLogo { get; set; }
        public string Location { get; set; }
        public decimal? SalaryMin { get; set; }
        public decimal? SalaryMax { get; set; }
        public string JobType { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? Deadline { get; set; }
        public List<string> Skills { get; set; } = new List<string>();

        // Helper property to display salary range
        public string SalaryRange 
        { 
            get 
            {
                if (SalaryMin.HasValue && SalaryMax.HasValue)
                    return $"{SalaryMin.Value:N0} - {SalaryMax.Value:N0}";
                if (SalaryMin.HasValue)
                    return $"> {SalaryMin.Value:N0}";
                if (SalaryMax.HasValue)
                    return $"< {SalaryMax.Value:N0}";
                return "Thỏa thuận";
            } 
        }
    }
}
