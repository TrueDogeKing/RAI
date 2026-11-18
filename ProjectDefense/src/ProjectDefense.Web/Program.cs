using System.Globalization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Localization;
using Microsoft.EntityFrameworkCore;
using ProjectDefense.Core.Common;
using ProjectDefense.Core.Entities;
using ProjectDefense.Web;
using ProjectDefense.Web.Api;
using ProjectDefense.Web.Data;
using ProjectDefense.Web.Email;
using ProjectDefense.Web.Infrastructure;
using ProjectDefense.Web.Services;
using QuestPDF.Infrastructure;

QuestPDF.Settings.License = LicenseType.Community;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

builder.Services.AddDbContext<ApplicationDbContext>(options => options.UseSqlite(connectionString));
builder.Services.AddDatabaseDeveloperPageExceptionFilter();

builder.Services
    .AddDefaultIdentity<ApplicationUser>(options =>
    {
        options.SignIn.RequireConfirmedAccount = true;
        options.User.RequireUniqueEmail = true;
        options.Password.RequiredLength = 8;
    })
    .AddRoles<IdentityRole>()
    .AddUserManager<ApplicationUserManager>()
    .AddErrorDescriber<LocalizedIdentityErrorDescriber>()
    .AddEntityFrameworkStores<ApplicationDbContext>();

builder.Services.Configure<EmailOptions>(builder.Configuration.GetSection(EmailOptions.SectionName));
var emailProvider = builder.Configuration["Email:Provider"] ?? "Smtp";
var emailSenderType = emailProvider.ToLowerInvariant() switch
{
    "sendgrid" => typeof(SendGridEmailSender),
    "file" => typeof(FileEmailSender),
    _ => typeof(SmtpEmailSender)
};
builder.Services.AddTransient(emailSenderType);
// Dekorator tłumaczy wiadomości wysyłane przez strony Identity (potwierdzenie konta, reset hasła).
builder.Services.AddTransient<IEmailSender>(sp => new LocalizedIdentityEmailSender(
    (IEmailSender)sp.GetRequiredService(emailSenderType),
    sp.GetRequiredService<Microsoft.Extensions.Localization.IStringLocalizer<SharedResource>>()));

builder.Services.AddAuthorizationBuilder()
    .AddPolicy(Roles.Lecturer, policy => policy.RequireRole(Roles.Lecturer))
    .AddPolicy(Roles.Student, policy => policy.RequireRole(Roles.Student));

builder.Services.AddLocalization(options => options.ResourcesPath = "Resources");
builder.Services.Configure<RequestLocalizationOptions>(options =>
{
    var cultures = CultureEndpoints.SupportedCultures.Select(c => new CultureInfo(c)).ToList();
    options.DefaultRequestCulture = new RequestCulture("pl-PL");
    options.SupportedCultures = cultures;
    options.SupportedUICultures = cultures;
    options.RequestCultureProviders =
    [
        new QueryStringRequestCultureProvider(),
        new CookieRequestCultureProvider(),
        new AcceptLanguageHeaderRequestCultureProvider()
    ];
});

builder.Services
    .AddRazorPages(options =>
    {
        options.Conventions.AuthorizeFolder("/");
        options.Conventions.AllowAnonymousToPage("/Error");
        options.Conventions.AuthorizeFolder("/Lecturer", Roles.Lecturer);
        options.Conventions.AuthorizeFolder("/Student", Roles.Student);
    })
    .AddViewLocalization()
    .AddDataAnnotationsLocalization(options =>
    {
        options.DataAnnotationLocalizerProvider = (_, factory) => factory.Create(typeof(SharedResource));
    });

// Etykiety i komunikaty walidacji formularzy Identity (InputModel z biblioteki Identity UI).
builder.Services.AddOptions<Microsoft.AspNetCore.Mvc.MvcOptions>()
    .Configure<Microsoft.Extensions.Localization.IStringLocalizerFactory>((options, factory) =>
        options.ModelMetadataDetailsProviders.Add(new IdentityUiLocalizationMetadataProvider(factory)));

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new() { Title = "ProjectDefense API", Version = "v1" });
});

builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddSingleton<ISlotGenerator, SlotGenerator>();
builder.Services.AddScoped<ISlotLabelFormatter, SlotLabelFormatter>();
builder.Services.AddScoped<IAvailabilityService, AvailabilityService>();
builder.Services.AddScoped<IBanService, BanService>();
builder.Services.AddScoped<IReservationService, ReservationService>();
builder.Services.AddScoped<IExportService, ExportService>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRequestLocalization();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.UseSwagger();
app.UseSwaggerUI(options => options.SwaggerEndpoint("/swagger/v1/swagger.json", "ProjectDefense API v1"));

app.MapRazorPages();
app.MapCultureSwitch();
app.MapProjectDefenseApi();

await DataSeeder.SeedAsync(app.Services);

app.Run();

public partial class Program
{
}
