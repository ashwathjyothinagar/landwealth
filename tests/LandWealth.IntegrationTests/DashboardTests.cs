using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;

namespace LandWealth.IntegrationTests;

public class DashboardTests : IClassFixture<LandWealthApiFactory>
{
    private readonly LandWealthApiFactory _factory;

    public DashboardTests(LandWealthApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Dashboard_SeparatesLiquidPropertyAndCashFlow()
    {
        var client = await AuthenticatedClient("dashboard@landwealth.test");
        var today = DateOnly.FromDateTime(DateTime.UtcNow).ToString("yyyy-MM-dd");
        var categories = (await client.GetFromJsonAsync<List<CategoryItem>>("/api/categories"))!;
        var purchase = categories.Single(category => category.Name == "Property Purchase").Id;
        var maintenance = categories.Single(category => category.Name == "Land Maintenance").Id;
        var salary = categories.Single(category => category.Name == "Salary & Professional Income").Id;

        var bankId = await CreateAccount(client, "HDFC", "BankAccount", 500000m, "4821");
        var cashId = await CreateAccount(client, "Petty cash", "CashAccount", 5000m, null);
        await CreateAccount(client, "ICICI Card", "CreditCard", 12000m, "9012");
        var closedId = await CreateAccount(client, "Old drawer", "CashAccount", 1000m, null);
        (await client.PutAsJsonAsync($"/api/accounts/{closedId}", new { name = "Old drawer", isActive = false })).StatusCode
            .Should().Be(HttpStatusCode.NoContent);

        var propertyId = await CreateProperty(client);
        (await client.PostAsJsonAsync($"/api/properties/{propertyId}/parcels", new
        {
            surveyNumber = "42",
            extent = 3,
            extentUnit = "Acres",
            ownershipPercentage = 100
        })).StatusCode.Should().Be(HttpStatusCode.Created);

        await Post(client, today, "PropertyPurchase", 200000m, "Purchase", propertyId, purchase, ("Debit", 200000m, null, propertyId), ("Credit", 200000m, bankId, null));
        await Post(client, today, "PropertyExpense", 8000m, "Weeding", propertyId, maintenance, ("Debit", 8000m, null, propertyId), ("Credit", 8000m, bankId, null));
        await Post(client, today, "Income", 40000m, "Salary", null, salary, ("Debit", 40000m, bankId, null), ("Credit", 40000m, null, null));
        await Post(client, today, "Transfer", 10000m, "Cash withdrawal", null, null, ("Debit", 10000m, cashId, null), ("Credit", 10000m, bankId, null));

        (await client.PostAsJsonAsync($"/api/properties/{propertyId}/valuations", new
        {
            valuationDate = today,
            estimatedValue = 350000,
            valuationSource = "LocalMarketSurvey"
        })).StatusCode.Should().Be(HttpStatusCode.Created);
        (await client.PostAsJsonAsync("/api/assets", new { name = "Sovereigns", assetType = "Gold", acquisitionCost = 20000, estimatedValue = 25000 }))
            .StatusCode.Should().Be(HttpStatusCode.Created);
        (await client.PostAsJsonAsync("/api/liabilities", new { name = "Land loan", liabilityType = "LandLoan", principalAmount = 40000 }))
            .StatusCode.Should().Be(HttpStatusCode.Created);

        var dashboard = await client.GetFromJsonAsync<DashboardItem>("/api/dashboard");
        dashboard!.LiquidNetWorth.Should().Be(325000m);
        dashboard.PropertyInvestment.Should().Be(200000m);
        dashboard.PropertyValue.Should().Be(350000m);
        dashboard.UnrealizedGain.Should().Be(150000m);
        dashboard.OtherAssets.Should().Be(25000m);
        dashboard.Liabilities.Should().Be(40000m);
        dashboard.TotalNetWorth.Should().Be(660000m);
        dashboard.MonthInflows.Should().Be(40000m);
        dashboard.MonthOutflows.Should().Be(208000m);
        dashboard.Portfolio.Should().ContainSingle();
        dashboard.Portfolio[0].Name.Should().Be("Chitnahalli Farm");
        dashboard.Portfolio[0].ActiveExtentAcres.Should().Be(3m);
        dashboard.Portfolio[0].CostBasis.Should().Be(200000m);
        dashboard.RecentTransactions.Should().HaveCount(4);

        var intruder = await AuthenticatedClient("dashboard-other@landwealth.test");
        var hidden = await intruder.GetFromJsonAsync<DashboardItem>("/api/dashboard");
        hidden!.Portfolio.Should().BeEmpty();
        hidden.LiquidNetWorth.Should().Be(0m);
        hidden.TotalNetWorth.Should().Be(0m);
    }

    private static async Task Post(
        HttpClient client,
        string date,
        string transactionType,
        decimal amount,
        string description,
        Guid? propertyId,
        Guid? categoryId,
        params (string LineType, decimal Amount, Guid? AccountId, Guid? PropertyId)[] lines)
    {
        var response = await client.PostAsJsonAsync("/api/transactions", new
        {
            transactionDate = date,
            transactionType,
            amount,
            description,
            propertyId,
            lines = lines.Select(line => new
            {
                lineType = line.LineType,
                amount = line.Amount,
                accountId = line.AccountId,
                propertyId = line.PropertyId,
                categoryId
            }).ToArray()
        });
        response.StatusCode.Should().Be(HttpStatusCode.Created);
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

    private static async Task<Guid> CreateAccount(HttpClient client, string name, string accountType, decimal openingBalance, string? lastFourDigits)
    {
        var response = await client.PostAsJsonAsync("/api/accounts", new
        {
            name,
            accountType,
            openingBalance,
            lastFourDigits,
            institution = accountType == "CashAccount" ? null : "HDFC Bank"
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
    private sealed record DashboardItem(
        decimal LiquidNetWorth,
        decimal PropertyInvestment,
        decimal PropertyValue,
        decimal UnrealizedGain,
        decimal OtherAssets,
        decimal Liabilities,
        decimal TotalNetWorth,
        decimal MonthInflows,
        decimal MonthOutflows,
        List<PortfolioItem> Portfolio,
        List<RecentItem> RecentTransactions);
    private sealed record PortfolioItem(string Name, decimal ActiveExtentAcres, decimal CostBasis);
    private sealed record RecentItem(string Description);
}
