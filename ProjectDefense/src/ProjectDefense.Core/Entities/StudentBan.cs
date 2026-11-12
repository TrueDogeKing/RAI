using System.ComponentModel.DataAnnotations;

namespace ProjectDefense.Core.Entities;

public class StudentBan
{
    public int Id { get; set; }

    [Required]
    public string StudentId { get; set; } = string.Empty;

    public ApplicationUser? Student { get; set; }

    [Required]
    public string BannedByLecturerId { get; set; } = string.Empty;

    public ApplicationUser? BannedByLecturer { get; set; }

    [MaxLength(500)]
    public string Reason { get; set; } = string.Empty;

    public DateTime BannedAt { get; set; }

    public DateTime? BannedUntil { get; set; }

    public DateTime? LiftedAt { get; set; }
}
