using System.ComponentModel.DataAnnotations;

namespace TransitDesk.Core.Enums;

public enum ServiceStatus
{
    [Display(Name = "W ruchu liniowym")]
    Active = 0,

    [Display(Name = "W naprawie / przeglądzie")]
    InMaintenance = 1,

    [Display(Name = "Rezerwa techniczna")]
    Reserve = 2,

    [Display(Name = "Wycofany z eksploatacji")]
    Withdrawn = 3
}
