using System.Net;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;

namespace TransitDesk.Tests;

/// <summary>
/// Testy integracyjne – WebApplicationFactory uruchamia całą aplikację (Program.cs, potok middleware,
/// routing, widoki Razor) w pamięci na TestServerze, bez otwierania portu sieciowego.
/// </summary>
public class MiddlewareAndStatusPagesTests(WebApplicationFactory<Program> factory)
    : IClassFixture<WebApplicationFactory<Program>>
{
    // AllowAutoRedirect = false – chcemy widzieć, że błąd NIE jest realizowany przekierowaniem 302.
    private readonly HttpClient _client = factory.CreateClient(new WebApplicationFactoryClientOptions
    {
        AllowAutoRedirect = false
    });

    [Fact]
    public async Task UnknownUrl_Returns404_RenderedByErrorsController()
    {
        var response = await _client.GetAsync("/nieistnieje/strona");
        var html = await response.Content.ReadAsStringAsync();

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        response.Content.Headers.ContentType!.MediaType.Should().Be("text/html");
        html.Should().Contain("data-testid=\"error-page\"")
            .And.Contain("Nie odnaleziono wskazanego zasobu taboru lub zgłoszenia.")
            .And.Contain("/nieistnieje/strona");
    }

    [Fact]
    public async Task ForbiddenAction_Returns403_WithDedicatedView()
    {
        var response = await _client.GetAsync("/Home/Dispatch");
        var html = await response.Content.ReadAsStringAsync();

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        html.Should().Contain("Brak uprawnień dostępu do modułu dyspozytorskiego.");
    }

    [Theory]
    [InlineData("/pojazd/999")]
    [InlineData("/pojazd/10000")]
    [InlineData("/pojazd/abc")]
    public async Task VehicleDetails_SideNumberRejectedByConstraint_Returns404(string url)
    {
        var response = await _client.GetAsync(url);
        var html = await response.Content.ReadAsStringAsync();

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        html.Should().Contain("data-testid=\"error-page\"");
    }

    [Fact]
    public async Task VehicleDetails_ValidSideNumber_Returns200()
    {
        var response = await _client.GetAsync("/pojazd/2072");
        var html = await response.Content.ReadAsStringAsync();

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        html.Should().Contain("Solaris Urbino 12");
    }

    [Theory]
    [InlineData("/Zajezdnia/Hallera", "Solaris Urbino 12")]
    [InlineData("/Zajezdnia/Wrzeszcz/tramwaj", "Pesa Swing")]
    public async Task DepotFleetRoute_MapsToDepotFleetList(string url, string expectedVehicle)
    {
        var response = await _client.GetAsync(url);
        var html = await response.Content.ReadAsStringAsync();

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        html.Should().Contain(expectedVehicle);
    }

    [Fact]
    public async Task DepotFleetRoute_TractionFilter_ExcludesOtherTraction()
    {
        var html = await _client.GetStringAsync("/Zajezdnia/Wrzeszcz/tramwaj");

        html.Should().NotContain("Solaris Urbino 18 Electric");
    }

    [Fact]
    public async Task RemoteValidation_ReturnsTrueForActiveVehicle_AndMessageForWithdrawn()
    {
        var ok = await _client.GetStringAsync("/Incidents/VerifyVehicleAvailable?VehicleSideNumber=2072");
        var withdrawn = await _client.GetStringAsync("/Incidents/VerifyVehicleAvailable?VehicleSideNumber=1311");

        ok.Should().Be("true");
        withdrawn.Should().Contain("1311");
    }

    [Fact]
    public async Task HomePage_IsLocalized_ByCultureQueryString()
    {
        var polish = await _client.GetStringAsync("/");
        var english = await _client.GetStringAsync("/?culture=en-US");

        polish.Should().Contain("Witaj w systemie TransitDesk");
        english.Should().Contain("Welcome to TransitDesk").And.Contain("Fleet registry");
    }

    [Fact]
    public async Task SessionCookie_IsHttpOnly_AndSameSiteLax()
    {
        // Sesja tworzy ciasteczko dopiero po pierwszym zapisie – dodajemy pojazd do listy monitorowanych.
        var (token, cookies) = await GetAntiforgeryAsync("/Vehicles/Create");
        using var request = new HttpRequestMessage(HttpMethod.Post, "/Vehicles/AddVehicleToWatchlist")
        {
            Content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["sideNumber"] = "2072",
                ["__RequestVerificationToken"] = token
            })
        };
        request.Headers.Add("Cookie", cookies);

        var response = await _client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.Redirect);
        var sessionCookie = response.Headers.GetValues("Set-Cookie")
            .Single(c => c.StartsWith(".TransitDesk.Session="));
        sessionCookie.Should().Contain("httponly").And.Contain("samesite=lax");
    }

    private async Task<(string Token, string Cookies)> GetAntiforgeryAsync(string formUrl)
    {
        var response = await _client.GetAsync(formUrl);
        var html = await response.Content.ReadAsStringAsync();

        const string marker = "name=\"__RequestVerificationToken\" type=\"hidden\" value=\"";
        var start = html.IndexOf(marker, StringComparison.Ordinal) + marker.Length;
        var token = html[start..html.IndexOf('"', start)];

        var cookies = string.Join("; ", response.Headers.GetValues("Set-Cookie").Select(c => c.Split(';')[0]));
        return (token, cookies);
    }
}
