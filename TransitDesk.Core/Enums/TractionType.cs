using System.ComponentModel.DataAnnotations;
using System.Xml.Linq;

namespace TransitDesk.Core.Enums;

public enum TractionType
{
    [Display(Name = "Autobus")]
    Bus = 0,

    [Display(Name = "Tramwaj")]
    Tram = 1
}
