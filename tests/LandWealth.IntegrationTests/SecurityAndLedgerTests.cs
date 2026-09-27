using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using LandWealth.Domain.Exceptions;
using LandWealth.Infrastructure.Storage;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace LandWealth.IntegrationTests;

public class SecurityAndLedgerTests : IClassFixture<LandWealthApiFactory>
{
    private readonly LandWealthApiFactory _factory;

    public SecurityAndLedgerTests(LandWealthApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task TenantIsolation_HidesAnotherUsersProperty()
    {
        var first = await AuthenticatedClient("owner-a@landwealth.test");
        var created = await first.PostAsJsonAsync("/api/properties", new
        {
            name = "Chitnahalli Farm",
            propertyType = "AgriculturalLand",
            state = "Karnataka",
            village = "Chitnahalli"
        });
        created.StatusCode.Should().Be(HttpStatusCode.Created);
        var propertyId = (await created.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();

        var second = await AuthenticatedClient("owner-b@landwealth.test");
        var hidden = await second.GetAsync($"/api/properties/{propertyId}");
        hidden.StatusCode.Should().Be(HttpStatusCode.NotFound);

        var visible = await first.GetAsync($"/api/properties/{propertyId}");
        visible.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Ledger_UpdatesCash_LeavesValuationsOffTheBank_AndRejectsUnbalancedEntries()
    {
        var client = await AuthenticatedClient("ledger@landwealth.test");
        var accountId = await CreateAccount(client, 500000m);
        var propertyId = await CreateProperty(client, "Ledger Farm");
        var categories = await client.GetFromJsonAsync<List<CategoryItem>>("/api/categories");
        var purchase = categories!.Single(category => category.Name == "Property Purchase").Id;
        var maintenance = categories.Single(category => category.Name == "Land Maintenance").Id;

        var unbalanced = await client.PostAsJsonAsync("/api/transactions", new
        {
            transactionDate = "2026-04-01",
            transactionType = "PropertyPurchase",
            amount = 200000,
            description = "Unbalanced purchase",
            propertyId,
            lines = new object[]
            {
                new { lineType = "Debit", amount = 200000, propertyId, categoryId = purchase },
                new { lineType = "Credit", amount = 100000, accountId }
            }
        });
        unbalanced.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var bought = await PostTransaction(client, new
        {
            transactionDate = "2026-04-01",
            transactionType = "PropertyPurchase",
            amount = 200000,
            description = "Purchase",
            propertyId,
            lines = new object[]
            {
                new { lineType = "Debit", amount = 200000, propertyId, categoryId = purchase },
                new { lineType = "Credit", amount = 200000, accountId }
            }
        });
        bought.StatusCode.Should().Be(HttpStatusCode.Created);

        var upkeep = await PostTransaction(client, new
        {
            transactionDate = "2026-04-02",
            transactionType = "PropertyExpense",
            amount = 5000,
            description = "Weeding",
            propertyId,
            lines = new object[]
            {
                new { lineType = "Debit", amount = 5000, propertyId, categoryId = maintenance },
                new { lineType = "Credit", amount = 5000, accountId }
            }
        });
        upkeep.StatusCode.Should().Be(HttpStatusCode.Created);

        var account = await client.GetFromJsonAsync<AccountItem>($"/api/accounts/{accountId}");
        account!.CurrentBalance.Should().Be(295000m);

        var accounting = await client.GetFromJsonAsync<AccountingItem>($"/api/properties/{propertyId}/accounting");
        accounting!.CostBasis.Should().Be(200000m);

        var valuation = await client.PostAsJsonAsync($"/api/properties/{propertyId}/valuations", new
        {
            valuationDate = "2026-05-01",
            estimatedValue = 350000,
            valuationSource = "GovernmentGuidanceValue"
        });
        valuation.StatusCode.Should().Be(HttpStatusCode.Created);

        var afterValuation = await client.GetFromJsonAsync<AccountItem>($"/api/accounts/{accountId}");
        afterValuation!.CurrentBalance.Should().Be(295000m);
        var parcel = await client.PostAsJsonAsync($"/api/properties/{propertyId}/parcels", new
        {
            surveyNumber = "42",
            extent = 3,
            extentUnit = "Acres",
            ownershipPercentage = 100
        });
        parcel.StatusCode.Should().Be(HttpStatusCode.Created);

        var gain = await client.GetFromJsonAsync<AccountingItem>($"/api/properties/{propertyId}/accounting?extentSold=1&grossProceeds=180000&sellingExpenses=5000");
        gain!.UnrealizedGain.Should().Be(150000m);
        gain.AllocatedCostBasis.Should().Be(66666.6667m);

        var malicious = await client.PostAsJsonAsync("/api/properties", new
        {
            name = "'; DROP TABLE Properties; --",
            propertyType = "AgriculturalLand",
            state = "Karnataka"
        });
        malicious.StatusCode.Should().Be(HttpStatusCode.Created);
        var list = await client.GetFromJsonAsync<List<PropertyItem>>("/api/properties");
        list!.Should().Contain(property => property.Name == "'; DROP TABLE Properties; --");
        (await client.GetAsync("/api/health")).StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Documents_RejectTraversal_AndBlockCrossTenantDownload()
    {
        var storage = new LocalFileStorage(Options.Create(new StorageSettings { DocumentRoot = _factory.StorageRoot }));
        var act = () => storage.OpenRead("../secret.txt");
        act.Should().Throw<DomainException>();

        var owner = await AuthenticatedClient("docs@landwealth.test");
        var propertyId = await CreateProperty(owner, "Deed Farm");
        using var content = new MultipartFormDataContent();
        var bytes = new ByteArrayContent("%PDF-1.4\n1 0 obj\n<<>>\nendobj\n"u8.ToArray());
        bytes.Headers.ContentType = new MediaTypeHeaderValue("application/pdf");
        content.Add(bytes, "file", "sale-deed.pdf");
        content.Add(new StringContent("SaleDeed"), "documentType");
        var uploaded = await owner.PostAsync($"/api/properties/{propertyId}/documents", content);
        uploaded.StatusCode.Should().Be(HttpStatusCode.Created);
        var documentId = (await uploaded.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();

        var download = await owner.GetAsync($"/api/documents/{documentId}/download");
        download.StatusCode.Should().Be(HttpStatusCode.OK);

        var intruder = await AuthenticatedClient("intruder@landwealth.test");
        var stolen = await intruder.GetAsync($"/api/documents/{documentId}/download");
        stolen.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task ExpiredToken_IsRejected()
    {
        var token = new JwtSecurityTokenHandler().WriteToken(new JwtSecurityToken(
            issuer: "LandWealth.Api",
            audience: "LandWealth.Web",
            claims: [new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString())],
            notBefore: DateTime.UtcNow.AddHours(-2),
            expires: DateTime.UtcNow.AddMinutes(-5),
            signingCredentials: new SigningCredentials(
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes("SuperSecretKeyForLandWealthJwtTokenValidationOnlyInDev2026!")),
                SecurityAlgorithms.HmacSha256)));

        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var response = await client.GetAsync("/api/properties");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
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

    private static async Task<Guid> CreateAccount(HttpClient client, decimal openingBalance)
    {
        var response = await client.PostAsJsonAsync("/api/accounts", new
        {
            name = "HDFC Savings",
            accountType = "BankAccount",
            openingBalance,
            institution = "HDFC Bank",
            lastFourDigits = "4821"
        });
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        return (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
    }

    private static async Task<Guid> CreateProperty(HttpClient client, string name)
    {
        var response = await client.PostAsJsonAsync("/api/properties", new
        {
            name,
            propertyType = "AgriculturalLand",
            state = "Karnataka"
        });
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        return (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
    }

    private static async Task<HttpResponseMessage> PostTransaction(HttpClient client, object payload)
        => await client.PostAsJsonAsync("/api/transactions", payload);

    private sealed record CategoryItem(Guid Id, string Name);
    private sealed record AccountItem(decimal CurrentBalance);
    private sealed record AccountingItem(decimal CostBasis, decimal? UnrealizedGain, decimal? AllocatedCostBasis);
    private sealed record PropertyItem(string Name);
}
