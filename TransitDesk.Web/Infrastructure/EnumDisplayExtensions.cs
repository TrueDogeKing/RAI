using System.ComponentModel.DataAnnotations;
using System.Reflection;

namespace TransitDesk.Web.Infrastructure;

public static class EnumDisplayExtensions
{
    /// <summary>Zwraca Name z [Display] członka enuma (lub jego nazwę, gdy atrybutu brak).</summary>
    public static string GetDisplayName(this Enum value) =>
        value.GetType().GetField(value.ToString())?.GetCustomAttribute<DisplayAttribute>()?.GetName()
        ?? value.ToString();
}
