namespace UTC_DATN.DTOs.Account
{
    public class UserProfileDto
    {
        public Guid UserId { get; set; }
        public string Email { get; set; }
        public string FullName { get; set; }
        public string Phone { get; set; }
        public string Role { get; set; }
        public string AvatarUrl { get; set; }
    }

    public class UpdateProfileDto
    {
        public string FullName { get; set; }
        public string Phone { get; set; }
        public string AvatarUrl { get; set; }
    }

    public class CompanyInfoDto
    {
        public string Name { get; set; }
        public string Website { get; set; }
        public string Industry { get; set; }
        public string Address { get; set; }
        public string Description { get; set; }
        public string LogoUrl { get; set; }
    }

    public class UpdateCompanyDto
    {
        public string Name { get; set; }
        public string Website { get; set; }
        public string Industry { get; set; }
        public string Address { get; set; }
        public string Description { get; set; }
        public string LogoUrl { get; set; }
    }
}
