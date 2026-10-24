using System.Globalization;

namespace TransitDesk.Web.Routing.Constraints;

/// <summary>
/// Dopuszcza wyłącznie numery boczne taboru GAiT z zakresu 1000–9999.
/// Niespełnienie ograniczenia oznacza brak dopasowania trasy, a więc odpowiedź 404.
/// </summary>
public class FleetSideNumberConstraint : IRouteConstraint
{
    public const int MinSideNumber = 1000;
    public const int MaxSideNumber = 9999;

    public bool Match(
        HttpContext? httpContext,
        IRouter? route,
        string routeKey,
        RouteValueDictionary values,
        RouteDirection routeDirection)
    {
        if (!values.TryGetValue(routeKey, out var rawValue) || rawValue is null)
        {
            return false;
        }

        var text = Convert.ToString(rawValue, CultureInfo.InvariantCulture);

        // NumberStyles.None: odrzuca znak, spacje, separatory tysięcy ("+1000", " 1000", "1 000").
        return int.TryParse(text, NumberStyles.None, CultureInfo.InvariantCulture, out var number)
               && number is >= MinSideNumber and <= MaxSideNumber;
    }
}
