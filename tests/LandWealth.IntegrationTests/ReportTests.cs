using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;

namespace LandWealth.IntegrationTests;

public class ReportTests : IClassFixture<LandWealthApiFactory>
{
    private readonly LandWealthApiFactory _factory;

    public ReportTests(LandWealthApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Reports_StateProfitability_BalanceSheet_AndValuationHistory()
    {
        var client = await AuthenticatedClient("reports@landwealth.test");
        var today = DateOnly.FromDateTime(DateTime.UtcNow).ToString("yyyy-MM-dd");
        var categories = (await client.GetFromJsonAsync<List<CategoryItem>>("/api/categories"))!;
        var purchase = categories.Single(category => category.Name == "Property Purchase").Id;
        var stamp = categories.Single(category => category.Name == "Stamp Duty & Registration").Id;
        var borewell = categories.Single(category => category.Name == "Borewell").Id;
        var maintenance = categories.Single(category => category.Name == "Land Maintenance").Id;
        var rental = categories.Single(category => category.Name == "Rental Income").Id;
        var bankId = await CreateAccount(client);

        var propertyId = await CreateProperty(client);
        await Spend(client, today, "PropertyPurchase", 180000m, "Purchase", propertyId, bankId, purchase);
        await Spend(client, today, "PropertyPurchase", 20000m, "Stamp duty", propertyId, bankId, stamp);
        await Spend(client, today, "PropertyExpense", 25000m, "Borewell", propertyId, bankId, borewell);
        var upkeep = await Spend(client, today, "PropertyExpense", 8000m, "Weeding", propertyId, bankId, maintenance);
        (await client.PostAsJsonAsync("/api/transactions", new
        {
            transactionDate = today,
            transactionType = "PropertyIncome",
            amount = 15000,
            description = "Lease rent",
            propertyId,
            lines = new object[]
            {
                new { lineType = "Debit", amount = 15000, accountId = bankId },
                new { lineType = "Credit", amount = 15000, propertyId, categoryId = rental }
            }
        })).StatusCode.Should().Be(HttpStatusCode.Created);

        var statement = await client.GetFromJsonAsync<StatementItem>("/api/reports/profitability");
        var row = statement!.Properties.Single();
        row.AcquisitionCost.Should().Be(200000m);
        row.Improvements.Should().Be(25000m);
        row.CostBasis.Should().Be(225000m);
        row.OperatingIncome.Should().Be(15000m);
        row.OperatingExpenses.Should().Be(8000m);
        statement.TotalCostBasis.Should().Be(225000m);

        var upkeepId = (await upkeep.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
        (await client.PostAsJsonAsync($"/api/transactions/{upkeepId}/reverse", new { description = "Weeding was not done" }))
            .StatusCode.Should().Be(HttpStatusCode.Created);
        var afterReversal = await client.GetFromJsonAsync<StatementItem>("/api/reports/profitability");
        afterReversal!.Properties.Single().OperatingExpenses.Should().Be(0m);
        afterReversal.Properties.Single().CostBasis.Should().Be(225000m);

        (await client.PostAsJsonAsync($"/api/properties/{propertyId}/valuations", new
        {
            valuationDate = "2024-01-01",
            estimatedValue = 200000,
            valuationSource = "GovernmentGuidanceValue"
        })).StatusCode.Should().Be(HttpStatusCode.Created);
        (await client.PostAsJsonAsync($"/api/properties/{propertyId}/valuations", new
        {
            valuationDate = "2026-01-01",
            estimatedValue = 350000,
            valuationSource = "LocalMarketSurvey"
        })).StatusCode.Should().Be(HttpStatusCode.Created);

        var valued = (await client.GetFromJsonAsync<StatementItem>("/api/reports/profitability"))!.Properties.Single();
        valued.UnrealizedGain.Should().Be(125000m);
        var timeline = await client.GetFromJsonAsync<List<ValuationItem>>($"/api/reports/properties/{propertyId}/valuations");
        timeline!.Select(point => point.Date).Should().Equal("2024-01-01", "2026-01-01");

        var csv = await client.GetStringAsync($"/api/reports/properties/{propertyId}/valuations.csv");
        csv.Should().Contain("2024-01-01");
        csv.Should().Contain("2026-01-01");
        csv.Should().Contain("GovernmentGuidanceValue");

        var sheet = await client.GetFromJsonAsync<BalanceSheetItem>("/api/reports/balance-sheet");
        sheet!.Lines.Should().Contain(line => line.Section == "Asset" && line.Name == "Chitnahalli Farm" && line.Amount == 350000m);
        sheet.NetWorth.Should().Be(sheet.TotalAssets - sheet.Liabilities);
        sheet.NetWorth.Should().Be(640000m);

        var profitabilityCsv = await client.GetStringAsync("/api/reports/profitability.csv");
        profitabilityCsv.Should().Contain("Chitnahalli Farm");
        profitabilityCsv.Should().Contain("CostBasis");

        (await client.GetAsync($"/api/reports/cash-flow?from={today}&to=2020-01-01")).StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var intruder = await AuthenticatedClient("reports-other@landwealth.test");
        (await intruder.GetAsync($"/api/reports/properties/{propertyId}/profitability")).StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await intruder.GetFromJsonAsync<StatementItem>("/api/reports/profitability"))!.Properties.Should().BeEmpty();
    }

    private static async Task<HttpResponseMessage> Spend(
        HttpClient client, string date, string transactionType, decimal amount, string description, Guid propertyId, Guid accountId, Guid categoryId)
    {
        var response = await client.PostAsJsonAsync("/api/transactions", new
        {
            transactionDate = date,
            transactionType,
            amount,
            description,
            propertyId,
            lines = new object[]
            {
                new { lineType = "Debit", amount, propertyId, categoryId },
                new { lineType = "Credit", amount, accountId }
            }
        });
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        return response;
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
    private sealed record StatementItem(List<ProfitRow> Properties, decimal TotalCostBasis);
    private sealed record ProfitRow(
        decimal AcquisitionCost,
        decimal Improvements,
        decimal CostBasis,
        decimal OperatingIncome,
        decimal OperatingExpenses,
        decimal? UnrealizedGain);
    private sealed record ValuationItem(string Date, decimal EstimatedValue, string Source);
    private sealed record BalanceSheetItem(decimal TotalAssets, decimal Liabilities, decimal NetWorth, List<SheetLine> Lines);
    private sealed record SheetLine(string Section, string Name, decimal Amount);
}
