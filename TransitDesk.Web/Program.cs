using System.Globalization;
using System.Text.Encodings.Web;
using System.Text.Unicode;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.Extensions.WebEncoders;
using TransitDesk.Core.Services;
using TransitDesk.Web.Routing.Constraints;

var builder = WebApplication.CreateBuilder(args);

// ───────────────────────── 1. Rejestracja usług (kontener DI) ─────────────────────────

// Lokalizacja: pliki .resx szukane w folderze Resources/ (np. Resources/Views/Home/Index.en-US.resx).
builder.Services.AddLocalization(options => options.ResourcesPath = "Resources");

builder.Services
    .AddControllersWithViews()
    .AddViewLocalization(LanguageViewLocationExpanderFormat.Suffix);

// Domyślny HtmlEncoder koduje wszystko spoza Basic Latin ("ł" → "&#x142;").
// Dopuszczamy pełny Unicode – znaki <, >, &, " nadal są kodowane, więc ochrona przed XSS pozostaje.
builder.Services.Configure<WebEncoderOptions>(options =>
{
    options.TextEncoderSettings = new TextEncoderSettings(UnicodeRanges.All);
});

// Pamięć rozproszona (tu: implementacja in-memory) jest magazynem danych dla ISession.
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(15);
    options.Cookie.Name = ".TransitDesk.Session";
    options.Cookie.HttpOnly = true;          // niedostępne dla JavaScriptu (ochrona przed XSS)
    options.Cookie.IsEssential = true;       // działa nawet bez zgody na ciasteczka (RODO: niezbędne)
    options.Cookie.SameSite = SameSiteMode.Lax;
});

// Polityka ciasteczek (RODO): nieistotne ciasteczka wymagają zgody, istotne (sesja, antiforgery) nie.
builder.Services.Configure<CookiePolicyOptions>(options =>
{
    options.CheckConsentNeeded = _ => true;
    options.MinimumSameSitePolicy = SameSiteMode.Lax;
    options.HttpOnly = Microsoft.AspNetCore.CookiePolicy.HttpOnlyPolicy.Always;
    options.Secure = CookieSecurePolicy.SameAsRequest;
});

// TempData domyślnie korzysta z ciasteczka, które NIE jest oznaczone jako niezbędne.
// Przy CheckConsentNeeded = true komunikat PRG by zniknął, więc jawnie oznaczamy je jako niezbędne.
builder.Services.Configure<CookieTempDataProviderOptions>(options =>
{
    options.Cookie.IsEssential = true;
});

// Własne ograniczenie trasy dostępne w szablonach jako {param:fleetSideNo}.
builder.Services.Configure<RouteOptions>(options =>
{
    options.ConstraintMap.Add("fleetSideNo", typeof(FleetSideNumberConstraint));
});

// UseAuthentication/UseAuthorization wymagają zarejestrowanych usług (bez schematów – brak logowania na tym etapie).
builder.Services.AddAuthentication();
builder.Services.AddAuthorization();

// Serwisy domenowe in-memory – Singleton, bo przechowują stan całej aplikacji.
builder.Services.AddSingleton<IVehicleService, InMemoryVehicleService>();
builder.Services.AddSingleton<IIncidentService, InMemoryIncidentService>();

var supportedCultures = new[] { new CultureInfo("pl-PL"), new CultureInfo("en-US") };
builder.Services.Configure<RequestLocalizationOptions>(options =>
{
    options.DefaultRequestCulture = new RequestCulture("pl-PL");
    options.SupportedCultures = supportedCultures;
    options.SupportedUICultures = supportedCultures;
    // Domyślni dostawcy: QueryString (?culture=en-US) → Cookie → nagłówek Accept-Language.
});

var app = builder.Build();

// ───────────────────────── 2. Potok middleware (kolejność ma znaczenie!) ─────────────────────────

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

// Puste odpowiedzi 4xx/5xx są ponownie przetwarzane przez /Errors/Status?code=XXX
// – wewnętrznie, bez przekierowania 302, z zachowaniem oryginalnego kodu statusu.
app.UseStatusCodePagesWithReExecute("/Errors/Status", "?code={0}");

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseCookiePolicy();
app.UseRequestLocalization();

app.UseRouting();

app.UseSession();
app.UseAuthentication();
app.UseAuthorization();

// Trasa nazwana musi być przed trasą domyślną – trasy konwencjonalne dopasowywane są w kolejności rejestracji.
app.MapControllerRoute(
    name: "depot_fleet",
    pattern: "Zajezdnia/{depotName}/{tractionType?}",
    defaults: new { controller = "Vehicles", action = "DepotFleetList" });

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();

// Udostępnia klasę Program dla WebApplicationFactory<Program> w projekcie testów.
public partial class Program;
