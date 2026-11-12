using System.Diagnostics;
using Microsoft.AspNetCore.Mvc.Filters;

namespace TransitDesk.Web.Filters;

/// <summary>
/// Filtr audytu: loguje kto (IP), co (kontroler/akcja) i kiedy (UTC) wywołał akcję,
/// a czas jej wykonania zwraca w nagłówku X-Execution-Duration-Ms.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class AuditLogAttribute : ActionFilterAttribute
{
    public const string DurationHeaderName = "X-Execution-Duration-Ms";
    private static readonly object StopwatchKey = new();

    public override void OnActionExecuting(ActionExecutingContext context)
    {
        context.HttpContext.Items[StopwatchKey] = Stopwatch.StartNew();

        var logger = GetLogger(context.HttpContext);
        var routeValues = context.ActionDescriptor.RouteValues;

        logger.LogInformation(
            "[AUDIT START] IP: {ClientIp} | Kontroler: {Controller} | Akcja: {Action} | UTC: {TimestampUtc:O}",
            context.HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            routeValues.TryGetValue("controller", out var controller) ? controller : "?",
            routeValues.TryGetValue("action", out var action) ? action : "?",
            DateTime.UtcNow);
    }

    public override void OnActionExecuted(ActionExecutedContext context)
    {
        if (context.HttpContext.Items[StopwatchKey] is not Stopwatch stopwatch)
        {
            return;
        }

        stopwatch.Stop();
        var elapsedMs = stopwatch.ElapsedMilliseconds;

        context.HttpContext.Response.Headers[DurationHeaderName] = elapsedMs.ToString();

        GetLogger(context.HttpContext).LogInformation(
            "[AUDIT END] {Action} wykonana w {ElapsedMs} ms (wyjątek: {HasException})",
            context.ActionDescriptor.DisplayName, elapsedMs, context.Exception is not null);
    }

    private static ILogger GetLogger(HttpContext httpContext) =>
        httpContext.RequestServices.GetRequiredService<ILogger<AuditLogAttribute>>();
}
