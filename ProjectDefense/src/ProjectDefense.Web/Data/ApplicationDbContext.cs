using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using ProjectDefense.Core.Entities;

namespace ProjectDefense.Web.Data;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<Room> Rooms => Set<Room>();

    public DbSet<LecturerAvailability> LecturerAvailabilities => Set<LecturerAvailability>();

    public DbSet<ReservationSlot> ReservationSlots => Set<ReservationSlot>();

    public DbSet<StudentBan> StudentBans => Set<StudentBan>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<Room>(room =>
        {
            room.HasIndex(r => r.RoomNumber).IsUnique();
        });

        builder.Entity<LecturerAvailability>(availability =>
        {
            availability.HasOne(a => a.Lecturer)
                .WithMany()
                .HasForeignKey(a => a.LecturerId)
                .OnDelete(DeleteBehavior.Restrict);

            availability.HasOne(a => a.Room)
                .WithMany(r => r.Availabilities)
                .HasForeignKey(a => a.RoomId)
                .OnDelete(DeleteBehavior.Restrict);

            availability.HasIndex(a => new { a.RoomId, a.StartDate, a.EndDate });
        });

        builder.Entity<ReservationSlot>(slot =>
        {
            slot.HasOne(s => s.LecturerAvailability)
                .WithMany(a => a.Slots)
                .HasForeignKey(s => s.LecturerAvailabilityId)
                .OnDelete(DeleteBehavior.Cascade);

            slot.HasOne(s => s.Student)
                .WithMany(u => u.Reservations)
                .HasForeignKey(s => s.StudentId)
                .OnDelete(DeleteBehavior.SetNull);

            slot.HasIndex(s => s.StartTime);
            slot.HasIndex(s => s.StudentId);
        });

        builder.Entity<StudentBan>(ban =>
        {
            ban.HasOne(b => b.Student)
                .WithMany()
                .HasForeignKey(b => b.StudentId)
                .OnDelete(DeleteBehavior.Cascade);

            ban.HasOne(b => b.BannedByLecturer)
                .WithMany()
                .HasForeignKey(b => b.BannedByLecturerId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }
}
