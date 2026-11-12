using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using ProjectDefense.Core.Common;
using ProjectDefense.Core.Entities;
using ProjectDefense.Web.Services;

namespace ProjectDefense.Web.Data;

public static class DataSeeder
{
    public const string LecturerEmail = "prowadzacy@projectdefense.local";
    public const string FirstStudentEmail = "student1@projectdefense.local";
    public const string SecondStudentEmail = "student2@projectdefense.local";

    public static async Task SeedAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var provider = scope.ServiceProvider;
        var context = provider.GetRequiredService<ApplicationDbContext>();
        var configuration = provider.GetRequiredService<IConfiguration>();

        await context.Database.EnsureCreatedAsync();

        var roleManager = provider.GetRequiredService<RoleManager<IdentityRole>>();
        foreach (var role in new[] { Roles.Lecturer, Roles.Student })
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new IdentityRole(role));
            }
        }

        var password = configuration["Seed:DefaultPassword"] ?? "Haslo123!";
        var userManager = provider.GetRequiredService<UserManager<ApplicationUser>>();

        var lecturer = await EnsureUserAsync(userManager, LecturerEmail, "dr inz. Jan Prowadzacy", null, Roles.Lecturer, password);
        await EnsureUserAsync(userManager, FirstStudentEmail, "Anna Studentka", "198001", Roles.Student, password);
        await EnsureUserAsync(userManager, SecondStudentEmail, "Piotr Student", "198002", Roles.Student, password);

        if (!await context.Rooms.AnyAsync())
        {
            context.Rooms.AddRange(
                new Room { Name = "Laboratorium Sieciowe", RoomNumber = "B214", WorkstationsCount = 16 },
                new Room { Name = "Laboratorium Programowania", RoomNumber = "A105", WorkstationsCount = 24 },
                new Room { Name = "Sala Seminaryjna", RoomNumber = "C302", WorkstationsCount = 1 });
            await context.SaveChangesAsync();
        }

        if (configuration.GetValue("Seed:DemoAvailability", true) && !await context.LecturerAvailabilities.AnyAsync())
        {
            var availabilityService = provider.GetRequiredService<IAvailabilityService>();
            var timeProvider = provider.GetRequiredService<TimeProvider>();
            var room = await context.Rooms.OrderBy(r => r.Id).FirstAsync();
            var tomorrow = DateOnly.FromDateTime(timeProvider.GetLocalNow().DateTime).AddDays(1);

            await availabilityService.CreateAsync(lecturer.Id, new AvailabilityRequest(
                room.Id,
                tomorrow,
                tomorrow.AddDays(4),
                new TimeOnly(10, 0),
                new TimeOnly(12, 0),
                15));
        }
    }

    private static async Task<ApplicationUser> EnsureUserAsync(
        UserManager<ApplicationUser> userManager,
        string email,
        string fullName,
        string? indexNumber,
        string role,
        string password)
    {
        var user = await userManager.FindByEmailAsync(email);
        if (user is null)
        {
            user = new ApplicationUser
            {
                UserName = email,
                Email = email,
                EmailConfirmed = true,
                FullName = fullName,
                StudentIndexNumber = indexNumber
            };

            var result = await userManager.CreateAsync(user, password);
            if (!result.Succeeded)
            {
                throw new InvalidOperationException(string.Join("; ", result.Errors.Select(e => e.Description)));
            }
        }

        if (role == Roles.Lecturer && await userManager.IsInRoleAsync(user, Roles.Student))
        {
            await userManager.RemoveFromRoleAsync(user, Roles.Student);
        }

        if (!await userManager.IsInRoleAsync(user, role))
        {
            await userManager.AddToRoleAsync(user, role);
        }

        return user;
    }
}
