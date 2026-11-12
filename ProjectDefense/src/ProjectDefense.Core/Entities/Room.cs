using System.ComponentModel.DataAnnotations;

namespace ProjectDefense.Core.Entities;

public class Room
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Required")]
    [StringLength(100, ErrorMessage = "MaxLength")]
    [Display(Name = "RoomName")]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "Required")]
    [StringLength(20, ErrorMessage = "MaxLength")]
    [Display(Name = "RoomNumber")]
    public string RoomNumber { get; set; } = string.Empty;

    [Range(1, 500, ErrorMessage = "Range")]
    [Display(Name = "WorkstationsCount")]
    public int WorkstationsCount { get; set; }

    public ICollection<LecturerAvailability> Availabilities { get; set; } = new List<LecturerAvailability>();
}
