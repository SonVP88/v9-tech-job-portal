using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace UTC_DATN.Entities;

[Table("ExEmployees")]
public class ExEmployee
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Required]
    [MaxLength(50)]
    public string EmployeeCode { get; set; } = string.Empty;

    [MaxLength(20)]
    public string? CitizenId { get; set; }

    [MaxLength(20)]
    public string? PhoneNormalized { get; set; }

    [Required]
    [MaxLength(150)]
    public string FullName { get; set; } = string.Empty;

    [MaxLength(100)]
    public string? DepartmentName { get; set; }

    public DateTime? ResignedDate { get; set; }

    [MaxLength(255)]
    public string? ResignationReason { get; set; }

    public bool IsEligibleForRehire { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

