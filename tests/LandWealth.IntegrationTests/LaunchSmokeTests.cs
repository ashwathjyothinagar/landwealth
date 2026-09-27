using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;

namespace LandWealth.IntegrationTests;

public class LaunchSmokeTests : IClassFixture<LandWealthApiFactory>
{
    private readonly LandWealthApiFactory _factory;

    public LaunchSmokeTests(LandWealthApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task ReleasePath_KeepsTheCanonicalRupeeAmountExact()
    {
        var health = await _factory.CreateClient().GetAsync("/api/health");
        health.StatusCode.Should().Be(HttpStatusCode.OK);

        var owner = await AuthenticatedClient("launch@landwealth.test");
        var today = DateOnly.FromDateTime(DateTime.UtcNow).ToString("yyyy-MM-dd");
        var categories = (await owner.GetFromJsonAsync<List<CategoryItem>>("/api/categories"))!;
        var purchase = categories.Single(category => category.Name == "Property Purchase").Id;

        var bank = await owner.PostAsJsonAsync("/api/accounts", new
        {
            name = "HDFC",
            accountType = "BankAccount",
            openingBalance = 5000000,
            lastFourDigits = "4821",
            institution = "HDFC Bank"
        });
        bank.StatusCode.Should().Be(HttpStatusCode.Created);
        var accountId = (await bank.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();

        var property = await owner.PostAsJsonAsync("/api/properties", new
        {
            name = "Chitnahalli Farm",
            propertyType = "AgriculturalLand",
            state = "Karnataka"
        });
        property.StatusCode.Should().Be(HttpStatusCode.Created);
        var propertyId = (await property.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
        (await owner.PostAsJsonAsync($"/api/properties/{propertyId}/parcels", new
        {
            surveyNumber = "42",
            extent = 3,
            extentUnit = "Acres",
            ownershipPercentage = 100
        })).StatusCode.Should().Be(HttpStatusCode.Created);

        (await owner.PostAsJsonAsync("/api/transactions", new
        {
            transactionDate = today,
            transactionType = "PropertyPurchase",
            amount = 1234567.50m,
            description = "Purchase",
            propertyId,
            lines = new object[]
            {
                new { lineType = "Debit", amount = 1234567.50m, propertyId, categoryId = purchase },
                new { lineType = "Credit", amount = 1234567.50m, accountId }
            }
        })).StatusCode.Should().Be(HttpStatusCode.Created);

        (await owner.PostAsJsonAsync($"/api/properties/{propertyId}/valuations", new
        {
            valuationDate = today,
            estimatedValue = 2000000,
            valuationSource = "LocalMarketSurvey"
        })).StatusCode.Should().Be(HttpStatusCode.Created);

        var dashboardJson = await owner.GetStringAsync("/api/dashboard");
        dashboardJson.Should().Contain("1234567.5");
        var dashboard = JsonSerializer.Deserialize<DashboardItem>(dashboardJson, new JsonSerializerOptions(JsonSerializerDefaults.Web));
        dashboard!.PropertyInvestment.Should().Be(1234567.50m);
        dashboard.PropertyValue.Should().Be(2000000m);
        dashboard.UnrealizedGain.Should().Be(765432.50m);
        dashboard.LiquidNetWorth.Should().Be(3765432.50m);
        dashboard.TotalNetWorth.Should().Be(5765432.50m);
        dashboard.Portfolio.Should().ContainSingle(row => row.Name == "Chitnahalli Farm" && row.ActiveExtentAcres == 3m);

        var audit = await owner.GetFromJsonAsync<List<AuditItem>>("/api/audit-logs");
        audit!.Should().Contain(entry => entry.EntityName == "Transaction" && entry.Summary == "Purchase");

        var other = await AuthenticatedClient("launch-other@landwealth.test");
        (await other.GetAsync($"/api/properties/{propertyId}")).StatusCode.Should().Be(HttpStatusCode.NotFound);
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

    private sealed record CategoryItem(Guid Id, string Name);
    private sealed record DashboardItem(
        decimal PropertyInvestment,
        decimal PropertyValue,
        decimal UnrealizedGain,
        decimal LiquidNetWorth,
        decimal TotalNetWorth,
        List<PortfolioItem> Portfolio);
    private sealed record PortfolioItem(string Name, decimal ActiveExtentAcres);
    private sealed record AuditItem(string EntityName, string? Summary);
}
