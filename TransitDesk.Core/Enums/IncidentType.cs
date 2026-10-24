using System.ComponentModel.DataAnnotations;

namespace TransitDesk.Core.Enums;

public enum IncidentType
{
    [Display(Name = "Awaria techniczna pojazdu")]
    TechnicalFailure = 0,

    [Display(Name = "Kolizja / wypadek drogowy")]
    Collision = 1,

    [Display(Name = "Incydent z udziałem pasażera")]
    PassengerIncident = 2,

    [Display(Name = "Akt wandalizmu")]
    Vandalism = 3
}
