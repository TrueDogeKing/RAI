using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ProjectDefense.Core.Dtos;
using ProjectDefense.Web.Data;

namespace ProjectDefense.Tests;

public sealed class ProjectDefenseApiFactory : WebApplicationFactory<Program>
{
    private readonly string _databaseName = "api-tests-" + Guid.NewGuid();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureServices(services =>
        {
            var registrations = services
                .Where(d => d.ServiceType == typeof(DbContextOptions<ApplicationDbContext>) || d.ServiceType == typeof(DbContextOptions))
                .ToList();
            registrations.ForEach(d => services.Remove(d));

            services.AddDbContext<ApplicationDbContext>(options => options.UseInMemoryDatabase(_databaseName));
        });
    }
}

public class MinimalApiIntegrationTests : IClassFixture<ProjectDefenseApiFactory>
{
    private readonly HttpClient _client;

    public MinimalApiIntegrationTests(ProjectDefenseApiFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Get_rooms_returns_ok_with_room_dtos()
    {
        var response = await _client.GetAsync("/api/rooms");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType!.MediaType.Should().Be("application/json");

        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        json.RootElement.ValueKind.Should().Be(JsonValueKind.Array);
        var first = json.RootElement.EnumerateArray().First();
        first.EnumerateObject().Select(p => p.Name).Should().BeEquivalentTo(new[] { "id", "name", "roomNumber", "workstationsCount" });

        var rooms = await response.Content.ReadFromJsonAsync<List<RoomDto>>();
        rooms.Should().Contain(r => r.RoomNumber == "B214" && r.Name == "Laboratorium Sieciowe");
    }

    [Fact]
    public async Task Get_available_slots_returns_ok_with_slot_dtos()
    {
        var response = await _client.GetAsync("/api/slots/available");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var slots = json.RootElement.EnumerateArray().ToList();
        slots.Should().NotBeEmpty();
        slots[0].EnumerateObject().Select(p => p.Name).Should().BeEquivalentTo(
            new[] { "id", "roomId", "roomNumber", "roomName", "lecturerName", "startTime", "endTime", "label" });
        slots[0].EnumerateObject().Select(p => p.Name).Should().NotContain(new[] { "studentId", "lecturerAvailability", "isBlockedByLecturer" });

        var dtos = await response.Content.ReadFromJsonAsync<List<AvailableSlotDto>>();
        dtos.Should().OnlyContain(s => s.StartTime > DateTime.Now && s.EndTime > s.StartTime);
    }

    [Fact]
    public async Task Booking_through_api_enforces_single_active_reservation()
    {
        var slots = await _client.GetFromJsonAsync<List<AvailableSlotDto>>("/api/slots/available");
        var request = new BookSlotRequestDto(DataSeeder.SecondStudentEmail);

        var first = await _client.PostAsJsonAsync($"/api/slots/{slots![0].Id}/book", request);
        var second = await _client.PostAsJsonAsync($"/api/slots/{slots[1].Id}/book", request);

        first.StatusCode.Should().Be(HttpStatusCode.OK);
        (await first.Content.ReadFromJsonAsync<BookingResultDto>())!.SlotId.Should().Be(slots[0].Id);
        second.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Booking_for_unknown_student_returns_not_found()
    {
        var response = await _client.PostAsJsonAsync("/api/slots/1/book", new BookSlotRequestDto("nobody@example.com"));

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
