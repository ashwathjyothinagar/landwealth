using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;

namespace LandWealth.IntegrationTests;

public class ValuationTests : IClassFixture<LandWealthApiFactory>
{
    private readonly LandWealthApiFactory _factory;

    public ValuationTests(LandWealthApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task ValuationHistory_RecomputesUnrealizedGain_WithoutTouchingCash()
    {
        var client = await AuthenticatedClient("valuations@landwealth.test");
        var categories = (await client.GetFromJsonAsync<List<CategoryItem>>("/api/categories"))!;
        var purchase = categories.Single(category => category.Name == "Property Purchase").Id;
        var bankId = await CreateAccount(client);
        var propertyId = await CreateProperty(client);

        (await client.PostAsJsonAsync("/api/transactions", new
        {
            transactionDate = "2026-01-01",
            transactionType = "PropertyPurchase",
            amount = 100000,
            description = "Purchase",
            propertyId,
            lines = new object[]
            {
                new { lineType = "Debit", amount = 100000, propertyId, categoryId = purchase },
                new { lineType = "Credit", amount = 100000, accountId = bankId }
            }
        })).StatusCode.Should().Be(HttpStatusCode.Created);

        var transactionsBefore = await client.GetFromJsonAsync<TransactionPage>("/api/transactions");
        var bankBefore = (await client.GetFromJsonAsync<AccountItem>($"/api/accounts/{bankId}"))!.CurrentBalance;

        (await client.PostAsJsonAsync("/api/properties/" + propertyId + "/valuations", new
        {
            valuationDate = "2026-01-15",
            estimatedValue = -1,
            valuationSource = "LocalMarketSurvey"
        })).StatusCode.Should().Be(HttpStatusCode.BadRequest);

        (await Add(client, propertyId, "2026-01-15", 350000m, "LocalMarketSurvey")).StatusCode.Should().Be(HttpStatusCode.Created);
        (await Add(client, propertyId, "2026-06-01", 180000m, "GovernmentGuidanceValue")).StatusCode.Should().Be(HttpStatusCode.Created);

        var accounting = await client.GetFromJsonAsync<AccountingItem>($"/api/properties/{propertyId}/accounting");
        accounting!.CostBasis.Should().Be(100000m);
        accounting.MarketValue.Should().Be(350000m);
        accounting.GuidanceValue.Should().Be(180000m);
        accounting.UnrealizedGain.Should().Be(250000m);
        (await client.GetFromJsonAsync<AccountItem>($"/api/accounts/{bankId}"))!.CurrentBalance.Should().Be(bankBefore);
        (await client.GetFromJsonAsync<TransactionPage>("/api/transactions"))!.TotalCount.Should().Be(transactionsBefore!.TotalCount);

        var history = await client.GetFromJsonAsync<List<ValuationItem>>($"/api/properties/{propertyId}/valuations");
        history!.Select(item => item.ValuationSource).Should().Equal("LocalMarketSurvey", "GovernmentGuidanceValue");

        (await Add(client, propertyId, "2026-07-01", 400000m, "BankValuation")).StatusCode.Should().Be(HttpStatusCode.Created);
        var updated = await client.GetFromJsonAsync<AccountingItem>($"/api/properties/{propertyId}/accounting");
        updated!.MarketValue.Should().Be(400000m);
        updated.GuidanceValue.Should().Be(180000m);
        updated.UnrealizedGain.Should().Be(300000m);
        (await client.GetFromJsonAsync<AccountItem>($"/api/accounts/{bankId}"))!.CurrentBalance.Should().Be(bankBefore);

        var kept = await client.GetFromJsonAsync<List<ValuationItem>>($"/api/properties/{propertyId}/valuations");
        kept!.Should().HaveCount(3);

        var intruder = await AuthenticatedClient("valuations-other@landwealth.test");
        (await intruder.GetAsync($"/api/properties/{propertyId}/valuations")).StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    private static Task<HttpResponseMessage> Add(HttpClient client, Guid propertyId, string date, decimal value, string source)
        => client.PostAsJsonAsync($"/api/properties/{propertyId}/valuations", new
        {
            valuationDate = date,
            estimatedValue = value,
            valuationSource = source
        });

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

    private static async Task<Guid> CreateAccount(HttpClient client)
    {
        var response = await client.PostAsJsonAsync("/api/accounts", new
        {
            name = "HDFC",
            accountType = "BankAccount",
            openingBalance = 500000,
            lastFourDigits = "4821",
            institution = "HDFC Bank"
        });
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        return (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
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

    private sealed record CategoryItem(Guid Id, string Name);
    private sealed record AccountItem(decimal CurrentBalance);
    private sealed record AccountingItem(decimal CostBasis, decimal? UnrealizedGain, decimal? GuidanceValue, decimal? MarketValue);
    private sealed record ValuationItem(string ValuationDate, decimal EstimatedValue, string ValuationSource);
    private sealed record TransactionPage(int TotalCount);
}
