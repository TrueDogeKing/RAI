using Microsoft.EntityFrameworkCore;
using ProjectDefense.Core.Common;
using ProjectDefense.Core.Entities;
using ProjectDefense.Web.Data;
using ProjectDefense.Web.Services;

namespace ProjectDefense.Tests.Support;

public sealed class BookingTestContext : IDisposable
{
    public static readonly DateTime Now = new(2026, 11, 16, 12, 0, 0);

    public BookingTestContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        Db = new ApplicationDbContext(options);
        Time = new FakeTimeProvider(Now);
        var localizer = new KeyLocalizer();

        Availability = new AvailabilityService(Db, new SlotGenerator(), Time, localizer);
        Bans = new BanService(Db, Time, localizer);
        Reservations = new ReservationService(Db, Bans, Time, localizer);

        var studentRole = new Microsoft.AspNetCore.Identity.IdentityRole(Roles.Student) { Id = "role-student", NormalizedName = Roles.Student.ToUpperInvariant() };
        Db.Roles.Add(studentRole);

        Lecturer = AddUser("lecturer", "Dr Prowadzacy", null);
        FirstStudent = AddUser("student-1", "Anna Studentka", studentRole.Id);
        SecondStudent = AddUser("student-2", "Piotr Student", studentRole.Id);

        RoomB214 = new Room { Name = "Laboratorium Sieciowe", RoomNumber = "B214", WorkstationsCount = 16 };
        RoomA105 = new Room { Name = "Laboratorium Programowania", RoomNumber = "A105", WorkstationsCount = 24 };
        Db.Rooms.AddRange(RoomB214, RoomA105);
        Db.SaveChanges();
    }

    public ApplicationDbContext Db { get; }

    public FakeTimeProvider Time { get; }

    public AvailabilityService Availability { get; }

    public BanService Bans { get; }

    public ReservationService Reservations { get; }

    public ApplicationUser Lecturer { get; }

    public ApplicationUser FirstStudent { get; }

    public ApplicationUser SecondStudent { get; }

    public Room RoomB214 { get; }

    public Room RoomA105 { get; }

    public static DateOnly Tomorrow => DateOnly.FromDateTime(Now).AddDays(1);

    public AvailabilityRequest Window(Room room, int startHour, int endHour, int days = 1, int minutes = 15)
        => new(room.Id, Tomorrow, Tomorrow.AddDays(days - 1), new TimeOnly(startHour, 0), new TimeOnly(endHour, 0), minutes);

    public async Task<List<ReservationSlot>> CreateSlotsAsync(Room room, int startHour = 8, int endHour = 10)
    {
        var result = await Availability.CreateAsync(Lecturer.Id, Window(room, startHour, endHour));
        result.Succeeded.Should().BeTrue(result.Message ?? string.Empty);
        return result.Value!.Slots.OrderBy(s => s.StartTime).ToList();
    }

    public void Dispose() => Db.Dispose();

    private ApplicationUser AddUser(string id, string name, string? roleId)
    {
        var user = new ApplicationUser
        {
            Id = id,
            UserName = id + "@test.local",
            Email = id + "@test.local",
            NormalizedEmail = (id + "@test.local").ToUpperInvariant(),
            FullName = name
        };
        Db.Users.Add(user);
        if (roleId is not null)
        {
            Db.UserRoles.Add(new Microsoft.AspNetCore.Identity.IdentityUserRole<string> { UserId = id, RoleId = roleId });
        }

        return user;
    }
}
