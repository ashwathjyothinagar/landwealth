using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;

namespace LandWealth.IntegrationTests;

public class ReminderTests : IClassFixture<LandWealthApiFactory>
{
    private readonly LandWealthApiFactory _factory;

    public ReminderTests(LandWealthApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Reminders_EscalateOverdueItems_AndSurfaceTheNextMonth()
    {
        var client = await AuthenticatedClient("reminders@landwealth.test");
        var propertyId = await CreateProperty(client);
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        var tax = await Create(client, propertyId, "Pay property tax", today.AddDays(-1), "Low");
        var survey = await Create(client, propertyId, "Agricultural survey", today.AddDays(10), "Medium");
        var lease = await Create(client, propertyId, "Lease expiry", today.AddDays(45), "Medium");

        var listed = await client.GetFromJsonAsync<List<ReminderItem>>("/api/reminders");
        listed!.Single(item => item.Id == tax).Status.Should().Be("Overdue");
        listed.Single(item => item.Id == tax).Priority.Should().Be("High");
        listed.Single(item => item.Id == survey).Status.Should().Be("Pending");
        listed.Single(item => item.Id == lease).Status.Should().Be("Pending");

        var dashboard = await client.GetFromJsonAsync<DashboardItem>("/api/dashboard");
        var titles = dashboard!.UpcomingReminders.Select(item => item.Title).ToList();
        titles.Should().Contain(new[] { "Pay property tax", "Agricultural survey" });
        titles.Should().NotContain("Lease expiry");

        (await client.PostAsJsonAsync($"/api/reminders/{tax}/complete", new { completedDate = today.ToString("yyyy-MM-dd") }))
            .StatusCode.Should().Be(HttpStatusCode.NoContent);
        (await client.PostAsJsonAsync($"/api/reminders/{tax}/complete", new { completedDate = today.ToString("yyyy-MM-dd") }))
            .StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var after = await client.GetFromJsonAsync<DashboardItem>("/api/dashboard");
        after!.UpcomingReminders.Select(item => item.Title).Should().NotContain("Pay property tax");
        after.UpcomingReminders.Select(item => item.Title).Should().Contain("Agricultural survey");

        (await client.PostAsync($"/api/reminders/{survey}/dismiss", null)).StatusCode.Should().Be(HttpStatusCode.NoContent);
        var dismissed = await client.GetFromJsonAsync<DashboardItem>("/api/dashboard");
        dismissed!.UpcomingReminders.Should().BeEmpty();

        var intruder = await AuthenticatedClient("reminders-other@landwealth.test");
        (await intruder.PostAsJsonAsync($"/api/reminders/{lease}/complete", new { completedDate = today.ToString("yyyy-MM-dd") }))
            .StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    private static async Task<Guid> Create(HttpClient client, Guid propertyId, string title, DateOnly dueDate, string priority)
    {
        var response = await client.PostAsJsonAsync("/api/reminders", new
        {
            title,
            dueDate = dueDate.ToString("yyyy-MM-dd"),
            priority,
            propertyId,
            description = "Scheduled from the property calendar."
        });
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        return (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
    }

    private async Task<HttpClient> AuthenticatedClient(string email)
    {
        var client = _factory.CreateClient();
        var register = await client.PostAsJsonAsync("/api/auth/register", new
        {
            email,
            password = "Password1!",
            fullName = "Test User"
        });
        register.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await register.Content.ReadFromJsonAsync<JsonElement>();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", body.GetProperty("token").GetString());
        return client;
    }

    private static async Task<Guid> CreateProperty(HttpClient client)
    {
        var response = await client.PostAsJsonAsync("/api/properties", new
        {
            name = "Chitnahalli Farm",
            propertyType = "AgriculturalLand",
            state = "Karnataka"
        });
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        return (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
    }

    private sealed record ReminderItem(Guid Id, string Title, string Status, string Priority);
    private sealed record DashboardItem(List<ReminderItem> UpcomingReminders);
}
