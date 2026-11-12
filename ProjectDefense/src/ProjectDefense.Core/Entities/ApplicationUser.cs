using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;

namespace ProjectDefense.Core.Entities;

public class ApplicationUser : IdentityUser
{
    [MaxLength(200)]
    public string FullName { get; set; } = string.Empty;

    [MaxLength(20)]
    public string? StudentIndexNumber { get; set; }

    public bool IsBanned { get; set; }

    public ICollection<ReservationSlot> Reservations { get; set; } = new List<ReservationSlot>();
}
