using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;

namespace LandWealth.IntegrationTests;

public class PropertyCostBasisTests : IClassFixture<LandWealthApiFactory>
{
    private readonly LandWealthApiFactory _factory;

    public PropertyCostBasisTests(LandWealthApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task CostBasis_SeparatesMaintenance_AndAllocatesAPartialSale()
    {
        var client = await AuthenticatedClient("basis@landwealth.test");
        var categories = (await client.GetFromJsonAsync<List<CategoryItem>>("/api/categories"))!;
        var purchase = categories.Single(category => category.Name == "Property Purchase").Id;
        var stamp = categories.Single(category => category.Name == "Stamp Duty & Registration").Id;
        var borewell = categories.Single(category => category.Name == "Borewell").Id;
        var maintenance = categories.Single(category => category.Name == "Land Maintenance").Id;
        var bankId = await CreateAccount(client);

        var property = await client.PostAsJsonAsync("/api/properties", new
        {
            name = "Chitnahalli Farm",
            propertyType = "AgriculturalLand",
            state = "Karnataka"
        });
        var propertyId = (await property.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
        var parcelResponse = await client.PostAsJsonAsync($"/api/properties/{propertyId}/parcels", new
        {
            surveyNumber = "42",
            extent = 3,
            extentUnit = "Acres",
            ownershipPercentage = 100
        });
        parcelResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var parcelId = (await parcelResponse.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();

        await Post(client, propertyId, bankId, "PropertyPurchase", 180000m, "Purchase", purchase);
        await Post(client, propertyId, bankId, "PropertyPurchase", 20000m, "Stamp duty", stamp);
        var improvement = await Post(client, propertyId, bankId, "PropertyExpense", 25000m, "Borewell", borewell);
        await Post(client, propertyId, bankId, "PropertyExpense", 8000m, "Weeding", maintenance);
        var bankAfterSpend = (await client.GetFromJsonAsync<AccountItem>($"/api/accounts/{bankId}"))!.CurrentBalance;

        var beforeSale = await client.GetFromJsonAsync<AccountingItem>($"/api/properties/{propertyId}/accounting");
        beforeSale!.AcquisitionCost.Should().Be(200000m);
        beforeSale.Improvements.Should().Be(25000m);
        beforeSale.Maintenance.Should().Be(8000m);
        beforeSale.CostBasis.Should().Be(225000m);
        beforeSale.ActiveExtentAcres.Should().Be(3m);

        (await client.PostAsJsonAsync($"/api/properties/{propertyId}/parcels/{parcelId}/subdivide", new
        {
            splitExtent = 1,
            remainderSubdivision = "1",
            splitSubdivision = "2"
        })).StatusCode.Should().Be(HttpStatusCode.Created);

        var sale = await client.GetFromJsonAsync<AccountingItem>(
            $"/api/properties/{propertyId}/accounting?extentSold=40&extentUnit=Guntas&grossProceeds=100000&sellingExpenses=5000");
        sale!.ActiveExtentAcres.Should().Be(3m);
        sale.AllocatedCostBasis.Should().Be(75000m);
        sale.RealizedGain.Should().Be(20000m);
        sale.CostBasis.Should().Be(225000m);
        (await client.GetFromJsonAsync<AccountItem>($"/api/accounts/{bankId}"))!.CurrentBalance.Should().Be(bankAfterSpend);

        var improvementId = (await improvement.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
        (await client.PostAsJsonAsync($"/api/transactions/{improvementId}/reverse", new { description = "Borewell was not drilled" }))
            .StatusCode.Should().Be(HttpStatusCode.Created);

        var bankAfterReversal = (await client.GetFromJsonAsync<AccountItem>($"/api/accounts/{bankId}"))!.CurrentBalance;
        bankAfterReversal.Should().Be(bankAfterSpend + 25000m);
        var afterReversal = await client.GetFromJsonAsync<AccountingItem>($"/api/properties/{propertyId}/accounting");
        afterReversal!.Improvements.Should().Be(0m);
        afterReversal.CostBasis.Should().Be(200000m);
        afterReversal.Maintenance.Should().Be(8000m);

        (await client.PostAsJsonAsync($"/api/properties/{propertyId}/valuations", new
        {
            valuationDate = "2026-08-01",
            estimatedValue = 350000,
            valuationSource = "GovernmentGuidanceValue"
        })).StatusCode.Should().Be(HttpStatusCode.Created);

        var valued = await client.GetFromJsonAsync<AccountingItem>($"/api/properties/{propertyId}/accounting");
        valued!.CostBasis.Should().Be(200000m);
        valued.UnrealizedGain.Should().Be(150000m);
        (await client.GetFromJsonAsync<AccountItem>($"/api/accounts/{bankId}"))!.CurrentBalance.Should().Be(bankAfterReversal);
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

    private static async Task<HttpResponseMessage> Post(
        HttpClient client, Guid propertyId, Guid accountId, string transactionType, decimal amount, string description, Guid categoryId)
    {
        var response = await client.PostAsJsonAsync("/api/transactions", new
        {
            transactionDate = "2026-06-01",
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

    private sealed record CategoryItem(Guid Id, string Name);
    private sealed record AccountItem(decimal CurrentBalance);
    private sealed record AccountingItem(
        decimal CostBasis,
        decimal? UnrealizedGain,
        decimal ActiveExtentAcres,
        decimal? AllocatedCostBasis,
        decimal? RealizedGain,
        decimal AcquisitionCost,
        decimal Improvements,
        decimal Maintenance);
}
